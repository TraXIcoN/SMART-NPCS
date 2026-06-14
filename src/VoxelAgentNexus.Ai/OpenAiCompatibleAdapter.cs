using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using VoxelAgentNexus.Core.Ai;

namespace VoxelAgentNexus.Ai;

/// <summary>
/// Concrete <see cref="INpcAiAdapter"/> talking to any OpenAI-compatible
/// /chat/completions endpoint. Used for development and quality; an on-device SLM
/// adapter is the shipping target behind the same interface. (DESIGN_BRIEF.md §4, §6.)
///
/// The cacheable prefix is sent first so providers that do automatic prefix
/// caching get a hit; cached-token counts are surfaced via <see cref="AiTokenUsage"/>.
/// The <see cref="HttpClient"/> is injected so it can be pooled and tested.
/// </summary>
public sealed class OpenAiCompatibleAdapter : INpcAiAdapter
{
    private static readonly JsonSerializerOptions IntentOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly OpenAiAdapterOptions _options;

    public OpenAiCompatibleAdapter(HttpClient http, OpenAiAdapterOptions options)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Capabilities = new AiAdapterCapabilities
        {
            ProviderName = options.ProviderName,
            IsLocal = options.IsLocal,
            SupportsStreaming = true,
            SupportsPromptCaching = options.SupportsPromptCaching,
            SupportsStructuredOutput = true,
            MaxContextTokens = options.MaxContextTokens,
        };
    }

    /// <inheritdoc />
    public AiAdapterCapabilities Capabilities { get; }

    /// <inheritdoc />
    public async ValueTask<NpcAiResponse> GenerateAsync(NpcAiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = BuildRequest(request, stream: false);
        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return Parse(request, body);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<NpcAiResponseChunk> StreamAsync(
        NpcAiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = BuildRequest(request, stream: true);
        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Length == 0 || !line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]")
            {
                yield return new NpcAiResponseChunk(string.Empty, IsFinal: true);
                yield break;
            }

            var delta = TryReadDelta(data);
            if (!string.IsNullOrEmpty(delta))
            {
                yield return new NpcAiResponseChunk(delta, IsFinal: false);
            }
        }
    }

    private HttpRequestMessage BuildRequest(NpcAiRequest request, bool stream)
    {
        var messages = new List<Dictionary<string, string>>(request.CacheablePrefix.Count + request.Volatile.Count);
        foreach (var message in request.CacheablePrefix)
        {
            messages.Add(ToWire(message));
        }

        foreach (var message in request.Volatile)
        {
            messages.Add(ToWire(message));
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["messages"] = messages,
            ["max_tokens"] = request.MaxOutputTokens,
            ["temperature"] = request.Temperature,
            ["stream"] = stream,
        };

        if (request.ResponseFormat == AiResponseFormat.StructuredIntent)
        {
            payload["response_format"] = new Dictionary<string, string> { ["type"] = "json_object" };
        }

        var endpoint = _options.BaseUrl.ToString().TrimEnd('/') + "/chat/completions";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        return httpRequest;
    }

    private NpcAiResponse Parse(NpcAiRequest request, string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var choice = root.GetProperty("choices")[0];
        var content = choice.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

        var finishReason = choice.TryGetProperty("finish_reason", out var fr)
            ? MapFinish(fr.GetString())
            : AiFinishReason.Stop;

        var usage = ReadUsage(root);

        var response = new NpcAiResponse
        {
            Text = request.ResponseFormat == AiResponseFormat.Text ? content : null,
            Intent = request.ResponseFormat == AiResponseFormat.StructuredIntent ? TryReadIntent(content) : null,
            Usage = usage,
            FinishReason = finishReason,
            ServedByModel = _options.Model,
        };

        return response;
    }

    private static AiTokenUsage ReadUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage))
        {
            return new AiTokenUsage(0, 0, 0);
        }

        var input = usage.TryGetProperty("prompt_tokens", out var p) ? p.GetInt32() : 0;
        var output = usage.TryGetProperty("completion_tokens", out var c) ? c.GetInt32() : 0;
        var cached = 0;
        if (usage.TryGetProperty("prompt_tokens_details", out var details)
            && details.TryGetProperty("cached_tokens", out var ct))
        {
            cached = ct.GetInt32();
        }

        return new AiTokenUsage(input, cached, output);
    }

    private static NpcIntent? TryReadIntent(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<NpcIntent>(content, IntentOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryReadDelta(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                return null;
            }

            var delta = choices[0].GetProperty("delta");
            return delta.TryGetProperty("content", out var contentElement) ? contentElement.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, string> ToWire(AiMessage message) => new()
    {
        ["role"] = message.Role switch
        {
            AiRole.System => "system",
            AiRole.User => "user",
            AiRole.Assistant => "assistant",
            AiRole.Tool => "tool",
            _ => "user",
        },
        ["content"] = message.Content,
    };

    private static AiFinishReason MapFinish(string? reason) => reason switch
    {
        "stop" => AiFinishReason.Stop,
        "length" => AiFinishReason.MaxTokens,
        "content_filter" => AiFinishReason.ContentFilter,
        null => AiFinishReason.Stop,
        _ => AiFinishReason.Stop,
    };
}

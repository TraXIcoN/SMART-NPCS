using System.Net;
using System.Text;
using VoxelAgentNexus.Ai;
using VoxelAgentNexus.Core.Ai;
using Xunit;

namespace VoxelAgentNexus.IntegrationTests;

public sealed class OpenAiCompatibleAdapterTests
{
    private const string CannedResponse =
        """
        {
          "choices": [
            { "message": { "role": "assistant", "content": "Well met, traveler." }, "finish_reason": "stop" }
          ],
          "usage": {
            "prompt_tokens": 120,
            "completion_tokens": 8,
            "total_tokens": 128,
            "prompt_tokens_details": { "cached_tokens": 100 }
          }
        }
        """;

    [Fact]
    public async Task GenerateAsync_Parses_Content_And_Usage()
    {
        var http = new HttpClient(new StubHandler(CannedResponse));
        var adapter = NewAdapter(http);

        var request = new NpcAiRequest
        {
            NpcId = "npc_brom",
            CacheablePrefix = [AiMessage.System("PERSONA_BLOCK")],
            Volatile = [AiMessage.User("Do you remember me?")],
        };

        var response = await adapter.GenerateAsync(request);

        Assert.Equal("Well met, traveler.", response.Text);
        Assert.Equal(120, response.Usage.InputTokens);
        Assert.Equal(100, response.Usage.CachedInputTokens);
        Assert.Equal(8, response.Usage.OutputTokens);
        Assert.Equal(AiFinishReason.Stop, response.FinishReason);
        Assert.Equal("gpt-test", response.ServedByModel);
    }

    [Fact]
    public async Task Request_Sends_Cacheable_Prefix_Before_Volatile()
    {
        string? sentBody = null;
        var http = new HttpClient(new StubHandler(CannedResponse, body => sentBody = body));
        var adapter = NewAdapter(http);

        var request = new NpcAiRequest
        {
            NpcId = "npc_brom",
            CacheablePrefix = [AiMessage.System("PERSONA_BLOCK")],
            Volatile = [AiMessage.User("VOLATILE_TURN")],
        };

        await adapter.GenerateAsync(request);

        Assert.NotNull(sentBody);
        var prefixIndex = sentBody!.IndexOf("PERSONA_BLOCK", StringComparison.Ordinal);
        var volatileIndex = sentBody.IndexOf("VOLATILE_TURN", StringComparison.Ordinal);
        Assert.True(prefixIndex >= 0 && volatileIndex >= 0);
        Assert.True(prefixIndex < volatileIndex, "Cacheable prefix must be sent before volatile content.");
    }

    private static OpenAiCompatibleAdapter NewAdapter(HttpClient http) => new(
        http,
        new OpenAiAdapterOptions
        {
            BaseUrl = new Uri("https://example.test/v1"),
            Model = "gpt-test",
            ApiKey = "sk-test",
        });

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly Action<string>? _captureBody;

        public StubHandler(string json, Action<string>? captureBody = null)
        {
            _json = json;
            _captureBody = captureBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_captureBody is not null && request.Content is not null)
            {
                _captureBody(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json"),
            };
        }
    }
}

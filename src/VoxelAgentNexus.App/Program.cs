using System.Text;
using VoxelAgentNexus.Ai;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Crypto;
using VoxelAgentNexus.Memory;
using VoxelAgentNexus.Persistence;

// ---------------------------------------------------------------------------
// Voxel-Agent-Nexus — minimal "talk to one remembering NPC" demo.
//
// Wires the implemented slices end-to-end:
//   DevCrypto (AES-GCM) -> SqliteEncryptedStore -> EncryptedMemoryStore
//   -> CompositeMemoryRetriever -> NpcPromptBuilder -> INpcAiAdapter
//
// Everything the NPC "remembers" is encrypted on disk. Re-run the program and
// the NPC recalls prior conversations. With no AI endpoint configured it uses
// the deterministic ScriptedFallbackAdapter, proving the memory loop without a
// model. Set NEXUS_AI_BASE_URL + NEXUS_AI_MODEL (+ NEXUS_AI_KEY) for real talk.
// ---------------------------------------------------------------------------

const string npcId = "npc_brom";

var persona = new NpcPersona
{
    NpcId = npcId,
    Name = "Brom",
    SystemPrompt =
        "You are Brom, a gruff but warm-hearted blacksmith in a voxel village. "
        + "You speak plainly, in one to three short sentences, always in character. "
        + "You remember and refer to your past interactions with the player.",
};

var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "nexus.sqlite");
var keyPath = Path.Combine(dataDir, "dev.key");

var (_, crypto) = await DevCrypto.CreatePersistentAsync(keyPath);
await using var store = await SqliteEncryptedStore.OpenAsync(dbPath);
var memory = new EncryptedMemoryStore(crypto, store);
var retriever = new CompositeMemoryRetriever();
var ai = BuildAdapter();

var priorMemories = await memory.LoadWorkingSetAsync(npcId);

Console.WriteLine($"=== Voxel-Agent-Nexus :: {persona.Name} the blacksmith ===");
Console.WriteLine($"AI backend : {ai.Capabilities.ProviderName}");
Console.WriteLine($"Encrypted store : {dbPath}");
Console.WriteLine($"{persona.Name} recalls {priorMemories.Count} prior memory(ies).");
Console.WriteLine("Chat below. Commands: /quit");
Console.WriteLine();

var recentTurns = new List<AiMessage>();

while (true)
{
    Console.Write("you> ");
    var input = Console.ReadLine();
    if (input is null)
    {
        break;
    }

    input = input.Trim();
    if (input.Length == 0)
    {
        continue;
    }

    if (string.Equals(input, "/quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    // 1. Retrieve relevant memories from the encrypted store (decrypted into RAM).
    var workingSet = await memory.LoadWorkingSetAsync(npcId);
    var query = new RetrievalQuery
    {
        NpcId = npcId,
        QueryEmbedding = ReadOnlyMemory<float>.Empty, // TODO: on-device embeddings for relevance
        Now = DateTimeOffset.UtcNow,
        TopK = 5,
    };
    var retrieved = retriever.Retrieve(query, workingSet);

    // 2. Assemble a cache-disciplined prompt (persona prefix + volatile memories/turns).
    recentTurns.Add(AiMessage.User(input));
    var request = NpcPromptBuilder.Build(persona, retrieved, recentTurns);

    // 3. Generate the reply (streamed). Works identically with the fallback adapter.
    Console.Write($"{persona.Name}> ");
    var reply = new StringBuilder();
    await foreach (var chunk in ai.StreamAsync(request))
    {
        Console.Write(chunk.TextDelta);
        reply.Append(chunk.TextDelta);
    }

    Console.WriteLine();
    Console.WriteLine();

    var replyText = reply.ToString();
    recentTurns.Add(AiMessage.Assistant(replyText));
    TrimWindow(recentTurns, maxMessages: 8);

    // 4. Persist the exchange as a new encrypted memory.
    await memory.AppendAsync(new MemoryRecord
    {
        Id = Guid.NewGuid(),
        NpcId = npcId,
        Kind = MemoryKind.Observation,
        Content = $"The player said: \"{input}\". I replied: \"{replyText}\".",
        Importance = 5f,
        CreatedAt = DateTimeOffset.UtcNow,
        LastAccessedAt = DateTimeOffset.UtcNow,
    });
}

Console.WriteLine($"{persona.Name} will remember this. Farewell.");
return;

INpcAiAdapter BuildAdapter()
{
    var baseUrl = Environment.GetEnvironmentVariable("NEXUS_AI_BASE_URL");
    var model = Environment.GetEnvironmentVariable("NEXUS_AI_MODEL");

    if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(model))
    {
        return new ScriptedFallbackAdapter();
    }

    var options = new OpenAiAdapterOptions
    {
        BaseUrl = new Uri(baseUrl),
        Model = model,
        ApiKey = Environment.GetEnvironmentVariable("NEXUS_AI_KEY"),
        IsLocal = baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || baseUrl.Contains("127.0.0.1", StringComparison.Ordinal),
    };
    return new OpenAiCompatibleAdapter(new HttpClient(), options);
}

static void TrimWindow(List<AiMessage> turns, int maxMessages)
{
    if (turns.Count > maxMessages)
    {
        turns.RemoveRange(0, turns.Count - maxMessages);
    }
}

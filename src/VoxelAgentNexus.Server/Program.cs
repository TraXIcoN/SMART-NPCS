using VoxelAgentNexus.Ai;
using VoxelAgentNexus.Core.Ai;
using VoxelAgentNexus.Core.Memory;
using VoxelAgentNexus.Crypto;
using VoxelAgentNexus.Memory;
using VoxelAgentNexus.Persistence;
using VoxelAgentNexus.Server;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5173");
builder.Services.AddSignalR();

// Server-side infrastructure. In the hosted model the crypto layer is
// encryption-at-rest with a server-held key (see docs/adr/0002).
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

var (_, crypto) = await DevCrypto.CreatePersistentAsync(Path.Combine(dataDir, "server.key"));
var store = await SqliteEncryptedStore.OpenAsync(Path.Combine(dataDir, "world.sqlite"));
IMemoryStore memory = new EncryptedMemoryStore(crypto, store);
IRelationshipGraph relationships = new InMemoryRelationshipGraph();
INpcAiAdapter ai = BuildAdapter();
var world = await GameWorld.CreateAsync(memory, relationships, DateTimeOffset.UtcNow);

builder.Services.AddSingleton(memory);
builder.Services.AddSingleton(relationships);
builder.Services.AddSingleton(ai);
builder.Services.AddSingleton(world);
builder.Services.AddSingleton<DialogueService>();
builder.Services.AddHostedService<WorldTickService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapHub<WorldHub>("/hub");

Console.WriteLine($"Voxel-Agent-Nexus shared world. AI backend: {ai.Capabilities.ProviderName}");
Console.WriteLine("Open http://localhost:5173 in a browser (open it twice to see two players share the world).");
app.Run();

static INpcAiAdapter BuildAdapter()
{
    var baseUrl = Environment.GetEnvironmentVariable("NEXUS_AI_BASE_URL");
    var model = Environment.GetEnvironmentVariable("NEXUS_AI_MODEL");
    if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(model))
    {
        return new ScriptedFallbackAdapter();
    }

    return new OpenAiCompatibleAdapter(
        new HttpClient(),
        new OpenAiAdapterOptions
        {
            BaseUrl = new Uri(baseUrl),
            Model = model,
            ApiKey = Environment.GetEnvironmentVariable("NEXUS_AI_KEY"),
        });
}

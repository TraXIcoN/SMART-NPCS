using Microsoft.AspNetCore.SignalR;

namespace VoxelAgentNexus.Server;

/// <summary>
/// The single authoritative tick loop. Advances the world ~10x/second and
/// broadcasts a snapshot to every connected client.
/// </summary>
public sealed class WorldTickService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);

    private readonly GameWorld _world;
    private readonly IHubContext<WorldHub> _hub;

    public WorldTickService(GameWorld world, IHubContext<WorldHub> hub)
    {
        _world = world;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var snapshot = _world.Tick(DateTimeOffset.UtcNow);
                await _hub.Clients.All.SendAsync("Snapshot", snapshot, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }
}

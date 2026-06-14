using Microsoft.AspNetCore.SignalR;
using VoxelAgentNexus.Core.Simulation;

namespace VoxelAgentNexus.Server;

/// <summary>
/// SignalR hub: the client-facing edge of the shared world. Clients call
/// Join/Move/Say; the server pushes Welcome/Snapshot/Dialogue back.
/// </summary>
public sealed class WorldHub : Hub
{
    private readonly GameWorld _world;
    private readonly DialogueService _dialogue;

    public WorldHub(GameWorld world, DialogueService dialogue)
    {
        _world = world;
        _dialogue = dialogue;
    }

    public override async Task OnConnectedAsync()
    {
        var name = $"Traveler-{Context.ConnectionId[..4]}";
        _world.AddPlayer(Context.ConnectionId, name);
        await Clients.Caller.SendAsync("Welcome", Context.ConnectionId, name);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _world.RemovePlayer(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Client reports its new absolute position.</summary>
    public void Move(int x, int y, int z) =>
        _world.MovePlayer(Context.ConnectionId, new VoxelPosition(x, y, z));

    /// <summary>Client speaks; routed to the nearest NPC for an AI reply.</summary>
    public Task Say(string text) => _dialogue.SayAsync(Context.ConnectionId, text);
}

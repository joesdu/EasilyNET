using EasilyNET.Raft.Core.Messages;

namespace EasilyNET.Test.Unit.Raft.Simulation;

internal sealed class SimNetwork(int seed)
{
    private readonly Random _random = new(seed);
    private readonly HashSet<string> _isolated = [];

    public int MinDelayTicks { get; set; } = 1;
    public int MaxDelayTicks { get; set; } = 3;
    public double DropRate { get; set; }

    public void Isolate(string nodeId) => _isolated.Add(nodeId);

    public void Heal(string nodeId) => _isolated.Remove(nodeId);

    public bool CanDeliver(string from, string to)
        => !_isolated.Contains(from) && !_isolated.Contains(to);

    public bool ShouldDrop()
        => DropRate > 0 && _random.NextDouble() < DropRate;

    public long NextDelayTicks()
        => _random.Next(MinDelayTicks, MaxDelayTicks + 1);

    public bool ShouldReorder()
        => _random.NextDouble() < 0.15;

    public SimEnvelope Wrap(string from, string to, RaftMessage message, long dueTick)
        => new(from, to, message, dueTick);
}

internal sealed record SimEnvelope(string From, string To, RaftMessage Message, long DueTick);

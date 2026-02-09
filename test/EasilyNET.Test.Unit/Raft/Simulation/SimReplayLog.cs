namespace EasilyNET.Test.Unit.Raft.Simulation;

internal sealed class SimReplayLog(int seed)
{
    private readonly List<string> _events = [];

    public int Seed { get; } = seed;

    public IReadOnlyList<string> Events => _events;

    public void Add(string value) => _events.Add(value);
}
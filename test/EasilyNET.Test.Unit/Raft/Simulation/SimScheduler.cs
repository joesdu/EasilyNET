namespace EasilyNET.Test.Unit.Raft.Simulation;

internal sealed class SimScheduler
{
    private readonly PriorityQueue<ScheduledAction, long> _queue = new();

    public bool HasPending => _queue.Count > 0;

    public void Schedule(long dueTick, Action action)
    {
        _queue.Enqueue(new(dueTick, action), dueTick);
    }

    public bool TryDequeue(out ScheduledAction action)
    {
        if (_queue.TryDequeue(out var item, out _))
        {
            action = item;
            return true;
        }
        action = default;
        return false;
    }

    internal readonly record struct ScheduledAction(long DueTick, Action Action);
}
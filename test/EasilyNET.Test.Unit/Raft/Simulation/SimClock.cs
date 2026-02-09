namespace EasilyNET.Test.Unit.Raft.Simulation;

internal sealed class SimClock
{
    public long NowTicks { get; private set; }

    public void AdvanceTo(long ticks)
    {
        if (ticks < NowTicks)
        {
            return;
        }
        NowTicks = ticks;
    }

    public void AdvanceBy(long deltaTicks)
    {
        if (deltaTicks <= 0)
        {
            return;
        }
        NowTicks += deltaTicks;
    }
}

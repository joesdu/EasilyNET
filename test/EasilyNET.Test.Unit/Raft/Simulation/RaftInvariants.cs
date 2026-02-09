using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Test.Unit.Raft.Simulation;

internal static class RaftInvariants
{
    public static void Assert(IReadOnlyDictionary<string, RaftNodeState> states)
    {
        var leadersByTerm = states.Values
                                  .Where(x => x.Role == RaftRole.Leader)
                                  .GroupBy(x => x.CurrentTerm)
                                  .ToDictionary(g => g.Key, g => g.Count());

        foreach (var pair in leadersByTerm)
        {
            if (pair.Value > 1)
            {
                throw new InvalidOperationException($"Invariant violated: term {pair.Key} has {pair.Value} leaders.");
            }
        }

        foreach (var state in states.Values)
        {
            if (state.LastApplied > state.CommitIndex)
            {
                throw new InvalidOperationException($"Invariant violated: {state.NodeId} lastApplied({state.LastApplied}) > commitIndex({state.CommitIndex}).");
            }
        }
    }
}

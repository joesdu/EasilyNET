using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Test.Unit.Raft.Simulation;

[TestClass]
public sealed class RaftChaosAndBaselineTests
{
    [TestMethod]
    public void ChaosSmoke_RandomDropsAndDelay_Should_KeepSafetyInvariant()
    {
        var sim = new DeterministicRaftClusterSimulator("n1", "n2", "n3");
        sim.SetDropRate(0.25);
        sim.SetDelayRange(1, 8);

        sim.TriggerElection("n1");
        sim.TriggerElection("n2");
        sim.TriggerElection("n3");
        sim.RunUntilIdle();

        sim.Heal("n1");
        sim.Heal("n2");
        sim.Heal("n3");
        sim.SetDropRate(0.1);
        sim.TriggerElection("n1");
        sim.RunUntilIdle();

        var leadersByTerm = sim.States.Values
                               .Where(x => x.Role == RaftRole.Leader)
                               .GroupBy(x => x.CurrentTerm)
                               .ToArray();
        Assert.IsTrue(leadersByTerm.All(group => group.Count() <= 1));
    }

    [TestMethod]
    public void PerformanceBaseline_Smoke_Should_ReplicateWithinBudget()
    {
        var sim = new DeterministicRaftClusterSimulator("n1", "n2", "n3");
        sim.SetDropRate(0);
        sim.SetDelayRange(1, 1);
        sim.TriggerElection("n1");
        sim.RunUntilIdle();

        var leader = sim.FindLeader();
        Assert.IsNotNull(leader);

        var start = DateTime.UtcNow;
        for (var i = 0; i < 200; i++)
        {
            sim.SubmitCommand(leader, [(byte)(i % 255)]);
        }
        sim.RunUntilIdle();
        sim.TickHeartbeat(leader);
        sim.RunUntilIdle();

        var elapsed = DateTime.UtcNow - start;
        Assert.IsTrue(elapsed < TimeSpan.FromSeconds(5));
        Assert.IsTrue(sim.States[leader].CommitIndex >= 1);
    }
}

using EasilyNET.Raft.Core.Messages;

namespace EasilyNET.Test.Unit.Raft.Simulation;

[TestClass]
public sealed class DeterministicRaftSimulationTests
{
    [TestMethod]
    public void Scenario1_Baseline_Should_ElectSingleLeader_AndCommit()
    {
        var sim = NewSimulator();
        sim.TriggerElection("n1");
        sim.RunUntilIdle();

        var leader = sim.FindLeader();
        Assert.IsNotNull(leader);
        var term = sim.States[leader].CurrentTerm;
        Assert.AreEqual(1, sim.LeaderCount(term));

        sim.SubmitCommand(leader, [1]);
        sim.RunUntilIdle();
        sim.TickHeartbeat(leader);
        sim.RunUntilIdle();

        Assert.IsTrue(sim.States.Values.Count(x => x.CommitIndex >= 1) >= 2);
    }

    [TestMethod]
    public void Scenario2_LeaderCrashAndRecover_Should_Reelect()
    {
        var sim = NewSimulator();
        sim.TriggerElection("n1");
        sim.RunUntilIdle();
        Assert.AreEqual("n1", sim.FindLeader());

        sim.Isolate("n1");
        sim.TriggerElection("n2");
        sim.RunUntilIdle();

        Assert.AreEqual("n2", sim.FindLeader());
        var term = sim.States["n2"].CurrentTerm;
        Assert.IsTrue(sim.LeaderCount(term) <= 1);
        sim.Heal("n1");
        sim.RunUntilIdle();
        Assert.IsTrue(sim.LeaderCount(sim.States["n2"].CurrentTerm) <= 1);
    }

    [TestMethod]
    public void Scenario3_MinorityPartition_Should_NotCommit()
    {
        var sim = NewSimulator();
        sim.TriggerElection("n1");
        sim.RunUntilIdle();

        sim.Isolate("n1");
        sim.SubmitCommand("n1", [2]);
        sim.RunUntilIdle();

        Assert.AreEqual(0, sim.States["n2"].CommitIndex);
        Assert.AreEqual(0, sim.States["n3"].CommitIndex);
    }

    [TestMethod]
    public void Scenario4_SplitVoteCompetition_Should_ConvergeSingleLeader()
    {
        var sim = NewSimulator();
        sim.SetDelayRange(1, 6);
        sim.SetDropRate(0.2);

        sim.TriggerElection("n1");
        sim.TriggerElection("n2");
        sim.TriggerElection("n3");
        sim.RunUntilIdle();

        if (sim.FindLeader() is null)
        {
            sim.TriggerElection("n1");
            sim.RunUntilIdle();
        }

        var leader = sim.FindLeader();
        Assert.IsNotNull(leader);
        var term = sim.States[leader].CurrentTerm;
        Assert.AreEqual(1, sim.LeaderCount(term));
    }

    [TestMethod]
    public void Scenario5_LogConflictRollback_Should_OverwriteFollowerConflict()
    {
        var sim = NewSimulator();
        sim.TriggerElection("n1");
        sim.RunUntilIdle();

        var req = new AppendEntriesRequest
        {
            SourceNodeId = "n2",
            Term = 1,
            LeaderId = "n2",
            PrevLogIndex = 0,
            PrevLogTerm = 0,
            Entries = [new(1, 0, [9])],
            LeaderCommit = 0
        };
        sim.InjectAppendEntries("n3", req);
        sim.RunUntilIdle();

        sim.SubmitCommand("n1", [3]);
        sim.RunUntilIdle();
        sim.TickHeartbeat("n1");
        sim.RunUntilIdle();

        Assert.AreEqual(1, sim.States["n3"].LastLogTerm);
        Assert.AreEqual(sim.States["n1"].LastLogIndex, sim.States["n3"].LastLogIndex);
    }

    [TestMethod]
    public void Scenario6_InstallSnapshotCatchup_Should_AdvanceLaggingFollower()
    {
        var sim = NewSimulator();
        sim.TriggerElection("n1");
        sim.RunUntilIdle();

        var install = new InstallSnapshotRequest
        {
            SourceNodeId = "n1",
            Term = sim.States["n1"].CurrentTerm,
            LeaderId = "n1",
            LastIncludedIndex = 10,
            LastIncludedTerm = sim.States["n1"].CurrentTerm,
            SnapshotData = [1, 2, 3, 4]
        };

        sim.InjectInstallSnapshot("n3", install);
        sim.RunUntilIdle();

        Assert.IsTrue(sim.States["n3"].CommitIndex >= 10);
    }

    [TestMethod]
    public void Scenario7_ConfigChangeInterleaving_Should_ApplySingleChangeSafely()
    {
        var sim = NewSimulator();
        sim.TriggerElection("n1");
        sim.RunUntilIdle();

        sim.SubmitCommand("n1", System.Text.Encoding.UTF8.GetBytes("cfg:Add:n4"));
        sim.SubmitCommand("n1", System.Text.Encoding.UTF8.GetBytes("cfg:Remove:n2"));
        sim.RunUntilIdle();
        sim.TickHeartbeat("n1");
        sim.RunUntilIdle();

        var state = sim.States["n1"];
        Assert.IsTrue(state.PendingConfigurationChangeIndex is null || state.PendingConfigurationChangeNodeId is not null);
    }

    [TestMethod]
    public void Replay_Should_HaveSeedAndEvents()
    {
        var sim = NewSimulator();
        sim.TriggerElection("n1");
        sim.RunUntilIdle();

        Assert.AreEqual(20260209, sim.Replay.Seed);
        Assert.IsTrue(sim.Replay.Events.Count > 0);
    }

    private static DeterministicRaftClusterSimulator NewSimulator()
    {
        var sim = new DeterministicRaftClusterSimulator("n1", "n2", "n3");
        sim.SetDropRate(0);
        sim.SetDelayRange(1, 2);
        return sim;
    }
}

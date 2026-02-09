using EasilyNET.Raft.Core.Actions;
using EasilyNET.Raft.Core.Engine;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;
using EasilyNET.Raft.Core.Options;

namespace EasilyNET.Test.Unit.Raft.Core;

[TestClass]
public sealed class RaftNodeTests
{
    [TestMethod]
    public void ElectionTimeout_Should_StartPreVote_WhenEnabled()
    {
        var node = new RaftNode(NewOptions(enablePreVote: true));
        var state = NewState();

        var result = node.Handle(state, new ElectionTimeoutElapsed
        {
            SourceNodeId = "n1",
            Term = 0
        });

        Assert.AreEqual(RaftRole.Follower, result.State.Role);
        Assert.IsTrue(result.Actions.OfType<SendMessageAction>().Any(a => a.Message is RequestVoteRequest { IsPreVote: true }));
    }

    [TestMethod]
    public void VoteResponses_Should_BecomeLeader_OnMajority()
    {
        var node = new RaftNode(NewOptions(enablePreVote: false));
        var state = NewState();

        node.Handle(state, new ElectionTimeoutElapsed { SourceNodeId = "n1", Term = 0 });
        var result = node.Handle(state, new RequestVoteResponse
        {
            SourceNodeId = "n2",
            Term = 1,
            VoteGranted = true,
            IsPreVote = false
        });

        Assert.AreEqual(RaftRole.Leader, result.State.Role);
        Assert.IsTrue(result.Actions.OfType<SendMessageAction>().Any(a => a.Message is AppendEntriesRequest));
    }

    [TestMethod]
    public void LeaderAppendResponse_Should_AdvanceCommit_ForCurrentTermEntries()
    {
        var node = new RaftNode(NewOptions(enablePreVote: false));
        var state = NewState();

        node.Handle(state, new ElectionTimeoutElapsed { SourceNodeId = "n1", Term = 0 });
        node.Handle(state, new RequestVoteResponse
        {
            SourceNodeId = "n2",
            Term = 1,
            VoteGranted = true,
            IsPreVote = false
        });
        node.Handle(state, new ClientCommandRequest
        {
            SourceNodeId = "client",
            Term = 1,
            Command = [1, 2, 3]
        });

        var result = node.Handle(state, new AppendEntriesResponse
        {
            SourceNodeId = "n2",
            Term = 1,
            Success = true,
            MatchIndex = 1
        });

        Assert.AreEqual(1, result.State.CommitIndex);
        Assert.IsTrue(result.Actions.OfType<ApplyToStateMachineAction>().Any());
    }

    [TestMethod]
    public void FollowerAppendEntries_Should_Reject_WhenPrevLogDoesNotMatch()
    {
        var node = new RaftNode(NewOptions());
        var state = NewState();
        state.Log.Add(new RaftLogEntry(1, 1, [9]));

        var result = node.Handle(state, new AppendEntriesRequest
        {
            SourceNodeId = "n2",
            Term = 1,
            LeaderId = "n2",
            PrevLogIndex = 1,
            PrevLogTerm = 999,
            Entries = [],
            LeaderCommit = 0
        });

        var response = result.Actions.OfType<SendMessageAction>().Select(x => x.Message).OfType<AppendEntriesResponse>().Single();
        Assert.IsFalse(response.Success);
    }

    [TestMethod]
    public void ReadIndex_Should_RequireQuorumConfirmation()
    {
        var node = new RaftNode(NewOptions(enablePreVote: false));
        var state = NewState();

        node.Handle(state, new ElectionTimeoutElapsed { SourceNodeId = "n1", Term = 0 });
        node.Handle(state, new RequestVoteResponse
        {
            SourceNodeId = "n2",
            Term = 1,
            VoteGranted = true,
            IsPreVote = false
        });
        state.CommitIndex = 5;
        state.MatchIndex["n1"] = 5;
        state.MatchIndex["n2"] = 5;
        state.MatchIndex["n3"] = -1;

        var result = node.Handle(state, new ReadIndexRequest
        {
            SourceNodeId = "api",
            Term = 1
        });

        var response = result.Actions.OfType<SendMessageAction>().Select(x => x.Message).OfType<ReadIndexResponse>().Single();
        Assert.IsTrue(response.Success);
        Assert.AreEqual(5, response.ReadIndex);

        state.MatchIndex["n2"] = -1;
        var failResult = node.Handle(state, new ReadIndexRequest
        {
            SourceNodeId = "api",
            Term = 1
        });
        var failResponse = failResult.Actions.OfType<SendMessageAction>().Select(x => x.Message).OfType<ReadIndexResponse>().Last();
        Assert.IsFalse(failResponse.Success);
    }

    [TestMethod]
    public void ConfigurationChange_Should_ApplyAfterJointAndFinalCommit()
    {
        var node = new RaftNode(NewOptions(enablePreVote: false));
        var state = NewState();

        node.Handle(state, new ElectionTimeoutElapsed { SourceNodeId = "n1", Term = 0 });
        node.Handle(state, new RequestVoteResponse
        {
            SourceNodeId = "n2",
            Term = 1,
            VoteGranted = true,
            IsPreVote = false
        });

        var proposal = node.Handle(state, new ConfigurationChangeRequest
        {
            SourceNodeId = "api",
            Term = 1,
            ChangeType = ConfigurationChangeType.Add,
            TargetNodeId = "n4"
        });

        var accept = proposal.Actions.OfType<SendMessageAction>().Select(x => x.Message).OfType<ConfigurationChangeResponse>().Single();
        Assert.IsTrue(accept.Success);

        node.Handle(state, new AppendEntriesResponse
        {
            SourceNodeId = "n2",
            Term = 1,
            Success = true,
            MatchIndex = 1
        });

        var afterJoint = node.Handle(state, new AppendEntriesResponse
        {
            SourceNodeId = "n3",
            Term = 1,
            Success = true,
            MatchIndex = 1
        });

        Assert.AreEqual(ConfigurationTransitionPhase.Finalizing, state.ConfigurationTransitionPhase);
        Assert.IsTrue(afterJoint.Actions.OfType<PersistEntriesAction>().Any());

        node.Handle(state, new AppendEntriesResponse
        {
            SourceNodeId = "n2",
            Term = 1,
            Success = true,
            MatchIndex = 2
        });

        node.Handle(state, new AppendEntriesResponse
        {
            SourceNodeId = "n3",
            Term = 1,
            Success = true,
            MatchIndex = 2
        });

        Assert.IsTrue(state.ClusterMembers.Contains("n4"));
        Assert.AreEqual(ConfigurationTransitionPhase.None, state.ConfigurationTransitionPhase);
        Assert.IsNull(state.PendingConfigurationChangeIndex);
    }

    [TestMethod]
    public void ConfigurationChange_Should_Reject_WhenTransitionInProgress()
    {
        var node = new RaftNode(NewOptions(enablePreVote: false));
        var state = NewState();

        node.Handle(state, new ElectionTimeoutElapsed { SourceNodeId = "n1", Term = 0 });
        node.Handle(state, new RequestVoteResponse
        {
            SourceNodeId = "n2",
            Term = 1,
            VoteGranted = true,
            IsPreVote = false
        });

        node.Handle(state, new ConfigurationChangeRequest
        {
            SourceNodeId = "api",
            Term = 1,
            ChangeType = ConfigurationChangeType.Add,
            TargetNodeId = "n4"
        });

        var second = node.Handle(state, new ConfigurationChangeRequest
        {
            SourceNodeId = "api",
            Term = 1,
            ChangeType = ConfigurationChangeType.Add,
            TargetNodeId = "n5"
        });

        var reject = second.Actions.OfType<SendMessageAction>().Select(x => x.Message).OfType<ConfigurationChangeResponse>().Single();
        Assert.IsFalse(reject.Success);
        Assert.AreEqual("configuration change in progress", reject.Reason);
    }

    private static RaftNodeState NewState() => new()
    {
        NodeId = "n1",
        ClusterMembers = ["n1", "n2", "n3"]
    };

    private static RaftOptions NewOptions(bool enablePreVote = true) => new()
    {
        NodeId = "n1",
        ClusterMembers = ["n1", "n2", "n3"],
        EnablePreVote = enablePreVote,
        MaxEntriesPerAppend = 100
    };
}

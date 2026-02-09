using EasilyNET.Raft.AspNetCore.Runtime;
using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Options;
using EasilyNET.Raft.Core.StateMachine;
using EasilyNET.Raft.Core.Storage.InMemory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EasilyNET.Test.Unit.Raft.AspNetCore;

[TestClass]
public sealed class RaftRuntimeReadIndexTests
{
    [TestMethod]
    public async Task ReadIndex_Should_Succeed_When_HeartbeatConfirmQuorum()
    {
        var transport = new FakeRaftTransport
        {
            AppendResultByTarget =
            {
                ["n2"] = true,
                ["n3"] = true
            }
        };
        var runtime = NewRuntime(transport);
        await BecomeLeaderAsync(runtime).ConfigureAwait(false);
        var state = runtime.GetState();
        state.CommitIndex = 5;
        state.MatchIndex["n1"] = 5;
        state.MatchIndex["n2"] = 5;
        state.MatchIndex["n3"] = -1;
        var response = await runtime.HandleRpcAsync<ReadIndexResponse>(new ReadIndexRequest
        {
            SourceNodeId = "api",
            Term = state.CurrentTerm
        }).ConfigureAwait(false);
        Assert.IsTrue(response.Success);
        Assert.AreEqual(5, response.ReadIndex);
    }

    [TestMethod]
    public async Task ReadIndex_Should_Fail_When_HeartbeatCannotConfirmQuorum()
    {
        var transport = new FakeRaftTransport
        {
            AppendResultByTarget =
            {
                ["n2"] = false,
                ["n3"] = false
            }
        };
        var runtime = NewRuntime(transport);
        await BecomeLeaderAsync(runtime).ConfigureAwait(false);
        var state = runtime.GetState();
        state.CommitIndex = 5;
        state.MatchIndex["n1"] = 5;
        state.MatchIndex["n2"] = 5;
        state.MatchIndex["n3"] = -1;
        var response = await runtime.HandleRpcAsync<ReadIndexResponse>(new ReadIndexRequest
        {
            SourceNodeId = "api",
            Term = state.CurrentTerm
        }).ConfigureAwait(false);
        Assert.IsFalse(response.Success);
    }

    private static async Task BecomeLeaderAsync(IRaftRuntime runtime)
    {
        await runtime.HandleAsync(new ElectionTimeoutElapsed
        {
            SourceNodeId = "n1",
            Term = 0
        }).ConfigureAwait(false);
        await runtime.HandleAsync(new RequestVoteResponse
        {
            SourceNodeId = "n2",
            Term = 1,
            VoteGranted = true,
            IsPreVote = false
        }).ConfigureAwait(false);
    }

    private static RaftRuntime NewRuntime(IRaftTransport transport)
    {
        var options = Options.Create(new RaftOptions
        {
            NodeId = "n1",
            ClusterMembers = ["n1", "n2", "n3"],
            EnablePreVote = false,
            MaxEntriesPerAppend = 100
        });
        return new(options,
            new InMemoryStateStore(),
            new InMemoryLogStore(),
            new InMemorySnapshotStore(),
            new NoopStateMachine(),
            transport,
            new(),
            NullLogger<RaftRuntime>.Instance);
    }

    private sealed class FakeRaftTransport : IRaftTransport
    {
        public Dictionary<string, bool> AppendResultByTarget { get; } = [];

        public Task<RaftMessage?> SendAsync(string targetNodeId, RaftMessage message, CancellationToken cancellationToken = default)
        {
            return message switch
            {
                AppendEntriesRequest append => Task.FromResult<RaftMessage?>(new AppendEntriesResponse
                {
                    SourceNodeId = targetNodeId,
                    Term = append.Term,
                    Success = AppendResultByTarget.GetValueOrDefault(targetNodeId, true),
                    MatchIndex = append.PrevLogIndex + append.Entries.Count
                }),
                RequestVoteRequest vote => Task.FromResult<RaftMessage?>(new RequestVoteResponse
                {
                    SourceNodeId = targetNodeId,
                    Term = vote.Term,
                    VoteGranted = true,
                    IsPreVote = vote.IsPreVote
                }),
                InstallSnapshotRequest install => Task.FromResult<RaftMessage?>(new InstallSnapshotResponse
                {
                    SourceNodeId = targetNodeId,
                    Term = install.Term,
                    Success = true
                }),
                _ => Task.FromResult<RaftMessage?>(null)
            };
        }
    }
}
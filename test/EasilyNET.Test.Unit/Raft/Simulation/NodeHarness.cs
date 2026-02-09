using EasilyNET.Raft.Core.Actions;
using EasilyNET.Raft.Core.Engine;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;
using EasilyNET.Raft.Core.Options;

namespace EasilyNET.Test.Unit.Raft.Simulation;

internal sealed class NodeHarness
{
    private readonly RaftNode _node;

    public NodeHarness(string nodeId, IReadOnlyList<string> allNodes, bool enablePreVote)
    {
        _node = new(new RaftOptions
        {
            NodeId = nodeId,
            ClusterMembers = [.. allNodes],
            EnablePreVote = enablePreVote,
            MaxEntriesPerAppend = 100
        });

        State = new()
        {
            NodeId = nodeId,
            ClusterMembers = [.. allNodes]
        };
    }

    public RaftNodeState State { get; }

    public IReadOnlyList<RaftAction> Handle(RaftMessage message)
        => _node.Handle(State, message).Actions;
}

using EasilyNET.Raft.Core.Actions;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Test.Unit.Raft.Simulation;

internal sealed class DeterministicRaftClusterSimulator(params string[] nodeIds)
{
    private readonly SimClock _clock = new();
    private readonly SimNetwork _network = new(20260209);
    private readonly Dictionary<string, NodeHarness> _nodes = nodeIds.ToDictionary(x => x, x => new NodeHarness(x, nodeIds, false));
    private readonly SimScheduler _scheduler = new();

    public IReadOnlyDictionary<string, RaftNodeState> States => _nodes.ToDictionary(x => x.Key, x => x.Value.State);

    public SimReplayLog Replay { get; } = new(20260209);

    public void Isolate(string nodeId) => _network.Isolate(nodeId);

    public void Heal(string nodeId) => _network.Heal(nodeId);

    public void SetDropRate(double dropRate) => _network.DropRate = dropRate;

    public void SetDelayRange(int minTicks, int maxTicks)
    {
        _network.MinDelayTicks = minTicks;
        _network.MaxDelayTicks = maxTicks;
    }

    public void TriggerElection(string nodeId)
    {
        Replay.Add($"trigger-election:{nodeId}@{_clock.NowTicks}");
        Dispatch(nodeId, new ElectionTimeoutElapsed { SourceNodeId = nodeId, Term = _nodes[nodeId].State.CurrentTerm });
    }

    public void SubmitCommand(string leaderNodeId, byte[] command)
    {
        Replay.Add($"submit:{leaderNodeId}:len={command.Length}@{_clock.NowTicks}");
        Dispatch(leaderNodeId, new ClientCommandRequest { SourceNodeId = "client", Term = _nodes[leaderNodeId].State.CurrentTerm, Command = command });
    }

    public void TickHeartbeat(string nodeId)
    {
        Replay.Add($"heartbeat:{nodeId}@{_clock.NowTicks}");
        Dispatch(nodeId, new HeartbeatTimeoutElapsed { SourceNodeId = nodeId, Term = _nodes[nodeId].State.CurrentTerm });
    }

    public void InjectAppendEntries(string toNode, AppendEntriesRequest request)
    {
        Dispatch(toNode, request);
    }

    public void InjectInstallSnapshot(string toNode, InstallSnapshotRequest request)
    {
        Dispatch(toNode, request);
    }

    public void RunUntilIdle(int maxSteps = 20_000)
    {
        var steps = 0;
        while (_scheduler.HasPending)
        {
            if (++steps > maxSteps)
            {
                throw new InvalidOperationException($"Simulation exceeded max steps. seed={Replay.Seed}");
            }
            if (!_scheduler.TryDequeue(out var action))
            {
                break;
            }
            _clock.AdvanceTo(action.DueTick);
            action.Action();
            RaftInvariants.Assert(States);
        }
    }

    public void RunSteps(int steps)
    {
        for (var i = 0; i < steps && _scheduler.HasPending; i++)
        {
            if (_scheduler.TryDequeue(out var action))
            {
                _clock.AdvanceTo(action.DueTick);
                action.Action();
                RaftInvariants.Assert(States);
            }
        }
    }

    public string? FindLeader() =>
        States.Where(x => x.Value.Role == RaftRole.Leader)
              .OrderByDescending(x => x.Value.CurrentTerm)
              .Select(x => x.Key)
              .FirstOrDefault();

    public int LeaderCount(long term) => States.Values.Count(x => x.Role == RaftRole.Leader && x.CurrentTerm == term);

    private void Dispatch(string nodeId, RaftMessage message)
    {
        var actions = _nodes[nodeId].Handle(message);
        foreach (var action in actions)
        {
            if (action is not SendMessageAction send)
            {
                continue;
            }
            if (!_network.CanDeliver(nodeId, send.TargetNodeId) || _network.ShouldDrop())
            {
                Replay.Add($"drop:{nodeId}->{send.TargetNodeId}:{send.Message.GetType().Name}@{_clock.NowTicks}");
                continue;
            }
            var due = _clock.NowTicks + _network.NextDelayTicks();
            var envelope = _network.Wrap(nodeId, send.TargetNodeId, send.Message, due);
            Replay.Add($"send:{envelope.From}->{envelope.To}:{envelope.Message.GetType().Name}@{due}");
            _scheduler.Schedule(due, () => Dispatch(envelope.To, envelope.Message));
            if (_network.ShouldReorder())
            {
                _scheduler.Schedule(due + 1, () => { });
            }
        }
    }
}
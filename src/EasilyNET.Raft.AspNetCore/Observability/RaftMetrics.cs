using System.Diagnostics.Metrics;
using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Raft.AspNetCore.Observability;

/// <summary>
///     <para xml:lang="en">Raft metrics recorder</para>
///     <para xml:lang="zh">Raft 指标记录器</para>
/// </summary>
public sealed class RaftMetrics
{
    private readonly object _sync = new();
    private readonly Meter _meter;
    private readonly Counter<long> _electionCount;
    private readonly Counter<long> _leaderChangeCount;
    private readonly Histogram<double> _appendLatencyMs;
    private readonly Histogram<double> _snapshotInstallMs;
    private readonly Counter<long> _commitAdvanceCount;
    private readonly Dictionary<string, long> _replicationLagByPeer = [];

    private long _currentTerm;
    private int _currentRole;
    private long _commitIndex;
    private long _lastApplied;

    /// <summary>
    /// Initializes metrics instruments.
    /// </summary>
    public RaftMetrics()
    {
        _meter = new Meter("EasilyNET.Raft", "1.0.0");
        _electionCount = _meter.CreateCounter<long>("raft_election_count");
        _leaderChangeCount = _meter.CreateCounter<long>("raft_leader_changes_total");
        _appendLatencyMs = _meter.CreateHistogram<double>("raft_append_latency");
        _snapshotInstallMs = _meter.CreateHistogram<double>("raft_snapshot_install_seconds");
        _commitAdvanceCount = _meter.CreateCounter<long>("raft_commit_index_advance_total");
        _ = _meter.CreateObservableGauge<long>("raft_term", () => _currentTerm);
        _ = _meter.CreateObservableGauge<int>("raft_role", () => _currentRole);
        _ = _meter.CreateObservableGauge<long>("raft_commit_index", () => _commitIndex);
        _ = _meter.CreateObservableGauge<long>("raft_last_applied", () => _lastApplied);
        _ = _meter.CreateObservableGauge<long>("raft_replication_lag", ObserveReplicationLag);
    }

    /// <summary>
    /// Records role and term transition.
    /// </summary>
    public void RecordTransition(RaftRole previousRole, long previousTerm, RaftNodeState current)
    {
        if (current.CurrentTerm > previousTerm)
        {
            _electionCount.Add(1);
        }
        if (previousRole != RaftRole.Leader && current.Role == RaftRole.Leader)
        {
            _leaderChangeCount.Add(1);
        }
    }

    /// <summary>
    /// Records append replication latency.
    /// </summary>
    public void RecordAppendLatency(double latencyMs) => _appendLatencyMs.Record(latencyMs);

    /// <summary>
    /// Records snapshot installation elapsed seconds.
    /// </summary>
    public void RecordSnapshotInstallSeconds(double seconds) => _snapshotInstallMs.Record(seconds);

    /// <summary>
    /// Records commit index advancement.
    /// </summary>
    public void RecordCommitAdvance() => _commitAdvanceCount.Add(1);

    /// <summary>
    /// Records raft state gauges and replication lag.
    /// </summary>
    public void RecordState(RaftNodeState state)
    {
        _currentTerm = state.CurrentTerm;
        _currentRole = (int)state.Role;
        _commitIndex = state.CommitIndex;
        _lastApplied = state.LastApplied;

        lock (_sync)
        {
            _replicationLagByPeer.Clear();
            foreach (var peer in state.ClusterMembers.Where(x => x != state.NodeId))
            {
                var matched = state.MatchIndex.GetValueOrDefault(peer, 0);
                _replicationLagByPeer[peer] = Math.Max(0, state.LastLogIndex - matched);
            }
        }
    }

    private IEnumerable<Measurement<long>> ObserveReplicationLag()
    {
        lock (_sync)
        {
            return _replicationLagByPeer
                   .Select(pair => new Measurement<long>(pair.Value, new KeyValuePair<string, object?>("peer", pair.Key)))
                   .ToArray();
        }
    }
}

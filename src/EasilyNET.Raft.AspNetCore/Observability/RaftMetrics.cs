using System.Diagnostics.Metrics;
using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Raft.AspNetCore.Observability;

/// <summary>
///     <para xml:lang="en">Raft metrics recorder</para>
///     <para xml:lang="zh">Raft 指标记录器</para>
/// </summary>
public sealed class RaftMetrics
{
    private readonly Histogram<double> _appendLatencyMs;
    private readonly Counter<long> _commitAdvanceCount;
    private readonly Counter<long> _electionCount;
    private readonly Counter<long> _leaderChangeCount;
    private readonly Dictionary<string, long> _replicationLagByPeer = [];
    private readonly Histogram<double> _snapshotInstallMs;
    private readonly Lock _sync = new();
    private long _commitIndex;
    private int _currentRole;

    private long _currentTerm;
    private long _lastApplied;

    /// <summary>
    /// Initializes metrics instruments.
    /// </summary>
    public RaftMetrics()
    {
        Meter meter = new("EasilyNET.Raft", "1.0.0");
        _electionCount = meter.CreateCounter<long>("raft_election_count");
        _leaderChangeCount = meter.CreateCounter<long>("raft_leader_changes_total");
        _appendLatencyMs = meter.CreateHistogram<double>("raft_append_latency");
        _snapshotInstallMs = meter.CreateHistogram<double>("raft_snapshot_install_seconds");
        _commitAdvanceCount = meter.CreateCounter<long>("raft_commit_index_advance_total");
        _ = meter.CreateObservableGauge("raft_term", () => _currentTerm);
        _ = meter.CreateObservableGauge("raft_role", () => _currentRole);
        _ = meter.CreateObservableGauge("raft_commit_index", () => _commitIndex);
        _ = meter.CreateObservableGauge("raft_last_applied", () => _lastApplied);
        _ = meter.CreateObservableGauge("raft_replication_lag", ObserveReplicationLag);
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
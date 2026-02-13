using System.Collections.Concurrent;
using EasilyNET.Raft.AspNetCore.Observability;
using EasilyNET.Raft.AspNetCore.Services;
using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Core.Actions;
using EasilyNET.Raft.Core.Engine;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;
using EasilyNET.Raft.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasilyNET.Raft.AspNetCore.Runtime;

/// <summary>
///     <para xml:lang="en">Single-threaded raft runtime executor</para>
///     <para xml:lang="zh">单线程 Raft 运行时执行器</para>
/// </summary>
public sealed class RaftRuntime : IRaftRuntime
{
    private const int MaxReplyDepth = 8;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private readonly ILogger<RaftRuntime> _logger;
    private readonly ILogStore _logStore;
    private readonly RaftMetrics _metrics;
    private readonly RaftNode _node;

    /// <summary>
    /// Tracks pending configuration change requests awaiting commit.
    /// Keyed by the joint-configuration log entry index.
    /// Completed when <see cref="ConfigurationTransitionPhase.None" /> is reached after commit,
    /// or failed on leadership loss.
    /// </summary>
    private readonly ConcurrentDictionary<long, TaskCompletionSource<ConfigurationChangeResponse>> _pendingConfigChanges = [];

    private readonly RaftOptions _raftOptions;
    private readonly IRaftTransport _raftTransport;
    private readonly ISnapshotStore _snapshotStore;

    private readonly RaftNodeState _state;
    private readonly IStateMachine _stateMachine;
    private readonly IStateStore _stateStore;
    private readonly IRaftTimerControl _timerControl;
    private volatile bool _initialized;

    /// <summary>
    ///     <para xml:lang="en">Initializes runtime and in-memory state</para>
    ///     <para xml:lang="zh">初始化运行时与内存状态</para>
    /// </summary>
    public RaftRuntime(
        IOptions<RaftOptions> options,
        IStateStore stateStore,
        ILogStore logStore,
        ISnapshotStore snapshotStore,
        IStateMachine stateMachine,
        IRaftTransport raftTransport,
        IRaftTimerControl timerControl,
        RaftMetrics metrics,
        ILogger<RaftRuntime> logger)
    {
        _stateStore = stateStore;
        _logStore = logStore;
        _snapshotStore = snapshotStore;
        _stateMachine = stateMachine;
        _raftTransport = raftTransport;
        _timerControl = timerControl;
        _metrics = metrics;
        _logger = logger;
        _raftOptions = options.Value;
        _node = new(_raftOptions);
        _state = new()
        {
            NodeId = _raftOptions.NodeId,
            ClusterMembers = [.. _raftOptions.ClusterMembers]
        };
    }

    /// <inheritdoc />
    public async Task HandleAsync(RaftMessage message, CancellationToken cancellationToken = default) => _ = await ProcessAsync(message, false, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<TResponse> HandleRpcAsync<TResponse>(RaftMessage message, CancellationToken cancellationToken = default) where TResponse : RaftMessage
    {
        if (message is ReadIndexRequest readIndexRequest)
        {
            var readIndexResponse = await HandleReadIndexWithConfirmationAsync(readIndexRequest, cancellationToken).ConfigureAwait(false);
            return readIndexResponse as TResponse ?? throw new InvalidOperationException($"Expected RPC response type '{typeof(TResponse).Name}' but got '{readIndexResponse.GetType().Name}'.");
        }
        if (message is ConfigurationChangeRequest)
        {
            var configResponse = await HandleConfigurationChangeWithCommitAsync(message, cancellationToken).ConfigureAwait(false);
            return configResponse as TResponse ?? throw new InvalidOperationException($"Expected RPC response type '{typeof(TResponse).Name}' but got '{configResponse.GetType().Name}'.");
        }
        var response = await ProcessAsync(message, true, cancellationToken).ConfigureAwait(false);
        return response as TResponse ?? throw new InvalidOperationException($"Expected RPC response type '{typeof(TResponse).Name}' but got '{response?.GetType().Name ?? "null"}'.");
    }

    /// <inheritdoc />
    public RaftNodeState GetState() => _state;

    /// <inheritdoc />
    public bool IsInitialized => _initialized;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }
        await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }
            await RecoverAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initGate.Release();
        }
    }

    private async Task<RaftMessage?> ProcessAsync(RaftMessage message, bool captureRpcResponse, CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var prevRole = _state.Role;
            var prevTerm = _state.CurrentTerm;
            var prevCommit = _state.CommitIndex;
            var result = _node.Handle(_state, message);
            var response = await ExecuteActionsAsync(result.Actions, captureRpcResponse, message.SourceNodeId, cancellationToken).ConfigureAwait(false);
            _metrics.RecordTransition(prevRole, prevTerm, _state);
            if (_state.CommitIndex > prevCommit)
            {
                _metrics.RecordCommitAdvance();
            }
            _metrics.RecordState(_state);
            CheckPendingConfigurationChanges();
            return response;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ReadIndexResponse> HandleReadIndexWithConfirmationAsync(ReadIndexRequest request, CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        // Phase 1: Under the gate — run pure engine, execute persistence/timer actions, capture state snapshot.
        // Network I/O (heartbeat confirmation) is deferred to Phase 2 outside the gate so that
        // other Raft messages (elections, AppendEntries, client commands) are not blocked.
        ReadIndexResponse readResponse;
        bool requiresConfirmation;
        int quorum;
        long responseTerm;
        long readCommitIndex;
        string nodeId;
        List<SendMessageAction> heartbeatActions;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var prevRole = _state.Role;
            var prevTerm = _state.CurrentTerm;
            var prevCommit = _state.CommitIndex;
            var result = _node.Handle(_state, request);
            readResponse = result.Actions
                                 .OfType<SendMessageAction>()
                                 .Where(x => x.TargetNodeId == request.SourceNodeId)
                                 .Select(x => x.Message)
                                 .OfType<ReadIndexResponse>()
                                 .FirstOrDefault() ??
                           new ReadIndexResponse
                           {
                               SourceNodeId = _state.NodeId,
                               Term = _state.CurrentTerm,
                               Success = false,
                               ReadIndex = _state.CommitIndex,
                               LeaderId = _state.LeaderId
                           };
            requiresConfirmation = _state.Role == RaftRole.Leader;
            quorum = _state.Quorum;
            responseTerm = _state.CurrentTerm;
            readCommitIndex = _state.CommitIndex;
            nodeId = _state.NodeId;
            heartbeatActions = [];
            // Execute persistence, timer, and apply actions under the gate; collect heartbeat sends for Phase 2.
            foreach (var action in result.Actions)
            {
                switch (action)
                {
                    case PersistStateAction persistState:
                        await _stateStore.SaveAsync(persistState.Term, persistState.VotedFor, cancellationToken).ConfigureAwait(false);
                        break;
                    case PersistEntriesAction persistEntries:
                        await _logStore.AppendAsync(persistEntries.Entries, cancellationToken).ConfigureAwait(false);
                        break;
                    case TruncateLogSuffixAction truncate:
                        await _logStore.TruncateSuffixAsync(truncate.FromIndexInclusive, cancellationToken).ConfigureAwait(false);
                        break;
                    case ApplyToStateMachineAction apply:
                        await _stateMachine.ApplyAsync(apply.Entries, cancellationToken).ConfigureAwait(false);
                        _state.LastApplied = Math.Max(_state.LastApplied, apply.Entries[^1].Index);
                        await TryCreateSnapshotAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    case TakeSnapshotAction snapshot:
                        await _snapshotStore.SaveAsync(snapshot.LastIncludedIndex, snapshot.LastIncludedTerm, snapshot.SnapshotData, cancellationToken).ConfigureAwait(false);
                        await _stateMachine.RestoreSnapshotAsync(snapshot.SnapshotData, cancellationToken).ConfigureAwait(false);
                        await _logStore.CompactPrefixAsync(snapshot.LastIncludedIndex, cancellationToken).ConfigureAwait(false);
                        _state.SnapshotLastIncludedIndex = snapshot.LastIncludedIndex;
                        _state.SnapshotLastIncludedTerm = snapshot.LastIncludedTerm;
                        break;
                    case SendMessageAction send when send.TargetNodeId == request.SourceNodeId:
                        // ReadIndex response to caller — already captured above, skip.
                        break;
                    case SendMessageAction send:
                        // Defer network I/O to Phase 2.
                        heartbeatActions.Add(send);
                        break;
                    case ResetElectionTimerAction:
                        _timerControl.ResetElectionTimer();
                        break;
                    case ResetHeartbeatTimerAction:
                        _timerControl.ResetHeartbeatTimer();
                        break;
                }
            }
            _metrics.RecordTransition(prevRole, prevTerm, _state);
            if (_state.CommitIndex > prevCommit)
            {
                _metrics.RecordCommitAdvance();
            }
            _metrics.RecordState(_state);
        }
        finally
        {
            _gate.Release();
        }
        // Phase 2: Outside the gate — send heartbeats in parallel to confirm Leader identity.
        // If the leader stepped down between Phase 1 and Phase 2, heartbeat acks won't reach
        // quorum and the read will correctly fail. The readIndex captured in Phase 1 was valid
        // at that point-in-time, so no re-acquisition of the gate is needed for the response.
        var readConfirmAcks = requiresConfirmation ? 1 : 0; // Leader counts itself
        if (requiresConfirmation && heartbeatActions.Count > 0)
        {
            var replyTasks = heartbeatActions.Select(async send =>
            {
                try
                {
                    var begin = DateTime.UtcNow;
                    var reply = await _raftTransport.SendAsync(send.TargetNodeId, send.Message, cancellationToken).ConfigureAwait(false);
                    if (send.Message is AppendEntriesRequest)
                    {
                        _metrics.RecordAppendLatency((DateTime.UtcNow - begin).TotalMilliseconds);
                    }
                    return reply;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(ex, "ReadIndex heartbeat to {TargetNodeId} failed", send.TargetNodeId);
                    }
                    return null;
                }
            });
            var replies = await Task.WhenAll(replyTasks).ConfigureAwait(false);
            // Count quorum acks from the captured replies.
            readConfirmAcks += replies.OfType<AppendEntriesResponse>().Count(r => r.Success && r.Term == responseTerm);
            // Phase 3: Re-acquire gate to feed replies into the engine so matchIndex/nextIndex stay consistent.
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var reply in replies)
                {
                    await ProcessReplyAsync(reply, false, request.SourceNodeId, 0, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _gate.Release();
            }
        }
        var confirmed = requiresConfirmation && readConfirmAcks >= quorum;
        return readResponse with
        {
            Success = readResponse.Success && confirmed,
            LeaderId = confirmed ? nodeId : readResponse.LeaderId,
            ReadIndex = readCommitIndex,
            Term = responseTerm
        };
    }

    /// <summary>
    /// Handles a configuration change request and waits for the change to be committed
    /// before returning success. If the request is rejected (validation failure), returns immediately.
    /// If the leader steps down before commit, the pending request is failed.
    /// </summary>
    private async Task<ConfigurationChangeResponse> HandleConfigurationChangeWithCommitAsync(RaftMessage message, CancellationToken cancellationToken)
    {
        var response = await ProcessAsync(message, true, cancellationToken).ConfigureAwait(false);
        if (response is not ConfigurationChangeResponse configResponse)
        {
            throw new InvalidOperationException($"Expected ConfigurationChangeResponse but got '{response?.GetType().Name ?? "null"}'.");
        }
        // Rejected requests (not leader, validation failure, etc.) return immediately.
        if (!configResponse.Success || configResponse.Committed || !configResponse.PendingIndex.HasValue)
        {
            return configResponse;
        }
        // Accepted but not yet committed — register a TCS and wait for commit or leadership loss.
        var tcs = new TaskCompletionSource<ConfigurationChangeResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingConfigChanges[configResponse.PendingIndex.Value] = tcs;
        // Register cancellation to unblock the caller if the token fires.
        await using var ctr = cancellationToken.Register(() =>
        {
            if (_pendingConfigChanges.TryRemove(configResponse.PendingIndex.Value, out var removed))
            {
                removed.TrySetCanceled(cancellationToken);
            }
        }).ConfigureAwait(false);
        return await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Called under the gate after state transitions. If the configuration transition completed
    /// (phase returned to None), completes the pending TCS with success. If leadership was lost,
    /// fails all pending config change requests.
    /// </summary>
    private void CheckPendingConfigurationChanges()
    {
        // Leadership lost — fail all pending requests.
        if (_state.Role != RaftRole.Leader && !_pendingConfigChanges.IsEmpty)
        {
            foreach (var (index, tcs) in _pendingConfigChanges)
            {
                if (_pendingConfigChanges.TryRemove(index, out _))
                {
                    tcs.TrySetResult(new()
                    {
                        SourceNodeId = _state.NodeId,
                        Term = _state.CurrentTerm,
                        Success = false,
                        Committed = false,
                        Reason = "leadership lost before configuration change was committed"
                    });
                }
            }
            return;
        }
        // Configuration transition completed — complete the pending TCS.
        if (_state.ConfigurationTransitionPhase == ConfigurationTransitionPhase.None && !_pendingConfigChanges.IsEmpty)
        {
            foreach (var (index, tcs) in _pendingConfigChanges)
            {
                if (_pendingConfigChanges.TryRemove(index, out _))
                {
                    tcs.TrySetResult(new()
                    {
                        SourceNodeId = _state.NodeId,
                        Term = _state.CurrentTerm,
                        Success = true,
                        Committed = true,
                        Reason = null
                    });
                }
            }
        }
    }

    private async Task RecoverAsync(CancellationToken cancellationToken)
    {
        var (term, votedFor) = await _stateStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        _state.CurrentTerm = term;
        _state.VotedFor = votedFor;
        var entries = await _logStore.GetAllAsync(cancellationToken).ConfigureAwait(false);
        _state.Log.Clear();
        _state.Log.AddRange(entries);
        var (lastIncludedIndex, lastIncludedTerm, data) = await _snapshotStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (data is { Length: > 0 })
        {
            await _stateMachine.RestoreSnapshotAsync(data, cancellationToken).ConfigureAwait(false);
            _state.SnapshotLastIncludedIndex = lastIncludedIndex;
            _state.SnapshotLastIncludedTerm = lastIncludedTerm;
            // 快照覆盖的条目已提交，但快照之后的日志条目不一定已提交
            // 不能在恢复时重放未提交条目，否则违反 State Machine Safety
            // Snapshot boundary is committed; entries beyond snapshot may NOT have been committed before crash.
            // Do NOT replay them — leader will re-commit after election via AppendEntries.
            _state.CommitIndex = lastIncludedIndex;
            _state.LastApplied = lastIncludedIndex;
        }
        else
        {
            // 无快照 — 不重放任何日志条目，因为持久化的条目不一定已提交
            // commitIndex 从 0 开始，Leader 选举后会通过 AppendEntries 推进
            // No snapshot — do NOT replay log entries. Persisted ≠ committed.
            // commitIndex starts at 0; leader will advance it after election.
            _state.CommitIndex = 0;
            _state.LastApplied = 0;
        }
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Raft runtime recovered. nodeId={nodeId}, term={term}, logLastIndex={lastIndex}, commitIndex={commitIndex}",
                _state.NodeId,
                _state.CurrentTerm,
                _state.LastLogIndex,
                _state.CommitIndex);
        }
        _metrics.RecordState(_state);
    }

    private async Task<RaftMessage?> ExecuteActionsAsync(IReadOnlyList<RaftAction> actions, bool captureRpcResponse, string sourceNodeId, CancellationToken cancellationToken, int depth = 0)
    {
        // Phase 1: Execute all persistence actions first (Raft requires persist-before-respond)
        foreach (var action in actions)
        {
            switch (action)
            {
                case PersistStateAction persistState:
                    await _stateStore.SaveAsync(persistState.Term, persistState.VotedFor, cancellationToken).ConfigureAwait(false);
                    break;
                case PersistEntriesAction persistEntries:
                    await _logStore.AppendAsync(persistEntries.Entries, cancellationToken).ConfigureAwait(false);
                    break;
                case TruncateLogSuffixAction truncate:
                    await _logStore.TruncateSuffixAsync(truncate.FromIndexInclusive, cancellationToken).ConfigureAwait(false);
                    break;
                case TakeSnapshotAction snapshot:
                    await _snapshotStore.SaveAsync(snapshot.LastIncludedIndex, snapshot.LastIncludedTerm, snapshot.SnapshotData, cancellationToken).ConfigureAwait(false);
                    await _stateMachine.RestoreSnapshotAsync(snapshot.SnapshotData, cancellationToken).ConfigureAwait(false);
                    await _logStore.CompactPrefixAsync(snapshot.LastIncludedIndex, cancellationToken).ConfigureAwait(false);
                    _state.SnapshotLastIncludedIndex = snapshot.LastIncludedIndex;
                    _state.SnapshotLastIncludedTerm = snapshot.LastIncludedTerm;
                    break;
            }
        }
        // Phase 2: Execute apply, send, timer, and snapshot-to-peer actions
        RaftMessage? rpcResponse = null;
        foreach (var action in actions)
        {
            switch (action)
            {
                case PersistStateAction or PersistEntriesAction or TruncateLogSuffixAction or TakeSnapshotAction:
                    // Already handled in phase 1
                    break;
                case ApplyToStateMachineAction apply:
                    await _stateMachine.ApplyAsync(apply.Entries, cancellationToken).ConfigureAwait(false);
                    _state.LastApplied = Math.Max(_state.LastApplied, apply.Entries[^1].Index);
                    await TryCreateSnapshotAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case SendMessageAction send:
                    var begin = DateTime.UtcNow;
                    if (captureRpcResponse && send.TargetNodeId == sourceNodeId)
                    {
                        rpcResponse ??= send.Message;
                        break;
                    }
                    if (send.Message is AppendEntriesRequest appendRequest)
                    {
                        var lagThreshold = Math.Max(1, _raftOptions.SnapshotThreshold / 2);
                        var isLaggingTooFar = appendRequest.PrevLogIndex <= Math.Max(0, _state.CommitIndex - lagThreshold);
                        var missingOnLeaderLog = appendRequest.PrevLogIndex < _state.SnapshotLastIncludedIndex;
                        if (isLaggingTooFar || missingOnLeaderLog)
                        {
                            var (LastIncludedIndex, LastIncludedTerm, Data) = await _snapshotStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                            if (Data is { Length: > 0 })
                            {
                                var snapshotReply = await _raftTransport.SendAsync(send.TargetNodeId,
                                                        new InstallSnapshotRequest
                                                        {
                                                            SourceNodeId = _state.NodeId,
                                                            Term = _state.CurrentTerm,
                                                            LeaderId = _state.NodeId,
                                                            LastIncludedIndex = LastIncludedIndex,
                                                            LastIncludedTerm = LastIncludedTerm,
                                                            SnapshotData = Data
                                                        },
                                                        cancellationToken).ConfigureAwait(false);
                                _metrics.RecordSnapshotInstallSeconds((DateTime.UtcNow - begin).TotalSeconds);
                                await ProcessReplyAsync(snapshotReply, captureRpcResponse, sourceNodeId, depth, cancellationToken).ConfigureAwait(false);
                                break;
                            }
                        }
                    }
                    var reply = await _raftTransport.SendAsync(send.TargetNodeId, send.Message, cancellationToken).ConfigureAwait(false);
                    if (send.Message is AppendEntriesRequest)
                    {
                        _metrics.RecordAppendLatency((DateTime.UtcNow - begin).TotalMilliseconds);
                    }
                    await ProcessReplyAsync(reply, captureRpcResponse, sourceNodeId, depth, cancellationToken).ConfigureAwait(false);
                    break;
                case ResetElectionTimerAction:
                    _timerControl.ResetElectionTimer();
                    break;
                case ResetHeartbeatTimerAction:
                    _timerControl.ResetHeartbeatTimer();
                    break;
                case SendSnapshotToPeerAction sendSnapshot:
                {
                    var (LastIncludedIndex, LastIncludedTerm, Data) = await _snapshotStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                    if (Data is { Length: > 0 })
                    {
                        var begin2 = DateTime.UtcNow;
                        var snapshotReply = await _raftTransport.SendAsync(sendSnapshot.TargetNodeId,
                                                new InstallSnapshotRequest
                                                {
                                                    SourceNodeId = _state.NodeId,
                                                    Term = _state.CurrentTerm,
                                                    LeaderId = _state.NodeId,
                                                    LastIncludedIndex = LastIncludedIndex,
                                                    LastIncludedTerm = LastIncludedTerm,
                                                    SnapshotData = Data
                                                },
                                                cancellationToken).ConfigureAwait(false);
                        _metrics.RecordSnapshotInstallSeconds((DateTime.UtcNow - begin2).TotalSeconds);
                        await ProcessReplyAsync(snapshotReply, captureRpcResponse, sourceNodeId, depth, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                }
            }
        }
        return rpcResponse;
    }

    private async Task TryCreateSnapshotAsync(CancellationToken cancellationToken)
    {
        var logCount = _state.Log.Count;
        if (logCount < _raftOptions.SnapshotThreshold)
        {
            return;
        }
        var snapshotData = await _stateMachine.CreateSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var lastIncludedIndex = _state.LastApplied;
        var lastEntry = _state.Log.FirstOrDefault(x => x.Index == lastIncludedIndex);
        var lastIncludedTerm = lastEntry?.Term ?? _state.SnapshotLastIncludedTerm;
        await _snapshotStore.SaveAsync(lastIncludedIndex, lastIncludedTerm, snapshotData, cancellationToken).ConfigureAwait(false);
        await _logStore.CompactPrefixAsync(lastIncludedIndex, cancellationToken).ConfigureAwait(false);
        _state.SnapshotLastIncludedIndex = lastIncludedIndex;
        _state.SnapshotLastIncludedTerm = lastIncludedTerm;
        _state.Log.RemoveAll(x => x.Index <= lastIncludedIndex);
    }

    private async Task ProcessReplyAsync(RaftMessage? reply, bool captureRpcResponse, string sourceNodeId, int depth, CancellationToken cancellationToken)
    {
        if (reply is null || depth >= MaxReplyDepth)
        {
            return;
        }
        var replyResult = _node.Handle(_state, reply);
        await ExecuteActionsAsync(replyResult.Actions, captureRpcResponse, sourceNodeId, cancellationToken, depth + 1).ConfigureAwait(false);
    }
}
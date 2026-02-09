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
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var prevRole = _state.Role;
            var prevTerm = _state.CurrentTerm;
            var prevCommit = _state.CommitIndex;
            var result = _node.Handle(_state, request);
            var readResponse = result.Actions
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
            var requiresConfirmation = _state.Role == RaftRole.Leader;
            var readConfirmAcks = requiresConfirmation ? 1 : 0;
            var quorum = _state.Quorum;
            var responseTerm = _state.CurrentTerm;
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
                        break;
                    case SendMessageAction(var targetNodeId, var outbound):
                    {
                        var begin = DateTime.UtcNow;
                        if (outbound is AppendEntriesRequest appendRequest)
                        {
                            var lagThreshold = Math.Max(1, _raftOptions.SnapshotThreshold / 2);
                            var isLaggingTooFar = appendRequest.PrevLogIndex <= Math.Max(0, _state.CommitIndex - lagThreshold);
                            var missingOnLeaderLog = appendRequest.PrevLogIndex < _state.SnapshotLastIncludedIndex;
                            if (isLaggingTooFar || missingOnLeaderLog)
                            {
                                var snapshot = await _snapshotStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                                if (snapshot.Data is { Length: > 0 })
                                {
                                    var snapshotReply = await _raftTransport.SendAsync(targetNodeId,
                                                                                new InstallSnapshotRequest
                                                                                {
                                                                                    SourceNodeId = _state.NodeId,
                                                                                    Term = _state.CurrentTerm,
                                                                                    LeaderId = _state.NodeId,
                                                                                    LastIncludedIndex = snapshot.LastIncludedIndex,
                                                                                    LastIncludedTerm = snapshot.LastIncludedTerm,
                                                                                    SnapshotData = snapshot.Data
                                                                                },
                                                                                cancellationToken)
                                                                            .ConfigureAwait(false);
                                    _metrics.RecordSnapshotInstallSeconds((DateTime.UtcNow - begin).TotalSeconds);
                                    await ProcessReplyAsync(snapshotReply, false, request.SourceNodeId, cancellationToken, 0).ConfigureAwait(false);
                                    break;
                                }
                            }
                        }
                        var reply = await _raftTransport.SendAsync(targetNodeId, outbound, cancellationToken).ConfigureAwait(false);
                        if (outbound is AppendEntriesRequest)
                        {
                            _metrics.RecordAppendLatency((DateTime.UtcNow - begin).TotalMilliseconds);
                        }
                        if (requiresConfirmation &&
                            outbound is AppendEntriesRequest &&
                            reply is AppendEntriesResponse { Success: true } appendReply &&
                            appendReply.Term == responseTerm)
                        {
                            readConfirmAcks++;
                        }
                        await ProcessReplyAsync(reply, false, request.SourceNodeId, cancellationToken, 0).ConfigureAwait(false);
                        break;
                    }
                    case ResetElectionTimerAction:
                        _timerControl.ResetElectionTimer();
                        break;
                    case ResetHeartbeatTimerAction:
                        _timerControl.ResetHeartbeatTimer();
                        break;
                    case SendSnapshotToPeerAction sendSnapshot:
                    {
                        var snapshot = await _snapshotStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                        if (snapshot.Data is { Length: > 0 })
                        {
                            var begin = DateTime.UtcNow;
                            var snapshotReply = await _raftTransport.SendAsync(sendSnapshot.TargetNodeId,
                                                    new InstallSnapshotRequest
                                                    {
                                                        SourceNodeId = _state.NodeId,
                                                        Term = _state.CurrentTerm,
                                                        LeaderId = _state.NodeId,
                                                        LastIncludedIndex = snapshot.LastIncludedIndex,
                                                        LastIncludedTerm = snapshot.LastIncludedTerm,
                                                        SnapshotData = snapshot.Data
                                                    },
                                                    cancellationToken).ConfigureAwait(false);
                            _metrics.RecordSnapshotInstallSeconds((DateTime.UtcNow - begin).TotalSeconds);
                            await ProcessReplyAsync(snapshotReply, false, request.SourceNodeId, cancellationToken, 0).ConfigureAwait(false);
                        }
                        break;
                    }
                }
            }
            _metrics.RecordTransition(prevRole, prevTerm, _state);
            if (_state.CommitIndex > prevCommit)
            {
                _metrics.RecordCommitAdvance();
            }
            _metrics.RecordState(_state);
            var confirmed = requiresConfirmation && readConfirmAcks >= quorum;
            return readResponse with
            {
                Success = readResponse.Success && confirmed,
                LeaderId = confirmed ? _state.NodeId : readResponse.LeaderId,
                ReadIndex = _state.CommitIndex,
                Term = _state.CurrentTerm
            };
        }
        finally
        {
            _gate.Release();
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
            _state.CommitIndex = lastIncludedIndex;
            _state.LastApplied = lastIncludedIndex;
            // Replay log entries beyond the snapshot to the state machine
            var entriesToReplay = _state.Log
                                        .Where(x => x.Index > lastIncludedIndex)
                                        .OrderBy(x => x.Index)
                                        .ToArray();
            if (entriesToReplay.Length > 0)
            {
                await _stateMachine.ApplyAsync(entriesToReplay, cancellationToken).ConfigureAwait(false);
                _state.CommitIndex = Math.Max(_state.CommitIndex, entriesToReplay[^1].Index);
                _state.LastApplied = entriesToReplay[^1].Index;
            }
        }
        else if (_state.Log.Count > 0)
        {
            // No snapshot — replay all log entries
            await _stateMachine.ApplyAsync(_state.Log, cancellationToken).ConfigureAwait(false);
            _state.CommitIndex = _state.Log[^1].Index;
            _state.LastApplied = _state.Log[^1].Index;
        }
        _logger.LogInformation("Raft runtime recovered. nodeId={nodeId}, term={term}, logLastIndex={lastIndex}, commitIndex={commitIndex}",
            _state.NodeId,
            _state.CurrentTerm,
            _state.LastLogIndex,
            _state.CommitIndex);
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
                            var snapshot = await _snapshotStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                            if (snapshot.Data is { Length: > 0 })
                            {
                                var snapshotReply = await _raftTransport.SendAsync(send.TargetNodeId,
                                                        new InstallSnapshotRequest
                                                        {
                                                            SourceNodeId = _state.NodeId,
                                                            Term = _state.CurrentTerm,
                                                            LeaderId = _state.NodeId,
                                                            LastIncludedIndex = snapshot.LastIncludedIndex,
                                                            LastIncludedTerm = snapshot.LastIncludedTerm,
                                                            SnapshotData = snapshot.Data
                                                        },
                                                        cancellationToken).ConfigureAwait(false);
                                _metrics.RecordSnapshotInstallSeconds((DateTime.UtcNow - begin).TotalSeconds);
                                await ProcessReplyAsync(snapshotReply, captureRpcResponse, sourceNodeId, cancellationToken, depth).ConfigureAwait(false);
                                break;
                            }
                        }
                    }
                    var reply = await _raftTransport.SendAsync(send.TargetNodeId, send.Message, cancellationToken).ConfigureAwait(false);
                    if (send.Message is AppendEntriesRequest)
                    {
                        _metrics.RecordAppendLatency((DateTime.UtcNow - begin).TotalMilliseconds);
                    }
                    await ProcessReplyAsync(reply, captureRpcResponse, sourceNodeId, cancellationToken, depth).ConfigureAwait(false);
                    break;
                case ResetElectionTimerAction:
                    _timerControl.ResetElectionTimer();
                    break;
                case ResetHeartbeatTimerAction:
                    _timerControl.ResetHeartbeatTimer();
                    break;
                case SendSnapshotToPeerAction sendSnapshot:
                {
                    var snapshot = await _snapshotStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                    if (snapshot.Data is { Length: > 0 })
                    {
                        var begin2 = DateTime.UtcNow;
                        var snapshotReply = await _raftTransport.SendAsync(sendSnapshot.TargetNodeId,
                                                new InstallSnapshotRequest
                                                {
                                                    SourceNodeId = _state.NodeId,
                                                    Term = _state.CurrentTerm,
                                                    LeaderId = _state.NodeId,
                                                    LastIncludedIndex = snapshot.LastIncludedIndex,
                                                    LastIncludedTerm = snapshot.LastIncludedTerm,
                                                    SnapshotData = snapshot.Data
                                                },
                                                cancellationToken).ConfigureAwait(false);
                        _metrics.RecordSnapshotInstallSeconds((DateTime.UtcNow - begin2).TotalSeconds);
                        await ProcessReplyAsync(snapshotReply, captureRpcResponse, sourceNodeId, cancellationToken, depth).ConfigureAwait(false);
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

    private async Task ProcessReplyAsync(RaftMessage? reply, bool captureRpcResponse, string sourceNodeId, CancellationToken cancellationToken, int depth)
    {
        if (reply is null || depth >= MaxReplyDepth)
        {
            return;
        }
        var replyResult = _node.Handle(_state, reply);
        await ExecuteActionsAsync(replyResult.Actions, captureRpcResponse, sourceNodeId, cancellationToken, depth + 1).ConfigureAwait(false);
    }
}
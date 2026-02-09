using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Core.Actions;
using EasilyNET.Raft.Core.Engine;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;
using EasilyNET.Raft.Core.Options;
using EasilyNET.Raft.AspNetCore.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasilyNET.Raft.AspNetCore.Runtime;

/// <summary>
///     <para xml:lang="en">Single-threaded raft runtime executor</para>
///     <para xml:lang="zh">单线程 Raft 运行时执行器</para>
/// </summary>
public sealed class RaftRuntime : IRaftRuntime
{
    private readonly ILogStore _logStore;
    private readonly ILogger<RaftRuntime> _logger;
    private readonly RaftNode _node;
    private readonly IRaftTransport _raftTransport;
    private readonly ISnapshotStore _snapshotStore;
    private readonly IStateMachine _stateMachine;
    private readonly IStateStore _stateStore;
    private readonly RaftOptions _raftOptions;
    private readonly RaftMetrics _metrics;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _initGate = new(1, 1);

    private readonly RaftNodeState _state;
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
        RaftMetrics metrics,
        ILogger<RaftRuntime> logger)
    {
        _stateStore = stateStore;
        _logStore = logStore;
        _snapshotStore = snapshotStore;
        _stateMachine = stateMachine;
        _raftTransport = raftTransport;
        _metrics = metrics;
        _logger = logger;
        _raftOptions = options.Value;
        _node = new RaftNode(_raftOptions);
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
            return (readIndexResponse as TResponse) ?? throw new InvalidOperationException($"Expected RPC response type '{typeof(TResponse).Name}' but got '{readIndexResponse.GetType().Name}'.");
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
                                     .FirstOrDefault()
                             ?? new ReadIndexResponse
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

                    case ApplyToStateMachineAction apply:
                        await _stateMachine.ApplyAsync(apply.Entries, cancellationToken).ConfigureAwait(false);
                        break;

                    case TakeSnapshotAction snapshot:
                        await _snapshotStore.SaveAsync(snapshot.LastIncludedIndex, snapshot.LastIncludedTerm, snapshot.SnapshotData, cancellationToken).ConfigureAwait(false);
                        await _logStore.TruncateSuffixAsync(snapshot.LastIncludedIndex + 1, cancellationToken).ConfigureAwait(false);
                        _state.SnapshotLastIncludedIndex = snapshot.LastIncludedIndex;
                        _state.SnapshotLastIncludedTerm = snapshot.LastIncludedTerm;
                        break;

                    case SendMessageAction send when send.TargetNodeId == request.SourceNodeId:
                        break;

                    case SendMessageAction send:
                    {
                        var begin = DateTime.UtcNow;
                        var outbound = send.Message;

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
                                    _ = await _raftTransport.SendAsync(send.TargetNodeId,
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
                                    break;
                                }
                            }
                        }

                        var reply = await _raftTransport.SendAsync(send.TargetNodeId, outbound, cancellationToken).ConfigureAwait(false);

                        if (outbound is AppendEntriesRequest)
                        {
                            _metrics.RecordAppendLatency((DateTime.UtcNow - begin).TotalMilliseconds);
                        }

                        if (requiresConfirmation &&
                            outbound is AppendEntriesRequest { Entries: { Count: 0 } } &&
                            reply is AppendEntriesResponse appendReply &&
                            appendReply.Success &&
                            appendReply.Term == responseTerm)
                        {
                            readConfirmAcks++;
                        }

                        break;
                    }

                    case ResetElectionTimerAction:
                    case ResetHeartbeatTimerAction:
                        break;
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
            _state.CommitIndex = Math.Max(_state.CommitIndex, lastIncludedIndex);
            _state.LastApplied = Math.Max(_state.LastApplied, lastIncludedIndex);
            _state.SnapshotLastIncludedIndex = lastIncludedIndex;
            _state.SnapshotLastIncludedTerm = lastIncludedTerm;
        }

            _logger.LogInformation(
                "Raft runtime recovered. nodeId={nodeId}, term={term}, logLastIndex={lastIndex}, commitIndex={commitIndex}",
                _state.NodeId,
                _state.CurrentTerm,
                _state.LastLogIndex,
                _state.CommitIndex);
            _metrics.RecordState(_state);
    }

    private async Task<RaftMessage?> ExecuteActionsAsync(IReadOnlyList<RaftAction> actions, bool captureRpcResponse, string sourceNodeId, CancellationToken cancellationToken)
    {
        RaftMessage? rpcResponse = null;
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

                case ApplyToStateMachineAction apply:
                    await _stateMachine.ApplyAsync(apply.Entries, cancellationToken).ConfigureAwait(false);
                    break;

                case TakeSnapshotAction snapshot:
                    await _snapshotStore.SaveAsync(snapshot.LastIncludedIndex, snapshot.LastIncludedTerm, snapshot.SnapshotData, cancellationToken).ConfigureAwait(false);
                    await _logStore.TruncateSuffixAsync(snapshot.LastIncludedIndex + 1, cancellationToken).ConfigureAwait(false);
                    _state.SnapshotLastIncludedIndex = snapshot.LastIncludedIndex;
                    _state.SnapshotLastIncludedTerm = snapshot.LastIncludedTerm;
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
                                _ = await _raftTransport.SendAsync(send.TargetNodeId,
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
                                break;
                            }
                        }
                    }

                    _ = await _raftTransport.SendAsync(send.TargetNodeId, send.Message, cancellationToken).ConfigureAwait(false);
                    if (send.Message is AppendEntriesRequest)
                    {
                        _metrics.RecordAppendLatency((DateTime.UtcNow - begin).TotalMilliseconds);
                    }
                    break;

                case ResetElectionTimerAction:
                case ResetHeartbeatTimerAction:
                    break;
            }
        }

        return rpcResponse;
    }
}

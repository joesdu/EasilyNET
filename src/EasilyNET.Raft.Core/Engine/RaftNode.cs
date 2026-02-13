using System.Collections.ObjectModel;
using System.Text;
using EasilyNET.Raft.Core.Actions;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;
using EasilyNET.Raft.Core.Options;

namespace EasilyNET.Raft.Core.Engine;

/// <summary>
///     <para xml:lang="en">Pure raft state machine engine</para>
///     <para xml:lang="zh">纯 Raft 状态机引擎</para>
/// </summary>
public sealed class RaftNode(RaftOptions options)
{
    /// <summary>
    ///     <para xml:lang="en">Handles a raft message and returns state transition result</para>
    ///     <para xml:lang="zh">处理 Raft 消息并返回状态转移结果</para>
    /// </summary>
    public RaftResult Handle(RaftNodeState state, RaftMessage message)
    {
        var actions = new List<RaftAction>();
        // PreVote 请求携带 currentTerm+1 但不应导致接收方 StepDown 和 term 膨胀，否则违背 PreVote 的设计初衷
        // PreVote requests carry term+1 but must NOT cause receiver to step down — that defeats PreVote's purpose
        if (message is not RequestVoteRequest { IsPreVote: true } && message.Term > state.CurrentTerm)
        {
            StepDown(state, message.Term, null, actions);
        }
        switch (message)
        {
            case ElectionTimeoutElapsed:
                HandleElectionTimeout(state, actions);
                break;
            case HeartbeatTimeoutElapsed:
                HandleHeartbeatTimeout(state, actions);
                break;
            case ClientCommandRequest request:
                HandleClientCommand(state, request, actions);
                break;
            case ReadIndexRequest request:
                HandleReadIndexRequest(state, request, actions);
                break;
            case ConfigurationChangeRequest request:
                HandleConfigurationChangeRequest(state, request, actions);
                break;
            case RequestVoteRequest request:
                HandleVoteRequest(state, request, actions);
                break;
            case RequestVoteResponse response:
                HandleVoteResponse(state, response, actions);
                break;
            case AppendEntriesRequest request:
                HandleAppendEntriesRequest(state, request, actions);
                break;
            case AppendEntriesResponse response:
                HandleAppendEntriesResponse(state, response, actions);
                break;
            case InstallSnapshotRequest request:
                HandleInstallSnapshotRequest(state, request, actions);
                break;
        }
        return new(state, actions);
    }

    private void HandleElectionTimeout(RaftNodeState state, List<RaftAction> actions)
    {
        if (state.Role == RaftRole.Leader)
        {
            return;
        }
        state.LeaderId = null;
        state.PreVotesGranted.Clear();
        state.VotesGranted.Clear();
        if (options.EnablePreVote)
        {
            state.PreVotesGranted.Add(state.NodeId);
            BroadcastVoteRequests(state, state.CurrentTerm + 1, true, actions);
            actions.Add(new ResetElectionTimerAction());
            return;
        }
        StartElection(state, actions);
    }

    private void HandleHeartbeatTimeout(RaftNodeState state, List<RaftAction> actions)
    {
        if (state.Role != RaftRole.Leader)
        {
            return;
        }
        BroadcastAppendEntries(state, actions);
        actions.Add(new ResetHeartbeatTimerAction());
    }

    private void HandleClientCommand(RaftNodeState state, ClientCommandRequest request, List<RaftAction> actions)
    {
        if (state.Role != RaftRole.Leader)
        {
            return;
        }
        var entry = new RaftLogEntry(state.LastLogIndex + 1, state.CurrentTerm, request.Command);
        state.Log.Add(entry);
        actions.Add(new PersistEntriesAction([entry]));
        state.MatchIndex[state.NodeId] = entry.Index;
        BroadcastAppendEntries(state, actions);
    }

    private void HandleReadIndexRequest(RaftNodeState state, ReadIndexRequest request, List<RaftAction> actions)
    {
        if (state.Role != RaftRole.Leader)
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new ReadIndexResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    ReadIndex = state.CommitIndex,
                    LeaderId = state.LeaderId
                }));
            return;
        }
        var quorumConfirmed = HasCommitQuorum(state, state.CommitIndex);
        BroadcastAppendEntries(state, actions);
        actions.Add(new SendMessageAction(request.SourceNodeId,
            new ReadIndexResponse
            {
                SourceNodeId = state.NodeId,
                Term = state.CurrentTerm,
                Success = quorumConfirmed,
                ReadIndex = state.CommitIndex,
                LeaderId = state.NodeId
            }));
    }

    private void HandleConfigurationChangeRequest(RaftNodeState state, ConfigurationChangeRequest request, List<RaftAction> actions)
    {
        if (state.Role != RaftRole.Leader)
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new ConfigurationChangeResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    Reason = "not leader"
                }));
            return;
        }
        if (string.IsNullOrWhiteSpace(request.TargetNodeId))
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new ConfigurationChangeResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    Reason = "target node id is required"
                }));
            return;
        }
        if (state.PendingConfigurationChangeIndex.HasValue || state.ConfigurationTransitionPhase != ConfigurationTransitionPhase.None)
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new ConfigurationChangeResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    Reason = "configuration change in progress"
                }));
            return;
        }
        if (request.ChangeType == ConfigurationChangeType.Add && state.ClusterMembers.Contains(request.TargetNodeId))
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new ConfigurationChangeResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    Reason = "node already exists"
                }));
            return;
        }
        if (request.ChangeType == ConfigurationChangeType.Remove && !state.ClusterMembers.Contains(request.TargetNodeId))
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new ConfigurationChangeResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    Reason = "node not found"
                }));
            return;
        }
        if (request.ChangeType == ConfigurationChangeType.Remove && state.ClusterMembers.Count <= 3)
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new ConfigurationChangeResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    Reason = "cluster size cannot drop below 3"
                }));
            return;
        }
        if (request.ChangeType == ConfigurationChangeType.Remove && request.TargetNodeId == state.NodeId)
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new ConfigurationChangeResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    Reason = "leader cannot remove itself directly"
                }));
            return;
        }
        var oldMembers = state.ClusterMembers.Distinct().ToList();
        var newMembers = oldMembers.ToList();
        if (request.ChangeType == ConfigurationChangeType.Add)
        {
            newMembers.Add(request.TargetNodeId);
        }
        else
        {
            newMembers.Remove(request.TargetNodeId);
        }
        var commandText = $"cfg:joint:{request.ChangeType}:{request.TargetNodeId}";
        var command = Encoding.UTF8.GetBytes(commandText);
        var entry = new RaftLogEntry(state.LastLogIndex + 1, state.CurrentTerm, command);
        state.Log.Add(entry);
        state.MatchIndex[state.NodeId] = entry.Index;
        state.ConfigurationTransitionPhase = ConfigurationTransitionPhase.Joint;
        state.OldConfigurationMembers.Clear();
        state.OldConfigurationMembers.AddRange(oldMembers);
        state.NewConfigurationMembers.Clear();
        state.NewConfigurationMembers.AddRange(newMembers);
        state.JointConfigurationIndex = entry.Index;
        state.FinalConfigurationIndex = null;
        state.PendingConfigurationChangeIndex = entry.Index;
        state.PendingConfigurationChangeType = request.ChangeType;
        state.PendingConfigurationChangeNodeId = request.TargetNodeId;
        foreach (var member in state.OldConfigurationMembers.Concat(state.NewConfigurationMembers).Distinct())
        {
            if (!state.NextIndex.ContainsKey(member))
            {
                state.NextIndex[member] = state.LastLogIndex + 1;
            }
            if (!state.MatchIndex.ContainsKey(member))
            {
                state.MatchIndex[member] = member == state.NodeId ? state.LastLogIndex : 0;
            }
        }
        actions.Add(new PersistEntriesAction([entry]));
        BroadcastAppendEntries(state, actions);
        actions.Add(new SendMessageAction(request.SourceNodeId,
            new ConfigurationChangeResponse
            {
                SourceNodeId = state.NodeId,
                Term = state.CurrentTerm,
                Success = true,
                Committed = false,
                PendingIndex = entry.Index,
                Reason = null
            }));
    }

    private static void HandleVoteRequest(RaftNodeState state, RequestVoteRequest request, List<RaftAction> actions)
    {
        var responseTerm = state.CurrentTerm;
        var voteGranted = false;
        if (!request.IsPreVote)
        {
            if (request.Term < state.CurrentTerm)
            {
                voteGranted = false;
            }
            else
            {
                var notVotedOrSameCandidate = state.VotedFor is null || state.VotedFor == request.CandidateId;
                if (notVotedOrSameCandidate && IsCandidateUpToDate(state, request.LastLogTerm, request.LastLogIndex))
                {
                    state.VotedFor = request.CandidateId;
                    voteGranted = true;
                    actions.Add(new PersistStateAction(state.CurrentTerm, state.VotedFor));
                    actions.Add(new ResetElectionTimerAction());
                }
            }
        }
        else
        {
            var expectedTerm = state.CurrentTerm + 1;
            if (request.Term == expectedTerm && IsCandidateUpToDate(state, request.LastLogTerm, request.LastLogIndex))
            {
                voteGranted = true;
            }
        }
        actions.Add(new SendMessageAction(request.SourceNodeId,
            new RequestVoteResponse
            {
                SourceNodeId = state.NodeId,
                Term = responseTerm,
                VoteGranted = voteGranted,
                IsPreVote = request.IsPreVote
            }));
    }

    private void HandleVoteResponse(RaftNodeState state, RequestVoteResponse response, List<RaftAction> actions)
    {
        if (response.Term < state.CurrentTerm || !response.VoteGranted)
        {
            return;
        }
        if (response.IsPreVote)
        {
            if (state.Role != RaftRole.Follower)
            {
                return;
            }
            state.PreVotesGranted.Add(response.SourceNodeId);
            if (HasVoteQuorum(state, state.PreVotesGranted))
            {
                StartElection(state, actions);
            }
            return;
        }
        if (state.Role != RaftRole.Candidate)
        {
            return;
        }
        state.VotesGranted.Add(response.SourceNodeId);
        if (HasVoteQuorum(state, state.VotesGranted))
        {
            BecomeLeader(state, actions);
        }
    }

    private static void HandleAppendEntriesRequest(RaftNodeState state, AppendEntriesRequest request, List<RaftAction> actions)
    {
        if (request.Term < state.CurrentTerm)
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new AppendEntriesResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    MatchIndex = state.LastLogIndex
                }));
            return;
        }
        state.Role = RaftRole.Follower;
        state.LeaderId = request.LeaderId;
        state.VotesGranted.Clear();
        state.PreVotesGranted.Clear();
        actions.Add(new ResetElectionTimerAction());
        if (!MatchPrevLog(state, request.PrevLogIndex, request.PrevLogTerm, out var conflictTerm, out var conflictIndex))
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new AppendEntriesResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    MatchIndex = Math.Min(state.LastLogIndex, request.PrevLogIndex),
                    ConflictTerm = conflictTerm,
                    ConflictIndex = conflictIndex
                }));
            return;
        }
        var persisted = new List<RaftLogEntry>();
        long? truncateFrom = null;
        foreach (var incoming in request.Entries)
        {
            var existing = FindLogEntry(state.Log, incoming.Index);
            if (existing is not null && existing.Term != incoming.Term)
            {
                truncateFrom ??= incoming.Index;
                var removePos = FindLogPosition(state.Log, incoming.Index);
                if (removePos >= 0)
                {
                    state.Log.RemoveRange(removePos, state.Log.Count - removePos);
                }
                state.Log.Add(incoming);
                persisted.Add(incoming);
                continue;
            }
            if (existing is not null)
            {
                continue;
            }
            state.Log.Add(incoming);
            persisted.Add(incoming);
        }
        if (truncateFrom.HasValue)
        {
            actions.Add(new TruncateLogSuffixAction(truncateFrom.Value));
        }
        if (persisted.Count > 0)
        {
            actions.Add(new PersistEntriesAction(persisted));
        }
        if (request.LeaderCommit > state.CommitIndex)
        {
            var prevCommit = state.CommitIndex;
            state.CommitIndex = Math.Min(request.LeaderCommit, state.LastLogIndex);
            EnqueueApplyActions(state, prevCommit, actions);
        }
        actions.Add(new SendMessageAction(request.SourceNodeId,
            new AppendEntriesResponse
            {
                SourceNodeId = state.NodeId,
                Term = state.CurrentTerm,
                Success = true,
                MatchIndex = state.LastLogIndex
            }));
    }

    private void HandleAppendEntriesResponse(RaftNodeState state, AppendEntriesResponse response, List<RaftAction> actions)
    {
        if (state.Role != RaftRole.Leader || response.Term < state.CurrentTerm)
        {
            return;
        }
        if (response.Success)
        {
            state.MatchIndex[response.SourceNodeId] = response.MatchIndex;
            state.NextIndex[response.SourceNodeId] = response.MatchIndex + 1;
            TryAdvanceCommit(state, actions);
            return;
        }
        var currentNext = state.NextIndex.GetValueOrDefault(response.SourceNodeId, state.LastLogIndex + 1);
        var fallback = Math.Max(1, currentNext - 1);
        if (response is { ConflictTerm: not null, ConflictIndex: not null })
        {
            // 在 Leader 日志中反向查找匹配冲突 term 的最后一条日志
            // Reverse search leader's log for last entry matching conflict term
            long? indexWithTerm = null;
            for (var i = state.Log.Count - 1; i >= 0; i--)
            {
                if (state.Log[i].Term != response.ConflictTerm.Value)
                {
                    continue;
                }
                indexWithTerm = state.Log[i].Index;
                break;
            }
            // 找到匹配 term 的最后一条日志时，nextIndex 应设为该索引 +1（即该 term 之后的第一条）
            // When matching term found, nextIndex should be the NEXT index after the last entry with that term
            fallback = indexWithTerm.HasValue ? indexWithTerm.Value + 1 : response.ConflictIndex.Value;
        }
        state.NextIndex[response.SourceNodeId] = fallback;
        SendAppendEntriesTo(state, response.SourceNodeId, actions);
    }

    private static void HandleInstallSnapshotRequest(RaftNodeState state, InstallSnapshotRequest request, List<RaftAction> actions)
    {
        if (request.Term < state.CurrentTerm)
        {
            actions.Add(new SendMessageAction(request.SourceNodeId,
                new InstallSnapshotResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false
                }));
            return;
        }
        state.Role = RaftRole.Follower;
        state.LeaderId = request.LeaderId;
        actions.Add(new ResetElectionTimerAction());
        actions.Add(new TakeSnapshotAction(request.LastIncludedIndex, request.LastIncludedTerm, request.SnapshotData));
        var cutPos = FindLogPosition(state.Log, request.LastIncludedIndex);
        if (cutPos >= 0)
        {
            state.Log.RemoveRange(0, cutPos + 1);
        }
        else
        {
            var insertionPoint = ~cutPos;
            if (insertionPoint > 0)
            {
                state.Log.RemoveRange(0, insertionPoint);
            }
        }
        state.SnapshotLastIncludedIndex = request.LastIncludedIndex;
        state.SnapshotLastIncludedTerm = request.LastIncludedTerm;
        state.CommitIndex = Math.Max(state.CommitIndex, request.LastIncludedIndex);
        state.LastApplied = Math.Max(state.LastApplied, request.LastIncludedIndex);
        actions.Add(new SendMessageAction(request.SourceNodeId,
            new InstallSnapshotResponse
            {
                SourceNodeId = state.NodeId,
                Term = state.CurrentTerm,
                Success = true
            }));
    }

    private static void StartElection(RaftNodeState state, List<RaftAction> actions)
    {
        state.Role = RaftRole.Candidate;
        state.CurrentTerm += 1;
        state.VotedFor = state.NodeId;
        state.VotesGranted.Clear();
        state.VotesGranted.Add(state.NodeId);
        actions.Add(new PersistStateAction(state.CurrentTerm, state.VotedFor));
        actions.Add(new ResetElectionTimerAction());
        BroadcastVoteRequests(state, state.CurrentTerm, false, actions);
    }

    private static void BroadcastVoteRequests(RaftNodeState state, long term, bool isPreVote, List<RaftAction> actions)
    {
        actions.AddRange(state.ClusterMembers.Where(x => x != state.NodeId)
                              .Select(peer => new SendMessageAction(peer, new RequestVoteRequest
                              {
                                  SourceNodeId = state.NodeId,
                                  Term = term,
                                  CandidateId = state.NodeId,
                                  LastLogIndex = state.LastLogIndex,
                                  LastLogTerm = state.LastLogTerm,
                                  IsPreVote = isPreVote
                              })));
    }

    private void BecomeLeader(RaftNodeState state, List<RaftAction> actions)
    {
        state.Role = RaftRole.Leader;
        state.LeaderId = state.NodeId;
        state.PreVotesGranted.Clear();
        state.VotesGranted.Clear();
        foreach (var peer in state.ClusterMembers)
        {
            state.NextIndex[peer] = state.LastLogIndex + 1;
            state.MatchIndex[peer] = peer == state.NodeId ? state.LastLogIndex : 0;
        }
        // Raft §5.4.2: 新 Leader 必须追加一条当前任期的 no-op 条目，以间接提交前任期的未提交日志
        // New leader must append a no-op entry in its current term to commit entries from previous terms
        var noopEntry = new RaftLogEntry(state.LastLogIndex + 1, state.CurrentTerm, []);
        state.Log.Add(noopEntry);
        state.MatchIndex[state.NodeId] = noopEntry.Index;
        actions.Add(new PersistEntriesAction([noopEntry]));
        BroadcastAppendEntries(state, actions);
        actions.Add(new ResetHeartbeatTimerAction());
    }

    private void BroadcastAppendEntries(RaftNodeState state, List<RaftAction> actions)
    {
        foreach (var peer in GetReplicationMembers(state).Where(x => x != state.NodeId))
        {
            SendAppendEntriesTo(state, peer, actions);
        }
    }

    private void SendAppendEntriesTo(RaftNodeState state, string peer, List<RaftAction> actions)
    {
        var nextIndex = state.NextIndex.GetValueOrDefault(peer, state.LastLogIndex + 1);
        var prevIndex = Math.Max(0, nextIndex - 1);
        long prevTerm;
        if (prevIndex == 0)
        {
            prevTerm = 0;
        }
        else
        {
            var prevEntry = FindLogEntry(state.Log, prevIndex);
            if (prevEntry is not null)
            {
                prevTerm = prevEntry.Term;
            }
            else if (prevIndex == state.SnapshotLastIncludedIndex)
            {
                prevTerm = state.SnapshotLastIncludedTerm;
            }
            else
            {
                // prevIndex falls into compacted region — follower needs snapshot install
                actions.Add(new SendSnapshotToPeerAction(peer));
                return;
            }
        }
        var startPos = FindLogPosition(state.Log, nextIndex);
        if (startPos < 0)
        {
            startPos = ~startPos;
        }
        var entries = state.Log
                           .Skip(startPos)
                           .Take(options.MaxEntriesPerAppend)
                           .ToArray();
        actions.Add(new SendMessageAction(peer,
            new AppendEntriesRequest
            {
                SourceNodeId = state.NodeId,
                Term = state.CurrentTerm,
                LeaderId = state.NodeId,
                PrevLogIndex = prevIndex,
                PrevLogTerm = prevTerm,
                Entries = entries,
                LeaderCommit = state.CommitIndex
            }));
    }

    private void TryAdvanceCommit(RaftNodeState state, List<RaftAction> actions)
    {
        for (var candidateIndex = state.LastLogIndex; candidateIndex > state.CommitIndex; candidateIndex--)
        {
            var entry = FindLogEntry(state.Log, candidateIndex);
            if (entry is null || entry.Term != state.CurrentTerm)
            {
                continue;
            }
            if (!HasCommitQuorum(state, candidateIndex))
            {
                continue;
            }
            var previous = state.CommitIndex;
            state.CommitIndex = candidateIndex;
            EnqueueApplyActions(state, previous, actions);
            AdvanceConfigurationTransitionAfterCommit(state, actions);
            break;
        }
    }

    private void AdvanceConfigurationTransitionAfterCommit(RaftNodeState state, List<RaftAction> actions)
    {
        if (state is { ConfigurationTransitionPhase: ConfigurationTransitionPhase.Joint, JointConfigurationIndex: not null } &&
            state.CommitIndex >= state.JointConfigurationIndex.Value &&
            !state.FinalConfigurationIndex.HasValue)
        {
            var commandText = $"cfg:final:{state.PendingConfigurationChangeType}:{state.PendingConfigurationChangeNodeId}";
            var command = Encoding.UTF8.GetBytes(commandText);
            var finalEntry = new RaftLogEntry(state.LastLogIndex + 1, state.CurrentTerm, command);
            state.Log.Add(finalEntry);
            state.MatchIndex[state.NodeId] = finalEntry.Index;
            state.FinalConfigurationIndex = finalEntry.Index;
            state.ConfigurationTransitionPhase = ConfigurationTransitionPhase.Finalizing;
            actions.Add(new PersistEntriesAction([finalEntry]));
            BroadcastAppendEntries(state, actions);
            return;
        }
        if (state.ConfigurationTransitionPhase != ConfigurationTransitionPhase.Finalizing ||
            !state.FinalConfigurationIndex.HasValue ||
            state.CommitIndex < state.FinalConfigurationIndex.Value)
        {
            return;
        }
        var finalMembers = state.NewConfigurationMembers.Distinct().ToList();
        state.ClusterMembers.Clear();
        state.ClusterMembers.AddRange(finalMembers);
        foreach (var member in finalMembers)
        {
            if (!state.NextIndex.ContainsKey(member))
            {
                state.NextIndex[member] = state.LastLogIndex + 1;
            }
            if (!state.MatchIndex.ContainsKey(member))
            {
                state.MatchIndex[member] = member == state.NodeId ? state.LastLogIndex : 0;
            }
        }
        var removedMembers = state.NextIndex.Keys.Where(x => !finalMembers.Contains(x)).ToArray();
        foreach (var member in removedMembers)
        {
            state.NextIndex.Remove(member);
            state.MatchIndex.Remove(member);
        }
        state.PendingConfigurationChangeIndex = null;
        state.PendingConfigurationChangeType = null;
        state.PendingConfigurationChangeNodeId = null;
        state.JointConfigurationIndex = null;
        state.FinalConfigurationIndex = null;
        state.ConfigurationTransitionPhase = ConfigurationTransitionPhase.None;
        state.OldConfigurationMembers.Clear();
        state.NewConfigurationMembers.Clear();
    }

    private static bool HasCommitQuorum(RaftNodeState state, long candidateIndex)
    {
        if (state.ConfigurationTransitionPhase == ConfigurationTransitionPhase.None)
        {
            var replicatedCount = state.MatchIndex.Values.Count(x => x >= candidateIndex);
            return replicatedCount >= state.Quorum;
        }
        var oldMajority = HasMajority(state, state.OldConfigurationMembers.AsReadOnly(), candidateIndex);
        var newMajority = HasMajority(state, state.NewConfigurationMembers.AsReadOnly(), candidateIndex);
        return oldMajority && newMajority;
    }

    private static bool HasVoteQuorum(RaftNodeState state, HashSet<string> votes)
    {
        if (state.ConfigurationTransitionPhase == ConfigurationTransitionPhase.None)
        {
            return votes.Count >= state.Quorum;
        }
        var oldThreshold = (state.OldConfigurationMembers.Count / 2) + 1;
        var newThreshold = (state.NewConfigurationMembers.Count / 2) + 1;
        var oldVotes = state.OldConfigurationMembers.Count(votes.Contains);
        var newVotes = state.NewConfigurationMembers.Count(votes.Contains);
        return oldVotes >= oldThreshold && newVotes >= newThreshold;
    }

    private static bool HasMajority(RaftNodeState state, ReadOnlyCollection<string> members, long candidateIndex)
    {
        if (members.Count == 0)
        {
            return false;
        }
        var threshold = (members.Count / 2) + 1;
        var replicated = members.Count(member => state.MatchIndex.GetValueOrDefault(member, -1) >= candidateIndex);
        return replicated >= threshold;
    }

    private static IEnumerable<string> GetReplicationMembers(RaftNodeState state) =>
        state.ConfigurationTransitionPhase == ConfigurationTransitionPhase.None
            ? state.ClusterMembers
            : state.OldConfigurationMembers.Concat(state.NewConfigurationMembers).Distinct();

    private static bool IsCandidateUpToDate(RaftNodeState state, long candidateLastLogTerm, long candidateLastLogIndex) =>
        candidateLastLogTerm != state.LastLogTerm
            ? candidateLastLogTerm > state.LastLogTerm
            : candidateLastLogIndex >= state.LastLogIndex;

    private static void StepDown(RaftNodeState state, long newTerm, string? leaderId, List<RaftAction> actions)
    {
        state.Role = RaftRole.Follower;
        state.CurrentTerm = newTerm;
        state.VotedFor = null;
        state.LeaderId = leaderId;
        state.VotesGranted.Clear();
        state.PreVotesGranted.Clear();
        actions.Add(new PersistStateAction(state.CurrentTerm, state.VotedFor));
        actions.Add(new ResetElectionTimerAction());
    }

    private static void EnqueueApplyActions(RaftNodeState state, long fromExclusive, List<RaftAction> actions)
    {
        if (state.CommitIndex <= fromExclusive)
        {
            return;
        }
        var startPos = FindLogPosition(state.Log, fromExclusive + 1);
        if (startPos < 0)
        {
            startPos = ~startPos;
        }
        var entries = new List<RaftLogEntry>();
        for (var i = startPos; i < state.Log.Count && state.Log[i].Index <= state.CommitIndex; i++)
        {
            entries.Add(state.Log[i]);
        }
        if (entries.Count == 0)
        {
            return;
        }
        actions.Add(new ApplyToStateMachineAction(entries));
    }

    private static bool MatchPrevLog(RaftNodeState state, long prevLogIndex, long prevLogTerm, out long? conflictTerm, out long? conflictIndex)
    {
        conflictTerm = null;
        conflictIndex = null;
        if (prevLogIndex == 0)
        {
            return true;
        }
        // Check if prevLogIndex falls at the snapshot boundary
        if (prevLogIndex == state.SnapshotLastIncludedIndex && state.SnapshotLastIncludedIndex > 0)
        {
            return state.SnapshotLastIncludedTerm == prevLogTerm;
        }
        // Check if prevLogIndex falls before the snapshot boundary (already compacted)
        if (prevLogIndex < state.SnapshotLastIncludedIndex && state.SnapshotLastIncludedIndex > 0)
        {
            // Entry is compacted — we can't verify term, force leader to send snapshot
            conflictIndex = state.SnapshotLastIncludedIndex + 1;
            return false;
        }
        var entry = FindLogEntry(state.Log, prevLogIndex);
        if (entry is null)
        {
            conflictIndex = state.LastLogIndex + 1;
            return false;
        }
        if (entry.Term == prevLogTerm)
        {
            return true;
        }
        conflictTerm = entry.Term;
        // 查找冲突 term 的第一条日志索引（用于快速回退）
        // Find first index of conflict term for fast rollback
        var pos = FindLogPosition(state.Log, entry.Index);
        if (pos >= 0)
        {
            while (pos > 0 && state.Log[pos - 1].Term == entry.Term)
            {
                pos--;
            }
            conflictIndex = state.Log[pos].Index;
        }
        else
        {
            conflictIndex = entry.Index;
        }
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Binary search for log entry by index. O(log n) instead of O(n).</para>
    ///     <para xml:lang="zh">按索引二分查找日志条目，O(log n) 替代 O(n)。</para>
    /// </summary>
    private static RaftLogEntry? FindLogEntry(List<RaftLogEntry> log, long index)
    {
        var pos = FindLogPosition(log, index);
        return pos >= 0 ? log[pos] : null;
    }

    /// <summary>
    ///     <para xml:lang="en">Binary search returning position in log list. Negative = bitwise complement of insertion point.</para>
    ///     <para xml:lang="zh">二分查找返回日志列表中的位置。负数 = 插入点的按位取反。</para>
    /// </summary>
    private static int FindLogPosition(List<RaftLogEntry> log, long index)
    {
        var lo = 0;
        var hi = log.Count - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var cmp = log[mid].Index.CompareTo(index);
            switch (cmp)
            {
                case 0:
                    return mid;
                case < 0:
                    lo = mid + 1;
                    break;
                default:
                    hi = mid - 1;
                    break;
            }
        }
        return ~lo;
    }
}
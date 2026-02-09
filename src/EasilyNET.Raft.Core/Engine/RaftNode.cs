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

        if (message.Term > state.CurrentTerm)
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
            BroadcastVoteRequests(state, state.CurrentTerm + 1, isPreVote: true, actions);
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
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
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

        var quorumConfirmed = state.MatchIndex.Values.Count(x => x >= state.CommitIndex) >= state.Quorum;
        BroadcastAppendEntries(state, actions);
        actions.Add(new SendMessageAction(
            request.SourceNodeId,
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
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
                new ConfigurationChangeResponse
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    Success = false,
                    Reason = "not leader"
                }));
            return;
        }

        if (state.PendingConfigurationChangeIndex.HasValue || state.ConfigurationTransitionPhase != ConfigurationTransitionPhase.None)
        {
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
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
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
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
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
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
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
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
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
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
        var command = System.Text.Encoding.UTF8.GetBytes(commandText);
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
        actions.Add(new SendMessageAction(
            request.SourceNodeId,
            new ConfigurationChangeResponse
            {
                SourceNodeId = state.NodeId,
                Term = state.CurrentTerm,
                Success = true,
                Reason = null
            }));
    }

    private void HandleVoteRequest(RaftNodeState state, RequestVoteRequest request, List<RaftAction> actions)
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
            if (request.Term >= expectedTerm && IsCandidateUpToDate(state, request.LastLogTerm, request.LastLogIndex))
            {
                voteGranted = true;
            }
        }

        actions.Add(new SendMessageAction(
            request.SourceNodeId,
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
            if (state.PreVotesGranted.Count >= state.Quorum)
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
        if (state.VotesGranted.Count >= state.Quorum)
        {
            BecomeLeader(state, actions);
        }
    }

    private void HandleAppendEntriesRequest(RaftNodeState state, AppendEntriesRequest request, List<RaftAction> actions)
    {
        if (request.Term < state.CurrentTerm)
        {
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
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
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
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
        foreach (var incoming in request.Entries)
        {
            var existing = state.Log.FirstOrDefault(x => x.Index == incoming.Index);
            if (existing is not null && existing.Term != incoming.Term)
            {
                state.Log.RemoveAll(x => x.Index >= incoming.Index);
                state.Log.Add(incoming);
                persisted.Add(incoming);
                continue;
            }
            if (existing is null)
            {
                state.Log.Add(incoming);
                persisted.Add(incoming);
            }
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

        actions.Add(new SendMessageAction(
            request.SourceNodeId,
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

        if (response.ConflictTerm.HasValue && response.ConflictIndex.HasValue)
        {
            var indexWithTerm = state.Log.LastOrDefault(x => x.Term == response.ConflictTerm.Value)?.Index;
            fallback = indexWithTerm ?? response.ConflictIndex.Value;
        }

        state.NextIndex[response.SourceNodeId] = fallback;
        SendAppendEntriesTo(state, response.SourceNodeId, actions);
    }

    private void HandleInstallSnapshotRequest(RaftNodeState state, InstallSnapshotRequest request, List<RaftAction> actions)
    {
        if (request.Term < state.CurrentTerm)
        {
            actions.Add(new SendMessageAction(
                request.SourceNodeId,
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
        actions.Add(new TakeSnapshotAction(request.LastIncludedIndex, request.LastIncludedTerm, request.SnapshotData));

        state.Log.RemoveAll(x => x.Index <= request.LastIncludedIndex);
        state.CommitIndex = Math.Max(state.CommitIndex, request.LastIncludedIndex);
        state.LastApplied = Math.Max(state.LastApplied, request.LastIncludedIndex);

        actions.Add(new SendMessageAction(
            request.SourceNodeId,
            new InstallSnapshotResponse
            {
                SourceNodeId = state.NodeId,
                Term = state.CurrentTerm,
                Success = true
            }));
    }

    private void StartElection(RaftNodeState state, List<RaftAction> actions)
    {
        state.Role = RaftRole.Candidate;
        state.CurrentTerm += 1;
        state.VotedFor = state.NodeId;
        state.VotesGranted.Clear();
        state.VotesGranted.Add(state.NodeId);
        actions.Add(new PersistStateAction(state.CurrentTerm, state.VotedFor));
        actions.Add(new ResetElectionTimerAction());
        BroadcastVoteRequests(state, state.CurrentTerm, isPreVote: false, actions);
    }

    private void BroadcastVoteRequests(RaftNodeState state, long term, bool isPreVote, List<RaftAction> actions)
    {
        foreach (var peer in state.ClusterMembers.Where(x => x != state.NodeId))
        {
            actions.Add(new SendMessageAction(
                peer,
                new RequestVoteRequest
                {
                    SourceNodeId = state.NodeId,
                    Term = term,
                    CandidateId = state.NodeId,
                    LastLogIndex = state.LastLogIndex,
                    LastLogTerm = state.LastLogTerm,
                    IsPreVote = isPreVote
                }));
        }
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
            state.MatchIndex[peer] = peer == state.NodeId ? state.LastLogIndex : -1;
        }

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
        var prevTerm = prevIndex == 0 ? 0 : state.Log.First(x => x.Index == prevIndex).Term;
        var entries = state.Log
                           .Where(x => x.Index >= nextIndex)
                           .Take(options.MaxEntriesPerAppend)
                           .ToArray();

        actions.Add(new SendMessageAction(
            peer,
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
            var entry = state.Log.FirstOrDefault(x => x.Index == candidateIndex);
            if (entry is null || entry.Term != state.CurrentTerm)
            {
                continue;
            }

            if (HasCommitQuorum(state, candidateIndex))
            {
                var previous = state.CommitIndex;
                state.CommitIndex = candidateIndex;
                EnqueueApplyActions(state, previous, actions);
                AdvanceConfigurationTransitionAfterCommit(state, actions);
                break;
            }
        }
    }

    private void AdvanceConfigurationTransitionAfterCommit(RaftNodeState state, List<RaftAction> actions)
    {
        if (state.ConfigurationTransitionPhase == ConfigurationTransitionPhase.Joint &&
            state.JointConfigurationIndex.HasValue &&
            state.CommitIndex >= state.JointConfigurationIndex.Value &&
            !state.FinalConfigurationIndex.HasValue)
        {
            var commandText = $"cfg:final:{state.PendingConfigurationChangeType}:{state.PendingConfigurationChangeNodeId}";
            var command = System.Text.Encoding.UTF8.GetBytes(commandText);
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

        var oldMajority = HasMajority(state, state.OldConfigurationMembers, candidateIndex);
        var newMajority = HasMajority(state, state.NewConfigurationMembers, candidateIndex);
        return oldMajority && newMajority;
    }

    private static bool HasMajority(RaftNodeState state, IReadOnlyCollection<string> members, long candidateIndex)
    {
        if (members.Count == 0)
        {
            return false;
        }

        var threshold = (members.Count / 2) + 1;
        var replicated = members.Count(member => state.MatchIndex.GetValueOrDefault(member, -1) >= candidateIndex);
        return replicated >= threshold;
    }

    private static IEnumerable<string> GetReplicationMembers(RaftNodeState state)
    {
        if (state.ConfigurationTransitionPhase == ConfigurationTransitionPhase.None)
        {
            return state.ClusterMembers;
        }

        return state.OldConfigurationMembers.Concat(state.NewConfigurationMembers).Distinct();
    }

    private static bool IsCandidateUpToDate(RaftNodeState state, long candidateLastLogTerm, long candidateLastLogIndex)
    {
        if (candidateLastLogTerm != state.LastLogTerm)
        {
            return candidateLastLogTerm > state.LastLogTerm;
        }
        return candidateLastLogIndex >= state.LastLogIndex;
    }

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

        var entries = state.Log
                           .Where(x => x.Index > fromExclusive && x.Index <= state.CommitIndex)
                           .OrderBy(x => x.Index)
                           .ToArray();
        if (entries.Length == 0)
        {
            return;
        }

        state.LastApplied = Math.Max(state.LastApplied, entries[^1].Index);
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

        var entry = state.Log.FirstOrDefault(x => x.Index == prevLogIndex);
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
        conflictIndex = state.Log.Where(x => x.Term == entry.Term).Min(x => x.Index);
        return false;
    }
}

using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;
using EasilyNET.Raft.Transport.Grpc.Protos;
using Google.Protobuf;

namespace EasilyNET.Raft.Transport.Grpc;

internal static class GrpcRaftMessageMapper
{
    public static RequestVoteRpcRequest ToRpc(RequestVoteRequest message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            CandidateId = message.CandidateId,
            LastLogIndex = message.LastLogIndex,
            LastLogTerm = message.LastLogTerm,
            IsPreVote = message.IsPreVote
        };
    }

    public static RequestVoteRequest FromRpc(RequestVoteRpcRequest message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            CandidateId = message.CandidateId,
            LastLogIndex = message.LastLogIndex,
            LastLogTerm = message.LastLogTerm,
            IsPreVote = message.IsPreVote
        };
    }

    public static RequestVoteRpcResponse ToRpc(RequestVoteResponse message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            VoteGranted = message.VoteGranted,
            IsPreVote = message.IsPreVote
        };
    }

    public static RequestVoteResponse FromRpc(RequestVoteRpcResponse message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            VoteGranted = message.VoteGranted,
            IsPreVote = message.IsPreVote
        };
    }

    public static AppendEntriesRpcRequest ToRpc(AppendEntriesRequest message)
    {
        var rpc = new AppendEntriesRpcRequest
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            LeaderId = message.LeaderId,
            PrevLogIndex = message.PrevLogIndex,
            PrevLogTerm = message.PrevLogTerm,
            LeaderCommit = message.LeaderCommit
        };

        foreach (var entry in message.Entries)
        {
            rpc.Entries.Add(new LogEntryRpc
            {
                Index = entry.Index,
                Term = entry.Term,
                Command = ByteString.CopyFrom(entry.Command)
            });
        }

        return rpc;
    }

    public static AppendEntriesRequest FromRpc(AppendEntriesRpcRequest message)
    {
        var entries = message.Entries
                             .Select(x => new RaftLogEntry(x.Index, x.Term, x.Command.ToByteArray()))
                             .ToArray();

        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            LeaderId = message.LeaderId,
            PrevLogIndex = message.PrevLogIndex,
            PrevLogTerm = message.PrevLogTerm,
            Entries = entries,
            LeaderCommit = message.LeaderCommit
        };
    }

    public static AppendEntriesRpcResponse ToRpc(AppendEntriesResponse message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            Success = message.Success,
            MatchIndex = message.MatchIndex,
            ConflictTerm = message.ConflictTerm ?? 0,
            ConflictIndex = message.ConflictIndex ?? 0,
            HasConflictTerm = message.ConflictTerm.HasValue,
            HasConflictIndex = message.ConflictIndex.HasValue
        };
    }

    public static AppendEntriesResponse FromRpc(AppendEntriesRpcResponse message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            Success = message.Success,
            MatchIndex = message.MatchIndex,
            ConflictTerm = message.HasConflictTerm ? message.ConflictTerm : null,
            ConflictIndex = message.HasConflictIndex ? message.ConflictIndex : null
        };
    }

    public static InstallSnapshotRpcRequest ToRpc(InstallSnapshotRequest message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            LeaderId = message.LeaderId,
            LastIncludedIndex = message.LastIncludedIndex,
            LastIncludedTerm = message.LastIncludedTerm,
            SnapshotData = ByteString.CopyFrom(message.SnapshotData)
        };
    }

    public static InstallSnapshotRequest FromRpc(InstallSnapshotRpcRequest message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            LeaderId = message.LeaderId,
            LastIncludedIndex = message.LastIncludedIndex,
            LastIncludedTerm = message.LastIncludedTerm,
            SnapshotData = message.SnapshotData.ToByteArray()
        };
    }

    public static InstallSnapshotRpcResponse ToRpc(InstallSnapshotResponse message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            Success = message.Success
        };
    }

    public static InstallSnapshotResponse FromRpc(InstallSnapshotRpcResponse message)
    {
        return new()
        {
            SourceNodeId = message.SourceNodeId,
            Term = message.Term,
            Success = message.Success
        };
    }
}

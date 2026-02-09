using EasilyNET.Raft.Transport.Grpc.Abstractions;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Transport.Grpc.Protos;
using Grpc.Core;

namespace EasilyNET.Raft.Transport.Grpc.Services;

/// <summary>
///     <para xml:lang="en">gRPC raft RPC endpoint implementation</para>
///     <para xml:lang="zh">Raft gRPC 服务端实现</para>
/// </summary>
public sealed class GrpcRaftService(IRaftRpcMessageHandler handler) : RaftRpc.RaftRpcBase
{
    /// <inheritdoc />
    public override async Task<RequestVoteRpcResponse> RequestVote(RequestVoteRpcRequest request, ServerCallContext context)
    {
        var result = await handler.HandleAsync(GrpcRaftMessageMapper.FromRpc(request), context.CancellationToken).ConfigureAwait(false);
        return GrpcRaftMessageMapper.ToRpc(result);
    }

    /// <inheritdoc />
    public override async Task<AppendEntriesRpcResponse> AppendEntries(AppendEntriesRpcRequest request, ServerCallContext context)
    {
        var result = await handler.HandleAsync(GrpcRaftMessageMapper.FromRpc(request), context.CancellationToken).ConfigureAwait(false);
        return GrpcRaftMessageMapper.ToRpc(result);
    }

    /// <inheritdoc />
    public override async Task<InstallSnapshotRpcResponse> InstallSnapshot(InstallSnapshotRpcRequest request, ServerCallContext context)
    {
        var result = await handler.HandleAsync(GrpcRaftMessageMapper.FromRpc(request), context.CancellationToken).ConfigureAwait(false);
        return GrpcRaftMessageMapper.ToRpc(result);
    }

    /// <inheritdoc />
    public override async Task<InstallSnapshotRpcResponse> InstallSnapshotStream(IAsyncStreamReader<InstallSnapshotChunkRpcRequest> requestStream, ServerCallContext context)
    {
        InstallSnapshotChunkRpcRequest? first = null;
        await using var buffer = new MemoryStream();

        while (await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false))
        {
            var chunk = requestStream.Current;
            first ??= chunk;
            if (chunk.ChunkData.Length > 0)
            {
                await buffer.WriteAsync(chunk.ChunkData.Memory, context.CancellationToken).ConfigureAwait(false);
            }
            if (chunk.IsLastChunk)
            {
                break;
            }
        }

        if (first is null)
        {
            return new InstallSnapshotRpcResponse
            {
                SourceNodeId = string.Empty,
                Term = 0,
                Success = false
            };
        }

        var request = new InstallSnapshotRequest
        {
            SourceNodeId = first.SourceNodeId,
            Term = first.Term,
            LeaderId = first.LeaderId,
            LastIncludedIndex = first.LastIncludedIndex,
            LastIncludedTerm = first.LastIncludedTerm,
            SnapshotData = buffer.ToArray()
        };

        var result = await handler.HandleAsync(request, context.CancellationToken).ConfigureAwait(false);
        return GrpcRaftMessageMapper.ToRpc(result);
    }
}

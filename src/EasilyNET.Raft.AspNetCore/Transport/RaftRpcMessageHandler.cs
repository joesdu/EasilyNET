using EasilyNET.Raft.AspNetCore.Runtime;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Transport.Grpc.Abstractions;

namespace EasilyNET.Raft.AspNetCore.Transport;

/// <summary>
///     <para xml:lang="en">Bridges gRPC RPC calls into raft runtime</para>
///     <para xml:lang="zh">将 gRPC RPC 调用桥接到 Raft 运行时</para>
/// </summary>
public sealed class RaftRpcMessageHandler(IRaftRuntime runtime) : IRaftRpcMessageHandler
{
    /// <inheritdoc />
    public Task<RequestVoteResponse> HandleAsync(RequestVoteRequest request, CancellationToken cancellationToken = default)
        => runtime.HandleRpcAsync<RequestVoteResponse>(request, cancellationToken);

    /// <inheritdoc />
    public Task<AppendEntriesResponse> HandleAsync(AppendEntriesRequest request, CancellationToken cancellationToken = default)
        => runtime.HandleRpcAsync<AppendEntriesResponse>(request, cancellationToken);

    /// <inheritdoc />
    public Task<InstallSnapshotResponse> HandleAsync(InstallSnapshotRequest request, CancellationToken cancellationToken = default)
        => runtime.HandleRpcAsync<InstallSnapshotResponse>(request, cancellationToken);
}

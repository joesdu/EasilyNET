using EasilyNET.Raft.Core.Messages;

namespace EasilyNET.Raft.Transport.Grpc.Abstractions;

/// <summary>
///     <para xml:lang="en">Receives incoming raft RPC messages and returns responses</para>
///     <para xml:lang="zh">接收 Raft RPC 并返回响应</para>
/// </summary>
public interface IRaftRpcMessageHandler
{
    /// <summary>
    ///     <para xml:lang="en">Handles RequestVote request</para>
    ///     <para xml:lang="zh">处理 RequestVote 请求</para>
    /// </summary>
    Task<RequestVoteResponse> HandleAsync(RequestVoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Handles AppendEntries request</para>
    ///     <para xml:lang="zh">处理 AppendEntries 请求</para>
    /// </summary>
    Task<AppendEntriesResponse> HandleAsync(AppendEntriesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Handles InstallSnapshot request</para>
    ///     <para xml:lang="zh">处理 InstallSnapshot 请求</para>
    /// </summary>
    Task<InstallSnapshotResponse> HandleAsync(InstallSnapshotRequest request, CancellationToken cancellationToken = default);
}

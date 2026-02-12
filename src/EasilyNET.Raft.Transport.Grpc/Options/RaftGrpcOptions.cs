// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.Raft.Transport.Grpc.Options;

/// <summary>
///     <para xml:lang="en">gRPC transport options for raft communication</para>
///     <para xml:lang="zh">Raft gRPC 传输配置</para>
/// </summary>
public sealed class RaftGrpcOptions
{
    /// <summary>
    ///     <para xml:lang="en">Peer endpoint map (node id -> address)</para>
    ///     <para xml:lang="zh">节点地址映射（nodeId -> 地址）</para>
    /// </summary>
    public Dictionary<string, string> PeerEndpoints { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">RPC request timeout in milliseconds</para>
    ///     <para xml:lang="zh">RPC 请求超时毫秒</para>
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 1500;

    /// <summary>
    ///     <para xml:lang="en">Maximum retry attempts for transient failures</para>
    ///     <para xml:lang="zh">瞬时失败最大重试次数</para>
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>
    ///     <para xml:lang="en">Base retry backoff in milliseconds</para>
    ///     <para xml:lang="zh">重试基准退避毫秒</para>
    /// </summary>
    public int RetryBackoffMs { get; set; } = 50;

    /// <summary>
    ///     <para xml:lang="en">Maximum in-flight outbound RPC per peer</para>
    ///     <para xml:lang="zh">每个对等节点最大并发 in-flight RPC</para>
    /// </summary>
    public int MaxInFlightPerPeer { get; set; } = 8;

    /// <summary>
    ///     <para xml:lang="en">Snapshot chunk bytes when using stream RPC</para>
    ///     <para xml:lang="zh">快照流式传输分块大小（字节）</para>
    /// </summary>
    public int SnapshotChunkBytes { get; set; } = 64 * 1024;
}
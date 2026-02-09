namespace EasilyNET.Raft.Core.Messages;

/// <summary>
///     <para xml:lang="en">Base raft message</para>
///     <para xml:lang="zh">Raft 消息基类</para>
/// </summary>
public abstract record RaftMessage
{
    /// <summary>
    ///     <para xml:lang="en">Message source node id</para>
    ///     <para xml:lang="zh">消息来源节点</para>
    /// </summary>
    public required string SourceNodeId { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Message term</para>
    ///     <para xml:lang="zh">消息任期</para>
    /// </summary>
    public required long Term { get; init; }
}

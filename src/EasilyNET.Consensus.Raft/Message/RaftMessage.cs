namespace EasilyNET.Consensus.Raft.Message;

/// <summary>
/// Raft RPC 消息
/// </summary>
public class RaftMessage
{
    /// <summary>
    /// 消息类型
    /// </summary>
    public RaftMessageType MessageType { get; set; }

    /// <summary>
    /// 目标节点ID
    /// </summary>
    public string TargetNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容（JSON字符串）
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// 消息ID（用于匹配请求和响应）
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
}
using System.Text.Json;

namespace EasilyNET.Consensus.Raft;

/// <summary>
/// Raft RPC 消息类型
/// </summary>
public enum RaftMessageType
{
    /// <summary>
    /// 请求投票
    /// </summary>
    RequestVote,

    /// <summary>
    /// 投票响应
    /// </summary>
    VoteResponse,

    /// <summary>
    /// 追加日志条目
    /// </summary>
    AppendEntries,

    /// <summary>
    /// 追加日志响应
    /// </summary>
    AppendEntriesResponse
}

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

/// <summary>
/// Raft 消息序列化器
/// </summary>
public static class RaftMessageSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// 序列化消息
    /// </summary>
    /// <param name="message">消息对象</param>
    /// <returns>JSON字符串</returns>
    public static string Serialize<T>(T message)
    {
        return JsonSerializer.Serialize(message, _jsonOptions);
    }

    /// <summary>
    /// 反序列化消息
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <typeparam name="T">消息类型</typeparam>
    /// <returns>消息对象</returns>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    /// <summary>
    /// 创建投票请求消息
    /// </summary>
    /// <param name="request">投票请求</param>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <returns>Raft消息</returns>
    public static RaftMessage CreateVoteRequestMessage(VoteRequest request, string targetNodeId)
    {
        return new RaftMessage
        {
            MessageType = RaftMessageType.RequestVote,
            TargetNodeId = targetNodeId,
            Payload = Serialize(request)
        };
    }

    /// <summary>
    /// 创建投票响应消息
    /// </summary>
    /// <param name="response">投票响应</param>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <param name="messageId">原始消息ID</param>
    /// <returns>Raft消息</returns>
    public static RaftMessage CreateVoteResponseMessage(VoteResponse response, string targetNodeId, string messageId)
    {
        return new RaftMessage
        {
            MessageType = RaftMessageType.VoteResponse,
            TargetNodeId = targetNodeId,
            Payload = Serialize(response),
            MessageId = messageId
        };
    }

    /// <summary>
    /// 创建追加日志请求消息
    /// </summary>
    /// <param name="request">追加请求</param>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <returns>Raft消息</returns>
    public static RaftMessage CreateAppendEntriesMessage(AppendEntriesRequest request, string targetNodeId)
    {
        return new RaftMessage
        {
            MessageType = RaftMessageType.AppendEntries,
            TargetNodeId = targetNodeId,
            Payload = Serialize(request)
        };
    }

    /// <summary>
    /// 创建追加日志响应消息
    /// </summary>
    /// <param name="response">追加响应</param>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <param name="messageId">原始消息ID</param>
    /// <returns>Raft消息</returns>
    public static RaftMessage CreateAppendEntriesResponseMessage(AppendEntriesResponse response, string targetNodeId, string messageId)
    {
        return new RaftMessage
        {
            MessageType = RaftMessageType.AppendEntriesResponse,
            TargetNodeId = targetNodeId,
            Payload = Serialize(response),
            MessageId = messageId
        };
    }
}
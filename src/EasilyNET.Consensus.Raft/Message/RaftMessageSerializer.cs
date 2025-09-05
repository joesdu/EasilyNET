using System.Text.Json;
using EasilyNET.Consensus.Raft.Protocols;

namespace EasilyNET.Consensus.Raft.Message;

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
    public static string Serialize<T>(T message) => JsonSerializer.Serialize(message, _jsonOptions);

    /// <summary>
    /// 反序列化消息
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <typeparam name="T">消息类型</typeparam>
    /// <returns>消息对象</returns>
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOptions);

    /// <summary>
    /// 创建投票请求消息
    /// </summary>
    /// <param name="request">投票请求</param>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <returns>Raft消息</returns>
    public static RaftMessage CreateVoteRequestMessage(VoteRequest request, string targetNodeId) =>
        new()
        {
            MessageType = RaftMessageType.RequestVote,
            TargetNodeId = targetNodeId,
            Payload = Serialize(request)
        };

    /// <summary>
    /// 创建投票响应消息
    /// </summary>
    /// <param name="response">投票响应</param>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <param name="messageId">原始消息ID</param>
    /// <returns>Raft消息</returns>
    public static RaftMessage CreateVoteResponseMessage(VoteResponse response, string targetNodeId, string messageId) =>
        new()
        {
            MessageType = RaftMessageType.VoteResponse,
            TargetNodeId = targetNodeId,
            Payload = Serialize(response),
            MessageId = messageId
        };

    /// <summary>
    /// 创建追加日志请求消息
    /// </summary>
    /// <param name="request">追加请求</param>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <returns>Raft消息</returns>
    public static RaftMessage CreateAppendEntriesMessage(AppendEntriesRequest request, string targetNodeId) =>
        new()
        {
            MessageType = RaftMessageType.AppendEntries,
            TargetNodeId = targetNodeId,
            Payload = Serialize(request)
        };

    /// <summary>
    /// 创建追加日志响应消息
    /// </summary>
    /// <param name="response">追加响应</param>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <param name="messageId">原始消息ID</param>
    /// <returns>Raft消息</returns>
    public static RaftMessage CreateAppendEntriesResponseMessage(AppendEntriesResponse response, string targetNodeId, string messageId) =>
        new()
        {
            MessageType = RaftMessageType.AppendEntriesResponse,
            TargetNodeId = targetNodeId,
            Payload = Serialize(response),
            MessageId = messageId
        };
}
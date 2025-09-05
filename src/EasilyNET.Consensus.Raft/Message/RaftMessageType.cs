namespace EasilyNET.Consensus.Raft.Message;

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
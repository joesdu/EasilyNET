namespace EasilyNET.Consensus.Raft.Protocols;

/// <summary>
/// 追加日志条目响应
/// </summary>
public class AppendEntriesResponse
{
    /// <summary>
    /// 当前任期号，用于领导者更新自己的任期号
    /// </summary>
    public int Term { get; set; }

    /// <summary>
    /// 如果跟随者包含匹配上 prevLogIndex 和 prevLogTerm 的日志条目则为真
    /// </summary>
    public bool Success { get; set; }
}
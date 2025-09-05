namespace EasilyNET.Consensus.Raft.Protocols;

/// <summary>
/// 投票请求
/// </summary>
public class VoteRequest
{
    /// <summary>
    /// 候选者任期号
    /// </summary>
    public int Term { get; set; }

    /// <summary>
    /// 候选者ID
    /// </summary>
    public string CandidateId { get; set; } = string.Empty;

    /// <summary>
    /// 候选者最后日志索引
    /// </summary>
    public long LastLogIndex { get; set; }

    /// <summary>
    /// 候选者最后日志任期
    /// </summary>
    public int LastLogTerm { get; set; }
}
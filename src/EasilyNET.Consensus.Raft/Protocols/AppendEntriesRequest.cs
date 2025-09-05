namespace EasilyNET.Consensus.Raft.Protocols;

/// <summary>
/// 追加日志条目请求
/// </summary>
public class AppendEntriesRequest
{
    /// <summary>
    /// 领导者任期号
    /// </summary>
    public int Term { get; set; }

    /// <summary>
    /// 领导者ID
    /// </summary>
    public string LeaderId { get; set; } = string.Empty;

    /// <summary>
    /// 紧邻新日志条目之前的日志条目的索引
    /// </summary>
    public long PrevLogIndex { get; set; }

    /// <summary>
    /// 紧邻新日志条目之前的日志条目的任期
    /// </summary>
    public int PrevLogTerm { get; set; }

    /// <summary>
    /// 需要被保存的日志条目（心跳时为空）
    /// </summary>
    public List<LogEntry> Entries { get; set; } = [];

    /// <summary>
    /// 领导者已经提交的日志条目的索引
    /// </summary>
    public long LeaderCommit { get; set; }
}
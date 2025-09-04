using System.Threading.Tasks;

namespace EasilyNET.Consensus.Raft;

/// <summary>
/// Raft RPC 接口
/// </summary>
public interface IRaftRpc
{
    /// <summary>
    /// 请求投票
    /// </summary>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <param name="request">投票请求</param>
    /// <returns>投票响应</returns>
    Task<VoteResponse> RequestVoteAsync(string targetNodeId, VoteRequest request);

    /// <summary>
    /// 追加日志条目
    /// </summary>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <param name="request">追加请求</param>
    /// <returns>追加响应</returns>
    Task<AppendEntriesResponse> AppendEntriesAsync(string targetNodeId, AppendEntriesRequest request);
}

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

/// <summary>
/// 投票响应
/// </summary>
public class VoteResponse
{
    /// <summary>
    /// 当前任期号，用于候选者更新自己的任期号
    /// </summary>
    public int Term { get; set; }

    /// <summary>
    /// 如果候选者获得选票则为真
    /// </summary>
    public bool VoteGranted { get; set; }
}

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
    public List<LogEntry> Entries { get; set; } = new();

    /// <summary>
    /// 领导者已经提交的日志条目的索引
    /// </summary>
    public long LeaderCommit { get; set; }
}

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
using EasilyNET.Consensus.Raft.Protocols;

namespace EasilyNET.Consensus.Raft.Rpc;

/// <summary>
/// Raft RPC 目标对象
/// </summary>
public class RaftRpcTarget
{
    private readonly RaftNode _raftNode;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="raftNode">Raft节点</param>
    public RaftRpcTarget(RaftNode raftNode)
    {
        _raftNode = raftNode;
    }

    /// <summary>
    /// 处理投票请求
    /// </summary>
    /// <param name="request">投票请求</param>
    /// <returns>投票响应</returns>
    public async Task<VoteResponse> RequestVoteAsync(VoteRequest request) => await _raftNode.HandleRequestVote(request);

    /// <summary>
    /// 处理追加日志请求
    /// </summary>
    /// <param name="request">追加请求</param>
    /// <returns>追加响应</returns>
    public async Task<AppendEntriesResponse> AppendEntriesAsync(AppendEntriesRequest request) => await _raftNode.HandleAppendEntries(request);
}
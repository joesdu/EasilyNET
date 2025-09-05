using EasilyNET.Consensus.Raft.Protocols;

namespace EasilyNET.Consensus.Raft.Rpc;

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
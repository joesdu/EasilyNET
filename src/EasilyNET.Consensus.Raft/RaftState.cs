namespace EasilyNET.Consensus.Raft;

/// <summary>
/// Raft 节点状态枚举
/// </summary>
public enum RaftState
{
    /// <summary>
    /// 跟随者状态
    /// </summary>
    Follower,

    /// <summary>
    /// 候选者状态
    /// </summary>
    Candidate,

    /// <summary>
    /// 领导者状态
    /// </summary>
    Leader
}
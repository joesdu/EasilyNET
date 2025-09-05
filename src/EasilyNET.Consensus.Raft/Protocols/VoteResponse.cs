namespace EasilyNET.Consensus.Raft.Protocols;

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
namespace EasilyNET.Raft.Core.Models;

/// <summary>
///     <para xml:lang="en">Raft node role</para>
///     <para xml:lang="zh">Raft 节点角色</para>
/// </summary>
public enum RaftRole
{
    /// <summary>
    ///     <para xml:lang="en">Follower role</para>
    ///     <para xml:lang="zh">跟随者</para>
    /// </summary>
    Follower = 0,

    /// <summary>
    ///     <para xml:lang="en">Candidate role</para>
    ///     <para xml:lang="zh">候选者</para>
    /// </summary>
    Candidate = 1,

    /// <summary>
    ///     <para xml:lang="en">Leader role</para>
    ///     <para xml:lang="zh">领导者</para>
    /// </summary>
    Leader = 2
}
namespace EasilyNET.Raft.AspNetCore.Services;

/// <summary>
///     <para xml:lang="en">Timer control interface for resetting raft election and heartbeat timers</para>
///     <para xml:lang="zh">Raft 选举与心跳计时器重置控制接口</para>
/// </summary>
public interface IRaftTimerControl
{
    /// <summary>
    ///     <para xml:lang="en">Resets the election timeout timer</para>
    ///     <para xml:lang="zh">重置选举超时计时器</para>
    /// </summary>
    void ResetElectionTimer();

    /// <summary>
    ///     <para xml:lang="en">Resets the heartbeat timer</para>
    ///     <para xml:lang="zh">重置心跳计时器</para>
    /// </summary>
    void ResetHeartbeatTimer();
}
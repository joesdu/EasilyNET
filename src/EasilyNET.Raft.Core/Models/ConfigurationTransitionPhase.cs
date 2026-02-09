namespace EasilyNET.Raft.Core.Models;

/// <summary>
///     <para xml:lang="en">Membership transition phase</para>
///     <para xml:lang="zh">成员变更阶段</para>
/// </summary>
public enum ConfigurationTransitionPhase
{
    /// <summary>
    ///     <para xml:lang="en">No transition</para>
    ///     <para xml:lang="zh">无过渡</para>
    /// </summary>
    None = 0,

    /// <summary>
    ///     <para xml:lang="en">Joint consensus phase</para>
    ///     <para xml:lang="zh">联合共识阶段</para>
    /// </summary>
    Joint = 1,

    /// <summary>
    ///     <para xml:lang="en">Finalizing new config phase</para>
    ///     <para xml:lang="zh">最终配置收敛阶段</para>
    /// </summary>
    Finalizing = 2
}

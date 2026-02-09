namespace EasilyNET.Raft.Core.Models;

/// <summary>
///     <para xml:lang="en">Cluster membership change operation</para>
///     <para xml:lang="zh">集群成员变更操作类型</para>
/// </summary>
public enum ConfigurationChangeType
{
    /// <summary>
    ///     <para xml:lang="en">Add one node</para>
    ///     <para xml:lang="zh">新增节点</para>
    /// </summary>
    Add = 0,

    /// <summary>
    ///     <para xml:lang="en">Remove one node</para>
    ///     <para xml:lang="zh">移除节点</para>
    /// </summary>
    Remove = 1
}

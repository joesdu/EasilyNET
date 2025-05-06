// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace EasilyNET.Core;

/// <summary>
///     <para xml:lang="en">Operator</para>
///     <para xml:lang="zh">操作人</para>
/// </summary>
/// <param name="rid">
///     <para xml:lang="en">Related ID</para>
///     <para xml:lang="zh">相关ID</para>
/// </param>
/// <param name="name">
///     <para xml:lang="en">Name</para>
///     <para xml:lang="zh">名称</para>
/// </param>
// ReSharper disable once UnusedType.Global
public class Operator(string rid, string name) : ReferenceItem(rid, name)
{
    /// <summary>
    ///     <para xml:lang="en">Time</para>
    ///     <para xml:lang="zh">时间</para>
    /// </summary>
    public DateTime? Time { get; set; }
}
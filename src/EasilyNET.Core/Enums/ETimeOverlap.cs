using System.ComponentModel;

namespace EasilyNET.Core.Enums;

/// <summary>
///     <para xml:lang="en">Time overlap situations</para>
///     <para xml:lang="zh">时间重合情况</para>
/// </summary>
public enum ETimeOverlap
{
    /// <summary>
    ///     <para xml:lang="en">Front Section Overlap</para>
    ///     <para xml:lang="zh">前段重合</para>
    /// </summary>
    [Description("FrontSectionOverlap")]
    前段重合,

    /// <summary>
    ///     <para xml:lang="en">Exact overlap</para>
    ///     <para xml:lang="zh">完全重合</para>
    /// </summary>
    [Description("ExactOverlap")]
    完全重合,

    /// <summary>
    ///     <para xml:lang="en">Rear Section Overlap</para>
    ///     <para xml:lang="zh">后段重合</para>
    /// </summary>
    [Description("RearSectionOverlap")]
    后段重合,

    /// <summary>
    ///     <para xml:lang="en">No overlap at all</para>
    ///     <para xml:lang="zh">完全不重合</para>
    /// </summary>
    [Description("AllNontOverlap")]
    完全不重合
}
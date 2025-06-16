using System.ComponentModel;

namespace EasilyNET.Core.Enums;

/// <summary>
///     <para xml:lang="en">Time overlap situations</para>
///     <para xml:lang="zh">时间重合情况</para>
/// </summary>
public enum ETimeOverlap
{
    /// <summary>
    ///     <para xml:lang="en">No overlap between the two time periods.</para>
    ///     <para xml:lang="zh">两个时间段完全不重合。</para>
    /// </summary>
    [Description("NoOverlap")]
    NoOverlap,

    /// <summary>
    ///     <para xml:lang="en">The 'sub' time period is completely within the 'source' time period.</para>
    ///     <para xml:lang="zh">'sub' 时间段完全在 'source' 时间段内部。</para>
    /// </summary>
    [Description("SubWithinSource")]
    SubWithinSource,

    /// <summary>
    ///     <para xml:lang="en">The 'source' time period is completely within the 'sub' time period.</para>
    ///     <para xml:lang="zh">'source' 时间段完全在 'sub' 时间段内部。</para>
    /// </summary>
    [Description("SourceWithinSub")]
    SourceWithinSub,

    /// <summary>
    ///     <para xml:lang="en">The 'sub' time period overlaps the start of the 'source' time period.</para>
    ///     <para xml:lang="zh">'sub' 时间段与 'source' 时间段的前部分重合。</para>
    /// </summary>
    [Description("SubOverlapsStartOfSource")]
    SubOverlapsStartOfSource,

    /// <summary>
    ///     <para xml:lang="en">The 'sub' time period overlaps the end of the 'source' time period.</para>
    ///     <para xml:lang="zh">'sub' 时间段与 'source' 时间段的后部分重合。</para>
    /// </summary>
    [Description("SubOverlapsEndOfSource")]
    SubOverlapsEndOfSource
}
using System.ComponentModel;

namespace EasilyNET.Core.Enums;

/// <summary>
/// 时间重合情况
/// </summary>
public enum ETimeOverlap
{
    /// <summary>
    /// Front Section Overlap
    /// </summary>
    [Description("FrontSectionOverlap")]
    前段重合,

    /// <summary>
    /// Exact overlap
    /// </summary>
    [Description("ExactOverlap")]
    完全重合,

    /// <summary>
    /// Rear Section Overlap
    /// </summary>
    [Description("RearSectionOverlap")]
    后段重合,

    /// <summary>
    /// No overlap at all
    /// </summary>
    [Description("AllNontOverlap")]
    完全不重合
}

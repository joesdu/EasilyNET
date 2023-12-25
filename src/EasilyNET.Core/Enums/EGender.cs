using System.ComponentModel;

namespace EasilyNET.Core.Enums;

/// <summary>
/// 性别枚举
/// </summary>
public enum EGender
{
    /// <summary>
    /// Female: ♀
    /// </summary>
    [Description("Female")]
    女 = 0,

    /// <summary>
    /// Male: ♂
    /// </summary>
    [Description("Male")]
    男 = 1
}
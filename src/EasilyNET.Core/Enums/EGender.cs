using System.ComponentModel;

namespace EasilyNET.Core.Enums;

/// <summary>
///     <para xml:lang="en">Gender enum</para>
///     <para xml:lang="zh">性别枚举</para>
/// </summary>
public enum EGender
{
    /// <summary>
    ///     <para xml:lang="en">Female: ♀</para>
    ///     <para xml:lang="zh">女: ♀</para>
    /// </summary>
    [Description("Female")]
    女 = 0,

    /// <summary>
    ///     <para xml:lang="en">Male: ♂</para>
    ///     <para xml:lang="zh">男: ♂</para>
    /// </summary>
    [Description("Male")]
    男 = 1
}
using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Enums;

/// <summary>
///     <para xml:lang="en">Camel case naming convention enum</para>
///     <para xml:lang="zh">驼峰命名枚举</para>
/// </summary>
public enum ECamelCase
{
    /// <summary>
    ///     <para xml:lang="en">Lower camel case naming convention</para>
    ///     <para xml:lang="zh">小驼峰式命名法</para>
    /// </summary>
    [Description("小驼峰式命名法")]
    LowerCamelCase,

    /// <summary>
    ///     <para xml:lang="en">Upper camel case naming convention</para>
    ///     <para xml:lang="zh">大驼峰式命名法</para>
    /// </summary>
    [Description("大驼峰式命名法")]
    UpperCamelCase
}
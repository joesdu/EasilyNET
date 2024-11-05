using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Enums;

/// <summary>
/// 驼峰命名枚举
/// </summary>
public enum ECamelCase
{
    /// <summary>
    /// 小驼峰式命名法
    /// </summary>
    [Description("小驼峰式命名法")]
    LowerCamelCase,

    /// <summary>
    /// 大驼峰式命名法
    /// </summary>
    [Description("大驼峰式命名法")]
    UpperCamelCase
}
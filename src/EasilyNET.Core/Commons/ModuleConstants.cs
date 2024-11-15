using System.Diagnostics.CodeAnalysis;

namespace EasilyNET.Core.Commons;

/// <summary>
/// 一些程序集内部使用的常量
/// </summary>
internal static class ModuleConstants
{
    /// <summary>
    /// 本库默认浮点数精度, 1E-6
    /// </summary>
    internal const double Epsilon = 1E-6;

    /// <summary>
    /// DateTime常见格式
    /// </summary>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    internal static readonly string[] DateTimeFormats =
    [
        "yyyy/MM/dd",
        "yyyy-MM-dd",
        "yyyyMMdd",
        "yyyy.MM.dd",
        "yyyy/MM/dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyyMMddHHmmss",
        "yyyy.MM.dd HH:mm:ss",
        "yyyy/MM/dd HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyyMMddHHmmssfff",
        "yyyy.MM.dd HH:mm:ss.fff"
    ];
}
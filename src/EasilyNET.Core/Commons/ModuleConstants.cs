using System.Diagnostics.CodeAnalysis;

namespace EasilyNET.Core.Commons;

/// <summary>
///     <para xml:lang="en">Some constants used internally by the assembly</para>
///     <para xml:lang="zh">一些程序集内部使用的常量</para>
/// </summary>
internal static class ModuleConstants
{
    /// <summary>
    ///     <para xml:lang="en">Default floating-point precision for this library, 1E-6</para>
    ///     <para xml:lang="zh">本库默认浮点数精度, 1E-6</para>
    /// </summary>
    internal const double Epsilon = 1E-6;

    /// <summary>
    ///     <para xml:lang="en">Common DateTime formats</para>
    ///     <para xml:lang="zh">DateTime常见格式</para>
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
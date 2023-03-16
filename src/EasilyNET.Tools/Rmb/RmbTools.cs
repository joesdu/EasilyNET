using System.Text.RegularExpressions;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Tools;

/// <summary>
/// 人民币工具类
/// </summary>
// ReSharper disable once UnusedType.Global
public static class RmbTools
{
    /// <summary>
    /// 转换人民币大小金额
    /// </summary>
    /// <param name="number">金额</param>
    /// <returns>返回大写形式</returns>
    public static string ToRMB(this decimal number)
    {
        var s = number.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
#pragma warning disable SYSLIB1045 // 转换为“GeneratedRegexAttribute”。
        var d = Regex.Replace(s, @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))", "${b}${z}");
        return Regex.Replace(d, ".", m => "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟万亿兆京垓秭穰"[m.Value[0] - '-'].ToString());
#pragma warning restore SYSLIB1045 // 转换为“GeneratedRegexAttribute”。
    }

    /// <summary>
    /// 转换人民币大小金额
    /// </summary>
    /// <param name="number">金额</param>
    /// <returns>返回大写形式</returns>
    public static string ToRMB(this double number) => ToRMB((decimal)number);

    /// <summary>
    /// 转换人民币大小金额
    /// </summary>
    /// <param name="number">金额</param>
    /// <returns>返回大写形式</returns>
    public static string ToRMB(this int number) => ToRMB((decimal)number);

    /// <summary>
    /// 转换人民币大小金额 .
    /// </summary>
    /// <param name="numStr">金额</param>
    /// <returns>返回大写形式</returns>
    public static string ToRMB(this string numStr) => ToRMB(Convert.ToDecimal(numStr));
}
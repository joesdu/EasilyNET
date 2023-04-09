// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using System.Text.RegularExpressions;

namespace EasilyNET.Core.Misc;

/// <summary>
/// double 扩展
/// </summary>
public static class DoubleExtensions
{
    /// <summary>
    /// 转decimal
    /// </summary>
    /// <param name="num"></param>
    /// <param name="precision">小数位数</param>
    /// <param name="mode">四舍五入策略</param>
    /// <returns></returns>
    public static decimal ToDecimal(this double num, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Math.Round((decimal)num, precision, mode);

    /// <summary>
    /// 转decimal
    /// </summary>
    /// <param name="num"></param>
    /// <param name="precision">小数位数</param>
    /// <param name="mode">四舍五入策略</param>
    /// <returns></returns>
    public static decimal ToDecimal(this float num, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Math.Round((decimal)num, precision, mode);

    /// <summary>
    /// 转换人民币大小金额
    /// </summary>
    /// <param name="number">金额</param>
    /// <returns>返回大写形式</returns>
    public static string ToRmb(this decimal number)
    {
        var s = number.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
#if !NETSTANDARD
#pragma warning disable SYSLIB1045 // 转换为“GeneratedRegexAttribute”。
#endif
        var d = Regex.Replace(s, @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))", "${b}${z}");
        return Regex.Replace(d, ".", m => "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟万亿兆京垓秭穰"[m.Value[0] - '-'].ToString());
#if !NETSTANDARD
#pragma warning restore SYSLIB1045 // 转换为“GeneratedRegexAttribute”。
#endif
    }

    /// <summary>
    /// 转换人民币大小金额
    /// </summary>
    /// <param name="number">金额</param>
    /// <returns>返回大写形式</returns>
    public static string ToRmb(this double number) => ((decimal)number).ToRmb();
}
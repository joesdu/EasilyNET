using System.Text.RegularExpressions;
using EasilyNET.Core.Commons;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Double extensions</para>
///     <para xml:lang="zh">double 扩展</para>
/// </summary>
public static partial class NumberExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Converts a double to a decimal</para>
    ///     <para xml:lang="zh">将 double 转换为 decimal</para>
    /// </summary>
    /// <param name="num">
    ///     <para xml:lang="en">The double number</para>
    ///     <para xml:lang="zh">double 数字</para>
    /// </param>
    /// <param name="precision">
    ///     <para xml:lang="en">The number of decimal places</para>
    ///     <para xml:lang="zh">小数位数</para>
    /// </param>
    /// <param name="mode">
    ///     <para xml:lang="en">The rounding strategy</para>
    ///     <para xml:lang="zh">四舍五入策略</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The converted decimal</para>
    ///     <para xml:lang="zh">转换后的 decimal</para>
    /// </returns>
    public static decimal ToDecimal(this double num, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Math.Round((decimal)num, precision, mode);

    /// <summary>
    ///     <para xml:lang="en">Converts a float to a decimal</para>
    ///     <para xml:lang="zh">将 float 转换为 decimal</para>
    /// </summary>
    /// <param name="num">
    ///     <para xml:lang="en">The float number</para>
    ///     <para xml:lang="zh">float 数字</para>
    /// </param>
    /// <param name="precision">
    ///     <para xml:lang="en">The number of decimal places</para>
    ///     <para xml:lang="zh">小数位数</para>
    /// </param>
    /// <param name="mode">
    ///     <para xml:lang="en">The rounding strategy</para>
    ///     <para xml:lang="zh">四舍五入策略</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The converted decimal</para>
    ///     <para xml:lang="zh">转换后的 decimal</para>
    /// </returns>
    public static decimal ToDecimal(this float num, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Math.Round((decimal)num, precision, mode);

    /// <summary>
    ///     <para xml:lang="en">Converts a decimal amount to its Chinese RMB representation</para>
    ///     <para xml:lang="zh">将 decimal 金额转换为中文人民币表示</para>
    /// </summary>
    /// <param name="number">
    ///     <para xml:lang="en">The amount</para>
    ///     <para xml:lang="zh">金额</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The Chinese RMB representation</para>
    ///     <para xml:lang="zh">返回大写形式</para>
    /// </returns>
    public static string ToRmb(this decimal number)
    {
        var s = number.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
        var d = AmountNumber().Replace(s, "${b}${z}");
        return ToCapitalized().Replace(d, m => "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟万亿兆京垓秭穰"[m.Value[0] - '-'].ToString());
    }

    [GeneratedRegex(@"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))", RegexOptions.Compiled)]
    private static partial Regex AmountNumber();

    [GeneratedRegex(".", RegexOptions.Compiled)]
    private static partial Regex ToCapitalized();

    /// <summary>
    ///     <para xml:lang="en">Converts a double amount to its Chinese RMB representation</para>
    ///     <para xml:lang="zh">将 double 金额转换为中文人民币表示</para>
    /// </summary>
    /// <param name="number">
    ///     <para xml:lang="en">The amount</para>
    ///     <para xml:lang="zh">金额</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The Chinese RMB representation</para>
    ///     <para xml:lang="zh">返回大写形式</para>
    /// </returns>
    public static string ToRmb(this double number) => ((decimal)number).ToRmb();

    /// <summary>
    ///     <para xml:lang="en">Compares two floating-point numbers for equality using relative error</para>
    ///     <para xml:lang="zh">使用相对误差比较两个浮点数是否相等</para>
    ///     <remarks>
    ///         <para>Usage:</para>
    ///         <code>
    ///         <![CDATA[
    ///         double num1 = 0.123456;
    ///         double num2 = 0.1234567;
    ///         bool result = AreAlmostEqual<double>(num1, num2);
    ///         Console.WriteLine(result); // Output: True
    ///         ]]>
    ///         </code>
    ///     </remarks>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The floating-point type: <see langword="float" />, <see langword="double" />, or <see langword="decimal" /></para>
    ///     <para xml:lang="zh">浮点数类型: <see langword="float" />, <see langword="double" /> 和 <see langword="decimal" /></para>
    /// </typeparam>
    /// <param name="a">
    ///     <para xml:lang="en">The first floating-point number</para>
    ///     <para xml:lang="zh">浮点数1</para>
    /// </param>
    /// <param name="b">
    ///     <para xml:lang="en">The second floating-point number</para>
    ///     <para xml:lang="zh">浮点数2</para>
    /// </param>
    /// <param name="epsilon">
    ///     <para xml:lang="en">The precision, default is 0.000001</para>
    ///     <para xml:lang="zh">精度默认: 0.000001</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the numbers are almost equal, otherwise false</para>
    ///     <para xml:lang="zh">如果数字几乎相等，则为 true，否则为 false</para>
    /// </returns>
    public static bool AreAlmostEqual<T>(this T a, T b, double epsilon = ModuleConstants.Epsilon) where T : struct, IComparable, IConvertible, IFormattable
    {
        if (typeof(T) == typeof(float))
        {
            var floatA = a.ConvertTo<float>();
            var floatB = b.ConvertTo<float>();
            return Math.Abs(floatA - floatB) < epsilon * Math.Max(Math.Abs(floatA), Math.Abs(floatB));
        }
        if (typeof(T) == typeof(double))
        {
            var doubleA = a.ConvertTo<double>();
            var doubleB = b.ConvertTo<double>();
            return Math.Abs(doubleA - doubleB) < epsilon * Math.Max(Math.Abs(doubleA), Math.Abs(doubleB));
        }
        // ReSharper disable once InvertIf
        if (typeof(T) == typeof(decimal))
        {
            var decimalA = a.ConvertTo<decimal>();
            var decimalB = b.ConvertTo<decimal>();
            return Math.Abs(decimalA - decimalB) < epsilon.ConvertTo<decimal>() * Math.Max(Math.Abs(decimalA), Math.Abs(decimalB));
        }
        throw new NotSupportedException("Unsupported types, only the following types are supported: float, double, and decimal.");
    }

    /// <summary>
    ///     <para xml:lang="en">Checks if floating-point number a is greater than floating-point number b</para>
    ///     <para xml:lang="zh">检查浮点数 a 是否大于浮点数 b</para>
    ///     <remarks>
    ///         <para>Usage:</para>
    ///         <code>
    ///         <![CDATA[
    ///         double num1 = 0.123456;
    ///         double num2 = 0.1234567;
    ///         bool result = IsGreaterThan<double>(num1, num2);
    ///         Console.WriteLine(result); // Output: True
    ///         ]]>
    ///         </code>
    ///     </remarks>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The floating-point type: <see langword="float" />, <see langword="double" />, or <see langword="decimal" /></para>
    ///     <para xml:lang="zh">浮点数类型: <see langword="float" />, <see langword="double" /> 和 <see langword="decimal" /></para>
    /// </typeparam>
    /// <param name="a">
    ///     <para xml:lang="en">The first floating-point number</para>
    ///     <para xml:lang="zh">浮点数 a</para>
    /// </param>
    /// <param name="b">
    ///     <para xml:lang="en">The second floating-point number</para>
    ///     <para xml:lang="zh">浮点数 b</para>
    /// </param>
    /// <param name="epsilon">
    ///     <para xml:lang="en">The precision, default is 0.000001</para>
    ///     <para xml:lang="zh">精度默认: 0.000001</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if a is greater than b, otherwise false</para>
    ///     <para xml:lang="zh">如果 a 大于 b，返回 true；否则返回 false</para>
    /// </returns>
    public static bool IsGreaterThan<T>(this T a, T b, double epsilon = ModuleConstants.Epsilon) where T : struct, IComparable, IConvertible, IFormattable
    {
        if (typeof(T) == typeof(float))
        {
            var floatA = a.ConvertTo<float>();
            var floatB = b.ConvertTo<float>();
            return floatA > floatB && !AreAlmostEqual(floatA, floatB, epsilon);
        }
        if (typeof(T) == typeof(double))
        {
            var doubleA = a.ConvertTo<double>();
            var doubleB = b.ConvertTo<double>();
            return doubleA > doubleB && !AreAlmostEqual(doubleA, doubleB, epsilon);
        }
        // ReSharper disable once InvertIf
        if (typeof(T) == typeof(decimal))
        {
            var decimalA = a.ConvertTo<decimal>();
            var decimalB = b.ConvertTo<decimal>();
            return decimalA > decimalB && !AreAlmostEqual(decimalA, decimalB, epsilon);
        }
        throw new NotSupportedException("Unsupported types, only the following types are supported: float, double, and decimal.");
    }
}
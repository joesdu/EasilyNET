using System.Text.RegularExpressions;
using EasilyNET.Core.Commons;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// double 扩展
/// </summary>
public static partial class NumberExtensions
{
    /// <summary>
    /// 转decimal
    /// </summary>
    /// <param name="num"><see cref="double" /> 数字</param>
    /// <param name="precision">小数位数</param>
    /// <param name="mode">四舍五入策略</param>
    /// <returns></returns>
    public static decimal ToDecimal(this double num, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Math.Round((decimal)num, precision, mode);

    /// <summary>
    /// 转 <see cref="decimal" />
    /// </summary>
    /// <param name="num"><see cref="float" /> 数字</param>
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
        var d = AmountNumber().Replace(s, "${b}${z}");
        return ToCapitalized().Replace(d, m => "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟万亿兆京垓秭穰"[m.Value[0] - '-'].ToString());
    }

    [GeneratedRegex(@"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))", RegexOptions.Compiled)]
    private static partial Regex AmountNumber();

    [GeneratedRegex(".", RegexOptions.Compiled)]
    private static partial Regex ToCapitalized();

    /// <summary>
    /// 转换人民币大小金额
    /// </summary>
    /// <param name="number">金额</param>
    /// <returns>返回大写形式</returns>
    public static string ToRmb(this double number) => ((decimal)number).ToRmb();

    /// <summary>
    /// 比较两个浮点数是否相等.使用相对误差.
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///     <![CDATA[
    ///     double num1 = 0.123456;
    ///     double num2 = 0.1234567;
    ///     bool result = AreAlmostEqual<double>(num1, num2);
    ///     Console.WriteLine(result); // Output: True
    ///   ]]>
    ///   </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <typeparam name="T">浮点数类型: <see langword="float" />, <see langword="double" /> 和 <see langword="decimal" /></typeparam>
    /// <param name="a">浮点数1</param>
    /// <param name="b">浮点数2</param>
    /// <param name="epsilon">精度默认: 0.000001</param>
    /// <returns></returns>
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
    /// 判断浮点数 a 是否大于浮点数 b.
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///     <![CDATA[
    ///     double num1 = 0.123456;
    ///     double num2 = 0.1234567;
    ///     bool result = IsGreaterThan<double>(num1, num2);
    ///     Console.WriteLine(result); // Output: True
    ///   ]]>
    ///   </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="a">浮点数 a</param>
    /// <param name="b">浮点数 b</param>
    /// <param name="epsilon">精度默认: 0.000001</param>
    /// <returns>如果 a 大于 b，返回 true；否则返回 false</returns>
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
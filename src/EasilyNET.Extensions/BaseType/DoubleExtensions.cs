// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Extensions.BaseType;

/// <summary>
/// double 扩展
/// </summary>
public static class DoubleExtensions
{
    /// <summary>
    /// 转decimal
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public static decimal ToDecimal(this double num) => (decimal)num;

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
    /// <returns></returns>
    public static decimal ToDecimal(this float num) => (decimal)num;

    /// <summary>
    /// 转decimal
    /// </summary>
    /// <param name="num"></param>
    /// <param name="precision">小数位数</param>
    /// <param name="mode">四舍五入策略</param>
    /// <returns></returns>
    public static decimal ToDecimal(this float num, int precision, MidpointRounding mode = MidpointRounding.AwayFromZero) => Math.Round((decimal)num, precision, mode);
}
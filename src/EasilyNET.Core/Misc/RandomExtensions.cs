// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0060 // 删除未使用的参数
#pragma warning disable IDE0048 // 为清楚起见，请添加括号

namespace EasilyNET.Core.Misc;

/// <summary>
/// 随机数扩展
/// </summary>
public static class RandomExtensions
{
    /// <summary>
    /// 生成真正的随机数
    /// </summary>
    /// <param name="rand"></param>
    /// <param name="startIndex">起始数,默认: 0</param>
    /// <param name="maxValue">
    ///     <see cref="int.MaxValue" />
    /// </param>
    /// <returns></returns>
    // ReSharper disable once UnusedParameter.Global
    public static int StrictNext(this Random rand, int startIndex = 0, int maxValue = int.MaxValue) => new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), startIndex)).Next(maxValue);

    /// <summary>
    /// 产生正态分布的随机数
    /// </summary>
    /// <param name="rand"></param>
    /// <param name="mean">均值</param>
    /// <param name="stdDev">方差</param>
    /// <returns></returns>
    public static double NextGauss(this Random rand, double mean, double stdDev)
    {
        var u1 = 1.0 - rand.NextDouble();
        var u2 = 1.0 - rand.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}
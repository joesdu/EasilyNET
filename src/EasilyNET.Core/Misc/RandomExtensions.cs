// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

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
    /// <param name="maxValue"></param>
    /// <returns></returns>
    // ReSharper disable once UnusedParameter.Global
#pragma warning disable IDE0060
    public static int StrictNext(this Random rand, int maxValue = int.MaxValue)
#pragma warning restore IDE0060
    {
        return new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)).Next(maxValue);
    }

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
#pragma warning disable IDE0048
        return mean + stdDev * randStdNormal;
#pragma warning restore IDE0048
    }
}

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using System.Security.Cryptography;

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Random number extensions</para>
///     <para xml:lang="zh">随机数扩展</para>
/// </summary>
public static class RandomExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Random number extensions</para>
    ///     <para xml:lang="zh">随机数扩展</para>
    /// </summary>
    extension(Random rd)
    {
        /// <summary>
        ///     <para xml:lang="en">Generates a truly random number</para>
        ///     <para xml:lang="zh">生成真正的随机数</para>
        /// </summary>
        /// <param name="startIndex">
        ///     <para xml:lang="en">The starting number, default is 0</para>
        ///     <para xml:lang="zh">起始数，默认是 0</para>
        /// </param>
        /// <param name="maxValue">
        ///     <para xml:lang="en">The maximum value, default is <see cref="int.MaxValue" /></para>
        ///     <para xml:lang="zh">最大值，默认是 <see cref="int.MaxValue" /></para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A random integer between startIndex and maxValue</para>
        ///     <para xml:lang="zh">在 startIndex 和 maxValue 之间的随机整数</para>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <para xml:lang="en">Thrown when startIndex is greater than maxValue</para>
        ///     <para xml:lang="zh">当 startIndex 大于 maxValue 时抛出</para>
        /// </exception>
        public static int StrictNext(int startIndex = 0, int maxValue = int.MaxValue)
        {
            if (startIndex > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be less than maxValue.");
            }
            Span<byte> buffer = stackalloc byte[4];
            RandomNumberGenerator.Fill(buffer);
            var randomValue = BitConverter.ToInt32(buffer);
            return new Random(randomValue).Next(startIndex, maxValue);
        }

        /// <summary>
        ///     <para xml:lang="en">Generates a random number with a normal distribution</para>
        ///     <para xml:lang="zh">产生正态分布的随机数</para>
        /// </summary>
        /// <param name="mean">
        ///     <para xml:lang="en">The mean value</para>
        ///     <para xml:lang="zh">均值</para>
        /// </param>
        /// <param name="stdDev">
        ///     <para xml:lang="en">The standard deviation</para>
        ///     <para xml:lang="zh">标准差</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">A random number with a normal distribution</para>
        ///     <para xml:lang="zh">正态分布的随机数</para>
        /// </returns>
        public double NextGauss(double mean, double stdDev)
        {
            var u1 = 1.0 - rd.NextDouble();
            var u2 = 1.0 - rd.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }
    }
}
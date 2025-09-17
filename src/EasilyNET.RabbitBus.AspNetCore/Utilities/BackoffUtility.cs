using System.Security.Cryptography;

namespace EasilyNET.RabbitBus.AspNetCore.Utilities;

/// <summary>
/// 统一指数退避与抖动工具
/// </summary>
internal static class BackoffUtility
{
    private static readonly TimeSpan DefaultMin = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DefaultMax = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 计算指数退避  baseDelay * 2^attempt  并裁剪在[min,max]之间, 可选全抖动(full)或部分抖动(partial)
    /// </summary>
    /// <param name="attempt">第几次(>=0)</param>
    /// <param name="baseDelay">基础延时(默认1s)</param>
    /// <param name="min">最小值(默认1s)</param>
    /// <param name="max">最大值(默认30s)</param>
    /// <param name="jitter">抖动策略 none|partial|full</param>
    public static TimeSpan Exponential(int attempt, TimeSpan? baseDelay = null, TimeSpan? min = null, TimeSpan? max = null, string jitter = "partial")
    {
        if (attempt < 0)
        {
            attempt = 0;
        }
        var b = baseDelay ?? DefaultMin;
        var minDelay = min ?? DefaultMin;
        var maxDelay = max ?? DefaultMax;
        double rawMs;
        try
        {
            rawMs = b.TotalMilliseconds * Math.Pow(2, attempt);
        }
        catch
        {
            rawMs = b.TotalMilliseconds;
        }
        rawMs = Math.Clamp(rawMs, minDelay.TotalMilliseconds, maxDelay.TotalMilliseconds);

        // 抖动
        // ReSharper disable once InvertIf
        if (!string.IsNullOrWhiteSpace(jitter) && !string.Equals(jitter, "none", StringComparison.OrdinalIgnoreCase))
        {
            var rnd = RandomNumberGenerator.GetInt32(0, 10000) / 10000d; // [0,1)
            if (string.Equals(jitter, "full", StringComparison.OrdinalIgnoreCase))
            {
                rawMs = rnd * rawMs; // 0~raw
            }
            else // partial
            {
                var factor = 0.5 + (rnd * 0.5); // 0.5~1.0
                rawMs *= factor;
            }
        }
        return TimeSpan.FromMilliseconds(rawMs);
    }
}
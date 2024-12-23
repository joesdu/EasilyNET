namespace EasilyNET.Core.System;

/// <remarks>
///     <para xml:lang="en">Based on Twitter's Snowflake algorithm</para>
///     <para xml:lang="zh">基于 Twitter 的 Snowflake 算法</para>
/// </remarks>
/// <param name="workerId">
///     <para xml:lang="en">Worker ID, representing the unique identifier of the current process</para>
///     <para xml:lang="zh">Worker ID，表示当前进程的唯一标识</para>
/// </param>
/// <param name="sequence">
///     <para xml:lang="en">Initial sequence</para>
///     <para xml:lang="zh">初始序列</para>
/// </param>
/// <param name="clockBackwardsInMinutes">
///     <para xml:lang="en">Clock backwards tolerance limit in minutes</para>
///     <para xml:lang="zh">时钟回拨容忍上限（分钟）</para>
/// </param>
public class SnowFlakeId(long workerId, long sequence = 0L, int clockBackwardsInMinutes = 2) : ISnowFlakeId
{
    // 基准时间
    private const long TwEpoch = 1288834974657L; // 2010-11-04 09:42:54

    // 机器标识位数
    private const int WorkerIdBits = 12;

    // 序列号识位数
    private const int SequenceBits = 10;

    // 机器ID偏左移10位
    private const int WorkerIdShift = SequenceBits;

    // 时间毫秒
    private const int TimestampLeftShift = SequenceBits + WorkerIdBits;

    // 最大序列号
    private const long MaxSequence = -1L ^ (-1L << SequenceBits);

    private readonly Lock __lock = new();

    // 最后时间
    private long _lastTimestamp = -1L;
    private long _sequence = sequence;

    /// <summary>
    ///     <para xml:lang="en">Default value</para>
    ///     <para xml:lang="zh">默认值</para>
    /// </summary>
    public static ISnowFlakeId Default { get; private set; } = new SnowFlakeId(0);

    /// <summary>
    ///     <para xml:lang="en">Gets the next ID, this method is thread-safe</para>
    ///     <para xml:lang="zh">获取下一个 ID，该方法线程安全</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">The next ID</para>
    ///     <para xml:lang="zh">下一个 ID</para>
    /// </returns>
    public long NextId()
    {
        lock (__lock)
        {
            var timestamp = TimeGen();
            // 时钟回拨检测：超过2分钟，则强制抛出异常
            if (TimeSpan.FromMilliseconds(_lastTimestamp - timestamp) >= TimeSpan.FromMinutes(clockBackwardsInMinutes))
            {
                throw new NotSupportedException($"时钟回拨超过容忍上限 {clockBackwardsInMinutes} 分钟");
            }
            // 解决时钟回拨
            while (timestamp < _lastTimestamp)
            {
                Thread.Sleep(1);
                timestamp = TimeGen();
            }
            // 上次生成时间和当前时间相同
            if (_lastTimestamp == timestamp)
            {
                // sequence 自增，和 sequenceMask 相与一下，去掉高位
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                {
                    // 等待到下一毫秒
                    timestamp = TilNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0;
            }
            _lastTimestamp = timestamp;
            return ((timestamp - TwEpoch) << TimestampLeftShift) |
                   (workerId << WorkerIdShift) |
                   _sequence;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Sets the default configuration</para>
    ///     <para xml:lang="zh">设置默认配置</para>
    /// </summary>
    /// <param name="snowflakeId">
    ///     <para xml:lang="en">The SnowFlakeId instance</para>
    ///     <para xml:lang="zh">SnowFlakeId 实例</para>
    /// </param>
    public static void SetDefaultSnowFlakeId(SnowFlakeId snowflakeId)
    {
        Default = snowflakeId;
    }

    /// <summary>
    ///     <para xml:lang="en">Prevents generating a time smaller than the previous time</para>
    ///     <para xml:lang="zh">禁止产生的时间比之前的时间还要小</para>
    /// </summary>
    /// <param name="lastTimestamp">
    ///     <para xml:lang="en">The last timestamp</para>
    ///     <para xml:lang="zh">最后的时间戳</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The next timestamp</para>
    ///     <para xml:lang="zh">下一个时间戳</para>
    /// </returns>
    private static long TilNextMillis(long lastTimestamp)
    {
        var timestamp = TimeGen();
        while (timestamp <= lastTimestamp)
        {
            timestamp = TimeGen();
            Thread.Sleep(1);
        }
        return timestamp;
    }

    // 获取 Unix 时间戳（毫秒）
    private static long TimeGen() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>
///     <para xml:lang="en">Snowflake ID interface</para>
///     <para xml:lang="zh">雪花 ID 接口</para>
/// </summary>
public interface ISnowFlakeId
{
    /// <summary>
    ///     <para xml:lang="en">Gets the next ID</para>
    ///     <para xml:lang="zh">获取下一个 ID</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">The next ID</para>
    ///     <para xml:lang="zh">下一个 ID</para>
    /// </returns>
    long NextId();
}
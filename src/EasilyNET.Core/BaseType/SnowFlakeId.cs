namespace EasilyNET.Core.BaseType;

/// <remarks>
/// 基于Twitter的snowflake算法
/// </remarks>
/// <param name="workerId">worker id，表示当前进程的唯一标识</param>
/// <param name="sequence">初始序列</param>
/// <param name="clockBackwardsInMinutes">时钟回拨容忍上限</param>
public class SnowFlakeId(long workerId, long sequence = 0L, int clockBackwardsInMinutes = 2) : ISnowFlakeId
{
    //基准时间
    private const long TwEpoch = 1288834974657L; //2010-11-04 09:42:54

    //机器标识位数
    private const int WorkerIdBits = 12;

    //序列号识位数
    private const int SequenceBits = 10;

    //机器ID偏左移10位
    private const int WorkerIdShift = SequenceBits;

    //时间毫秒
    private const int TimestampLeftShift = SequenceBits + WorkerIdBits;

    //最大序列号
    private const long MaxSequence = -1L ^ (-1L << SequenceBits);

    private readonly object __lock = new();

    //最后时间
    private long _lastTimestamp = -1L;

    /// <summary>
    /// 默认值
    /// </summary>
    public static ISnowFlakeId Default { get; private set; } = new SnowFlakeId(0);

    //序列号

    /// <summary>
    /// 获取下一个Id，该方法线程安全
    /// </summary>
    /// <returns></returns>
    public long NextId()
    {
        lock (__lock)
        {
            var timestamp = TimeGen();

            //  时钟回拨检测：超过2分钟，则强制抛出异常
            if (TimeSpan.FromMilliseconds(_lastTimestamp - timestamp) >= TimeSpan.FromMinutes(clockBackwardsInMinutes))
            {
                throw new NotSupportedException($"时钟回拨超过容忍上限{clockBackwardsInMinutes}分钟");
            }
            //解决时钟回拨
            while (timestamp < _lastTimestamp)
            {
                Thread.Sleep(1);
                timestamp = TimeGen();
            }
            //上次生成时间和当前时间相同,
            if (_lastTimestamp == timestamp)
            {
                //sequence自增，和sequenceMask相与一下，去掉高位
                sequence = (sequence + 1) & MaxSequence;
                if (sequence == 0)
                {
                    //等待到下一毫秒
                    timestamp = TilNextMillis(_lastTimestamp);
                }
            }
            else
            {
                sequence = 0;
            }
            _lastTimestamp = timestamp;
            return ((timestamp - TwEpoch) << TimestampLeftShift) |
                   (workerId << WorkerIdShift) |
                   sequence;
        }
    }

    /// <summary>
    /// 设置默认配置
    /// </summary>
    /// <param name="snowflakeId"></param>
    public static void SetDefaultSnowFlakeId(SnowFlakeId snowflakeId)
    {
        Default = snowflakeId;
    }

    /// <summary>
    /// 禁止产生的时间比之前的时间还要小
    /// </summary>
    /// <param name="lastTimestamp"></param>
    /// <returns></returns>
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

    //获取Unix时间戳（毫秒）
    private static long TimeGen() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>
/// 雪花ID接口
/// </summary>
public interface ISnowFlakeId
{
    /// <summary>
    /// 下一个Id
    /// </summary>
    /// <returns></returns>
    long NextId();
}

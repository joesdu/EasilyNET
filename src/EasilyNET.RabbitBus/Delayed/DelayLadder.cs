using System.Numerics;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.Delayed;

/// <summary>
///     <para xml:lang="en">
///     Pure math and naming helpers for the binary delay ladder (a "timing wheel" built from plain exchanges, queue level TTL and
///     dead-lettering). A delay expressed in seconds is encoded as a binary number inside the routing key; every set bit makes the
///     message wait in the queue of the matching level (level <c>n</c> waits <c>2^n</c> seconds), every clear bit makes it skip that
///     level. The sum of the visited levels equals the requested delay, so no queue ever holds messages with different TTLs and
///     head-of-line blocking cannot happen.
///     </para>
///     <para xml:lang="zh">
///     二进制延迟阶梯(由普通交换机 + 队列级 TTL + 死信转发构成的"时间轮")的纯计算与命名辅助类。
///     延迟秒数被编码为路由键中的二进制位:每个为 1 的位让消息在对应档位的队列中等待(档位 <c>n</c> 等待 <c>2^n</c> 秒),
///     为 0 的位则直接跳过该档位。所有经过档位的等待时间之和恰好等于请求的延迟,
///     因此任何一个队列内的消息 TTL 都完全相同,不会出现队头阻塞。
///     </para>
/// </summary>
public static class DelayLadder
{
    /// <summary>
    ///     <para xml:lang="en">Maximum number of ladder levels (28 levels cover up to 2^28-1 seconds, about 8.5 years)</para>
    ///     <para xml:lang="zh">阶梯的最大档位数(28 档最长可覆盖 2^28-1 秒,约 8.5 年)</para>
    /// </summary>
    public const int MaxSupportedLevelCount = 28;

    /// <summary>
    ///     <para xml:lang="en">Clamp a level count into the supported range</para>
    ///     <para xml:lang="zh">将档位数量限制到受支持的范围内</para>
    /// </summary>
    /// <param name="levelCount">
    ///     <para xml:lang="en">Requested level count</para>
    ///     <para xml:lang="zh">请求的档位数量</para>
    /// </param>
    public static int ClampLevelCount(int levelCount) => Math.Clamp(levelCount, 1, MaxSupportedLevelCount);

    /// <summary>
    ///     <para xml:lang="en">The maximum delay in seconds a ladder with the given level count can express</para>
    ///     <para xml:lang="zh">指定档位数量的阶梯所能表达的最大延迟秒数</para>
    /// </summary>
    /// <param name="levelCount">
    ///     <para xml:lang="en">Ladder level count</para>
    ///     <para xml:lang="zh">阶梯档位数量</para>
    /// </param>
    public static int MaxDelaySeconds(int levelCount) => (int)((1L << ClampLevelCount(levelCount)) - 1);

    /// <summary>
    ///     <para xml:lang="en">The smallest level count able to express the given maximum delay</para>
    ///     <para xml:lang="zh">能够表达指定最大延迟所需的最小档位数量</para>
    /// </summary>
    /// <param name="maxDelay">
    ///     <para xml:lang="en">Maximum delay that has to be supported</para>
    ///     <para xml:lang="zh">需要支持的最大延迟</para>
    /// </param>
    public static int LevelCountFor(TimeSpan maxDelay)
    {
        var seconds = ToDelaySeconds(maxDelay);
        // 2^n - 1 >= seconds  =>  n = 最高有效位下标 + 1
        return seconds <= 0 ? 1 : ClampLevelCount(64 - BitOperations.LeadingZeroCount((ulong)seconds));
    }

    /// <summary>
    ///     <para xml:lang="en">Convert a delay to whole seconds, always rounding up so a message is never delivered early</para>
    ///     <para xml:lang="zh">将延迟转换为整秒,始终向上取整以保证消息不会提前投递</para>
    /// </summary>
    /// <param name="delay">
    ///     <para xml:lang="en">Delay duration</para>
    ///     <para xml:lang="zh">延迟时长</para>
    /// </param>
    public static int ToDelaySeconds(TimeSpan delay) => delay <= TimeSpan.Zero ? 0 : (int)Math.Min(int.MaxValue, Math.Ceiling(delay.TotalSeconds));

    /// <summary>
    ///     <para xml:lang="en">
    ///     Binding key that routes a message from the exchange of a level into the queue of that same level, i.e. the key matching
    ///     "the bit of this level is set, so wait here"
    ///     </para>
    ///     <para xml:lang="zh">将消息从某档位的交换机路由进同档位队列的绑定键,即"该档位对应位为 1,在此等待"的匹配键</para>
    /// </summary>
    /// <param name="level">
    ///     <para xml:lang="en">Level index</para>
    ///     <para xml:lang="zh">档位下标</para>
    /// </param>
    /// <param name="levelCount">
    ///     <para xml:lang="en">Ladder level count</para>
    ///     <para xml:lang="zh">阶梯档位数量</para>
    /// </param>
    public static string QueueBindingKey(int level, int levelCount) => $"{Stars(levelCount, level)}1.#";

    /// <summary>
    ///     <para xml:lang="en">
    ///     Binding key that forwards a message from the exchange of a level to the next lower one without storing it, i.e. the key
    ///     matching "the bit of this level is clear, so skip this level"
    ///     </para>
    ///     <para xml:lang="zh">将消息从某档位的交换机直接透传到下一个更低档位的绑定键,即"该档位对应位为 0,跳过本档位"的匹配键</para>
    /// </summary>
    /// <param name="level">
    ///     <para xml:lang="en">Level index</para>
    ///     <para xml:lang="zh">档位下标</para>
    /// </param>
    /// <param name="levelCount">
    ///     <para xml:lang="en">Ladder level count</para>
    ///     <para xml:lang="zh">阶梯档位数量</para>
    /// </param>
    public static string PassThroughBindingKey(int level, int levelCount) => $"{Stars(levelCount, level)}0.#";

    /// <summary>
    ///     <para xml:lang="en">The binding key a destination uses to receive messages leaving the delivery exchange</para>
    ///     <para xml:lang="zh">目标端绑定到投递交换机时使用的绑定键</para>
    /// </summary>
    /// <param name="address">
    ///     <para xml:lang="en">Delay address of the destination</para>
    ///     <para xml:lang="zh">目标端的延迟地址</para>
    /// </param>
    public static string BindingKey(string address) => $"#.{address}";

    /// <summary>
    ///     <para xml:lang="en">
    ///     Build the ladder routing key for a delay. The key is <c>b(n-1).b(n-2)....b0.address</c>, where every <c>b</c> is the binary
    ///     representation of the delay in seconds
    ///     </para>
    ///     <para xml:lang="zh">
    ///     构建延迟对应的阶梯路由键。格式为 <c>b(n-1).b(n-2)....b0.address</c>,其中每个 <c>b</c> 是延迟秒数的二进制位
    ///     </para>
    /// </summary>
    /// <param name="delaySeconds">
    ///     <para xml:lang="en">Delay in seconds</para>
    ///     <para xml:lang="zh">延迟秒数</para>
    /// </param>
    /// <param name="address">
    ///     <para xml:lang="en">Delay address of the destination</para>
    ///     <para xml:lang="zh">目标端的延迟地址</para>
    /// </param>
    /// <param name="levelCount">
    ///     <para xml:lang="en">Ladder level count, must match the declared topology</para>
    ///     <para xml:lang="zh">阶梯档位数量,必须与已声明的拓扑一致</para>
    /// </param>
    /// <param name="startingLevel">
    ///     <para xml:lang="en">The level exchange the message has to be published to (the highest set bit)</para>
    ///     <para xml:lang="zh">消息应发布到的档位交换机(最高有效位)</para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">The delay exceeds what the ladder can express</para>
    ///     <para xml:lang="zh">延迟超出阶梯可表达的范围</para>
    /// </exception>
    public static string CalculateRoutingKey(int delaySeconds, string address, int levelCount, out int startingLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        var levels = ClampLevelCount(levelCount);
        if (delaySeconds < 0)
        {
            delaySeconds = 0;
        }
        var max = MaxDelaySeconds(levels);
        if (delaySeconds > max)
        {
            throw new ArgumentOutOfRangeException(nameof(delaySeconds), delaySeconds, $"The delay exceeds the maximum of {max} seconds supported by a {levels}-level delay ladder.");
        }
        startingLevel = delaySeconds is 0 ? 0 : BitOperations.Log2((uint)delaySeconds);
        return string.Create((2 * levels) + address.Length, (delaySeconds, address, levels), static (span, state) =>
        {
            var (seconds, addr, count) = state;
            var index = 0;
            for (var level = count - 1; level >= 0; level--)
            {
                span[index++] = ((seconds >> level) & 1) is not 0 ? '1' : '0';
                span[index++] = '.';
            }
            addr.AsSpan().CopyTo(span[index..]);
        });
    }

    // 档位越低，需要跳过的高位越多，前缀中的 '*' 数量即为 levelCount - 1 - level
    private static string Stars(int levelCount, int level)
    {
        var count = ClampLevelCount(levelCount) - 1 - level;
        return count <= 0 ? string.Empty : string.Create(2 * count, count, static (span, n) =>
        {
            for (var i = 0; i < n; i++)
            {
                span[2 * i] = '*';
                span[(2 * i) + 1] = '.';
            }
        });
    }
}

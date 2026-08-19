using EasilyNET.RabbitBus.Delayed;

namespace EasilyNET.Test.Unit.RabbitBus;

/// <summary>
/// 二进制延迟阶梯的路由计算测试。
/// 重点验证:发布端生成的路由键与拓扑声明所用的绑定键在 AMQP topic 匹配下能够正确协作,
/// 使消息在阶梯中累计的等待时间恰好等于请求的延迟
/// </summary>
[TestClass]
public class DelayLadderTest
{
    /// <summary>
    /// 延迟秒数向上取整,保证消息不会提前投递
    /// </summary>
    [TestMethod]
    public void ToDelaySecondsRoundsUp()
    {
        Assert.AreEqual(0, DelayLadder.ToDelaySeconds(TimeSpan.Zero));
        Assert.AreEqual(0, DelayLadder.ToDelaySeconds(TimeSpan.FromSeconds(-5)));
        Assert.AreEqual(1, DelayLadder.ToDelaySeconds(TimeSpan.FromMilliseconds(1)));
        Assert.AreEqual(1, DelayLadder.ToDelaySeconds(TimeSpan.FromSeconds(1)));
        Assert.AreEqual(2, DelayLadder.ToDelaySeconds(TimeSpan.FromSeconds(1.2)));
        Assert.AreEqual(3600, DelayLadder.ToDelaySeconds(TimeSpan.FromHours(1)));
    }

    /// <summary>
    /// 档位数量必须刚好覆盖所需的最大延迟
    /// </summary>
    [TestMethod]
    public void LevelCountCoversRequestedMaxDelay()
    {
        Assert.AreEqual(1, DelayLadder.LevelCountFor(TimeSpan.FromSeconds(1)));
        Assert.AreEqual(2, DelayLadder.LevelCountFor(TimeSpan.FromSeconds(2)));
        Assert.AreEqual(2, DelayLadder.LevelCountFor(TimeSpan.FromSeconds(3)));
        Assert.AreEqual(3, DelayLadder.LevelCountFor(TimeSpan.FromSeconds(4)));
        Assert.AreEqual(17, DelayLadder.LevelCountFor(TimeSpan.FromHours(24)));
        Assert.AreEqual(DelayLadder.MaxSupportedLevelCount, DelayLadder.LevelCountFor(TimeSpan.FromDays(365 * 100)));
        foreach (var hours in new[] { 1, 6, 24, 24 * 7, 24 * 30 })
        {
            var span = TimeSpan.FromHours(hours);
            var count = DelayLadder.LevelCountFor(span);
            Assert.IsTrue(DelayLadder.MaxDelaySeconds(count) >= span.TotalSeconds, $"{count} 个档位无法覆盖 {span}");
            Assert.IsTrue(count == 1 || DelayLadder.MaxDelaySeconds(count - 1) < span.TotalSeconds, $"{count} 个档位对 {span} 而言并非最小值");
        }
    }

    /// <summary>
    /// 路由键是"延迟秒数的二进制位 + 延迟地址",入口档位为最高有效位
    /// </summary>
    [TestMethod]
    public void RoutingKeyEncodesDelayAsBinary()
    {
        // 5 秒 = 0b0101,4 个档位
        var key = DelayLadder.CalculateRoutingKey(5, "q.orders", 4, out var startingLevel);
        Assert.AreEqual("0.1.0.1.q.orders", key);
        Assert.AreEqual(2, startingLevel);

        // 0 秒:全部为 0,从最低档位进入并被直接透传到投递交换机
        var zero = DelayLadder.CalculateRoutingKey(0, "q.orders", 4, out var zeroLevel);
        Assert.AreEqual("0.0.0.0.q.orders", zero);
        Assert.AreEqual(0, zeroLevel);
    }

    /// <summary>
    /// 超出阶梯可表达范围的延迟必须快速失败,而不是被静默截断
    /// </summary>
    [TestMethod]
    public void DelayBeyondLadderCapacityThrows()
    {
        Assert.AreEqual(15, DelayLadder.MaxDelaySeconds(4));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DelayLadder.CalculateRoutingKey(16, "q.orders", 4, out _));
    }

    /// <summary>
    /// 端到端模拟:按照实际声明的绑定键在阶梯中行走,累计等待时间必须等于请求的延迟,
    /// 且最终一定能抵达投递交换机并被目标端的 <c>#.{address}</c> 绑定命中
    /// </summary>
    [TestMethod]
    public void LadderTraversalAccumulatesExactDelay()
    {
        const string Address = "e.order_exchange.order.timeout";
        int[] levelCounts = [1, 4, 17, DelayLadder.MaxSupportedLevelCount];
        foreach (var levelCount in levelCounts)
        {
            var max = DelayLadder.MaxDelaySeconds(levelCount);
            foreach (var delay in SampleDelays(max))
            {
                var routingKey = DelayLadder.CalculateRoutingKey(delay, Address, levelCount, out var level);
                var waited = 0;
                var delivered = false;
                while (true)
                {
                    if (TopicMatch(DelayLadder.QueueBindingKey(level, levelCount), routingKey))
                    {
                        // 该位为 1:在本档位队列等待 2^level 秒后死信到下一跳
                        waited += 1 << level;
                    }
                    else if (!TopicMatch(DelayLadder.PassThroughBindingKey(level, levelCount), routingKey))
                    {
                        Assert.Fail($"档位 {level} 对延迟 {delay}s 既不入队也不透传,消息将丢失");
                    }
                    if (level is 0)
                    {
                        delivered = true;
                        break;
                    }
                    level--;
                }
                Assert.IsTrue(delivered);
                Assert.AreEqual(delay, waited, $"{levelCount} 档阶梯上延迟 {delay}s 的累计等待时间不正确");
                Assert.IsTrue(TopicMatch(DelayLadder.BindingKey(Address), routingKey), $"延迟 {delay}s 的消息无法被目标绑定命中");
            }
        }
    }

    private static IEnumerable<int> SampleDelays(int max)
    {
        int[] candidates = [0, 1, 2, 3, 5, 7, 15, 16, 60, 300, 3600, 86400, max];
        foreach (var candidate in candidates)
        {
            if (candidate <= max)
            {
                yield return candidate;
            }
        }
    }

    /// <summary>
    /// AMQP topic 交换机的匹配规则:'*' 匹配恰好一个单词,'#' 匹配零个或多个单词
    /// </summary>
    private static bool TopicMatch(string bindingKey, string routingKey)
    {
        var pattern = bindingKey.Split('.');
        var words = routingKey.Split('.');
        // matched[j] 表示 pattern 的前 i 段能否匹配 words 的前 j 个单词
        var matched = new bool[words.Length + 1];
        matched[0] = true;
        foreach (var segment in pattern)
        {
            var next = new bool[words.Length + 1];
            for (var j = 0; j <= words.Length; j++)
            {
                if (!matched[j])
                {
                    continue;
                }
                switch (segment)
                {
                    case "#":
                        for (var k = j; k <= words.Length; k++)
                        {
                            next[k] = true;
                        }
                        break;
                    case "*" when j < words.Length:
                        next[j + 1] = true;
                        break;
                    default:
                        if (j < words.Length && words[j] == segment)
                        {
                            next[j + 1] = true;
                        }
                        break;
                }
            }
            matched = next;
        }
        return matched[words.Length];
    }
}

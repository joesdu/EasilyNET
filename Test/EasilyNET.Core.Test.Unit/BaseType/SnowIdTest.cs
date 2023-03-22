using EasilyNET.Core.BaseType;
using FluentAssertions;
using Xunit.Abstractions;

namespace EasilyNET.Core.Test.Unit.BaseType;

/// <summary>
/// 测试雪花ID,其实是MongoDB的ObjectId,用来对没有使用Mongodb的情况下,获取雪花ID的一种方案.
/// </summary>
public class SnowIdTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testOutputHelper"></param>
    public SnowIdTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// Truncate Test
    /// </summary>
    [Fact]
    public void TestSnowId()
    {
        // 当suffix长度大于希望的最大长度
        var snow1 = SnowId.GenerateNewId();
        var snow2 = SnowId.GenerateNewId();
        var equal = snow1 == snow2 || snow1.Equals(snow2);
        equal.Should().BeFalse();
        var _2sub1 = snow2.CompareTo(snow1);
        _2sub1.Should().Be(1);
        var temp = snow1.ToString();
        snow1.Should().Be(SnowId.Parse(temp));
        _testOutputHelper.WriteLine(snow1.ToString());
    }
}
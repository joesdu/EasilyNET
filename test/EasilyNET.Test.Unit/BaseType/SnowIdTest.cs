using EasilyNET.Core.System;
using FluentAssertions;

namespace EasilyNET.Test.Unit;

/// <summary>
/// 测试雪花ID,其实是MongoDB的ObjectId,用来对没有使用Mongodb的情况下,获取雪花ID的一种方案.
/// </summary>
[TestClass]
public class SnowIdTest
{
    /// <summary>
    /// Truncate Test
    /// </summary>
    [TestMethod]
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
        Console.WriteLine(snow1.ToString());
    }
}
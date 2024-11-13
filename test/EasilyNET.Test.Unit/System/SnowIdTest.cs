using EasilyNET.Core.Misc;
using EasilyNET.Core.System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasilyNET.Test.Unit.System;

/// <summary>
/// 测试雪花ID,其实是MongoDB的ObjectId,用来对没有使用Mongodb的情况下,获取雪花ID的一种方案.
/// </summary>
[TestClass]
public class SnowIdTest(TestContext context)
{
    /// <summary>
    /// 测试生成的 SnowId 是否唯一
    /// </summary>
    [TestMethod]
    public void TestSnowIdUniqueness()
    {
        var snow1 = SnowId.GenerateNewId();
        var snow2 = SnowId.GenerateNewId();
        context.WriteLine($"snow1: {snow1}");
        context.WriteLine($"snow2: {snow2}");
        var equal = snow1 == snow2 || snow1.Equals(snow2);
        equal.Should().BeFalse();
    }

    /// <summary>
    /// 测试 SnowId 的比较功能
    /// </summary>
    [TestMethod]
    public void TestSnowIdComparison()
    {
        var snow1 = SnowId.GenerateNewId();
        var snow2 = SnowId.GenerateNewId();
        var comparison = snow2.CompareTo(snow1);
        comparison.Should().Be(1);
    }

    /// <summary>
    /// 测试 SnowId 的解析功能
    /// </summary>
    [TestMethod]
    public void TestSnowIdParsing()
    {
        var snow1 = SnowId.GenerateNewId();
        var snow1String = snow1.ToString();
        var parsedSnow1 = SnowId.Parse(snow1String);
        snow1.Should().Be(parsedSnow1);
    }

    /// <summary>
    /// 测试 SnowId 的字节数组转换功能
    /// </summary>
    [TestMethod]
    public void TestSnowIdByteArrayConversion()
    {
        var snow1 = SnowId.GenerateNewId();
        var byteArray = snow1.ToByteArray();
        var snowFromBytes = new SnowId(byteArray);
        snow1.Should().Be(snowFromBytes);
    }

    /// <summary>
    /// 测试 SnowId 的时间戳和创建时间
    /// </summary>
    [TestMethod]
    public void TestSnowIdTimestampAndCreationTime()
    {
        var snow1 = SnowId.GenerateNewId();
        var timestamp = snow1.Timestamp;
        var creationTime = snow1.CreationTime;
        creationTime.Should().Be(DateTimeStampExtensions.UnixEpoch.AddSeconds((uint)timestamp));
    }
}
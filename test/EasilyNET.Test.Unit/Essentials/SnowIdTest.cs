using System.Collections.Concurrent;
using EasilyNET.Core.Essentials;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace EasilyNET.Test.Unit.Essentials;

/// <summary>
/// 测试雪花ID,其实是MongoDB的ObjectId,用来对没有使用Mongodb的情况下,获取雪花ID的一种方案.
/// </summary>
[TestClass]
public class SnowIdTest
{
    public TestContext? TestContext { get; set; }

    /// <summary>
    /// 测试生成的 SnowId 是否唯一
    /// </summary>
    [TestMethod]
    public void TestSnowIdUniqueness()
    {
        var snow1 = SnowId.GenerateNewId();
        var snow2 = SnowId.GenerateNewId();
        TestContext?.WriteLine($"snow1: {snow1}");
        TestContext?.WriteLine($"snow2: {snow2}");
        var equal = snow1 == snow2 || snow1.Equals(snow2);
        Assert.IsFalse(equal);
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
        comparison.ShouldBe(1);
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
        snow1.ShouldBe(parsedSnow1);
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
        snow1.ShouldBe(snowFromBytes);
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
        creationTime.ShouldBe(DateTime.UnixEpoch.AddSeconds((uint)timestamp));
    }

    /// <summary>
    /// 测试 SnowId 的 TryParse 功能和无效输入
    /// </summary>
    [TestMethod]
    public void TestSnowIdTryParseAndInvalidInput()
    {
        var valid = SnowId.GenerateNewId().ToString();
        SnowId.TryParse(valid, out _).ShouldBeTrue();
        valid.ToUpperInvariant().Length.ShouldBe(24);
        SnowId.TryParse(valid.ToUpperInvariant(), out _).ShouldBeTrue();
        SnowId.TryParse("", out _).ShouldBeFalse();
        SnowId.TryParse("123", out _).ShouldBeFalse();
        SnowId.TryParse(new('f', 23), out _).ShouldBeFalse();
        SnowId.TryParse(new('g', 24), out _).ShouldBeFalse();
    }

    /// <summary>
    /// 测试 ToString/Parse/Equals/HashCode 的一致性
    /// </summary>
    [TestMethod]
    public void TestSnowIdToStringParseEqualsHashCode()
    {
        var snow = SnowId.GenerateNewId();
        var str = snow.ToString();
        var parsed = SnowId.Parse(str);
        snow.ShouldBe(parsed);
        snow.GetHashCode().ShouldBe(parsed.GetHashCode());
    }

    /// <summary>
    /// 测试不同构造方式的等价性
    /// </summary>
    [TestMethod]
    public void TestSnowIdConstructorsEquivalence()
    {
        var snow = SnowId.GenerateNewId();
        var bytes = snow.ToByteArray();
        var fromBytes = new SnowId(bytes);
        var fromString = new SnowId(snow.ToString());
        fromBytes.ShouldBe(snow);
        fromString.ShouldBe(snow);
    }

    /// <summary>
    /// 测试边界值
    /// </summary>
    [TestMethod]
    public void TestSnowIdBoundaryValues()
    {
        var zeroBytes = new byte[12];
        var zeroId = new SnowId(zeroBytes);
        zeroId.Timestamp.ShouldBe(0);
        zeroId.CreationTime.ShouldBe(DateTime.UnixEpoch);
        var maxBytes = new byte[12];
        for (var i = 0; i < 12; i++)
            maxBytes[i] = 0xFF;
        var maxId = new SnowId(maxBytes);
        maxId.Timestamp.ShouldBe(-1); // 0xFFFFFFFF as int
    }

    /// <summary>
    /// 测试 IConvertible 接口实现
    /// </summary>
    [TestMethod]
    public void TestSnowIdIConvertible()
    {
        var snow = SnowId.GenerateNewId();
        IConvertible convertible = snow;
        convertible.GetTypeCode().ShouldBe(TypeCode.Object);
        Should.Throw<InvalidCastException>(() => convertible.ToBoolean(null));
        Should.Throw<InvalidCastException>(() => convertible.ToInt32(null));
        convertible.ToString(null).ShouldBe(snow.ToString());
    }

    /// <summary>
    /// 测试多线程下的唯一性
    /// </summary>
    [TestMethod]
    public void TestSnowIdMultiThreadUniqueness()
    {
        var set = new ConcurrentDictionary<string, bool>();
        Parallel.For(0, 10000, _ =>
        {
            var id = SnowId.GenerateNewId().ToString();
            set.TryAdd(id, true).ShouldBeTrue();
        });
        set.Count.ShouldBe(10000);
    }

    /// <summary>
    /// 测试序列化/反序列化一致性
    /// </summary>
    [TestMethod]
    public void TestSnowIdSerializationConsistency()
    {
        var snow = SnowId.GenerateNewId();
        var bytes = snow.ToByteArray();
        var fromBytes = new SnowId(bytes);
        fromBytes.ShouldBe(snow);
        var str = snow.ToString();
        var fromStr = SnowId.Parse(str);
        fromStr.ShouldBe(snow);
    }
}
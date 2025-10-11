using System.Collections.Concurrent;
using EasilyNET.Core.Essentials;

namespace EasilyNET.Test.Unit.Essentials;

/// <summary>
/// 测试雪花ID,其实是MongoDB的ObjectId,用来对没有使用Mongodb的情况下,获取雪花ID的一种方案.
/// </summary>
[TestClass]
public class ObjectIdCompatTest
{
    public TestContext? TestContext { get; set; }

    /// <summary>
    /// 测试生成的 ObjectIdCompat 是否唯一
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatUniqueness()
    {
        var snow1 = ObjectIdCompat.GenerateNewId();
        var snow2 = ObjectIdCompat.GenerateNewId();
        TestContext?.WriteLine($"snow1: {snow1}");
        TestContext?.WriteLine($"snow2: {snow2}");
        var equal = snow1 == snow2 || snow1.Equals(snow2);
        Assert.IsFalse(equal);
    }

    /// <summary>
    /// 测试 ObjectIdCompat 的比较功能
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatComparison()
    {
        var snow1 = ObjectIdCompat.GenerateNewId();
        var snow2 = ObjectIdCompat.GenerateNewId();
        var comparison = snow2.CompareTo(snow1);
        Assert.AreEqual(1, comparison);
    }

    /// <summary>
    /// 测试 ObjectIdCompat 的解析功能
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatParsing()
    {
        var snow1 = ObjectIdCompat.GenerateNewId();
        var snow1String = snow1.ToString();
        var parsedSnow1 = ObjectIdCompat.Parse(snow1String);
        Assert.AreEqual(snow1, parsedSnow1);
    }

    /// <summary>
    /// 测试 ObjectIdCompat 的字节数组转换功能
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatByteArrayConversion()
    {
        var snow1 = ObjectIdCompat.GenerateNewId();
        var byteArray = snow1.ToByteArray();
        var snowFromBytes = new ObjectIdCompat(byteArray);
        Assert.AreEqual(snow1, snowFromBytes);
    }

    /// <summary>
    /// 测试 ObjectIdCompat 的时间戳和创建时间
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatTimestampAndCreationTime()
    {
        var snow1 = ObjectIdCompat.GenerateNewId();
        var timestamp = snow1.Timestamp;
        var creationTime = snow1.CreationTime;
        Assert.AreEqual(creationTime, DateTime.UnixEpoch.AddSeconds((uint)timestamp));
    }

    /// <summary>
    /// 测试 ObjectIdCompat 的 TryParse 功能和无效输入
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatTryParseAndInvalidInput()
    {
        var valid = ObjectIdCompat.GenerateNewId().ToString();
        Assert.IsTrue(ObjectIdCompat.TryParse(valid, out _));
        Assert.AreEqual(24, valid.ToUpperInvariant().Length);
        Assert.IsTrue(ObjectIdCompat.TryParse(valid.ToUpperInvariant(), out _));
        Assert.IsFalse(ObjectIdCompat.TryParse("", out _));
        Assert.IsFalse(ObjectIdCompat.TryParse("123", out _));
        Assert.IsFalse(ObjectIdCompat.TryParse(new('f', 23), out _));
        Assert.IsFalse(ObjectIdCompat.TryParse(new('g', 24), out _));
    }

    /// <summary>
    /// 测试 ToString/Parse/Equals/HashCode 的一致性
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatToStringParseEqualsHashCode()
    {
        var snow = ObjectIdCompat.GenerateNewId();
        var str = snow.ToString();
        var parsed = ObjectIdCompat.Parse(str);
        Assert.AreEqual(snow, parsed);
        Assert.AreEqual(snow.GetHashCode(), parsed.GetHashCode());
    }

    /// <summary>
    /// 测试不同构造方式的等价性
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatConstructorsEquivalence()
    {
        var snow = ObjectIdCompat.GenerateNewId();
        var bytes = snow.ToByteArray();
        var fromBytes = new ObjectIdCompat(bytes);
        var fromString = new ObjectIdCompat(snow.ToString());
        Assert.AreEqual(snow, fromBytes);
        Assert.AreEqual(snow, fromString);
    }

    /// <summary>
    /// 测试边界值
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatBoundaryValues()
    {
        var zeroBytes = new byte[12];
        var zeroId = new ObjectIdCompat(zeroBytes);
        Assert.AreEqual(0, zeroId.Timestamp);
        Assert.AreEqual(DateTime.UnixEpoch, zeroId.CreationTime);
        var maxBytes = new byte[12];
        for (var i = 0; i < 12; i++)
            maxBytes[i] = 0xFF;
        var maxId = new ObjectIdCompat(maxBytes);
        Assert.AreEqual(-1, maxId.Timestamp); // 0xFFFFFFFF as int
    }

    /// <summary>
    /// 测试 IConvertible 接口实现
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatIConvertible()
    {
        var snow = ObjectIdCompat.GenerateNewId();
        IConvertible convertible = snow;
        Assert.AreEqual(TypeCode.Object, convertible.GetTypeCode());
        Assert.Throws<InvalidCastException>(() => convertible.ToBoolean(null));
        Assert.Throws<InvalidCastException>(() => convertible.ToInt32(null));
        Assert.AreEqual(snow.ToString(), convertible.ToString(null));
    }

    /// <summary>
    /// 测试多线程下的唯一性
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatMultiThreadUniqueness()
    {
        var set = new ConcurrentDictionary<string, bool>();
        Parallel.For(0, 10000, _ =>
        {
            var id = ObjectIdCompat.GenerateNewId().ToString();
            Assert.IsTrue(set.TryAdd(id, true));
        });
        Assert.HasCount(10000, set);
    }

    /// <summary>
    /// 测试序列化/反序列化一致性
    /// </summary>
    [TestMethod]
    public void TestObjectIdCompatSerializationConsistency()
    {
        var snow = ObjectIdCompat.GenerateNewId();
        var bytes = snow.ToByteArray();
        var fromBytes = new ObjectIdCompat(bytes);
        Assert.AreEqual(snow, fromBytes);
        var str = snow.ToString();
        var fromStr = ObjectIdCompat.Parse(str);
        Assert.AreEqual(snow, fromStr);
    }
}
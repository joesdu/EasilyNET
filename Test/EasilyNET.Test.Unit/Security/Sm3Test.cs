using EasilyNET.Security;
using FluentAssertions;

namespace EasilyNET.Test.Unit.Security;

/// <summary>
/// SM3TEST
/// </summary>
[TestClass]
public class Sm3Test
{
    private const string data = "Microsoft";

    /// <summary>
    /// SM3测试16进制字符串格式
    /// </summary>
    [TestMethod]
    public void SM3HexString()
    {
        data.ToSm3String().ToUpper().Should().Be("1749CE3E4EF7622F1EBABB52078EC86309CABD5A6073C8A0711BF35E19BA51B8");
    }

    /// <summary>
    /// SM3测试Base64字符串格式
    /// </summary>
    [TestMethod]
    public void SM3Base64()
    {
        data.ToSm3Base64().ToUpper().Should().Be("F0NOPK73YI8EURTSB47IYWNKVVPGC8IGCRVZXHM6UBG=");
    }
}
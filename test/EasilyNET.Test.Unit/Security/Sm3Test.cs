using EasilyNET.Security;

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
        var byte_data = Sm3Signature.Hash(data);
        var hex = Convert.ToHexString(byte_data);
        Assert.AreEqual("1749CE3E4EF7622F1EBABB52078EC86309CABD5A6073C8A0711BF35E19BA51B8", hex.ToUpper());
    }

    /// <summary>
    /// SM3测试Base64字符串格式
    /// </summary>
    [TestMethod]
    public void SM3Base64()
    {
        var byte_data = Sm3Signature.Hash(data);
        var base64 = Convert.ToBase64String(byte_data);
        Assert.AreEqual("F0NOPK73YI8EURTSB47IYWNKVVPGC8IGCRVZXHM6UBG=", base64.ToUpper());
    }
}
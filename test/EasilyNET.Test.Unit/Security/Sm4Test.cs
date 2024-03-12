using EasilyNET.Core.Misc;
using EasilyNET.Security;
using FluentAssertions;
using System.Text;

namespace EasilyNET.Test.Unit.Security;

/// <summary>
/// SM4测试
/// </summary>
[TestClass]
public class Sm4Test
{
    /// <summary>
    /// SM4ECB模式加密到Base64格式
    /// </summary>
    [TestMethod]
    public void Sm4EncryptECBToBase64()
    {
        const string data = "Microsoft";
        // 将原文解析到二进制数组格式
        var byte_data = Encoding.UTF8.GetBytes(data);
        // 进制格式密钥加密数据
        var result = Sm4Crypt.EncryptECB("701d1cc0cfbe7ee11824df718855c0c6", true, byte_data);
        // 获取Base64格式的字符串结果
        var base64 = result.ToBase64();
        base64.Should().Be("ThRruxZZm1GrHE5KkP4UmQ==");
        var hex = result.ToHex();
        hex.ToUpper().Should().Be("4E146BBB16599B51AB1C4E4A90FE1499");
    }

    /// <summary>
    /// SM4ECB模式解密Base64格式到字符串
    /// </summary>
    [TestMethod]
    public void Sm4DecryptECBTest()
    {
        // Base64格式的
        const string data = "ThRruxZZm1GrHE5KkP4UmQ==";
        // 将Base64格式字符串转为 byte[]
        var byte_data = data.FromBase64();
        // 通过16进制格式密钥解密数据
        var result = Sm4Crypt.DecryptECB("701d1cc0cfbe7ee11824df718855c0c6", true, byte_data);
        // 解析结果获取字符串
        var str = Encoding.UTF8.GetString(result);
        str.Should().Be("Microsoft");
    }

    /// <summary>
    /// SM4ECB模式加密到16进制字符串
    /// </summary>
    [TestMethod]
    public void Sm4EncryptECBToHex16()
    {
        const string data = "Microsoft";
        // 将原文解析到二进制数组格式
        var byte_data = Encoding.UTF8.GetBytes(data);
        // 使用16位长度的密钥加密
        var result = Sm4Crypt.EncryptECB("1cc0cfbe7ee11824", false, byte_data);
        // 将结果转为16进制字符串
        var hex = result.ToHex();
        hex.ToUpper().Should().Be("D265DF0510C05FE836D3113B3ACEC714");
        var base64 = result.ToBase64();
        base64.Should().Be("0mXfBRDAX+g20xE7Os7HFA==");
    }

    /// <summary>
    /// SM4ECB模式解密16进制字符串格式密文
    /// </summary>
    [TestMethod]
    public void Sm4DecryptECBTest2()
    {
        const string data = "D265DF0510C05FE836D3113B3ACEC714";
        var byte_data = data.FromHex();
        var result = Sm4Crypt.DecryptECB("1cc0cfbe7ee11824", false, byte_data);
        // 解析结果获取字符串
        var str = Encoding.UTF8.GetString(result);
        str.Should().Be("Microsoft");
    }

    /// <summary>
    /// SM4CBC模式加密到Base64格式
    /// </summary>
    [TestMethod]
    public void Sm4EncryptCBCTest()
    {
        const string data = "Microsoft";
        var byte_data = Encoding.UTF8.GetBytes(data);
        var result = Sm4Crypt.EncryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", byte_data);
        var base64 = result.ToBase64();
        base64.Should().Be("Q2iUaMuSHjLvq6GhUQnGTg==");
        var hex = result.ToHex();
        hex.ToUpper().Should().Be("43689468CB921E32EFABA1A15109C64E");
    }

    /// <summary>
    /// SM4CBC模式从Base64解密
    /// </summary>
    [TestMethod]
    public void Sm4DecryptCBCTest()
    {
        const string data = "Q2iUaMuSHjLvq6GhUQnGTg==";
        var byte_data = data.FromBase64();
        var result = Sm4Crypt.DecryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", byte_data);
        var str = Encoding.UTF8.GetString(result);
        str.Should().Be("Microsoft");
    }

    /// <summary>
    /// SM4CBC模式加密到16进制字符串
    /// </summary>
    [TestMethod]
    public void Sm4EncryptCBCTest2()
    {
        const string data = "Microsoft";
        var byte_data = Encoding.UTF8.GetBytes(data);
        var result = Sm4Crypt.EncryptCBC("1cc0cfbe7ee11824", false, "1cc0cfbe7ee12824", byte_data);
        var hex = result.ToHex();
        hex.ToUpper().Should().Be("1BD7A32E49B60B17698AAC9D1E4FEE4A");
        var base64 = result.ToBase64();
        base64.Should().Be("G9ejLkm2CxdpiqydHk/uSg==");
    }

    /// <summary>
    /// SM4CBC模式从Hex16解密到字符串
    /// </summary>
    [TestMethod]
    public void Sm4DecryptCBCTest2()
    {
        const string data = "1BD7A32E49B60B17698AAC9D1E4FEE4A";
        var byte_data = data.FromHex();
        var result = Sm4Crypt.DecryptCBC("1cc0cfbe7ee11824", false, "1cc0cfbe7ee12824", byte_data);
        var str = Encoding.UTF8.GetString(result);
        str.Should().Be("Microsoft");
    }
}

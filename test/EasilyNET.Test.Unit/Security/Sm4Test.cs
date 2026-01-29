using System.Text;
using EasilyNET.Security;

namespace EasilyNET.Test.Unit.Security;

/// <summary>
/// SM4测试
/// </summary>
[TestClass]
public class Sm4Test
{
    #region New Secure CBC API Tests (Recommended)

    /// <summary>
    /// SM4 新安全 API - 加密解密测试 (自动 IV)
    /// </summary>
    [TestMethod]
    public void Sm4SecureEncryptDecryptTest()
    {
        const string data = "Microsoft";
        const string key = "701d1cc0cfbe7ee11824df718855c0c6";

        // 加密
        var encrypted = Sm4Crypt.EncryptToBase64(key, true, data);
        Assert.IsNotNull(encrypted);
        Assert.IsGreaterThan(0, encrypted.Length);

        // 解密
        var decrypted = Sm4Crypt.DecryptFromBase64(key, true, encrypted);
        Assert.AreEqual(data, decrypted);
    }

    /// <summary>
    /// SM4 新安全 API - Hex 格式测试
    /// </summary>
    [TestMethod]
    public void Sm4SecureEncryptDecryptHexTest()
    {
        const string data = "Hello SM4 Secure API";
        const string key = "1cc0cfbe7ee11824"; // 16 bytes key

        // 加密
        var encrypted = Sm4Crypt.EncryptToHex(key, false, data);
        Assert.IsNotNull(encrypted);
        Assert.IsGreaterThan(0, encrypted.Length);

        // 解密
        var decrypted = Sm4Crypt.DecryptFromHex(key, false, encrypted);
        Assert.AreEqual(data, decrypted);
    }

    /// <summary>
    /// SM4 新安全 API - 字节数组测试
    /// </summary>
    [TestMethod]
    public void Sm4SecureEncryptDecryptBytesTest()
    {
        const string data = "Test data for SM4";
        const string key = "701d1cc0cfbe7ee11824df718855c0c6";
        var plainBytes = Encoding.UTF8.GetBytes(data);

        // 加密
        var encrypted = Sm4Crypt.Encrypt(key, true, plainBytes);
        Assert.IsNotNull(encrypted);
        Assert.IsGreaterThan(plainBytes.Length, encrypted.Length); // 应该包含 IV

        // 解密
        var decrypted = Sm4Crypt.Decrypt(key, true, encrypted);
        var result = Encoding.UTF8.GetString(decrypted);
        Assert.AreEqual(data, result);
    }

    #endregion

    #region Legacy ECB API Tests (Obsolete - for backward compatibility)

#pragma warning disable CS0618 // Type or member is obsolete

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
        var base64 = Convert.ToBase64String(result);
        Assert.AreEqual("ThRruxZZm1GrHE5KkP4UmQ==", base64);
        var hex = Convert.ToHexString(result);
        Assert.AreEqual("4E146BBB16599B51AB1C4E4A90FE1499", hex.ToUpper());
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
        var byte_data = Convert.FromBase64String(data);
        // 通过16进制格式密钥解密数据
        var result = Sm4Crypt.DecryptECB("701d1cc0cfbe7ee11824df718855c0c6", true, byte_data);
        // 解析结果获取字符串
        var str = Encoding.UTF8.GetString(result);
        Assert.AreEqual("Microsoft", str);
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
        var hex = Convert.ToHexString(result);
        Assert.AreEqual("D265DF0510C05FE836D3113B3ACEC714", hex.ToUpper());
        var base64 = Convert.ToBase64String(result);
        Assert.AreEqual("0mXfBRDAX+g20xE7Os7HFA==", base64);
    }

    /// <summary>
    /// SM4ECB模式解密16进制字符串格式密文
    /// </summary>
    [TestMethod]
    public void Sm4DecryptECBTest2()
    {
        const string data = "D265DF0510C05FE836D3113B3ACEC714";
        var byte_data = Convert.FromHexString(data);
        var result = Sm4Crypt.DecryptECB("1cc0cfbe7ee11824", false, byte_data);
        // 解析结果获取字符串
        var str = Encoding.UTF8.GetString(result);
        Assert.AreEqual("Microsoft", str);
    }

#pragma warning restore CS0618

    #endregion

    #region CBC API Tests (with explicit IV)

    /// <summary>
    /// SM4CBC模式加密到Base64格式
    /// </summary>
    [TestMethod]
    public void Sm4EncryptCBCTest()
    {
        const string data = "Microsoft";
        var byte_data = Encoding.UTF8.GetBytes(data);
        var result = Sm4Crypt.EncryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", byte_data);
        var base64 = Convert.ToBase64String(result);
        Assert.AreEqual("Q2iUaMuSHjLvq6GhUQnGTg==", base64);
        var hex = Convert.ToHexString(result);
        Assert.AreEqual("43689468CB921E32EFABA1A15109C64E", hex.ToUpper());
    }

    /// <summary>
    /// SM4CBC模式从Base64解密
    /// </summary>
    [TestMethod]
    public void Sm4DecryptCBCTest()
    {
        const string data = "Q2iUaMuSHjLvq6GhUQnGTg==";
        var byte_data = Convert.FromBase64String(data);
        var result = Sm4Crypt.DecryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", byte_data);
        var str = Encoding.UTF8.GetString(result);
        Assert.AreEqual("Microsoft", str);
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
        var hex = Convert.ToHexString(result);
        Assert.AreEqual("1BD7A32E49B60B17698AAC9D1E4FEE4A", hex.ToUpper());
        var base64 = Convert.ToBase64String(result);
        Assert.AreEqual("G9ejLkm2CxdpiqydHk/uSg==", base64);
    }

    /// <summary>
    /// SM4CBC模式从Hex16解密到字符串
    /// </summary>
    [TestMethod]
    public void Sm4DecryptCBCTest2()
    {
        const string data = "1BD7A32E49B60B17698AAC9D1E4FEE4A";
        var byte_data = Convert.FromHexString(data);
        var result = Sm4Crypt.DecryptCBC("1cc0cfbe7ee11824", false, "1cc0cfbe7ee12824", byte_data);
        var str = Encoding.UTF8.GetString(result);
        Assert.AreEqual("Microsoft", str);
    }

    #endregion
}
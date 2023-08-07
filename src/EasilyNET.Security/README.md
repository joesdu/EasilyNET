##### EasilyNET.Security

一个.Net 中常用的加密算法的封装.降低加密解密的使用复杂度.

- 目前有的算法:AES,DES,RC4,TripleDES,RSA,SM2,SM3,SM4
- 支持 RSA XML 结构的 SecurityKey 和 Base64 格式的互转.

- 本库不是去实现加密算法,而是基于.Net 提供的接口封装,为了方便使用

- 未经测试的预测,若是遇到了解密乱码,可能是需要引入一个包.
- 在主项目中添加 System.Text.Encoding.CodePages 库,并在程序入口处添加注册代码. Programe.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
```

- 新增 SM3 和 SM4,使用案例

###### SM3

```csharp
 private const string data = "Microsoft";

/// <summary>
/// SM3测试16进制字符串格式
/// </summary>
public void SM3HexString()
{
    var byte_data = Sm3Crypt.Crypt(data);
    var hex = byte_data.ToHex();
    hex.ToUpper().Should().Be("1749CE3E4EF7622F1EBABB52078EC86309CABD5A6073C8A0711BF35E19BA51B8");
}

/// <summary>
/// SM3测试Base64字符串格式
/// </summary>
public void SM3Base64()
{
    var byte_data = Sm3Crypt.Crypt(data);
    var base64 = byte_data.ToBase64();
    base64.ToUpper().Should().Be("F0NOPK73YI8EURTSB47IYWNKVVPGC8IGCRVZXHM6UBG=");
}
```

###### SM4

```csharp
/// <summary>
/// SM4ECB模式加密到Base64格式
/// </summary>
public void Sm4EncryptECBToBase64()
{
    const string data = "Microsoft";
    // 将原文解析到二进制数组格式
    var byte_data = Encoding.UTF8.GetBytes(data);
    // 进制格式密钥加密数据
    var result = Sm4Crypt.EncryptECB("701d1cc0cfbe7ee11824df718855c0c6", true, byte_data);
    // 获取Base64格式的字符串结果
    var base64 = result.ToBase64();
    // ThRruxZZm1GrHE5KkP4UmQ==
}

/// <summary>
/// SM4ECB模式解密Base64格式到字符串
/// </summary>
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
    // Microsoft
}

/// <summary>
/// SM4ECB模式加密到16进制字符串
/// </summary>
public void Sm4EncryptECBToHex16()
{
    const string data = "Microsoft";
    // 将原文解析到二进制数组格式
    var byte_data = Encoding.UTF8.GetBytes(data);
    // 使用16位长度的密钥加密
    var result = Sm4Crypt.EncryptECB("1cc0cfbe7ee11824", false, byte_data);
    // 将结果转为16进制字符串
    var hex = result.ToHexString();
    // D265DF0510C05FE836D3113B3ACEC714
}

/// <summary>
/// SM4ECB模式解密16进制字符串格式密文
/// </summary>
public void Sm4DecryptECBTest2()
{
    const string data = "D265DF0510C05FE836D3113B3ACEC714";
    var byte_data = data.FromHex();
    var result = Sm4Crypt.DecryptECB("1cc0cfbe7ee11824", false, byte_data);
    // 解析结果获取字符串
    var str = Encoding.UTF8.GetString(result);
    // Microsoft
}

/// <summary>
/// SM4CBC模式加密到Base64格式
/// </summary>
public void Sm4EncryptCBCTest()
{
    const string data = "Microsoft";
    var byte_data = Encoding.UTF8.GetBytes(data);
    var result = Sm4Crypt.EncryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", byte_data);
    var base64 = result.ToBase64();
    // Q2iUaMuSHjLvq6GhUQnGTg==
}

/// <summary>
/// SM4CBC模式从Base64解密
/// </summary>
public void Sm4DecryptCBCTest()
{
    const string data = "Q2iUaMuSHjLvq6GhUQnGTg==";
    var byte_data = Convert.FromBase64String(data);
    var result = Sm4Crypt.DecryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", byte_data);
    var str = Encoding.UTF8.GetString(result);
    // Microsoft
}

/// <summary>
/// SM4CBC模式加密到16进制字符串
/// </summary>
public void Sm4EncryptCBCTest2()
{
    const string data = "Microsoft";
    var byte_data = Encoding.UTF8.GetBytes(data);
    var result = Sm4Crypt.EncryptCBC("1cc0cfbe7ee11824", false, "1cc0cfbe7ee12824", byte_data);
    var hex = result.ToHexString();
    // 1BD7A32E49B60B17698AAC9D1E4FEE4A
}

/// <summary>
/// SM4CBC模式从Hex16解密到字符串
/// </summary>
public void Sm4DecryptCBCTest2()
{
    const string data = "1BD7A32E49B60B17698AAC9D1E4FEE4A";
    var byte_data = data.FromHex();
    var result = Sm4Crypt.DecryptCBC("1cc0cfbe7ee11824", false, "1cc0cfbe7ee12824", byte_data);
    var str = Encoding.UTF8.GetString(result);
    // Microsoft
}
```

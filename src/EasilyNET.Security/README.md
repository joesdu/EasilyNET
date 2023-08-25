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

###### RC4

```csharp
/// <summary>
/// RC4
/// </summary>
public void Rc4()
{
    const string data = "Microsoft";
    var key = "123456"u8.ToArray();
    var byte_data = Encoding.UTF8.GetBytes(data);
    var secret = Rc4Crypt.Encrypt(byte_data, key);
    var base64 = secret.ToBase64();
    // TZEdFUtAevoL
    var data_result = Rc4Crypt.Decrypt(secret, key);
    var result = Encoding.UTF8.GetString(data_result);
    // result == data
}
```

###### RSA

```csharp
private const string PublicKey =
    "<RSAKeyValue><Modulus>p0H+4fFMZFIQq6LAhXNf3Zml3imiOCU6mzMiMcqRu/x5x08X3pWF6m+v+1T7fOo4P/2GYluZiuQzKveJnRLxV0O6kRdVivApZ6C/Lb8vnsnocaSlmEOoswdOZvmhe3s4tAQ4KlZerWdRYx3dUgoH862IUv6FNiSyF/z3TP4M50E=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

private const string PrivateKey =
    "<RSAKeyValue><Modulus>p0H+4fFMZFIQq6LAhXNf3Zml3imiOCU6mzMiMcqRu/x5x08X3pWF6m+v+1T7fOo4P/2GYluZiuQzKveJnRLxV0O6kRdVivApZ6C/Lb8vnsnocaSlmEOoswdOZvmhe3s4tAQ4KlZerWdRYx3dUgoH862IUv6FNiSyF/z3TP4M50E=</Modulus><Exponent>AQAB</Exponent><P>2Nhp0/mM3qRkTqzUe5DeI5r5hmDS24XYsSteHKLNuD66sa0K1EFprTstw96+ZJdXW/bJ+R58j78YP1frkLFVpw==</P><Q>xXVq4nyP7UppDXXyKTjWindWmMTuD0v9el6EnyWiqaoaGRF6IAL1nKOcrh5O/BiFgBZm5cTn0knUNE8BzaFI1w==</Q><DP>T4ur3qe3pmC1ryq1U5LD+lm6WTIhh4flArD++BA82O6h+9WXmF9ajcKcujJ2s13VHRY95xXo6a5XDb2J221CFw==</DP><DQ>JpxaJSV0Q1MsjZxFmPfrQ7IoNcE0R7t1OktnJXOHhiOj7Mj1F3NcsZ9wkL+OdE8bM7utrTo+lmknXH8ifCIQiw==</DQ><InverseQ>VvMW94/iyBgKNH8N1Xdikn17BLC0a8tYsm/H/VDMXjaVngAXGqeN370Hd1Zukj0EGVw/vEl2YsAoJ6o9KmQgNA==</InverseQ><D>CkO8lrUuUQHQUBg+5HG+MmVZjpbTg8qVHC05LgEWjIjkGF08Q8a9XzPXgv8mJ/Zf2V1/v82LUMDMexiR83fUI8NxNuZjD+ldR1ZqmtE96+4laA0/WUH0fb1nBA6foVS+WB643nN01dJs0/3IV65k35GfkuqKDct6gld8/UFaN1k=</D></RSAKeyValue>";

private static readonly RsaSecretKey key = new(PrivateKey, PublicKey);

/// <summary>
/// 转换Key,Base64和XML格式互转. Java和其他语言里一般使用base64格式,.Net中一般为XML格式
/// </summary>
public void XmlToBase64Key()
{
    // 私钥转换为Base64格式
    var pri = RsaKeyConverter.FromXmlPrivateKey(PrivateKey);
    // MIICdQIBADANBgkqhkiG9w0BAQEFAASCAl8wggJbAgEAAoGBAKdB/uHxTGRSEKuiwIVzX92Zpd4pojglOpszIjHKkbv8ecdPF96Vhepvr/tU+3zqOD/9hmJbmYrkMyr3iZ0S8VdDupEXVYrwKWegvy2/L57J6HGkpZhDqLMHTmb5oXt7OLQEOCpWXq1nUWMd3VIKB/OtiFL+hTYkshf890z+DOdBAgMBAAECgYAKQ7yWtS5RAdBQGD7kcb4yZVmOltODypUcLTkuARaMiOQYXTxDxr1fM9eC/yYn9l/ZXX+/zYtQwMx7GJHzd9Qjw3E25mMP6V1HVmqa0T3r7iVoDT9ZQfR9vWcEDp+hVL5YHrjec3TV0mzT/chXrmTfkZ+S6ooNy3qCV3z9QVo3WQJBANjYadP5jN6kZE6s1HuQ3iOa+YZg0tuF2LErXhyizbg+urGtCtRBaa07LcPevmSXV1v2yfkefI+/GD9X65CxVacCQQDFdWrifI/tSmkNdfIpONaKd1aYxO4PS/16XoSfJaKpqhoZEXogAvWco5yuHk78GIWAFmblxOfSSdQ0TwHNoUjXAkBPi6vep7emYLWvKrVTksP6WbpZMiGHh+UCsP74EDzY7qH71ZeYX1qNwpy6MnazXdUdFj3nFejprlcNvYnbbUIXAkAmnFolJXRDUyyNnEWY9+tDsig1wTRHu3U6S2clc4eGI6PsyPUXc1yxn3CQv450Txszu62tOj6WaSdcfyJ8IhCLAkBW8xb3j+LIGAo0fw3Vd2KSfXsEsLRry1iyb8f9UMxeNpWeABcap43fvQd3Vm6SPQQZXD+8SXZiwCgnqj0qZCA0
    
    // 公钥转换为Base64格式
    var pub = RsaKeyConverter.FromXmlPublicKey(PublicKey);
    // MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCnQf7h8UxkUhCrosCFc1/dmaXeKaI4JTqbMyIxypG7/HnHTxfelYXqb6/7VPt86jg//YZiW5mK5DMq94mdEvFXQ7qRF1WK8ClnoL8tvy+eyehxpKWYQ6izB05m+aF7ezi0BDgqVl6tZ1FjHd1SCgfzrYhS/oU2JLIX/PdM/gznQQIDAQAB

    // 一样可以将Base64格式转换为XML格式
    var xml_pri = RsaKeyConverter.ToXmlPrivateKey(pri);
    // xml_pri == PrivateKey
    var xml_pub = RsaKeyConverter.ToXmlPublicKey(pub);
    // xml_pub == PublicKey
}

/// <summary>
/// RSA加密解密测试
/// </summary>
public void RsaEncryptAndDecrypt()
{
    const string data = "Microsoft";
    // 将原文解析到二进制数组格式
    var byte_data = Encoding.UTF8.GetBytes(data);
    RsaCrypt.Encrypt(key.PublicKey, byte_data, out var secret_data);
    var secret_str = secret_data.ToBase64();
    Console.WriteLine(secret_str);
    RsaCrypt.Decrypt(key.PrivateKey, secret_str.FromBase64(), out var data_byte);
    var result = Encoding.UTF8.GetString(data_byte);
    // result == data
}
```

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

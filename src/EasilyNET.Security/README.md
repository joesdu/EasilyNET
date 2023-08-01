##### EasilyNET.Security

一个.Net 中常用的加密算法的封装.降低加密解密的使用复杂度.

- 目前有的算法:AES,DES,SHA,RC4,TripleDES,RSA,SM2,SM3,SM4
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
    data.ToSm3String().ToUpper().Should().Be("1749CE3E4EF7622F1EBABB52078EC86309CABD5A6073C8A0711BF35E19BA51B8");
}

/// <summary>
/// SM3测试Base64字符串格式
/// </summary>
public void SM3Base64()
{
    data.ToSm3Base64().ToUpper().Should().Be("F0NOPK73YI8EURTSB47IYWNKVVPGC8IGCRVZXHM6UBG=");
}
```

###### SM4

```csharp
/// <summary>
/// SM4ECB
/// </summary>
public void Sm4EncryptECBTest()
{
    const string data = "Microsoft";
    var result = Sm4Crypt.EncryptECB("701d1cc0cfbe7ee11824df718855c0c6", true, data);
    // 701d1cc0cfbe7ee11824df718855c0c6
}

/// <summary>
/// SM4ECB
/// </summary>
public void Sm4DecryptECBTest()
{
    var data = "ThRruxZZm1GrHE5KkP4UmQ==";
    var result = Sm4Crypt.DecryptECB("701d1cc0cfbe7ee11824df718855c0c6", true, data);
    // Microsoft
}

/// <summary>
/// SM4ECB
/// </summary>
public void Sm4EncryptECBTest2()
{
    const string data = "Microsoft";
    var result = Sm4Crypt.EncryptECB("1cc0cfbe7ee11824", false, data);
    // 0mXfBRDAX+g20xE7Os7HFA==
}

/// <summary>
/// SM4ECB
/// </summary>
public void Sm4DecryptECBTest2()
{
    const string data = "0mXfBRDAX+g20xE7Os7HFA==";
    var result = Sm4Crypt.DecryptECB("1cc0cfbe7ee11824", false, data.Hex16ToBase64());
    // Microsoft
}

/// <summary>
/// SM4CBC
/// </summary>
public void Sm4EncryptCBCTest()
{
    const string data = "Microsoft";
    var result = Sm4Crypt.EncryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", data);
    // 43689468CB921E32EFABA1A15109C64E
}

/// <summary>
/// SM4CBC
/// </summary>
public void Sm4DecryptCBCTest()
{
    const string data = "43689468CB921E32EFABA1A15109C64E";
    var result = Sm4Crypt.DecryptCBC("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", data);
    // Microsoft
}

/// <summary>
/// SM4CBC
/// </summary>
public void Sm4EncryptCBCTest2()
{
    const string data = "Microsoft";
    var result = Sm4Crypt.EncryptCBC("1cc0cfbe7ee11824", false, "1cc0cfbe7ee12824", data);
    // 1BD7A32E49B60B17698AAC9D1E4FEE4A
}

/// <summary>
/// SM4CBC
/// </summary>
public void Sm4DecryptCBCTest2()
{
    const string data = "1BD7A32E49B60B17698AAC9D1E4FEE4A";
    var result = Sm4Crypt.DecryptCBC("1cc0cfbe7ee11824", false, "1cc0cfbe7ee12824", data);
    // Microsoft
}
```

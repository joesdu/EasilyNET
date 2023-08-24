using EasilyNET.Core.Misc;
using EasilyNET.Security;
using FluentAssertions;
using System.Text;

namespace EasilyNET.Test.Unit.Security;

/// <summary>
/// RsaTest
/// </summary>
[TestClass]
public class RsaTest
{
    private const string PublicKey =
        "<RSAKeyValue><Modulus>p0H+4fFMZFIQq6LAhXNf3Zml3imiOCU6mzMiMcqRu/x5x08X3pWF6m+v+1T7fOo4P/2GYluZiuQzKveJnRLxV0O6kRdVivApZ6C/Lb8vnsnocaSlmEOoswdOZvmhe3s4tAQ4KlZerWdRYx3dUgoH862IUv6FNiSyF/z3TP4M50E=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    private const string PrivateKey =
        "<RSAKeyValue><Modulus>p0H+4fFMZFIQq6LAhXNf3Zml3imiOCU6mzMiMcqRu/x5x08X3pWF6m+v+1T7fOo4P/2GYluZiuQzKveJnRLxV0O6kRdVivApZ6C/Lb8vnsnocaSlmEOoswdOZvmhe3s4tAQ4KlZerWdRYx3dUgoH862IUv6FNiSyF/z3TP4M50E=</Modulus><Exponent>AQAB</Exponent><P>2Nhp0/mM3qRkTqzUe5DeI5r5hmDS24XYsSteHKLNuD66sa0K1EFprTstw96+ZJdXW/bJ+R58j78YP1frkLFVpw==</P><Q>xXVq4nyP7UppDXXyKTjWindWmMTuD0v9el6EnyWiqaoaGRF6IAL1nKOcrh5O/BiFgBZm5cTn0knUNE8BzaFI1w==</Q><DP>T4ur3qe3pmC1ryq1U5LD+lm6WTIhh4flArD++BA82O6h+9WXmF9ajcKcujJ2s13VHRY95xXo6a5XDb2J221CFw==</DP><DQ>JpxaJSV0Q1MsjZxFmPfrQ7IoNcE0R7t1OktnJXOHhiOj7Mj1F3NcsZ9wkL+OdE8bM7utrTo+lmknXH8ifCIQiw==</DQ><InverseQ>VvMW94/iyBgKNH8N1Xdikn17BLC0a8tYsm/H/VDMXjaVngAXGqeN370Hd1Zukj0EGVw/vEl2YsAoJ6o9KmQgNA==</InverseQ><D>CkO8lrUuUQHQUBg+5HG+MmVZjpbTg8qVHC05LgEWjIjkGF08Q8a9XzPXgv8mJ/Zf2V1/v82LUMDMexiR83fUI8NxNuZjD+ldR1ZqmtE96+4laA0/WUH0fb1nBA6foVS+WB643nN01dJs0/3IV65k35GfkuqKDct6gld8/UFaN1k=</D></RSAKeyValue>";

    private static readonly RsaSecretKey key = new(PrivateKey, PublicKey);

    /// <summary>
    /// 转换Key
    /// </summary>
    [TestMethod]
    public void XmlToBase64Key()
    {
        var pri = RsaKeyConverter.FromXmlPrivateKey(PrivateKey);
        var pub = RsaKeyConverter.FromXmlPublicKey(PublicKey);
        pri.Should().Be(
            "MIICdQIBADANBgkqhkiG9w0BAQEFAASCAl8wggJbAgEAAoGBAKdB/uHxTGRSEKuiwIVzX92Zpd4pojglOpszIjHKkbv8ecdPF96Vhepvr/tU+3zqOD/9hmJbmYrkMyr3iZ0S8VdDupEXVYrwKWegvy2/L57J6HGkpZhDqLMHTmb5oXt7OLQEOCpWXq1nUWMd3VIKB/OtiFL+hTYkshf890z+DOdBAgMBAAECgYAKQ7yWtS5RAdBQGD7kcb4yZVmOltODypUcLTkuARaMiOQYXTxDxr1fM9eC/yYn9l/ZXX+/zYtQwMx7GJHzd9Qjw3E25mMP6V1HVmqa0T3r7iVoDT9ZQfR9vWcEDp+hVL5YHrjec3TV0mzT/chXrmTfkZ+S6ooNy3qCV3z9QVo3WQJBANjYadP5jN6kZE6s1HuQ3iOa+YZg0tuF2LErXhyizbg+urGtCtRBaa07LcPevmSXV1v2yfkefI+/GD9X65CxVacCQQDFdWrifI/tSmkNdfIpONaKd1aYxO4PS/16XoSfJaKpqhoZEXogAvWco5yuHk78GIWAFmblxOfSSdQ0TwHNoUjXAkBPi6vep7emYLWvKrVTksP6WbpZMiGHh+UCsP74EDzY7qH71ZeYX1qNwpy6MnazXdUdFj3nFejprlcNvYnbbUIXAkAmnFolJXRDUyyNnEWY9+tDsig1wTRHu3U6S2clc4eGI6PsyPUXc1yxn3CQv450Txszu62tOj6WaSdcfyJ8IhCLAkBW8xb3j+LIGAo0fw3Vd2KSfXsEsLRry1iyb8f9UMxeNpWeABcap43fvQd3Vm6SPQQZXD+8SXZiwCgnqj0qZCA0");
        pub.Should()
           .Be("MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCnQf7h8UxkUhCrosCFc1/dmaXeKaI4JTqbMyIxypG7/HnHTxfelYXqb6/7VPt86jg//YZiW5mK5DMq94mdEvFXQ7qRF1WK8ClnoL8tvy+eyehxpKWYQ6izB05m+aF7ezi0BDgqVl6tZ1FjHd1SCgfzrYhS/oU2JLIX/PdM/gznQQIDAQAB");
    }

    /// <summary>
    /// RSA加密解密测试
    /// </summary>
    [TestMethod]
    public void RsaEncryptAndDecrypt()
    {
        const string data = "Microsoft";
        // 将原文解析到二进制数组格式
        var byte_data = Encoding.UTF8.GetBytes(data);
        RsaCrypt.Encrypt(key.PublicKey, byte_data, out var secret_data);
        var secret_str = secret_data.ToBase64();
        Console.WriteLine(secret_str);
        RsaCrypt.Decrypt(key.PrivateKey, secret_str.FromBase64(), out var data_byte);
        Encoding.UTF8.GetString(data_byte).Should().Be(data);
    }
}
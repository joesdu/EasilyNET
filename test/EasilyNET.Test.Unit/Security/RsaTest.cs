using System.Text;
using EasilyNET.Security;
using Shouldly;

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

    private const string base64pri =
        "MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQCSs6J/yem0Wx7RoPCl2iAaBZtwDts5/oB5/wxAOAX0pua8rsK0BIZEoofcpn7TiHLGatAoq1dxrpKChJ7S3fVEaRI0BhPjk/wfiFg90ZW5szdfR2Nzr9Ox2cCBX++fHQWVrR6aYrEyyvWuWAk0X5yDIfXYGQLwvjDVQ9rab43Vdztt4Spd4G0iP6q7ktEu51ovQZmq2vM0rdXLTuNvkBBprrt0W/yeagx3QurpqWA+7C9Ao30NkJYt8XkUpCyAeJKOCOrnxzYQn+gA3pdSlgyMLjs25egNTsjHQ3OqK1y9QmFzfOWWYaIBVCeZfOBq8IgoVeqKDMETmDC8BhXb6EqHAgMBAAECggEAcdrGoCTlo1sgxRMCEcYDKg71/vcYv568uXHvYRvZy3GJHCEJ7Uqhpjz58o6pWaTJZyLY4Odx20HgZTlmRkOLKgfd39BjuTlN8G8SBRBXAqOLsv+luNBaHOrh08bQIw4UGoEcgjdcTQ5ltGSQ6DvYLZG6yndG5+7D2ZBrFyKC0oslHWoXjWRZXXLIa4MxxvAnntrOJGWttM7Io1dRFlUJCF8dqKKi6znU65VMTUmXpFDEZN9OkrxKM0qHt7EmDFVmE14WNawi1noJ20FpW6O4XXX63HtZQTO5UJV9eIDqFr6uO408T0o3bjyTz3wXng27Ua7lx9oajZYHSirrxnjuGQKBgQDC3LbXoN1QIBZfRJzP2u8qdJ1L2271+tOPvweLO/07w8r5EwrzJkT/j3Tyidgu/Jl3XkLeGhQJQmPsnqqhP4AGHjpI30/PRp+cAIK3wxRGS76HZ9PsPXJ4q7EHpBMoPP/E10dRGLC4vfv1FJjh36+mjE3Yw+Fwyce+cSXAv3s/hQKBgQDAuq68Incl9VdX8gNH96CEQBaJLMrT79FgulrM6puQ+weCa593JuVtB/cU1Wv4tlwd2izIUCrQEwECMx7TXbPo5YBCWsGeEeKKEtBjGeMqWYJ4I4j/kaVt2M75qoeqbxfml0ohiWaHKkT5wAkOyP45xxqb8JQztO5vOKZ4zx0RmwKBgFqwaDRAvN9+n4rlHuop5adnsJFOZfz7KJ089eDaIYhAHmX/c9goFnKuLGp4tvFfRHlmmE5P6sVIbcMBMT5slEPEq7GgpL8+CiiLoEqv8u6ob9sK+nl3O6Bnn7ODrBrNEOhmnN1kVVMVsH6mgGSXO2OS5uQcff6FGn5KoJxtQYoVAoGAcTI0RtOHX4gF0OWX/8D1SjfKBK+GQYxtUX6irhBtZm3KL1O+yWDTB4LSIC5pyB5zZCUsgEp1mthOk1grFsHGVVfWSSK87XZbs/Tw6APgZJNGCgH1CQYmP6pDhmgeXn/5bboWFDR7P5AYCwg7Sa/LgWvruQNISEZQdIq1W+dpj1kCgYEAtJq3bQrQpA1DVguQ/myQ42rTyKmaNVp5yt8YbnZbaedU82IV0zw4HK9pp17WL2sE+X3gbvcHGIiE4GMvTE5AIIprSO1vGG8TnUcs03WF9X75aq1eiz5SkBjJEIRlL4JcXAhvoQOMGRh75MJiGLGy5lLVguWlFzPCM/IKKcnryug=";

    private const string base64pub =
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAkrOif8nptFse0aDwpdogGgWbcA7bOf6Aef8MQDgF9KbmvK7CtASGRKKH3KZ+04hyxmrQKKtXca6SgoSe0t31RGkSNAYT45P8H4hYPdGVubM3X0djc6/TsdnAgV/vnx0Fla0emmKxMsr1rlgJNF+cgyH12BkC8L4w1UPa2m+N1Xc7beEqXeBtIj+qu5LRLudaL0GZqtrzNK3Vy07jb5AQaa67dFv8nmoMd0Lq6algPuwvQKN9DZCWLfF5FKQsgHiSjgjq58c2EJ/oAN6XUpYMjC47NuXoDU7Ix0NzqitcvUJhc3zllmGiAVQnmXzgavCIKFXqigzBE5gwvAYV2+hKhwIDAQAB";

    private static readonly RsaSecretKey key = new(PrivateKey, PublicKey);

    /// <summary>
    /// 转换Key
    /// </summary>
    [TestMethod]
    public void XmlToBase64Key()
    {
        var pri = RsaKeyConverter.ToBase64PrivateKey(PrivateKey);
        var pub = RsaKeyConverter.ToBase64PublicKey(PublicKey);
        pri.ShouldBe(
            "MIICdQIBADANBgkqhkiG9w0BAQEFAASCAl8wggJbAgEAAoGBAKdB/uHxTGRSEKuiwIVzX92Zpd4pojglOpszIjHKkbv8ecdPF96Vhepvr/tU+3zqOD/9hmJbmYrkMyr3iZ0S8VdDupEXVYrwKWegvy2/L57J6HGkpZhDqLMHTmb5oXt7OLQEOCpWXq1nUWMd3VIKB/OtiFL+hTYkshf890z+DOdBAgMBAAECgYAKQ7yWtS5RAdBQGD7kcb4yZVmOltODypUcLTkuARaMiOQYXTxDxr1fM9eC/yYn9l/ZXX+/zYtQwMx7GJHzd9Qjw3E25mMP6V1HVmqa0T3r7iVoDT9ZQfR9vWcEDp+hVL5YHrjec3TV0mzT/chXrmTfkZ+S6ooNy3qCV3z9QVo3WQJBANjYadP5jN6kZE6s1HuQ3iOa+YZg0tuF2LErXhyizbg+urGtCtRBaa07LcPevmSXV1v2yfkefI+/GD9X65CxVacCQQDFdWrifI/tSmkNdfIpONaKd1aYxO4PS/16XoSfJaKpqhoZEXogAvWco5yuHk78GIWAFmblxOfSSdQ0TwHNoUjXAkBPi6vep7emYLWvKrVTksP6WbpZMiGHh+UCsP74EDzY7qH71ZeYX1qNwpy6MnazXdUdFj3nFejprlcNvYnbbUIXAkAmnFolJXRDUyyNnEWY9+tDsig1wTRHu3U6S2clc4eGI6PsyPUXc1yxn3CQv450Txszu62tOj6WaSdcfyJ8IhCLAkBW8xb3j+LIGAo0fw3Vd2KSfXsEsLRry1iyb8f9UMxeNpWeABcap43fvQd3Vm6SPQQZXD+8SXZiwCgnqj0qZCA0");
        pub.ShouldBe("MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCnQf7h8UxkUhCrosCFc1/dmaXeKaI4JTqbMyIxypG7/HnHTxfelYXqb6/7VPt86jg//YZiW5mK5DMq94mdEvFXQ7qRF1WK8ClnoL8tvy+eyehxpKWYQ6izB05m+aF7ezi0BDgqVl6tZ1FjHd1SCgfzrYhS/oU2JLIX/PdM/gznQQIDAQAB");
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
        var secret_str = Convert.ToBase64String(secret_data);
        Console.WriteLine(secret_str);
        RsaCrypt.Decrypt(key.PrivateKey, Convert.FromBase64String(secret_str), out var data_byte);
        Encoding.UTF8.GetString(data_byte).ShouldBe(data);
    }

    private static RsaSecretKey GetXmlFromBase64()
    {
        var pri_base64 = RsaKeyConverter.ToXmlPrivateKey(base64pri);
        var pub_base64 = RsaKeyConverter.ToXmlPublicKey(base64pub);
        return new(pri_base64, pub_base64);
    }

    /// <summary>
    /// RSA加密解密测试
    /// </summary>
    [TestMethod]
    public void RsaEncryptAndDecryptBase64Key()
    {
        const string data = "Microsoft";
        // 将原文解析到二进制数组格式
        var byte_data = Encoding.UTF8.GetBytes(data);
        var key_from_base64 = GetXmlFromBase64();
        RsaCrypt.Encrypt(key_from_base64.PublicKey, byte_data, out var secret_data);
        var secret_str = Convert.ToBase64String(secret_data);
        Console.WriteLine(secret_str);
        RsaCrypt.Decrypt(key_from_base64.PrivateKey, Convert.FromBase64String(secret_str), out var data_byte);
        Encoding.UTF8.GetString(data_byte).ShouldBe(data);
    }

    /// <summary>
    /// RsaEncryptAndDecrypt2
    /// </summary>
    [TestMethod]
    public void RsaEncryptAndDecrypt2()
    {
        const string data = "Microsoft";
        var keys = GetXmlFromBase64();
        // 将原文解析到二进制数组格式
        var byte_data = Encoding.UTF8.GetBytes(data);
        var secret_data = RsaCrypt.Encrypt(keys.PublicKey, byte_data);
        var secret_str = Convert.ToBase64String(secret_data);
        Console.WriteLine(secret_str);
        var data_byte = RsaCrypt.Decrypt(keys.PrivateKey, secret_data);
        Encoding.UTF8.GetString(data_byte).ShouldBe(data);
    }
}
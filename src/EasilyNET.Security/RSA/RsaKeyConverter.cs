using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Security.Cryptography;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// RSAKey转化扩展类,用于将XML格式和Base64这种互转.如C#和Java的编码就不一样.
/// </summary>
public static class RsaKeyConverter
{
    /// <summary>
    /// XML私钥 👉 Base64私钥
    /// </summary>
    /// <param name="xmlPrivate">XML私钥</param>
    /// <returns>Base64私钥</returns>
    public static string ToBase64PrivateKey(string xmlPrivate)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPrivate);
        var param = rsa.ExportParameters(true);
        var privateKeyParam = new RsaPrivateCrtKeyParameters(new(1, param.Modulus), new(1, param.Exponent),
            new(1, param.D), new(1, param.P),
            new(1, param.Q), new(1, param.DP),
            new(1, param.DQ), new(1, param.InverseQ));
        var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKeyParam);
        return Convert.ToBase64String(privateKey.ToAsn1Object().GetEncoded());
    }

    /// <summary>
    /// XML公钥 👉 Base64公钥
    /// </summary>
    /// <param name="xmlPublic">XML公钥</param>
    /// <returns>Base64公钥</returns>
    public static string ToBase64PublicKey(string xmlPublic)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPublic);
        var p = rsa.ExportParameters(false);
        var keyParams = new RsaKeyParameters(false, new(1, p.Modulus), new(1, p.Exponent));
        var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyParams);
        return Convert.ToBase64String(publicKeyInfo.ToAsn1Object().GetEncoded());
    }

    /// <summary>
    /// Base64私钥 👉 XML私钥
    /// </summary>
    /// <param name="base64Private">Base64私钥</param>
    /// <returns>XML私钥</returns>
    public static string ToXmlPrivateKey(string base64Private)
    {
        var privateKeyParams = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(base64Private));
        using var rsa = new RSACryptoServiceProvider();
        var rsaParams = new RSAParameters
        {
            Modulus = privateKeyParams.Modulus.ToByteArrayUnsigned(),
            Exponent = privateKeyParams.PublicExponent.ToByteArrayUnsigned(),
            D = privateKeyParams.Exponent.ToByteArrayUnsigned(),
            DP = privateKeyParams.DP.ToByteArrayUnsigned(),
            DQ = privateKeyParams.DQ.ToByteArrayUnsigned(),
            P = privateKeyParams.P.ToByteArrayUnsigned(),
            Q = privateKeyParams.Q.ToByteArrayUnsigned(),
            InverseQ = privateKeyParams.QInv.ToByteArrayUnsigned()
        };
        rsa.ImportParameters(rsaParams);
        return rsa.ToXmlString(true);
    }

    /// <summary>
    /// Base64公钥 👉 XML公钥
    /// </summary>
    /// <param name="base64Public">Base64公钥</param>
    /// <returns>XML公钥</returns>
    public static string ToXmlPublicKey(string base64Public)
    {
        var p = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(base64Public));
        using var rsa = new RSACryptoServiceProvider();
        var rsaParams = new RSAParameters
        {
            Modulus = p.Modulus.ToByteArrayUnsigned(),
            Exponent = p.Exponent.ToByteArrayUnsigned()
        };
        rsa.ImportParameters(rsaParams);
        return rsa.ToXmlString(false);
    }
}
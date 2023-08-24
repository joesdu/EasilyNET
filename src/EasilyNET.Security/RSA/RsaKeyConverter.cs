using EasilyNET.Core.Misc;
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
    /// <param name="xmlPrivateKey">XML私钥</param>
    /// <returns>Base64私钥</returns>
    public static string FromXmlPrivateKey(string xmlPrivateKey)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPrivateKey);
        var param = rsa.ExportParameters(true);
        var privateKeyParam = new RsaPrivateCrtKeyParameters(new(1, param.Modulus), new(1, param.Exponent),
            new(1, param.D), new(1, param.P),
            new(1, param.Q), new(1, param.DP),
            new(1, param.DQ), new(1, param.InverseQ));
        var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKeyParam);
        return privateKey.ToAsn1Object().GetEncoded().ToBase64();
    }

    /// <summary>
    /// XML公钥 👉 Base64公钥
    /// </summary>
    /// <param name="xmlPublicKey">XML公钥</param>
    /// <returns>Base64公钥</returns>
    public static string FromXmlPublicKey(string xmlPublicKey)
    {
        using var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPublicKey);
        var p = rsa.ExportParameters(false);
        var keyParams = new RsaKeyParameters(false, new(1, p.Modulus), new(1, p.Exponent));
        var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyParams);
        return publicKeyInfo.ToAsn1Object().GetEncoded().ToBase64();
    }

    /// <summary>
    /// Base64私钥 👉 XML私钥
    /// </summary>
    /// <param name="privateKey">Base64私钥</param>
    /// <returns>XML私钥</returns>
    public static string ToXmlPrivateKey(string privateKey)
    {
        var privateKeyParams = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(privateKey));
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
    /// <param name="publicKey">Base64公钥字符串</param>
    /// <returns>XML公钥字符串</returns>
    public static string ToXmlPublicKey(string publicKey)
    {
        var p = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
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
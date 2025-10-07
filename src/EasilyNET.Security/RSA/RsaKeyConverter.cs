using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">
///     RSA key conversion extension class, used to convert between XML format and Base64. For example, the encoding of C# and Java
///     is different
///     </para>
///     <para xml:lang="zh">RSAKey转化扩展类,用于将XML格式和Base64这种互转.如C#和Java的编码就不一样</para>
/// </summary>
public static class RsaKeyConverter
{
    /// <summary>
    ///     <para xml:lang="en">XML private key 👉 Base64 private key</para>
    ///     <para xml:lang="zh">XML私钥 👉 Base64私钥</para>
    /// </summary>
    /// <param name="xmlPrivate">
    ///     <para xml:lang="en">XML private key</para>
    ///     <para xml:lang="zh">XML私钥</para>
    /// </param>
    public static string ToBase64PrivateKey(string xmlPrivate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPrivate);
        using var rsa = RSA.Create();
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
    ///     <para xml:lang="en">XML public key 👉 Base64 public key</para>
    ///     <para xml:lang="zh">XML公钥 👉 Base64公钥</para>
    /// </summary>
    /// <param name="xmlPublic">
    ///     <para xml:lang="en">XML public key</para>
    ///     <para xml:lang="zh">XML公钥</para>
    /// </param>
    public static string ToBase64PublicKey(string xmlPublic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPublic);
        using var rsa = RSA.Create();
        rsa.FromXmlString(xmlPublic);
        var p = rsa.ExportParameters(false);
        var keyParams = new RsaKeyParameters(false, new(1, p.Modulus), new(1, p.Exponent));
        var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyParams);
        return Convert.ToBase64String(publicKeyInfo.ToAsn1Object().GetEncoded());
    }

    /// <summary>
    ///     <para xml:lang="en">Base64 private key 👉 XML private key</para>
    ///     <para xml:lang="zh">Base64私钥 👉 XML私钥</para>
    /// </summary>
    /// <param name="base64Private">
    ///     <para xml:lang="en">Base64 private key</para>
    ///     <para xml:lang="zh">Base64私钥</para>
    /// </param>
    public static string ToXmlPrivateKey(string base64Private)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Private);
        var privateKeyParams = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(base64Private));
        using var rsa = RSA.Create();
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
    ///     <para xml:lang="en">Base64 public key 👉 XML public key</para>
    ///     <para xml:lang="zh">Base64公钥 👉 XML公钥</para>
    /// </summary>
    /// <param name="base64Public">
    ///     <para xml:lang="en">Base64 public key</para>
    ///     <para xml:lang="zh">Base64公钥</para>
    /// </param>
    public static string ToXmlPublicKey(string base64Public)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Public);
        var p = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(base64Public));
        using var rsa = RSA.Create();
        var rsaParams = new RSAParameters
        {
            Modulus = p.Modulus.ToByteArrayUnsigned(),
            Exponent = p.Exponent.ToByteArrayUnsigned()
        };
        rsa.ImportParameters(rsaParams);
        return rsa.ToXmlString(false);
    }
}
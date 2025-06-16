using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">BouncyCastle(BC) implementation of SM2 encryption, decryption, signing, and verification</para>
///     <para xml:lang="zh">BouncyCastle(BC) 实现SM2国密加解密、签名、验签</para>
/// </summary>
public static class Sm2Crypt
{
    private static readonly X9ECParameters x9 = GMNamedCurves.GetByName("SM2P256V1");

    /// <summary>
    ///     <para xml:lang="en">Generate public and private keys</para>
    ///     <para xml:lang="zh">构建公钥和私钥</para>
    /// </summary>
    /// <param name="publicKey">
    ///     <para xml:lang="en">Public key</para>
    ///     <para xml:lang="zh">公钥</para>
    /// </param>
    /// <param name="privateKey">
    ///     <para xml:lang="en">Private key</para>
    ///     <para xml:lang="zh">私钥</para>
    /// </param>
    public static void GenerateKey(out byte[] publicKey, out byte[] privateKey)
    {
        var g = new ECKeyPairGenerator();
        g.Init(new ECKeyGenerationParameters(new ECDomainParameters(x9), new()));
        var k = g.GenerateKeyPair();
        publicKey = ((ECPublicKeyParameters)k.Public).Q.GetEncoded(false);
        privateKey = ((ECPrivateKeyParameters)k.Private).D.ToByteArray();
    }

    /// <summary>
    ///     <para xml:lang="en">SM2 encryption, default: C1C3C2</para>
    ///     <para xml:lang="zh">SM2加密,默认:C1C3C2</para>
    /// </summary>
    /// <param name="publicKey">
    ///     <para xml:lang="en">Public key</para>
    ///     <para xml:lang="zh">公钥</para>
    /// </param>
    /// <param name="data">
    ///     <para xml:lang="en">Data to be encrypted</para>
    ///     <para xml:lang="zh">需要加密的数据</para>
    /// </param>
    /// <param name="model">
    ///     <para xml:lang="en">Mode</para>
    ///     <para xml:lang="zh">模式</para>
    /// </param>
    /// <param name="userId">
    ///     <para xml:lang="en">User ID</para>
    ///     <para xml:lang="zh">用户ID</para>
    /// </param>
    public static byte[] Encrypt(byte[] publicKey, byte[] data, byte[]? userId = null, Mode model = Mode.C1C3C2)
    {
        var sm2 = new SM2Engine(new SM3Digest(), Mode.C1C3C2);
        ICipherParameters cp = new ParametersWithRandom(new ECPublicKeyParameters(x9.Curve.DecodePoint(publicKey), new(x9)));
        if (userId is not null)
        {
            cp = new ParametersWithID(cp, userId);
        }
        sm2.Init(true, cp);
        data = sm2.ProcessBlock(data, 0, data.Length);
        if (model == Mode.C1C2C3)
        {
            data = C132ToC123(data);
        }
        return data;
    }

    /// <summary>
    ///     <para xml:lang="en">SM2 decryption, default: C1C3C2</para>
    ///     <para xml:lang="zh">SM2解密,默认:C1C3C2</para>
    /// </summary>
    /// <param name="privateKey">
    ///     <para xml:lang="en">Private key</para>
    ///     <para xml:lang="zh">私钥</para>
    /// </param>
    /// <param name="data">
    ///     <para xml:lang="en">Data to be decrypted</para>
    ///     <para xml:lang="zh">需要解密的数据</para>
    /// </param>
    /// <param name="model">
    ///     <para xml:lang="en">Mode</para>
    ///     <para xml:lang="zh">模式</para>
    /// </param>
    /// <param name="userId">
    ///     <para xml:lang="en">User ID</para>
    ///     <para xml:lang="zh">用户ID</para>
    /// </param>
    public static byte[] Decrypt(byte[] privateKey, byte[] data, byte[]? userId = null, Mode model = Mode.C1C3C2)
    {
        if (model == Mode.C1C2C3)
        {
            data = C123ToC132(data);
        }
        var sm2 = new SM2Engine(new SM3Digest(), Mode.C1C3C2);
        ICipherParameters cp = new ECPrivateKeyParameters(new(1, privateKey), new(x9));
        if (userId is not null)
        {
            cp = new ParametersWithID(cp, userId);
        }
        sm2.Init(false, cp);
        return sm2.ProcessBlock(data, 0, data.Length);
    }

    /// <summary>
    ///     <para xml:lang="en">SM2 signature</para>
    ///     <para xml:lang="zh">SM2签名</para>
    /// </summary>
    /// <param name="privateKey">
    ///     <para xml:lang="en">Private key</para>
    ///     <para xml:lang="zh">私钥</para>
    /// </param>
    /// <param name="msg">
    ///     <para xml:lang="en">Data</para>
    ///     <para xml:lang="zh">数据</para>
    /// </param>
    /// <param name="userId">
    ///     <para xml:lang="en">User ID</para>
    ///     <para xml:lang="zh">用户ID</para>
    /// </param>
    public static byte[] Signature(byte[] privateKey, byte[] msg, byte[]? userId = null)
    {
        var sm2 = new SM2Signer(new SM3Digest());
        // var sm2 = new SM2Signer(StandardDsaEncoding.Instance, new SM3Digest());
        // var sm2 = new SM2Signer(PlainDsaEncoding.Instance, new SM3Digest());
        ICipherParameters cp = new ParametersWithRandom(new ECPrivateKeyParameters(new(1, privateKey), new(x9)));
        if (userId is not null)
        {
            cp = new ParametersWithID(cp, userId);
        }
        sm2.Init(true, cp);
        sm2.BlockUpdate(msg, 0, msg.Length);
        return sm2.GenerateSignature();
    }

    /// <summary>
    ///     <para xml:lang="en">SM2 verification</para>
    ///     <para xml:lang="zh">SM2验签</para>
    /// </summary>
    /// <param name="publicKey">
    ///     <para xml:lang="en">Public key</para>
    ///     <para xml:lang="zh">公钥</para>
    /// </param>
    /// <param name="msg">
    ///     <para xml:lang="en">Data</para>
    ///     <para xml:lang="zh">数据</para>
    /// </param>
    /// <param name="signature">
    ///     <para xml:lang="en">Signature data</para>
    ///     <para xml:lang="zh">签名数据</para>
    /// </param>
    /// <param name="userId">
    ///     <para xml:lang="en">User ID</para>
    ///     <para xml:lang="zh">用户ID</para>
    /// </param>
    public static bool Verify(byte[] publicKey, byte[] msg, byte[] signature, byte[]? userId = null)
    {
        var sm2 = new SM2Signer(new SM3Digest());
        ICipherParameters cp = new ECPublicKeyParameters(x9.Curve.DecodePoint(publicKey), new(x9));
        if (userId is not null)
        {
            cp = new ParametersWithID(cp, userId);
        }
        sm2.Init(false, cp);
        sm2.BlockUpdate(msg, 0, msg.Length);
        return sm2.VerifySignature(signature);
    }

    /// <summary>
    ///     <para xml:lang="en">Convert C1C2C3 to C1C3C2</para>
    ///     <para xml:lang="zh">C1C2C3转成C1C3C2</para>
    /// </summary>
    /// <param name="c1c2c3">
    ///     <para xml:lang="en">Input data in C1C2C3 format</para>
    ///     <para xml:lang="zh">C1C2C3格式的输入数据</para>
    /// </param>
    private static byte[] C123ToC132(ReadOnlySpan<byte> c1c2c3)
    {
        var c1Len = ((x9.Curve.FieldSize + 7) >> 3 << 1) + 1; //sm2p256v1的这个固定65。可看GMNamedCurves、ECCurve代码。
        const int c3Len = 32;
        var result = new byte[c1c2c3.Length];
        c1c2c3[..c1Len].CopyTo(result);
        c1c2c3[^c3Len..].CopyTo(result.AsSpan(c1Len));
        c1c2c3[c1Len..^c3Len].CopyTo(result.AsSpan(c1Len + c3Len));
        return result;
    }

    /// <summary>
    ///     <para xml:lang="en">Convert C1C3C2 to C1C2C3</para>
    ///     <para xml:lang="zh">C1C3C2转成C1C2C3</para>
    /// </summary>
    /// <param name="c1c3c2">
    ///     <para xml:lang="en">Input data in C1C3C2 format</para>
    ///     <para xml:lang="zh">C1C3C2格式的输入数据</para>
    /// </param>
    private static byte[] C132ToC123(ReadOnlySpan<byte> c1c3c2)
    {
        var c1Len = ((x9.Curve.FieldSize + 7) >> 3 << 1) + 1;
        const int c3Len = 32;
        var result = new byte[c1c3c2.Length];
        c1c3c2[..c1Len].CopyTo(result);
        c1c3c2[(c1Len + c3Len)..].CopyTo(result.AsSpan(c1Len));
        c1c3c2[c1Len..(c1Len + c3Len)].CopyTo(result.AsSpan(^c3Len));
        return result;
    }
}
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Security;

/// <summary>
/// BouncyCastle(BC) 实现SM2国密加解密、签名、验签
/// </summary>
public static class Sm2Crypt
{
    private static readonly X9ECParameters x9 = GMNamedCurves.GetByName("SM2P256V1");

    /// <summary>
    /// 构建公钥和私钥
    /// </summary>
    /// <param name="publicKey"></param>
    /// <param name="privateKey"></param>
    public static void GenerateKey(out byte[] publicKey, out byte[] privateKey)
    {
        var g = new ECKeyPairGenerator();
        g.Init(new ECKeyGenerationParameters(new ECDomainParameters(x9), new()));
        var k = g.GenerateKeyPair();
        publicKey = ((ECPublicKeyParameters)k.Public).Q.GetEncoded(false);
        privateKey = ((ECPrivateKeyParameters)k.Private).D.ToByteArray();
    }

    /// <summary>
    /// SM2加密
    /// </summary>
    /// <param name="publicKey">公钥</param>
    /// <param name="data">需要加密的数据</param>
    /// <param name="model">模式</param>
    /// <returns></returns>
    public static byte[] Encrypt(byte[] publicKey, byte[] data, Sm2Model model)
    {
        var sm2 = new SM2Engine(new SM3Digest());
        var cp = new ParametersWithRandom(new ECPublicKeyParameters(x9.Curve.DecodePoint(publicKey), new(x9)));
        sm2.Init(true, cp);
        data = sm2.ProcessBlock(data, 0, data.Length);
        if (model == Sm2Model.C1C3C2) data = C123ToC132(data);
        return data;
    }

    /// <summary>
    /// SM2解密
    /// </summary>
    /// <param name="privateKey">私钥</param>
    /// <param name="data">需要解密的数据</param>
    /// <param name="model">模式</param>
    /// <returns></returns>
    public static byte[] Decrypt(byte[] privateKey, byte[] data, Sm2Model model)
    {
        if (model == Sm2Model.C1C3C2) data = C132ToC123(data);
        var sm2 = new SM2Engine(new SM3Digest());
        var cp = new ECPrivateKeyParameters(new(1, privateKey), new(x9));
        sm2.Init(false, cp);
        return sm2.ProcessBlock(data, 0, data.Length);
    }

    /// <summary>
    /// SM2签名
    /// </summary>
    /// <param name="privateKey">私钥</param>
    /// <param name="msg">数据</param>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public static byte[] Signature(byte[] privateKey, byte[] msg, byte[]? userId = null)
    {
        var sm2 = new SM2Signer(StandardDsaEncoding.Instance, new SM3Digest());
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
    /// SM2验签
    /// </summary>
    /// <param name="publicKey">公钥</param>
    /// <param name="msg">数据</param>
    /// <param name="signature">签名数据</param>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public static bool Verify(byte[] publicKey, byte[] msg, byte[] signature, byte[]? userId = null)
    {
        var sm2 = new SM2Signer(StandardDsaEncoding.Instance, new SM3Digest());
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
    /// C123转成C132
    /// </summary>
    /// <param name="c1c2c3"></param>
    /// <returns></returns>
    private static byte[] C123ToC132(byte[] c1c2c3)
    {
        var c1Len = (((x9.Curve.FieldSize + 7) >> 3) << 1) + 1; //sm2p256v1的这个固定65。可看GMNamedCurves、ECCurve代码。
        const int c3Len = 32;
        var result = new byte[c1c2c3.Length];
        Array.Copy(c1c2c3, 0, result, 0, c1Len);                                         //c1
        Array.Copy(c1c2c3, c1c2c3.Length - c3Len, result, c1Len, c3Len);                 //c3
        Array.Copy(c1c2c3, c1Len, result, c1Len + c3Len, c1c2c3.Length - c1Len - c3Len); //c2
        return result;
    }

    /// <summary>
    /// C132转成C123
    /// </summary>
    /// <param name="c1c3c2"></param>
    /// <returns></returns>
    private static byte[] C132ToC123(byte[] c1c3c2)
    {
        var c1Len = (((x9.Curve.FieldSize + 7) >> 3) << 1) + 1;
        const int c3Len = 32;
        var result = new byte[c1c3c2.Length];
        Array.Copy(c1c3c2, 0, result, 0, c1Len);                                         //c1: 0->65
        Array.Copy(c1c3c2, c1Len + c3Len, result, c1Len, c1c3c2.Length - c1Len - c3Len); //c2
        Array.Copy(c1c3c2, c1Len, result, c1c3c2.Length - c3Len, c3Len);                 //c3
        return result;
    }
}
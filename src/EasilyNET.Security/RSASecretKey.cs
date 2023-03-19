// ReSharper disable UnusedMember.Global

namespace Hoyo.Security;

/// <summary>
/// RSA密钥
/// </summary>
public struct RSASecretKey
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="privateKey"></param>
    /// <param name="publicKey"></param>
    public RSASecretKey(string privateKey, string publicKey)
    {
        PrivateKey = privateKey;
        PublicKey = publicKey;
    }

    /// <summary>
    /// 公钥
    /// </summary>
    public string PublicKey { get; set; }

    /// <summary>
    /// 私钥
    /// </summary>
    public string PrivateKey { get; set; }

    /// <summary>
    /// ToSting
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"""
        PrivateKey: {PrivateKey}
        PublicKey: {PublicKey}
        """;
}
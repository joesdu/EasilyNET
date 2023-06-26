// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// RSA密钥
/// </summary>
/// <param name="privateKey"></param>
/// <param name="publicKey"></param>
public struct RSASecretKey(string privateKey, string publicKey)
{

    /// <summary>
    /// 公钥
    /// </summary>
    public string PublicKey { get; set; } = publicKey;

    /// <summary>
    /// 私钥
    /// </summary>
    public string PrivateKey { get; set; } = privateKey;

    /// <summary>
    /// ToSting
    /// </summary>
    /// <returns></returns>
    public readonly override string ToString() =>
        $"""
        PrivateKey: {PrivateKey}
        PublicKey: {PublicKey}
        """;
}
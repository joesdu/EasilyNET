// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
/// RSA密钥
/// </summary>
/// <param name="privateKey"></param>
/// <param name="publicKey"></param>
public struct RsaSecretKey(string privateKey, string publicKey)
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
    /// 得到XML格式的私钥和公钥
    /// </summary>
    /// <returns></returns>
    public readonly string ToXmlString() =>
        $"""
         -----BEGIN RSA XML PRIVATE KEY-----
         {PrivateKey}
         -----END RSA XML PRIVATE KEY-----

         -----BEGIN RSA XML PUBLIC KEY-----
         {PublicKey}
         -----END RSA XML PUBLIC KEY-----
         """;

    /// <summary>
    /// 得到Base64格式的私钥和公钥
    /// </summary>
    /// <returns></returns>
    public readonly string ToBase64String() =>
        $"""
         -----BEGIN RSA BASE64 PRIVATE KEY-----
         {RsaKeyConverter.ToBase64PrivateKey(PrivateKey)}
         -----END RSA BASE64 PRIVATE KEY-----

         -----BEGIN RSA BASE64 PUBLIC KEY-----
         {RsaKeyConverter.ToBase64PublicKey(PublicKey)}
         -----END RSA BASE64 PUBLIC KEY-----
         """;
}
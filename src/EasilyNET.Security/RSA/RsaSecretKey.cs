// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">RSA secret key</para>
///     <para xml:lang="zh">RSA密钥</para>
/// </summary>
/// <param name="privateKey">
///     <para xml:lang="en">Private key</para>
///     <para xml:lang="zh">私钥</para>
/// </param>
/// <param name="publicKey">
///     <para xml:lang="en">Public key</para>
///     <para xml:lang="zh">公钥</para>
/// </param>
public struct RsaSecretKey(string privateKey, string publicKey)
{
    /// <summary>
    ///     <para xml:lang="en">Public key</para>
    ///     <para xml:lang="zh">公钥</para>
    /// </summary>
    public string PublicKey { get; set; } = publicKey;

    /// <summary>
    ///     <para xml:lang="en">Private key</para>
    ///     <para xml:lang="zh">私钥</para>
    /// </summary>
    public string PrivateKey { get; set; } = privateKey;

    /// <summary>
    ///     <para xml:lang="en">Gets the private and public keys in XML format</para>
    ///     <para xml:lang="zh">得到XML格式的私钥和公钥</para>
    /// </summary>
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
    ///     <para xml:lang="en">Gets the private and public keys in Base64 format</para>
    ///     <para xml:lang="zh">得到Base64格式的私钥和公钥</para>
    /// </summary>
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
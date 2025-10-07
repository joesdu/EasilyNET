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
public readonly struct RsaSecretKey(string privateKey, string publicKey)
{
    /// <summary>
    ///     <para xml:lang="en">Public key</para>
    ///     <para xml:lang="zh">公钥</para>
    /// </summary>
    public string PublicKey { get; init; } = publicKey;

    /// <summary>
    ///     <para xml:lang="en">Private key</para>
    ///     <para xml:lang="zh">私钥</para>
    /// </summary>
    public string PrivateKey { get; init; } = privateKey;

    /// <summary>
    ///     <para xml:lang="en">Gets the private and public keys in XML format</para>
    ///     <para xml:lang="zh">得到XML格式的私钥和公钥</para>
    /// </summary>
    public string ToXmlString() =>
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
    public string ToBase64String() =>
        $"""
         -----BEGIN RSA BASE64 PRIVATE KEY-----
         {RsaKeyConverter.ToBase64PrivateKey(PrivateKey)}
         -----END RSA BASE64 PRIVATE KEY-----

         -----BEGIN RSA BASE64 PUBLIC KEY-----
         {RsaKeyConverter.ToBase64PublicKey(PublicKey)}
         -----END RSA BASE64 PUBLIC KEY-----
         """;

    /// <summary>
    ///     <para xml:lang="en">Gets the private and public keys in PEM format</para>
    ///     <para xml:lang="zh">得到PEM格式的私钥和公钥</para>
    /// </summary>
    public string ToPemString() =>
        $"""
         -----BEGIN RSA PEM PRIVATE KEY-----
         {RsaCrypt.ExportPrivateKeyToPem(PrivateKey)}
         -----END RSA PEM PRIVATE KEY-----
         
         -----BEGIN RSA PEM PUBLIC KEY-----
         {RsaCrypt.ExportPublicKeyToPem(PublicKey)}
         -----END RSA PEM PUBLIC KEY-----
         """;

    /// <summary>
    ///     <para xml:lang="en">Gets only the private key in PEM format</para>
    ///     <para xml:lang="zh">仅获取PEM格式的私钥</para>
    /// </summary>
    public string GetPrivateKeyPem() => RsaCrypt.ExportPrivateKeyToPem(PrivateKey);

    /// <summary>
    ///     <para xml:lang="en">Gets only the public key in PEM format</para>
    ///     <para xml:lang="zh">仅获取PEM格式的公钥</para>
    /// </summary>
    public string GetPublicKeyPem() => RsaCrypt.ExportPublicKeyToPem(PublicKey);

    /// <summary>
    ///     <para xml:lang="en">Create RsaSecretKey from PEM format keys</para>
    ///     <para xml:lang="zh">从PEM格式密钥创建RsaSecretKey</para>
    /// </summary>
    /// <param name="pemPrivateKey">
    ///     <para xml:lang="en">PEM format private key</para>
    ///     <para xml:lang="zh">PEM格式私钥</para>
    /// </param>
    /// <param name="pemPublicKey">
    ///     <para xml:lang="en">PEM format public key</para>
    ///     <para xml:lang="zh">PEM格式公钥</para>
    /// </param>
    public static RsaSecretKey FromPem(string pemPrivateKey, string pemPublicKey)
    {
        return new RsaSecretKey(RsaCrypt.ImportPrivateKeyFromPem(pemPrivateKey), RsaCrypt.ImportPublicKeyFromPem(pemPublicKey));
    }
}
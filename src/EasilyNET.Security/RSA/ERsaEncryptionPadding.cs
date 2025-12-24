// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">RSA encryption padding mode</para>
///     <para xml:lang="zh">RSA加密填充模式</para>
/// </summary>
public enum ERsaEncryptionPadding
{
    /// <summary>
    ///     <para xml:lang="en">PKCS#1 v1.5 padding (compatible with older systems, less secure)</para>
    ///     <para xml:lang="zh">PKCS#1 v1.5 填充(兼容旧系统,安全性较低)</para>
    /// </summary>
    Pkcs1,

    /// <summary>
    ///     <para xml:lang="en">OAEP padding with SHA1 (for compatibility with some third-party systems)</para>
    ///     <para xml:lang="zh">OAEP填充使用SHA1(用于兼容某些第三方系统)</para>
    /// </summary>
    OaepSHA1,

    /// <summary>
    ///     <para xml:lang="en">OAEP padding with SHA256 (recommended, more secure)</para>
    ///     <para xml:lang="zh">OAEP填充使用SHA256(推荐使用,更安全)</para>
    /// </summary>
    OaepSHA256,

    /// <summary>
    ///     <para xml:lang="en">OAEP padding with SHA384</para>
    ///     <para xml:lang="zh">OAEP填充使用SHA384</para>
    /// </summary>
    OaepSHA384,

    /// <summary>
    ///     <para xml:lang="en">OAEP padding with SHA512</para>
    ///     <para xml:lang="zh">OAEP填充使用SHA512</para>
    /// </summary>
    OaepSHA512
}
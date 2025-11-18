// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">RSA signature padding mode</para>
///     <para xml:lang="zh">RSA签名填充模式</para>
/// </summary>
public enum ERsaSignaturePadding
{
    /// <summary>
    ///     <para xml:lang="en">PKCS#1 v1.5 padding (widely compatible)</para>
    ///     <para xml:lang="zh">PKCS#1 v1.5 填充(广泛兼容)</para>
    /// </summary>
    Pkcs1,

    /// <summary>
    ///     <para xml:lang="en">PSS padding (Probabilistic Signature Scheme, more secure)</para>
    ///     <para xml:lang="zh">PSS填充(概率签名方案,更安全)</para>
    /// </summary>
    Pss
}

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">Key length</para>
///     <para xml:lang="zh">密钥长度</para>
/// </summary>
public enum ERsaKeyLength
{
    /// <summary>
    ///     <para xml:lang="en">512 bit (Obsolete: Not secure, use at least 2048 bits)</para>
    ///     <para xml:lang="zh">512 位 (已过时:不安全,请至少使用2048位)</para>
    /// </summary>
    [Obsolete("512位密钥不安全,建议至少使用2048位")]
    Bit512 = 512,

    /// <summary>
    ///     <para xml:lang="en">1024 bit (Obsolete: Not secure, use at least 2048 bits)</para>
    ///     <para xml:lang="zh">1024 位 (已过时:不安全,请至少使用2048位)</para>
    /// </summary>
    [Obsolete("1024位密钥不安全,建议至少使用2048位")]
    Bit1024 = 1024,

    /// <summary>
    ///     <para xml:lang="en">2048 bit (Recommended minimum)</para>
    ///     <para xml:lang="zh">2048 位 (推荐最小值)</para>
    /// </summary>
    Bit2048 = 2048,

    /// <summary>
    ///     <para xml:lang="en">4096 bit (High security)</para>
    ///     <para xml:lang="zh">4096 位 (高安全性)</para>
    /// </summary>
    Bit4096 = 4096,

    /// <summary>
    ///     <para xml:lang="en">8192 bit (Very high security, may be slow)</para>
    ///     <para xml:lang="zh">8192 位 (非常高的安全性,可能较慢)</para>
    /// </summary>
    Bit8192 = 8192
}
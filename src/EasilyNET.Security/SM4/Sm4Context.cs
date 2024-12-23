namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">Sm4Context</para>
///     <para xml:lang="zh">Sm4Context</para>
/// </summary>
internal sealed class Sm4Context
{
    /// <summary>
    ///     <para xml:lang="en">Whether to pad the hexadecimal string</para>
    ///     <para xml:lang="zh">是否补足16进制字符串</para>
    /// </summary>
    internal bool IsPadding { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Encrypt or decrypt</para>
    ///     <para xml:lang="zh">加密或者解密</para>
    /// </summary>
    internal ESm4Model Mode { get; set; } = ESm4Model.Encrypt;

    /// <summary>
    ///     <para xml:lang="en">Key</para>
    ///     <para xml:lang="zh">密钥</para>
    /// </summary>
    internal long[] Key { get; } = new long[32];
}
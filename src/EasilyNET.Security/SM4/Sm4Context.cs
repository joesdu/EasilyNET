namespace EasilyNET.Security;

/// <summary>
/// Sm4Context
/// </summary>
internal sealed class Sm4Context
{
    /// <summary>
    /// 是否补足16进制字符串
    /// </summary>
    internal bool IsPadding { get; set; } = true;

    /// <summary>
    /// 加密或者解密
    /// </summary>
    internal ESm4Model Mode { get; set; } = ESm4Model.Encrypt;

    /// <summary>
    /// 密钥
    /// </summary>
    internal long[] Key { get; } = new long[32];
}
namespace EasilyNET.Security;

/// <summary>
/// Sm4Context
/// </summary>
internal sealed class Sm4Context
{
    /// <summary>
    /// 是否补足16进制字符串
    /// </summary>
    public bool IsPadding { get; set; } = true;

    /// <summary>
    /// 加密或者解密
    /// </summary>
    public ESm4Model Mode { get; set; } = ESm4Model.加密;

    /// <summary>
    /// 密钥
    /// </summary>
    public long[] Key { get; } = new long[32];
}
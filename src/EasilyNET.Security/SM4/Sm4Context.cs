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
    /// 1表示加密，0表示解密
    /// </summary>
    public int Mode { get; set; } = 1;

    /// <summary>
    /// 密钥
    /// </summary>
    public long[] Key { get; } = new long[32];
}
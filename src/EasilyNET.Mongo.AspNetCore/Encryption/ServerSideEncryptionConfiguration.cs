namespace EasilyNET.Mongo.AspNetCore.Encryption;

/// <summary>
///     <para xml:lang="en">Server-Side Encryption Configuration</para>
///     <para xml:lang="zh">服务器端加密配置</para>
/// </summary>
public class ServerSideEncryptionConfiguration
{
    /// <summary>
    ///     <para xml:lang="en">Whether SSE is enabled</para>
    ///     <para xml:lang="zh">是否启用SSE</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Encryption algorithm</para>
    ///     <para xml:lang="zh">加密算法</para>
    /// </summary>
    public string Algorithm { get; set; } = "AES256";

    /// <summary>
    ///     <para xml:lang="en">KMS Key ID (for future use)</para>
    ///     <para xml:lang="zh">KMS密钥ID（未来使用）</para>
    /// </summary>
    public string? KmsKeyId { get; set; }
}
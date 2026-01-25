// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.Mongo.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">Upload validation options</para>
///     <para xml:lang="zh">上传验证选项</para>
/// </summary>
public sealed class UploadValidationOptions
{
    /// <summary>
    ///     <para xml:lang="en">Maximum allowed file size in bytes (default: 2GB)</para>
    ///     <para xml:lang="zh">允许的最大文件大小(字节)(默认: 2GB)</para>
    /// </summary>
    public long MaxFileSize { get; set; } = 2L * 1024 * 1024 * 1024;

    /// <summary>
    ///     <para xml:lang="en">Enable magic number validation (default: true)</para>
    ///     <para xml:lang="zh">启用魔数验证(默认: true)</para>
    /// </summary>
    public bool EnableMagicNumberValidation { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Allowed file extensions (empty means allow all)</para>
    ///     <para xml:lang="zh">允许的文件扩展名(空集合表示全部允许)</para>
    /// </summary>
    public HashSet<string> AllowedExtensions { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Allowed content types (empty means allow all)</para>
    ///     <para xml:lang="zh">允许的内容类型(空集合表示全部允许)</para>
    /// </summary>
    public HashSet<string> AllowedContentTypes { get; } = [];
}
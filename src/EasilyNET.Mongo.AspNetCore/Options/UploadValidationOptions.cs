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
    public HashSet<string> AllowedExtensions { get; } =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".pdf", ".tif", ".tiff",
        ".mp3", ".wav", ".flac", ".mp4", ".m4v", ".mov", ".avi", ".mkv", ".webm", ".flv",
        ".7z", ".rar", ".docx", ".xlsx", ".pptx", ".apk", ".jar", ".war", ".epub",
        ".mobi", ".zip", ".gz", ".tar", ".iso", ".dmg", ".heic", ".heif", ".svg", ".psd",
        ".ai", ".dwg", ".dxf", ".ogg", ".oga", ".opus", ".aac", ".exe", ".dll", ".msi",
        ".cab", ".elf", ".macho", ".wasm", ".txt", ".csv"
    ];

    /// <summary>
    ///     <para xml:lang="en">Allowed content types (empty means allow all)</para>
    ///     <para xml:lang="zh">允许的内容类型(空集合表示全部允许)</para>
    /// </summary>
    public HashSet<string> AllowedContentTypes { get; } =
    [
        "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp", "image/tiff",
        "application/pdf", "audio/mpeg", "audio/wav", "audio/flac", "video/mp4", "video/x-m4v",
        "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm", "video/x-flv",
        "application/x-7z-compressed", "application/vnd.rar",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.android.package-archive", "application/java-archive", "application/epub+zip",
        "application/x-mobipocket-ebook", "application/zip", "application/gzip", "application/x-tar",
        "application/x-iso9660-image", "application/x-apple-diskimage", "image/heic", "image/heif",
        "image/svg+xml", "image/vnd.adobe.photoshop", "application/postscript", "image/vnd.dwg",
        "image/vnd.dxf", "audio/ogg", "audio/opus", "audio/aac",
        "application/vnd.microsoft.portable-executable", "application/x-msi", "application/vnd.ms-cab-compressed",
        "application/x-elf", "application/x-mach-binary", "application/wasm", "text/plain", "text/csv"
    ];
}
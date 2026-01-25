using EasilyNET.Mongo.AspNetCore.Abstraction;
using EasilyNET.Mongo.AspNetCore.Common;
using EasilyNET.Mongo.AspNetCore.Models;
using EasilyNET.Mongo.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">Default upload validator for GridFS uploads</para>
///     <para xml:lang="zh">GridFS 上传默认验证器</para>
/// </summary>
internal sealed class DefaultUploadValidator(IOptions<UploadValidationOptions> options) : IUploadValidator
{
    private static readonly Dictionary<string, List<byte[]>> MagicNumberSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] =
        [
            [0xFF, 0xD8, 0xFF]
        ],
        [".jpeg"] =
        [
            [0xFF, 0xD8, 0xFF]
        ],
        [".png"] =
        [
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
        ],
        [".gif"] =
        [
            "GIF87a"u8.ToArray(),
            "GIF89a"u8.ToArray()
        ],
        [".bmp"] =
        [
            "BM"u8.ToArray()
        ],
        [".webp"] =
        [
            "RIFF"u8.ToArray()
        ],
        [".pdf"] =
        [
            "%PDF-"u8.ToArray()
        ],
        [".tif"] =
        [
            "II*\0"u8.ToArray(),
            "MM\0*"u8.ToArray()
        ],
        [".tiff"] =
        [
            "II*\0"u8.ToArray(),
            "MM\0*"u8.ToArray()
        ],
        [".mp3"] =
        [
            "ID3"u8.ToArray(),
            [0xFF, 0xFB]
        ],
        [".wav"] =
        [
            "RIFF"u8.ToArray()
        ],
        [".flac"] =
        [
            "fLaC"u8.ToArray()
        ],
        [".mp4"] =
        [
            [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70],
            "\0\0\0 ftyp"u8.ToArray()
        ],
        [".m4v"] =
        [
            [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70],
            "\0\0\0 ftyp"u8.ToArray()
        ],
        [".mov"] =
        [
            [0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74, 0x20, 0x20]
        ],
        [".avi"] =
        [
            "RIFF"u8.ToArray()
        ],
        [".mkv"] =
        [
            [0x1A, 0x45, 0xDF, 0xA3]
        ],
        [".webm"] =
        [
            [0x1A, 0x45, 0xDF, 0xA3]
        ],
        [".flv"] =
        [
            "FLV"u8.ToArray()
        ],
        [".7z"] =
        [
            [0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C]
        ],
        [".rar"] =
        [
            [0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00],
            [0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00]
        ],
        [".docx"] =
        [
            [0x50, 0x4B, 0x03, 0x04]
        ],
        [".xlsx"] =
        [
            [0x50, 0x4B, 0x03, 0x04]
        ],
        [".pptx"] =
        [
            [0x50, 0x4B, 0x03, 0x04]
        ],
        [".apk"] =
        [
            [0x50, 0x4B, 0x03, 0x04]
        ],
        [".jar"] =
        [
            [0x50, 0x4B, 0x03, 0x04]
        ],
        [".war"] =
        [
            [0x50, 0x4B, 0x03, 0x04]
        ],
        [".epub"] =
        [
            [0x50, 0x4B, 0x03, 0x04]
        ],
        [".mobi"] =
        [
            "BOOKMOBI"u8.ToArray()
        ],
        [".zip"] =
        [
            [0x50, 0x4B, 0x03, 0x04],
            [0x50, 0x4B, 0x05, 0x06],
            [0x50, 0x4B, 0x07, 0x08]
        ],
        [".gz"] =
        [
            [0x1F, 0x8B]
        ],
        [".tar"] =
        [
            "ustar"u8.ToArray()
        ],
        [".iso"] =
        [
            "CD001"u8.ToArray()
        ],
        [".dmg"] =
        [
            [0x78, 0x01, 0x73, 0x0D, 0x62, 0x62, 0x60]
        ],
        [".heic"] =
        [
            "ftypheic"u8.ToArray(),
            "ftypheix"u8.ToArray(),
            "ftypmif1"u8.ToArray(),
            "ftypmsf1"u8.ToArray(),
            "ftyphevc"u8.ToArray()
        ],
        [".heif"] =
        [
            "ftypheic"u8.ToArray(),
            "ftypheix"u8.ToArray(),
            "ftypmif1"u8.ToArray(),
            "ftypmsf1"u8.ToArray(),
            "ftyphevc"u8.ToArray()
        ],
        [".svg"] =
        [
            "<svg"u8.ToArray()
        ],
        [".psd"] =
        [
            "8BPS"u8.ToArray()
        ],
        [".ai"] =
        [
            "%PDF-"u8.ToArray()
        ],
        [".dwg"] =
        [
            "AC10"u8.ToArray()
        ],
        [".dxf"] =
        [
            "0 "u8.ToArray()
        ],
        [".ogg"] =
        [
            "OggS"u8.ToArray()
        ],
        [".oga"] =
        [
            "OggS"u8.ToArray()
        ],
        [".opus"] =
        [
            "OggS"u8.ToArray()
        ],
        [".aac"] =
        [
            [0xFF, 0xF1],
            [0xFF, 0xF9]
        ],
        [".exe"] =
        [
            "MZ"u8.ToArray()
        ],
        [".dll"] =
        [
            "MZ"u8.ToArray()
        ],
        [".msi"] =
        [
            [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]
        ],
        [".cab"] =
        [
            "MSCF"u8.ToArray()
        ],
        [".elf"] =
        [
            [0x7F, 0x45, 0x4C, 0x46]
        ],
        [".macho"] =
        [
            [0xFE, 0xED, 0xFA, 0xCE],
            [0xFE, 0xED, 0xFA, 0xCF],
            [0xCE, 0xFA, 0xED, 0xFE],
            [0xCF, 0xFA, 0xED, 0xFE]
        ],
        [".wasm"] =
        [
            "\0asm"u8.ToArray()
        ]
    };

    private static readonly Dictionary<string, string> ExtensionContentTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".webp"] = "image/webp",
        [".tif"] = "image/tiff",
        [".tiff"] = "image/tiff",
        [".pdf"] = "application/pdf",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".flac"] = "audio/flac",
        [".mp4"] = "video/mp4",
        [".m4v"] = "video/x-m4v",
        [".mov"] = "video/quicktime",
        [".avi"] = "video/x-msvideo",
        [".mkv"] = "video/x-matroska",
        [".webm"] = "video/webm",
        [".flv"] = "video/x-flv",
        [".7z"] = "application/x-7z-compressed",
        [".rar"] = "application/vnd.rar",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".apk"] = "application/vnd.android.package-archive",
        [".jar"] = "application/java-archive",
        [".war"] = "application/java-archive",
        [".epub"] = "application/epub+zip",
        [".mobi"] = "application/x-mobipocket-ebook",
        [".zip"] = "application/zip",
        [".gz"] = "application/gzip",
        [".tar"] = "application/x-tar",
        [".iso"] = "application/x-iso9660-image",
        [".dmg"] = "application/x-apple-diskimage",
        [".heic"] = "image/heic",
        [".heif"] = "image/heif",
        [".svg"] = "image/svg+xml",
        [".psd"] = "image/vnd.adobe.photoshop",
        [".ai"] = "application/postscript",
        [".dwg"] = "image/vnd.dwg",
        [".dxf"] = "image/vnd.dxf",
        [".ogg"] = "audio/ogg",
        [".oga"] = "audio/ogg",
        [".opus"] = "audio/opus",
        [".aac"] = "audio/aac",
        [".exe"] = "application/vnd.microsoft.portable-executable",
        [".dll"] = "application/vnd.microsoft.portable-executable",
        [".msi"] = "application/x-msi",
        [".cab"] = "application/vnd.ms-cab-compressed",
        [".elf"] = "application/x-elf",
        [".macho"] = "application/x-mach-binary",
        [".wasm"] = "application/wasm",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv"
    };

    private readonly UploadValidationOptions _options = options.Value;

    /// <inheritdoc />
    public Task ValidateSessionAsync(
        string filename,
        long totalSize,
        string? contentType,
        int? chunkSize,
        string? fileHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Filename cannot be empty.", nameof(filename));
        }
        if (totalSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSize), "Total size must be greater than 0.");
        }
        if (_options.MaxFileSize > 0 && totalSize > _options.MaxFileSize)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSize), $"Total size exceeds limit: {_options.MaxFileSize} bytes.");
        }
        if (chunkSize.HasValue && chunkSize.Value % GridFSDefaults.StandardChunkSize != 0)
        {
            throw new ArgumentException($"Chunk size must be a multiple of {GridFSDefaults.StandardChunkSize} bytes (2MB).", nameof(chunkSize));
        }
        if (!string.IsNullOrEmpty(fileHash) && !IsValidSha256(fileHash))
        {
            throw new ArgumentException("File hash must be a valid SHA256 hex string.", nameof(fileHash));
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ValidateChunkAsync(
        GridFSUploadSession session,
        int chunkNumber,
        byte[] data,
        string chunkHash,
        CancellationToken cancellationToken = default)
    {
        if (chunkNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkNumber), "Chunk number must be non-negative.");
        }
        if (data.Length == 0)
        {
            throw new ArgumentException("Chunk data cannot be empty.", nameof(data));
        }
        if (data.Length > session.ChunkSize)
        {
            throw new ArgumentException("Chunk data length exceeds session chunk size.", nameof(data));
        }
        if (string.IsNullOrWhiteSpace(chunkHash) || !IsValidSha256(chunkHash))
        {
            throw new ArgumentException("Chunk hash must be a valid SHA256 hex string.", nameof(chunkHash));
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ValidateFinalizeAsync(
        GridFSUploadSession session,
        string? verifyHash,
        bool skipHashValidation,
        CancellationToken cancellationToken = default)
    {
        if (skipHashValidation)
        {
            return Task.CompletedTask;
        }
        if (!string.IsNullOrEmpty(verifyHash) && !IsValidSha256(verifyHash))
        {
            throw new ArgumentException("Verify hash must be a valid SHA256 hex string.", nameof(verifyHash));
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ValidateContentTypeAsync(string filename, string? contentType, CancellationToken cancellationToken = default)
    {
        var extension = NormalizeExtension(Path.GetExtension(filename));
        if (_options.AllowedExtensions.Count > 0 && !string.IsNullOrEmpty(extension) && !_options.AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Extension '{extension}' is not allowed.", nameof(filename));
        }
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Task.CompletedTask;
        }
        var normalizedContentType = contentType.Trim();
        if (_options.AllowedContentTypes.Count > 0 && !_options.AllowedContentTypes.Contains(normalizedContentType))
        {
            throw new ArgumentException($"Content type '{normalizedContentType}' is not allowed.", nameof(contentType));
        }
        if (string.IsNullOrEmpty(extension) || !ExtensionContentTypeMap.TryGetValue(extension, out var expectedType))
        {
            return Task.CompletedTask;
        }
        if (!string.Equals(expectedType, normalizedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Content type '{normalizedContentType}' does not match extension '{extension}'.", nameof(contentType));
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ValidateMagicNumberAsync(string filename, byte[] data, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableMagicNumberValidation || data.Length == 0)
        {
            return Task.CompletedTask;
        }
        var extension = NormalizeExtension(Path.GetExtension(filename));
        if (string.IsNullOrEmpty(extension) || !MagicNumberSignatures.TryGetValue(extension, out var signatures))
        {
            return Task.CompletedTask;
        }
        var headerLength = Math.Min(data.Length, 512);
        var header = new ReadOnlySpan<byte>(data, 0, headerLength);
        foreach (var signature in signatures)
        {
            if (header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) &&
                header.IndexOf(signature) >= 0)
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".tar", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 257 + signature.Length &&
                header.Slice(257, signature.Length).SequenceEqual(signature))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                var ftypOffset = header.IndexOf("ftyp"u8);
                if (ftypOffset >= 4)
                {
                    return Task.CompletedTask;
                }
            }
            if (extension.Equals(".m4v", StringComparison.OrdinalIgnoreCase))
            {
                var ftypOffset = header.IndexOf("ftyp"u8);
                if (ftypOffset >= 4)
                {
                    return Task.CompletedTask;
                }
            }
            if (extension.Equals(".mov", StringComparison.OrdinalIgnoreCase))
            {
                var ftypOffset = header.IndexOf("ftyp"u8);
                if (ftypOffset >= 4)
                {
                    return Task.CompletedTask;
                }
            }
            if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 12 &&
                header.Slice(8, 4).SequenceEqual("WEBP"u8))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 12 &&
                header.Slice(8, 4).SequenceEqual("WAVE"u8))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".avi", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 12 &&
                header.Slice(8, 4).SequenceEqual("AVI "u8))
            {
                return Task.CompletedTask;
            }
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (extension is ".docx" or ".xlsx" or ".pptx" or ".apk" or ".jar" or ".war")
            {
                if (headerLength >= 4 && header[..4].SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
                {
                    return Task.CompletedTask;
                }
            }
            if (extension.Equals(".epub", StringComparison.OrdinalIgnoreCase))
            {
                if (headerLength >= 4 && header[..4].SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
                {
                    return Task.CompletedTask;
                }
            }
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (extension is ".ogg" or ".oga" or ".opus")
            {
                if (headerLength >= 4 && header[..4].SequenceEqual("OggS"u8))
                {
                    return Task.CompletedTask;
                }
            }
            if (extension is ".heic" or ".heif" && headerLength >= 12)
            {
                var brandLength = Math.Min(8, headerLength - 4);
                var brand = header.Slice(4, brandLength);
                foreach (var sig in MagicNumberSignatures[extension])
                {
                    if (brandLength == sig.Length && brand.SequenceEqual(sig))
                    {
                        return Task.CompletedTask;
                    }
                }
            }
            const int mobiOffset = 60;
            const int mobiLength = 8;
            if (extension.Equals(".mobi", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= mobiOffset + mobiLength &&
                header.Slice(mobiOffset, mobiLength).SequenceEqual("BOOKMOBI"u8))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) &&
                header.IndexOf("<svg"u8) >= 0)
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".psd", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 4 &&
                header[..4].SequenceEqual("8BPS"u8))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".ai", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 5 &&
                header[..5].SequenceEqual("%PDF-"u8))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".dwg", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 4 &&
                header[..4].SequenceEqual("AC10"u8))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".dxf", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 2 &&
                header[..2].SequenceEqual("0 "u8))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".cab", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 4 &&
                header[..4].SequenceEqual("MSCF"u8))
            {
                return Task.CompletedTask;
            }
            if (extension.Equals(".elf", StringComparison.OrdinalIgnoreCase) &&
                headerLength >= 4 &&
                header[..4].SequenceEqual(new byte[] { 0x7F, 0x45, 0x4C, 0x46 }))
            {
                return Task.CompletedTask;
            }
            // 扩展类型的特殊魔数检查仅在最后一个签名迭代时执行，避免在多签名扩展上重复执行
            if (ReferenceEquals(signature, signatures[signatures.Count - 1]))
            {
                if (extension.Equals(".macho", StringComparison.OrdinalIgnoreCase))
                {
                    var machoMagic = new[]
                    {
                        new byte[] { 0xFE, 0xED, 0xFA, 0xCE },
                        new byte[] { 0xFE, 0xED, 0xFA, 0xCF },
                        new byte[] { 0xCE, 0xFA, 0xED, 0xFE },
                        new byte[] { 0xCF, 0xFA, 0xED, 0xFE }
                    };
                    foreach (var machoSignature in machoMagic)
                    {
                        if (headerLength >= machoSignature.Length && header[..machoSignature.Length].SequenceEqual(machoSignature))
                        {
                            return Task.CompletedTask;
                        }
                    }
                }
                if (extension.Equals(".wasm", StringComparison.OrdinalIgnoreCase))
                {
                    if (headerLength >= 4 && header[..4].SequenceEqual("\0asm"u8))
                    {
                        return Task.CompletedTask;
                    }
                }
                // ReSharper disable once InvertIf
                if (extension.Equals(".iso", StringComparison.OrdinalIgnoreCase))
                {
                    const int isoOffset = 0x8001; // 32769
                    if (data.Length >= isoOffset + 5 && new ReadOnlySpan<byte>(data, isoOffset, 5).SequenceEqual("CD001"u8))
                    {
                        return Task.CompletedTask;
                    }
                }
            }
        }
        throw new ArgumentException($"File signature does not match extension '{extension}'.", nameof(filename));
    }

    private static bool IsValidSha256(string hash)
    {
        return hash.Length == 64 && hash.Select(ch => ch is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F').All(isHex => isHex);
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }
        var trimmed = extension.Trim();
        return trimmed.StartsWith('.') ? trimmed.ToLowerInvariant() : $".{trimmed.ToLowerInvariant()}";
    }
}
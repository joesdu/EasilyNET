using EasilyNET.Mongo.AspNetCore.Models;

// ReSharper disable UnusedParameter.Global
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Global

namespace EasilyNET.Mongo.AspNetCore.Abstraction;

/// <summary>
///     <para xml:lang="en">Upload validation interface for GridFS uploads</para>
///     <para xml:lang="zh">GridFS 上传验证接口</para>
/// </summary>
public interface IUploadValidator
{
    /// <summary>
    ///     <para xml:lang="en">Validate upload session creation inputs</para>
    ///     <para xml:lang="zh">验证上传会话创建参数</para>
    /// </summary>
    /// <param name="filename">
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </param>
    /// <param name="totalSize">
    ///     <para xml:lang="en">Total file size in bytes</para>
    ///     <para xml:lang="zh">文件总大小(字节)</para>
    /// </param>
    /// <param name="contentType">
    ///     <para xml:lang="en">Content type</para>
    ///     <para xml:lang="zh">内容类型</para>
    /// </param>
    /// <param name="chunkSize">
    ///     <para xml:lang="en">Chunk size in bytes</para>
    ///     <para xml:lang="zh">块大小(字节)</para>
    /// </param>
    /// <param name="fileHash">
    ///     <para xml:lang="en">File SHA256 hash</para>
    ///     <para xml:lang="zh">文件 SHA256 哈希</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task ValidateSessionAsync(
        string filename,
        long totalSize,
        string? contentType,
        int? chunkSize,
        string? fileHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Validate an upload chunk before storage</para>
    ///     <para xml:lang="zh">在存储前验证上传块</para>
    /// </summary>
    /// <param name="session">
    ///     <para xml:lang="en">Upload session</para>
    ///     <para xml:lang="zh">上传会话</para>
    /// </param>
    /// <param name="chunkNumber">
    ///     <para xml:lang="en">Chunk number</para>
    ///     <para xml:lang="zh">块编号</para>
    /// </param>
    /// <param name="data">
    ///     <para xml:lang="en">Chunk data</para>
    ///     <para xml:lang="zh">块数据</para>
    /// </param>
    /// <param name="chunkHash">
    ///     <para xml:lang="en">Chunk SHA256 hash</para>
    ///     <para xml:lang="zh">块 SHA256 哈希</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task ValidateChunkAsync(
        GridFSUploadSession session,
        int chunkNumber,
        byte[] data,
        string chunkHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Validate upload finalization</para>
    ///     <para xml:lang="zh">验证上传完成操作</para>
    /// </summary>
    /// <param name="session">
    ///     <para xml:lang="en">Upload session</para>
    ///     <para xml:lang="zh">上传会话</para>
    /// </param>
    /// <param name="verifyHash">
    ///     <para xml:lang="en">Expected file hash</para>
    ///     <para xml:lang="zh">预期文件哈希</para>
    /// </param>
    /// <param name="skipHashValidation">
    ///     <para xml:lang="en">Skip full hash validation</para>
    ///     <para xml:lang="zh">跳过全量哈希校验</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task ValidateFinalizeAsync(
        GridFSUploadSession session,
        string? verifyHash,
        bool skipHashValidation,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Validate content type and extension consistency</para>
    ///     <para xml:lang="zh">验证内容类型与扩展名一致性</para>
    /// </summary>
    /// <param name="filename">
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </param>
    /// <param name="contentType">
    ///     <para xml:lang="en">Content type</para>
    ///     <para xml:lang="zh">内容类型</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task ValidateContentTypeAsync(string filename, string? contentType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Validate file signature (magic numbers)</para>
    ///     <para xml:lang="zh">验证文件签名(魔数)</para>
    /// </summary>
    /// <param name="filename">
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </param>
    /// <param name="data">
    ///     <para xml:lang="en">Chunk data</para>
    ///     <para xml:lang="zh">块数据</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task ValidateMagicNumberAsync(string filename, byte[] data, CancellationToken cancellationToken = default);
}
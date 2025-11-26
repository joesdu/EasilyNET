// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.Models;

/// <summary>
///     <para xml:lang="en">File information</para>
///     <para xml:lang="zh">文件信息</para>
/// </summary>
internal sealed class GridFileInfo
{
    /// <summary>
    ///     <para xml:lang="en">File ID</para>
    ///     <para xml:lang="zh">文件 ID</para>
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">File size in bytes</para>
    ///     <para xml:lang="zh">文件大小(字节)</para>
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Upload date</para>
    ///     <para xml:lang="zh">上传日期</para>
    /// </summary>
    public DateTime UploadDate { get; set; }
}
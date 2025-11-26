namespace EasilyNET.Mongo.AspNetCore.Models;

/// <summary>
///     <para xml:lang="en">GridFS storage statistics</para>
///     <para xml:lang="zh">GridFS 存储统计信息</para>
/// </summary>
internal sealed class GridFSStorageStats
{
    /// <summary>
    ///     <para xml:lang="en">Total number of files</para>
    ///     <para xml:lang="zh">文件总数</para>
    /// </summary>
    public long TotalFiles { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Total size in bytes</para>
    ///     <para xml:lang="zh">总大小(字节)</para>
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">List of largest files</para>
    ///     <para xml:lang="zh">最大文件列表</para>
    /// </summary>
    public List<GridFileInfo> LargestFiles { get; set; } = [];
}
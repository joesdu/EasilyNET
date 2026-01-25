namespace EasilyNET.Mongo.AspNetCore.Common;

/// <summary>
///     <para xml:lang="en">Some static default parameters for MongoDB</para>
///     <para xml:lang="zh">MongoDb的一些静态默认参数</para>
/// </summary>
internal static class Constant
{
    /// <summary>
    ///     <para xml:lang="en">Some conversion configurations for MongoDB</para>
    ///     <para xml:lang="zh">MongoDB一些转换配置</para>
    /// </summary>
    internal const string Pack = "Easily-Pack";

    /// <summary>
    ///     <para xml:lang="en">Default database name</para>
    ///     <para xml:lang="zh">默认数据库名称</para>
    /// </summary>
    internal const string DefaultDbName = "easilynet";

    /// <summary>
    ///     <para xml:lang="en">Default GridFS bucket name</para>
    ///     <para xml:lang="zh">默认GridFS存储桶名称</para>
    /// </summary>
    internal const string BucketName = "easilyfs";

    /// <summary>
    ///     <para xml:lang="en">Default configuration name</para>
    ///     <para xml:lang="zh">默认配置名称</para>
    /// </summary>
    internal const string ConfigName = "EasilyNetGridFS";
}

/// <summary>
///     <para xml:lang="en">GridFS default configuration values</para>
///     <para xml:lang="zh">GridFS 默认配置值</para>
/// </summary>
internal static class GridFSDefaults
{
    /// <summary>
    ///     <para xml:lang="en">Standard GridFS chunk size (2MB) - MongoDB's internal chunk size</para>
    ///     <para xml:lang="zh">标准 GridFS 块大小 (2MB) - MongoDB 内部块大小</para>
    /// </summary>
    internal const int StandardChunkSize = 2 * 1024 * 1024;

    /// <summary>
    ///     <para xml:lang="en">Default streaming chunk size (255KB) - optimized for streaming performance</para>
    ///     <para xml:lang="zh">默认流式传输块大小 (255KB) - 为流式传输性能优化</para>
    /// </summary>
    internal const int StreamingChunkSize = 255 * 1024;

    /// <summary>
    ///     <para xml:lang="en">Default session expiration in hours</para>
    ///     <para xml:lang="zh">默认会话过期时间（小时）</para>
    /// </summary>
    internal const int DefaultSessionExpirationHours = 24;

    /// <summary>
    ///     <para xml:lang="en">Small file size threshold (20MB) - uses 2MB chunks</para>
    ///     <para xml:lang="zh">小文件大小阈值 (20MB) - 使用 2MB 分片</para>
    /// </summary>
    internal const long SmallFileSizeThreshold = 20 * 1024 * 1024;

    /// <summary>
    ///     <para xml:lang="en">Medium file size threshold (100MB) - uses 4MB chunks</para>
    ///     <para xml:lang="zh">中等文件大小阈值 (100MB) - 使用 4MB 分片</para>
    /// </summary>
    internal const long MediumFileSizeThreshold = 100 * 1024 * 1024;

    /// <summary>
    ///     <para xml:lang="en">Upload session collection name</para>
    ///     <para xml:lang="zh">上传会话集合名称</para>
    /// </summary>
    internal const string UploadSessionCollectionName = "fs.upload_sessions";
}
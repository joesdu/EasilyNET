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
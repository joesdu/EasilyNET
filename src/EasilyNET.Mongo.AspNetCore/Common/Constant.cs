namespace EasilyNET.Mongo.AspNetCore.Common;

/// <summary>
/// MongoDb的一些静态默认参数
/// </summary>
internal static class Constant
{
    /// <summary>
    /// MongoDB一些转换配置
    /// </summary>
    internal const string Pack = "easily-pack";

    /// <summary>
    /// 默认数据库名称
    /// </summary>
    internal const string DefaultDbName = "easilynet";

    /// <summary>
    /// 默认配置名称
    /// </summary>
    internal const string ConfigName = "EasilyNetGridfs";

    /// <summary>
    /// 默认桶名称
    /// </summary>
    internal const string BucketName = "easilyfs";
}
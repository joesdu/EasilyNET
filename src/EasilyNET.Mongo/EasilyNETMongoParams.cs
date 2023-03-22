using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Linq;

namespace EasilyNET.Mongo;

/// <summary>
/// MongoDB注册时使用的一些参数配置
/// </summary>
public sealed class EasilyNETMongoParams
{
    /// <summary>
    /// DbContextOptions
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Action<EasilyNETMongoOptions>? Options { get; set; }

    /// <summary>
    /// 当前主要用于支持 SkyAPMSkyApm.Diagnostics.MongoDB,请直接填入: cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
    /// 其他的需求请自行使用.
    /// </summary>
    public Action<ClusterBuilder>? ClusterBuilder { get; set; }

    /// <summary>
    /// LinqProvider版本,用来设置以兼容Linq V2的代码.默认为V3
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public LinqProvider LinqProvider { get; set; } = LinqProvider.V3;

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string DatabaseName { get; set; } = EasilyNETConstant.DbName;

    /// <summary>
    /// DBContext的构造函数参数,用于支持自定义非无参构造函数的DbContext
    /// </summary>
    public List<object> ContextParams { get; set; } = new();
}
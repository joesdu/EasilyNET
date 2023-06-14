using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Linq;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo;

/// <summary>
/// Mongodb配置选项
/// </summary>
public sealed class EasilyMongoOptions
{
    /// <summary>
    /// 数据库名称
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// ObjectId到String转换的类型[该列表中的对象,不会将Id,ID字段转化为ObjectId类型.在数据库中存为字符串格式]
    /// </summary>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public List<Type> ObjectIdToStringTypes { get; set; } = new();

    /// <summary>
    /// 是否使用本库提供的默认转换,默认:true
    /// 1.驼峰名称格式
    /// 2.忽略代码中未定义的字段
    /// 3._id映射为实体中的ID或者Id,反之亦然
    /// 4.将枚举类型存储为字符串格式
    /// </summary>
    public bool DefaultConventionRegistry { get; set; } = true;

    /// <summary>
    /// 添加自己的一些Convention配置,用于设置mongodb序列化反序列化的一些表现.
    /// </summary>
    public Dictionary<string, ConventionPack> ConventionRegistry { get; set; } = new();

    /// <summary>
    /// 用于支持一些事件订阅.如:SkyAPM,以及我们自己的DebugConsole
    /// </summary>
    public Action<ClusterBuilder>? ClusterBuilder { get; set; }

    /// <summary>
    /// LinqProvider版本,用来设置以兼容Linq V2的代码.默认为V3
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public LinqProvider LinqProvider { get; set; } = LinqProvider.V3;
}
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore;

/// <summary>
/// Mongodb配置选项
/// </summary>
public class BasicClientOptions
{
    /// <summary>
    /// 数据库名称 <see langword="string" />
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// <see cref="ObjectId" /> 到 <see cref="string" /> 转换的类型
    /// <remarks>
    ///     <para>
    ///     该列表中的对象,不会将 <see langword="Id" /> 或者 <see langword="ID" /> 字段转化为 <see cref="ObjectId" /> 类型.在数据库中存为字符串格式
    ///     </para>
    /// </remarks>
    /// </summary>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public List<Type> ObjectIdToStringTypes { get; set; } = [];

    /// <summary>
    /// 是否使用本库提供的默认转换,默认: <see langword="true" />
    /// <remarks>
    ///     <para>默认的配置如下:</para>
    ///     <list type="number">
    ///         <item>驼峰名称格式</item>
    ///         <item>忽略代码中未定义的字段</item>
    ///         <item><see langword="_id" /> 映射为实体中的 <see langword="ID" /> 或者 <see langword="Id" />,反之亦然</item>
    ///         <item>将枚举类型存储为 <see langword="string" /> 格式</item>
    ///     </list>
    /// </remarks>
    /// </summary>
    public bool DefaultConventionRegistry { get; set; } = true;

    /// <summary>
    /// 添加自己的一些Convention配置,用于设置mongodb序列化反序列化的一些表现.
    /// </summary>
    public Dictionary<string, ConventionPack> ConventionRegistry { get; set; } = [];
}

/// <summary>
/// <see cref="MongoClientSettings" /> 配置,在不适用 <see cref="MongoClientSettings" /> 配置时可通过该对象添加一些特性.
/// </summary>
public sealed class ClientOptions : BasicClientOptions
{
    /// <summary>
    /// 配置 <see cref="MongoClientSettings" />
    /// </summary>
    public Action<MongoClientSettings>? ClientSettings { get; set; }
}

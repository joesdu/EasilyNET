using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">MongoDB configuration options</para>
///     <para xml:lang="zh">Mongodb配置选项</para>
/// </summary>
public class BasicClientOptions
{
    /// <summary>
    ///     <para xml:lang="en">Database name <see langword="string" /></para>
    ///     <para xml:lang="zh">数据库名称 <see langword="string" /></para>
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Types for converting <see cref="ObjectId" /> to <see cref="string" /></para>
    ///     <para xml:lang="zh"><see cref="ObjectId" /> 到 <see cref="string" /> 转换的类型</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         Objects in this list will not convert <see langword="Id" /> or <see langword="ID" /> fields to <see cref="ObjectId" /> type. They will be
    ///         stored as strings in the database.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         该列表中的对象,不会将 <see langword="Id" /> 或者 <see langword="ID" /> 字段转化为 <see cref="ObjectId" /> 类型.在数据库中存为字符串格式
    ///         </para>
    ///     </remarks>
    /// </summary>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public List<Type> ObjectIdToStringTypes { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">Whether to use the default conversion provided by this library, default: <see langword="true" /></para>
    ///     <para xml:lang="zh">是否使用本库提供的默认转换,默认: <see langword="true" /></para>
    ///     <remarks>
    ///         <para xml:lang="en">The default configuration is as follows:</para>
    ///         <para xml:lang="zh">默认的配置如下:</para>
    ///         <list type="number">
    ///             <item>
    ///                 <description>
    ///                     <para xml:lang="en">Camel case naming format</para>
    ///                     <para xml:lang="zh">驼峰名称格式</para>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <para xml:lang="en">Ignore fields not defined in the code</para>
    ///                     <para xml:lang="zh">忽略代码中未定义的字段</para>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <para xml:lang="en"><see langword="_id" /> maps to <see langword="ID" /> or <see langword="Id" /> in the entity, and vice versa</para>
    ///                     <para xml:lang="zh"><see langword="_id" /> 映射为实体中的 <see langword="ID" /> 或者 <see langword="Id" />,反之亦然</para>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <para xml:lang="en">Store enum types as <see langword="string" /></para>
    ///                     <para xml:lang="zh">将枚举类型存储为 <see langword="string" /> 格式</para>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </remarks>
    /// </summary>
    public bool DefaultConventionRegistry { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Add your own convention configurations to set some behaviors for MongoDB serialization and deserialization</para>
    ///     <para xml:lang="zh">添加自己的一些Convention配置,用于设置mongodb序列化反序列化的一些表现</para>
    /// </summary>
    public Dictionary<string, ConventionPack> ConventionRegistry { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">
///     <see cref="MongoClientSettings" /> configuration, can add some features through this object when not using
///     <see cref="MongoClientSettings" /> configuration
///     </para>
///     <para xml:lang="zh"><see cref="MongoClientSettings" /> 配置,在不适用 <see cref="MongoClientSettings" /> 配置时可通过该对象添加一些特性</para>
/// </summary>
public sealed class ClientOptions : BasicClientOptions
{
    /// <summary>
    ///     <para xml:lang="en">Configure <see cref="MongoClientSettings" /></para>
    ///     <para xml:lang="zh">配置 <see cref="MongoClientSettings" /></para>
    /// </summary>
    public Action<MongoClientSettings>? ClientSettings { get; set; }
}
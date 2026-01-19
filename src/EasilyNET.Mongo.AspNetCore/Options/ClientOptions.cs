using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">
///     <see cref="MongoClientSettings" /> configuration, can add some features through this object when not using
///     <see cref="MongoClientSettings" /> configuration
///     </para>
///     <para xml:lang="zh"><see cref="MongoClientSettings" /> 配置,在不使用 <see cref="MongoClientSettings" /> 配置时可通过该对象添加一些特性</para>
/// </summary>
public sealed class ClientOptions : BasicClientOptions
{
    /// <summary>
    ///     <para xml:lang="en">Configure <see cref="MongoClientSettings" /></para>
    ///     <para xml:lang="zh">配置 <see cref="MongoClientSettings" /></para>
    /// </summary>
    public Action<MongoClientSettings>? ClientSettings { get; set; }
}

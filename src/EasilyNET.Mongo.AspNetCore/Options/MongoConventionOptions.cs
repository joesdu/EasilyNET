using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.Mongo.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">
///     Global MongoDB convention configuration options. Must be configured at most once, before any <c>AddMongoContext</c> call.
///     When <c>ConfigureMongoConventions</c> is called, only the conventions explicitly added via <see cref="AddConvention" />
///     will be registered — the library's built-in defaults (camelCase, IgnoreExtraElements, etc.) will NOT be applied.
///     If <c>ConfigureMongoConventions</c> is never called, the built-in defaults are applied automatically.
///     </para>
///     <para xml:lang="zh">
///     全局 MongoDB Convention 配置选项。最多配置一次，且必须在所有 <c>AddMongoContext</c> 调用之前。
///     当调用 <c>ConfigureMongoConventions</c> 时，仅注册通过 <see cref="AddConvention" /> 显式添加的约定 —
///     本库的内置默认约定（驼峰命名、忽略未知字段等）不会被应用。
///     若从未调用 <c>ConfigureMongoConventions</c>，则自动应用内置默认约定。
///     </para>
/// </summary>
public sealed class MongoConventionOptions
{
    private readonly List<ConventionPackEntry> _conventions = [];

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
    public List<Type> ObjectIdToStringTypes { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">
    ///     The <see cref="DateTimeKind" /> used by the built-in global <c>DateTime</c> serializer. Default is
    ///     <see cref="DateTimeKind.Local" /> (kept for backward compatibility). Set to <see cref="DateTimeKind.Utc" />
    ///     to avoid cross-timezone drift when the same data is read on hosts in different timezones.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     内置全局 <c>DateTime</c> 序列化器使用的 <see cref="DateTimeKind" />。默认 <see cref="DateTimeKind.Local" />（为向后兼容保留）。
    ///     设为 <see cref="DateTimeKind.Utc" /> 可避免同一数据在不同时区主机上读取时产生时间漂移。
    ///     </para>
    /// </summary>
    public DateTimeKind DateTimeSerializerKind { get; set; } = DateTimeKind.Local;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Whether to register the library's built-in global <c>DateTime</c>/<c>Decimal</c> serializers.
    ///     Set to <see langword="false" /> to register your own (improves testability and avoids the global static side effect).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     是否注册本库内置的全局 <c>DateTime</c>/<c>Decimal</c> 序列化器。设为 <see langword="false" /> 可改为注册自定义序列化器（利于单测，避免全局静态副作用）。
    ///     </para>
    /// </summary>
    public bool RegisterDefaultSerializers { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Gets the registered convention packs (read-only).</para>
    ///     <para xml:lang="zh">获取已注册的约定包列表（只读）。</para>
    /// </summary>
    internal IReadOnlyList<ConventionPackEntry> Conventions => _conventions;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Add a named convention pack. All conventions added here will be registered globally for MongoDB BSON serialization.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     添加一个命名的约定包。此处添加的所有约定将全局注册到 MongoDB BSON 序列化中。
    ///     </para>
    /// </summary>
    /// <param name="name">
    ///     <para xml:lang="en">A unique name for this convention pack.</para>
    ///     <para xml:lang="zh">此约定包的唯一名称。</para>
    /// </param>
    /// <param name="pack">
    ///     <para xml:lang="en">The <see cref="ConventionPack" /> to register.</para>
    ///     <para xml:lang="zh">要注册的 <see cref="ConventionPack" />。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The current <see cref="MongoConventionOptions" /> instance for fluent chaining.</para>
    ///     <para xml:lang="zh">当前 <see cref="MongoConventionOptions" /> 实例，支持链式调用。</para>
    /// </returns>
    // ReSharper disable once UnusedMember.Global
    public MongoConventionOptions AddConvention(string name, ConventionPack pack)
    {
        _conventions.Add(new(name, pack));
        return this;
    }
}

/// <summary>
///     <para xml:lang="en">Represents a named convention pack entry.</para>
///     <para xml:lang="zh">表示一个命名的约定包条目。</para>
/// </summary>
/// <param name="Name">
///     <para xml:lang="en">The unique name of the convention pack.</para>
///     <para xml:lang="zh">约定包的唯一名称。</para>
/// </param>
/// <param name="Pack">
///     <para xml:lang="en">The convention pack.</para>
///     <para xml:lang="zh">约定包。</para>
/// </param>
internal sealed record ConventionPackEntry(string Name, ConventionPack Pack);
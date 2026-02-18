// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
///     <para xml:lang="en">Marks a class as a capped collection with specified size and document limits</para>
///     <para xml:lang="zh">标记一个类为固定大小集合，并指定大小和文档数量限制</para>
///     <see href="https://www.mongodb.com/docs/manual/core/capped-collections/" />
/// </summary>
/// <param name="collectionName">
///     <para xml:lang="en">The name of the capped collection to create</para>
///     <para xml:lang="zh">要创建的固定大小集合的名称</para>
/// </param>
/// <param name="maxSize">
///     <para xml:lang="en">The maximum size of the collection in bytes. Must be greater than 0.</para>
///     <para xml:lang="zh">集合的最大大小（字节）。必须大于 0。</para>
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CappedCollectionAttribute(string collectionName, long maxSize) : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">The name of the capped collection</para>
    ///     <para xml:lang="zh">固定大小集合的名称</para>
    /// </summary>
    public string CollectionName { get; } = collectionName;

    /// <summary>
    ///     <para xml:lang="en">The maximum size of the collection in bytes</para>
    ///     <para xml:lang="zh">集合的最大大小（字节）</para>
    /// </summary>
    public long MaxSize { get; } = maxSize > 0 ? maxSize : throw new ArgumentOutOfRangeException(nameof(maxSize), "MaxSize must be greater than 0.");

    /// <summary>
    ///     <para xml:lang="en">
    ///     Optional. The maximum number of documents allowed in the capped collection.
    ///     If not set, MongoDB only enforces the size limit.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     可选。固定大小集合中允许的最大文档数量。
    ///     如果未设置，MongoDB 仅强制执行大小限制。
    ///     </para>
    /// </summary>
    public long? MaxDocuments { get; set; }
}
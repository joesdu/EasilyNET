using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.Core.BulkWrite;

/// <summary>
///     <para xml:lang="en">Fluent builder for constructing bulk write operations</para>
///     <para xml:lang="zh">用于构建批量写入操作的 Fluent 构建器</para>
/// </summary>
/// <typeparam name="TDocument">
///     <para xml:lang="en">The document type</para>
///     <para xml:lang="zh">文档类型</para>
/// </typeparam>
public sealed class BulkOperationBuilder<TDocument>
{
    private readonly List<WriteModel<TDocument>> _operations = [];

    /// <summary>
    ///     <para xml:lang="en">Add an insert operation</para>
    ///     <para xml:lang="zh">添加插入操作</para>
    /// </summary>
    /// <param name="document">
    ///     <para xml:lang="en">The document to insert</para>
    ///     <para xml:lang="zh">要插入的文档</para>
    /// </param>
    public BulkOperationBuilder<TDocument> InsertOne(TDocument document)
    {
        _operations.Add(new InsertOneModel<TDocument>(document));
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Add multiple insert operations</para>
    ///     <para xml:lang="zh">添加多个插入操作</para>
    /// </summary>
    /// <param name="documents">
    ///     <para xml:lang="en">The documents to insert</para>
    ///     <para xml:lang="zh">要插入的文档集合</para>
    /// </param>
    public BulkOperationBuilder<TDocument> InsertMany(IEnumerable<TDocument> documents)
    {
        foreach (var doc in documents)
        {
            _operations.Add(new InsertOneModel<TDocument>(doc));
        }
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Add an update-one operation</para>
    ///     <para xml:lang="zh">添加更新单个文档操作</para>
    /// </summary>
    /// <param name="filter">
    ///     <para xml:lang="en">The filter to match the document</para>
    ///     <para xml:lang="zh">匹配文档的过滤器</para>
    /// </param>
    /// <param name="update">
    ///     <para xml:lang="en">The update definition</para>
    ///     <para xml:lang="zh">更新定义</para>
    /// </param>
    /// <param name="isUpsert">
    ///     <para xml:lang="en">Whether to insert a new document if no match is found. Defaults to <see langword="false" />.</para>
    ///     <para xml:lang="zh">如果未找到匹配项，是否插入新文档。默认为 <see langword="false" />。</para>
    /// </param>
    public BulkOperationBuilder<TDocument> UpdateOne(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, bool isUpsert = false)
    {
        _operations.Add(new UpdateOneModel<TDocument>(filter, update) { IsUpsert = isUpsert });
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Add an update-many operation</para>
    ///     <para xml:lang="zh">添加更新多个文档操作</para>
    /// </summary>
    /// <param name="filter">
    ///     <para xml:lang="en">The filter to match the documents</para>
    ///     <para xml:lang="zh">匹配文档的过滤器</para>
    /// </param>
    /// <param name="update">
    ///     <para xml:lang="en">The update definition</para>
    ///     <para xml:lang="zh">更新定义</para>
    /// </param>
    /// <param name="isUpsert">
    ///     <para xml:lang="en">Whether to insert a new document if no match is found. Defaults to <see langword="false" />.</para>
    ///     <para xml:lang="zh">如果未找到匹配项，是否插入新文档。默认为 <see langword="false" />。</para>
    /// </param>
    public BulkOperationBuilder<TDocument> UpdateMany(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update, bool isUpsert = false)
    {
        _operations.Add(new UpdateManyModel<TDocument>(filter, update) { IsUpsert = isUpsert });
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Add a replace-one operation</para>
    ///     <para xml:lang="zh">添加替换单个文档操作</para>
    /// </summary>
    /// <param name="filter">
    ///     <para xml:lang="en">The filter to match the document</para>
    ///     <para xml:lang="zh">匹配文档的过滤器</para>
    /// </param>
    /// <param name="replacement">
    ///     <para xml:lang="en">The replacement document</para>
    ///     <para xml:lang="zh">替换文档</para>
    /// </param>
    /// <param name="isUpsert">
    ///     <para xml:lang="en">Whether to insert a new document if no match is found. Defaults to <see langword="false" />.</para>
    ///     <para xml:lang="zh">如果未找到匹配项，是否插入新文档。默认为 <see langword="false" />。</para>
    /// </param>
    public BulkOperationBuilder<TDocument> ReplaceOne(FilterDefinition<TDocument> filter, TDocument replacement, bool isUpsert = false)
    {
        _operations.Add(new ReplaceOneModel<TDocument>(filter, replacement) { IsUpsert = isUpsert });
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Add a delete-one operation</para>
    ///     <para xml:lang="zh">添加删除单个文档操作</para>
    /// </summary>
    /// <param name="filter">
    ///     <para xml:lang="en">The filter to match the document</para>
    ///     <para xml:lang="zh">匹配文档的过滤器</para>
    /// </param>
    public BulkOperationBuilder<TDocument> DeleteOne(FilterDefinition<TDocument> filter)
    {
        _operations.Add(new DeleteOneModel<TDocument>(filter));
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Add a delete-many operation</para>
    ///     <para xml:lang="zh">添加删除多个文档操作</para>
    /// </summary>
    /// <param name="filter">
    ///     <para xml:lang="en">The filter to match the documents</para>
    ///     <para xml:lang="zh">匹配文档的过滤器</para>
    /// </param>
    public BulkOperationBuilder<TDocument> DeleteMany(FilterDefinition<TDocument> filter)
    {
        _operations.Add(new DeleteManyModel<TDocument>(filter));
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Get the collected write models</para>
    ///     <para xml:lang="zh">获取收集的写入模型</para>
    /// </summary>
    public IReadOnlyList<WriteModel<TDocument>> Build() => _operations;
}
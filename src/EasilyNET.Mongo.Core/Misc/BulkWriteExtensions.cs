using EasilyNET.Mongo.Core.BulkWrite;
using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.Core.Misc;

/// <summary>
///     <para xml:lang="en">Extension methods for fluent bulk write operations on <see cref="IMongoCollection{TDocument}" /></para>
///     <para xml:lang="zh"><see cref="IMongoCollection{TDocument}" /> 上的 Fluent 批量写入操作扩展方法</para>
/// </summary>
public static class BulkWriteExtensions
{
    /// <param name="collection">
    ///     <see cref="IMongoCollection{TDocument}" />
    /// </param>
    /// <typeparam name="TDocument">
    ///     <para xml:lang="en">The document type</para>
    ///     <para xml:lang="zh">文档类型</para>
    /// </typeparam>
    extension<TDocument>(IMongoCollection<TDocument> collection)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///     Execute bulk write operations using a fluent builder pattern.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     使用 Fluent 构建器模式执行批量写入操作。
        ///     </para>
        ///     <example>
        ///         <code>
        ///     var result = await collection.BulkWriteAsync(bulk => bulk
        ///         .InsertOne(new Order { ... })
        ///         .UpdateOne(
        ///             Builders&lt;Order&gt;.Filter.Eq(o => o.Id, "123"),
        ///             Builders&lt;Order&gt;.Update.Set(o => o.Status, "shipped"))
        ///         .DeleteOne(Builders&lt;Order&gt;.Filter.Eq(o => o.Id, "456")),
        ///         cancellationToken: ct);
        ///         </code>
        ///     </example>
        /// </summary>
        /// <param name="configure">
        ///     <para xml:lang="en">A delegate to configure the bulk operations</para>
        ///     <para xml:lang="zh">配置批量操作的委托</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional bulk write options</para>
        ///     <para xml:lang="zh">可选的批量写入选项</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation token</para>
        ///     <para xml:lang="zh">取消令牌</para>
        /// </param>
        /// <returns>
        ///     <see cref="BulkWriteResult{TDocument}" />
        /// </returns>
        public async Task<BulkWriteResult<TDocument>> BulkWriteAsync(Action<BulkOperationBuilder<TDocument>> configure,
            BulkWriteOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new BulkOperationBuilder<TDocument>();
            configure(builder);
            var operations = builder.Build();
            return operations.Count == 0
                       ? throw new InvalidOperationException("No bulk write operations were configured. Add at least one operation before executing.")
                       : await collection.BulkWriteAsync(operations, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Execute bulk write operations synchronously using a fluent builder pattern.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     使用 Fluent 构建器模式同步执行批量写入操作。
        ///     </para>
        /// </summary>
        /// <param name="configure">
        ///     <para xml:lang="en">A delegate to configure the bulk operations</para>
        ///     <para xml:lang="zh">配置批量操作的委托</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">Optional bulk write options</para>
        ///     <para xml:lang="zh">可选的批量写入选项</para>
        /// </param>
        /// <returns>
        ///     <see cref="BulkWriteResult{TDocument}" />
        /// </returns>
        public BulkWriteResult<TDocument> BulkWrite(Action<BulkOperationBuilder<TDocument>> configure,
            BulkWriteOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new BulkOperationBuilder<TDocument>();
            configure(builder);
            var operations = builder.Build();
            return operations.Count == 0
                       ? throw new InvalidOperationException("No bulk write operations were configured. Add at least one operation before executing.")
                       : collection.BulkWrite(operations, options);
        }
    }
}
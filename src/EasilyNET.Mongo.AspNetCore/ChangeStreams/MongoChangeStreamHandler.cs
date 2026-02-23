using EasilyNET.Mongo.AspNetCore.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.ChangeStreams;

/// <summary>
///     <para xml:lang="en">
///     Abstract base class for consuming MongoDB Change Streams as a hosted background service.
///     Handles cursor lifecycle, resume token management, automatic reconnection with exponential backoff,
///     and graceful shutdown.
///     </para>
///     <para xml:lang="zh">
///     作为托管后台服务消费 MongoDB 变更流的抽象基类。
///     处理游标生命周期、恢复令牌管理、指数退避自动重连和优雅关闭。
///     </para>
///     <example>
///         <code>
///     public class OrderChangeHandler : MongoChangeStreamHandler&lt;Order&gt;
///     {
///         public OrderChangeHandler(MyDbContext db, ILogger&lt;OrderChangeHandler&gt; logger)
///             : base(db.Database, "orders", logger) { }
/// 
///         protected override Task HandleChangeAsync(ChangeStreamDocument&lt;Order&gt; change, CancellationToken ct)
///         {
///             // Process the change
///             return Task.CompletedTask;
///         }
///     }
///         </code>
///     </example>
/// </summary>
/// <typeparam name="TDocument">
///     <para xml:lang="en">The document type being watched</para>
///     <para xml:lang="zh">被监视的文档类型</para>
/// </typeparam>
public abstract class MongoChangeStreamHandler<TDocument>(
    IMongoDatabase database,
    string collectionName,
    ILogger logger,
    ChangeStreamHandlerOptions? options = null) : BackgroundService
{
    private readonly ChangeStreamHandlerOptions _options = options ?? new();
    private BsonDocument? _resumeToken;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Override to specify which operation types to watch.
    ///     Defaults to all operation types (no filter).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     重写以指定要监视的操作类型。
    ///     默认为所有操作类型（无过滤）。
    ///     </para>
    /// </summary>
    protected virtual ChangeStreamOperationType[]? WatchOperations => null;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Override to provide a custom pipeline filter for the change stream.
    ///     This is applied in addition to <see cref="WatchOperations" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     重写以提供变更流的自定义管道过滤器。
    ///     此过滤器在 <see cref="WatchOperations" /> 之外额外应用。
    ///     </para>
    /// </summary>
    protected virtual PipelineDefinition<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>>? Pipeline => null;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Process a single change stream event. Implement this method to handle changes.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     处理单个变更流事件。实现此方法以处理变更。
    ///     </para>
    /// </summary>
    /// <param name="change">
    ///     <para xml:lang="en">The change stream document</para>
    ///     <para xml:lang="zh">变更流文档</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    abstract protected Task HandleChangeAsync(ChangeStreamDocument<TDocument> change, CancellationToken cancellationToken);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.PersistResumeToken)
        {
            Interlocked.Exchange(ref _resumeToken, await LoadResumeTokenAsync(stoppingToken).ConfigureAwait(false));
        }
        var retryCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WatchCollectionAsync(stoppingToken).ConfigureAwait(false);
                retryCount = 0; // Reset on successful connection
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (_options.MaxRetryAttempts > 0 && retryCount > _options.MaxRetryAttempts)
                {
                    logger.LogError(ex, "Change stream for collection {CollectionName} exceeded max retry attempts ({MaxRetries}). Stopping.",
                        collectionName, _options.MaxRetryAttempts);
                    break;
                }
                var delay = CalculateRetryDelay(retryCount);
                logger.LogWarning(ex, "Change stream for collection {CollectionName} encountered an error. Retrying in {Delay}s (attempt {Attempt}/{MaxAttempts}).",
                    collectionName, delay.TotalSeconds, retryCount, _options.MaxRetryAttempts);
                try
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        logger.LogInformation("Change stream handler for collection {CollectionName} has stopped.", collectionName);
    }

    private async Task WatchCollectionAsync(CancellationToken stoppingToken)
    {
        var collection = database.GetCollection<TDocument>(collectionName);
        var pipeline = BuildPipeline();
        var changeStreamOptions = new ChangeStreamOptions
        {
            FullDocument = _options.FullDocument
        };
        var resumeToken = Interlocked.CompareExchange(ref _resumeToken, null, null);
        if (resumeToken is not null)
        {
            changeStreamOptions.ResumeAfter = resumeToken;
            logger.LogInformation("Resuming change stream for collection {CollectionName} from saved token.", collectionName);
        }
        using var cursor = pipeline is not null
                               ? await collection.WatchAsync(pipeline, changeStreamOptions, stoppingToken).ConfigureAwait(false)
                               : await collection.WatchAsync(changeStreamOptions, stoppingToken).ConfigureAwait(false);
        logger.LogInformation("Change stream started for collection {CollectionName}.", collectionName);
        while (await cursor.MoveNextAsync(stoppingToken).ConfigureAwait(false))
        {
            foreach (var change in cursor.Current)
            {
                try
                {
                    await HandleChangeAsync(change, stoppingToken).ConfigureAwait(false);
                    // Update resume token after successful processing
                    Interlocked.Exchange(ref _resumeToken, change.ResumeToken);
                    if (_options.PersistResumeToken)
                    {
                        await SaveResumeTokenAsync(change.ResumeToken, stoppingToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error handling change event for collection {CollectionName}. OperationType={OperationType}",
                        collectionName, change.OperationType);
                }
            }
        }
    }

    private PipelineDefinition<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>>? BuildPipeline()
    {
        if (Pipeline is not null)
        {
            return Pipeline;
        }
        if (WatchOperations is not { Length: > 0 })
        {
            return null;
        }
        var operationTypes = WatchOperations.Select(op => op.ToString().ToLowerInvariant()).ToArray();
        var matchStage = new BsonDocument("$match", new BsonDocument("operationType", new BsonDocument("$in", new BsonArray(operationTypes))));
        return new EmptyPipelineDefinition<ChangeStreamDocument<TDocument>>().AppendStage<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>>(matchStage);
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        var delay = TimeSpan.FromTicks(_options.RetryDelay.Ticks * (long)Math.Pow(2, retryCount - 1));
        return delay > _options.MaxRetryDelay ? _options.MaxRetryDelay : delay;
    }

    private async Task<BsonDocument?> LoadResumeTokenAsync(CancellationToken ct)
    {
        try
        {
            var tokenCollection = database.GetCollection<BsonDocument>(_options.ResumeTokenCollectionName);
            var handlerName = GetType().FullName ?? GetType().Name;
            var doc = await tokenCollection.Find(Builders<BsonDocument>.Filter.Eq("_id", handlerName)).FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (doc is not null && doc.Contains("resumeToken"))
            {
                logger.LogInformation("Loaded resume token for handler {HandlerName}.", handlerName);
                return doc["resumeToken"].AsBsonDocument;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load resume token for change stream handler. Starting from current position.");
        }
        return null;
    }

    private async Task SaveResumeTokenAsync(BsonDocument resumeToken, CancellationToken ct)
    {
        try
        {
            var tokenCollection = database.GetCollection<BsonDocument>(_options.ResumeTokenCollectionName);
            var handlerName = GetType().FullName ?? GetType().Name;
            var filter = Builders<BsonDocument>.Filter.Eq("_id", handlerName);
            var update = Builders<BsonDocument>.Update
                                               .Set("resumeToken", resumeToken)
                                               .Set("updatedAt", DateTime.UtcNow);
            await tokenCollection.UpdateOneAsync(filter, update, new() { IsUpsert = true }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist resume token for change stream handler.");
        }
    }
}
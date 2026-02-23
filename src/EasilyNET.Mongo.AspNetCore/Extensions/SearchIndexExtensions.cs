using EasilyNET.Mongo.AspNetCore.SearchIndex;
using EasilyNET.Mongo.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Extension methods for automatically creating MongoDB Atlas Search and Vector Search indexes</para>
///     <para xml:lang="zh">自动创建 MongoDB Atlas Search 和 Vector Search 索引的扩展方法</para>
/// </summary>
public static class SearchIndexExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Register a hosted background service that automatically creates MongoDB Atlas Search and Vector Search indexes
    ///     for entity objects marked with <c>MongoSearchIndexAttribute</c>.
    ///     The service runs once at application startup and then completes.
    ///     Requires MongoDB Atlas or MongoDB 8.2+ Community Edition.
    ///     On unsupported deployments, the service logs a warning and skips index creation.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     注册一个托管后台服务，自动为标记了 <c>MongoSearchIndexAttribute</c> 的实体对象创建 MongoDB Atlas Search 和 Vector Search 索引。
    ///     该服务在应用启动时运行一次后完成。
    ///     需要 MongoDB Atlas 或 MongoDB 8.2+ 社区版。
    ///     在不支持的部署上，该服务记录警告并跳过索引创建。
    ///     </para>
    /// </summary>
    /// <typeparam name="T">
    ///     <see cref="MongoContext" />
    /// </typeparam>
    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    /// <returns>
    ///     <see cref="IServiceCollection" />
    /// </returns>
    public static IServiceCollection AddMongoSearchIndexCreation<T>(this IServiceCollection services) where T : MongoContext
    {
        services.AddHostedService<SearchIndexBackgroundService<T>>();
        return services;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Automatically create MongoDB Atlas Search and Vector Search indexes for entity objects marked with
    ///     <c>MongoSearchIndexAttribute</c>.
    ///     Requires MongoDB Atlas or MongoDB 8.2+ Community Edition.
    ///     On unsupported deployments, this method logs a warning and skips index creation.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     对标记 <c>MongoSearchIndexAttribute</c> 的实体对象，自动创建 MongoDB Atlas Search 和 Vector Search 索引。
    ///     需要 MongoDB Atlas 或 MongoDB 8.2+ 社区版。
    ///     在不支持的部署上，此方法记录警告并跳过索引创建。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     This method starts a background service via <see cref="IHostApplicationLifetime" /> that runs once at startup.
    ///     The service is properly managed with graceful cancellation and disposal.
    ///     For new code, prefer using <see cref="AddMongoSearchIndexCreation{T}" /> during service registration instead.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     此方法通过 <see cref="IHostApplicationLifetime" /> 启动一个在启动时运行一次的后台服务。
    ///     该服务具有正确的取消和释放管理。
    ///     对于新代码，建议在服务注册阶段使用 <see cref="AddMongoSearchIndexCreation{T}" /> 代替。
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">
    ///     <see cref="MongoContext" />
    /// </typeparam>
    /// <param name="app">
    ///     <see cref="IApplicationBuilder" />
    /// </param>
    [Obsolete("Use AddMongoSearchIndexCreation<T>() during service registration to let the host manage lifecycle.")]
    public static IApplicationBuilder UseCreateMongoSearchIndexes<T>(this IApplicationBuilder app) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(app);
        var serviceProvider = app.ApplicationServices;
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(SearchIndexExtensions));
        var lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
        // Create the background service when the application has fully started.
        // The service is properly started, awaited, and disposed on shutdown.
        lifetime.ApplicationStarted.Register(void () =>
        {
            var backgroundService = ActivatorUtilities.CreateInstance<SearchIndexBackgroundService<T>>(serviceProvider);
            var stoppingToken = lifetime.ApplicationStopping;
            var disposed = 0;

            // Register disposal on application stopping to prevent resource leaks.
            stoppingToken.Register(async void () =>
            {
                try
                {
                    await backgroundService.StopAsync(CancellationToken.None).ConfigureAwait(false);
                    DisposeOnce();
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "Failed to stop {ServiceType} during shutdown.", typeof(SearchIndexBackgroundService<T>).Name);
                }
            });
            var startTask = backgroundService.StartAsync(stoppingToken);
            _ = startTask.ContinueWith(task =>
            {
                if (task.Exception is null)
                {
                    return;
                }
                logger?.LogError(task.Exception.Flatten(), "Failed to start {ServiceType}.", typeof(SearchIndexBackgroundService<T>).Name);
                DisposeOnce();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            _ = Task.Run(async () =>
            {
                try
                {
                    await startTask.ConfigureAwait(false);
                    if (backgroundService.ExecuteTask is not null)
                    {
                        await backgroundService.ExecuteTask.ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // graceful stop
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "{ServiceType} terminated unexpectedly.", typeof(SearchIndexBackgroundService<T>).Name);
                }
                finally
                {
                    DisposeOnce();
                }
            }, stoppingToken);
            return;

            void DisposeOnce()
            {
                if (Interlocked.Exchange(ref disposed, 1) != 0)
                {
                    return;
                }
                try
                {
                    backgroundService.Dispose();
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "Failed to dispose {ServiceType}.", typeof(SearchIndexBackgroundService<T>).Name);
                }
            }
        });
        return app;
    }
}
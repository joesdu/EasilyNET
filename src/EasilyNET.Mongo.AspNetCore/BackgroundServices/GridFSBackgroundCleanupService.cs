using EasilyNET.Mongo.AspNetCore.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Mongo.AspNetCore.BackgroundServices;

/// <summary>
///     <para xml:lang="en">Background service for cleaning up GridFS temporary files and expired sessions</para>
///     <para xml:lang="zh">用于清理 GridFS 临时文件和过期会话的后台服务</para>
/// </summary>
internal sealed class GridFSBackgroundCleanupService(IServiceProvider sp, ILogger<GridFSBackgroundCleanupService> logger) : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("GridFS Cleanup Service is starting.");
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error occurred while cleaning up GridFS resources.");
                }
            }

            // Wait for next cleanup cycle
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("GridFS Cleanup Service is stopping.");
        }
    }

    private async Task CleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = sp.CreateScope();
        // Ensure GridFSCleanupHelper is registered in DI
        var cleanupHelper = scope.ServiceProvider.GetService<GridFSCleanupHelper>();
        if (cleanupHelper == null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("GridFSCleanupHelper is not registered. Skipping cleanup.");
            }
            return;
        }
        // 1. Cleanup expired sessions (DB + Temp files)
        var deletedSessions = await cleanupHelper.CleanupExpiredSessionsAsync(stoppingToken);
        if (deletedSessions > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Cleaned up {Count} expired upload sessions.", deletedSessions);
        }
        // 2. Cleanup orphaned chunks (GridFS)
        var deletedChunks = await cleanupHelper.CleanupOrphanedChunksAsync(stoppingToken);
        if (deletedChunks > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Cleaned up {Count} orphaned GridFS chunks.", deletedChunks);
        }
    }
}
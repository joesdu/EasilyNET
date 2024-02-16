using EasilyNET.IdentityServer.MongoStorage.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// Helper to clean up expired persisted grants.
/// </summary>
public class TokenCleanupService(IServiceProvider serviceProvider, TokenCleanupOptions options, ILogger<TokenCleanupService> logger) : BackgroundService
{
    private readonly ILogger<TokenCleanupService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly TokenCleanupOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        if (!_options.Enable)
        {
            _logger.LogDebug("Grant removal is not enabled");
            return;
        }
        if (_options.Interval < 1)
        {
            _logger.LogDebug("Grant removal interval must be more than 1 second");
            return;
        }
        try
        {
            _logger.LogDebug("Grant removal started");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_options.Interval * 1000, stoppingToken); // ms
                await RemoveExpiredGrantsAsync();
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError("Error running grant removal: {exception}", ex.Message);
        }
        finally
        {
            _logger.LogDebug("Grant removal ended");
        }
    }

    private async Task RemoveExpiredGrantsAsync()
    {
        using var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var tokenCleanup = serviceScope.ServiceProvider.GetRequiredService<TokenCleanup>();
        await tokenCleanup.RemoveExpiredGrantsAsync();
    }
}
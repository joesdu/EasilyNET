using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// Background service for event subscription.
/// </summary>
internal sealed class SubscribeService(IServiceProvider serviceProvider) : BackgroundService
{
    /// <summary>
    /// Executes the background service to subscribe to events.
    /// </summary>
    /// <param name="cancelToken">Token to monitor for cancellation requests.</param>
    protected override async Task ExecuteAsync(CancellationToken cancelToken)
    {
        using var scope = serviceProvider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>() as EventBus ?? throw new InvalidOperationException("IBus service is not registered.");
        await bus.Subscribe();
        while (!cancelToken.IsCancellationRequested) await Task.Delay(5000, cancelToken);
    }
}
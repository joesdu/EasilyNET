using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
///     <para xml:lang="en">Background service for event subscription</para>
///     <para xml:lang="zh">用于事件订阅的后台服务</para>
/// </summary>
internal sealed class SubscribeService(IServiceProvider serviceProvider) : BackgroundService
{
    /// <summary>
    ///     <para xml:lang="en">Executes the background service to subscribe to events</para>
    ///     <para xml:lang="zh">执行后台服务以订阅事件</para>
    /// </summary>
    /// <param name="cancelToken">
    ///     <para xml:lang="en">Token to monitor for cancellation requests</para>
    ///     <para xml:lang="zh">用于监控取消请求的令牌</para>
    /// </param>
    protected override async Task ExecuteAsync(CancellationToken cancelToken)
    {
        using var scope = serviceProvider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>() as EventBus ?? throw new InvalidOperationException("IBus service is not registered.");
        await bus.RunRabbit();
        while (!cancelToken.IsCancellationRequested)
        {
            await Task.Delay(5000, cancelToken);
        }
    }
}
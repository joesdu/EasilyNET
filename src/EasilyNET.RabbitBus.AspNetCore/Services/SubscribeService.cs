using EasilyNET.RabbitBus.AspNetCore.Manager;
using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasilyNET.RabbitBus.AspNetCore.Services;

/// <summary>
///     <para xml:lang="en">Background service for event subscription and message retry</para>
///     <para xml:lang="zh">用于事件订阅和消息重试的后台服务</para>
/// </summary>
internal sealed class SubscribeService(IServiceProvider sp, PersistentConnection connection) : BackgroundService
{
    /// <summary>
    ///     <para xml:lang="en">Executes the background service to subscribe to events and handle retries</para>
    ///     <para xml:lang="zh">执行后台服务以订阅事件并处理重试</para>
    /// </summary>
    /// <param name="cancelToken">
    ///     <para xml:lang="en">Token to monitor for cancellation requests</para>
    ///     <para xml:lang="zh">用于监控取消请求的令牌</para>
    /// </param>
    protected override async Task ExecuteAsync(CancellationToken cancelToken)
    {
        // 初始化连接并启动消费者
        await connection.InitializeAsync(cancelToken);
        var ibus = sp.GetRequiredService<IBus>() as EventBus ?? throw new InvalidOperationException("IBus service is not registered or is not of type EventBus.");
        await ibus.RunRabbit(cancelToken);
    }
}
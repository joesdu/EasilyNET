using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// 后台任务进行事件订阅
/// </summary>
internal class SubscribeService(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancelToken)
    {
        using var scope = serviceProvider.CreateScope();
        var eventBus = scope.ServiceProvider.GetService<IBus>() as EventBus ?? throw new("RabbitMQ集成事件总线没有注册");
        eventBus.Subscribe();
        while (!cancelToken.IsCancellationRequested) await Task.Delay(5000, cancelToken);
    }
}
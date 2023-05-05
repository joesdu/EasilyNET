using EasilyNET.RabbitBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasilyNET.RabbitBus.Manager;

/// <summary>
/// 后台任务进行事件订阅
/// </summary>
internal class SubscribeService : BackgroundService
{
    private readonly IServiceProvider _rootServiceProvider;

    public SubscribeService(IServiceProvider serviceProvider)
    {
        _rootServiceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelToken)
    {
        using var scope = _rootServiceProvider.CreateScope();
        var eventBus = scope.ServiceProvider.GetService<IIntegrationEventBus>() as IntegrationEventBus ?? throw new("RabbitMQ集成事件总线没有注册");
        eventBus.Subscribe();
        while (!cancelToken.IsCancellationRequested) await Task.Delay(5000, cancelToken);
    }
}
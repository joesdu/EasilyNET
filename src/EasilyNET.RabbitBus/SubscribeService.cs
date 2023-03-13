using EasilyNET.RabbitBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasilyNET.RabbitBus;

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
        var eventBus = (IntegrationEventBus)scope.ServiceProvider.GetService<IIntegrationEventBus>()! ?? throw new("RabbitMQ集成事件总线没有注册");
        eventBus.Subscribe();
        while (!cancelToken.IsCancellationRequested) await Task.Delay(5000, cancelToken);
    }
}
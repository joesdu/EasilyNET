using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.RabbitBus.Core;
using System.Text.Json;
using WebApi.Test.Unit.Events;

// ReSharper disable UnusedType.Global

namespace WebApi.Test.Unit.EventHandlers;

/// <inheritdoc />
/// 若是要测试死信队列,需要将这个注释掉.
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class HelloWorldEventHandlers : IIntegrationEventHandler<HelloWorldEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(HelloWorldEvent @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(HelloWorldEventHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class DeadLetterEventHandlers : IIntegrationEventDeadLetterHandler<HelloWorldEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(HelloWorldEvent @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(DeadLetterEventHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class WorkQueuesEventOneHandlers : IIntegrationEventHandler<WorkQueuesEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(WorkQueuesEvent @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(WorkQueuesEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

#region Fanout(发布/订阅)模式

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class FanoutEventOneHandlers : IIntegrationEventHandler<FanoutEventOne>
{
    /// <inheritdoc />
    public Task HandleAsync(FanoutEventOne @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(FanoutEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class FanoutEventTwoHandlers : IIntegrationEventHandler<FanoutEventTwo>
{
    /// <inheritdoc />
    public Task HandleAsync(FanoutEventTwo @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(FanoutEventTwoHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

#endregion

#region Routing(路由)模式

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class DirectEventOneHandlers : IIntegrationEventHandler<DirectEventOne>
{
    /// <inheritdoc />
    public Task HandleAsync(DirectEventOne @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(DirectEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class DirectEventTwoHandlers : IIntegrationEventHandler<DirectEventTwo>
{
    /// <inheritdoc />
    public Task HandleAsync(DirectEventTwo @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(DirectEventTwoHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

#endregion

#region (Topic)主题模式

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class TopicEventOneHandlers : IIntegrationEventHandler<TopicEventOne>
{
    /// <inheritdoc />
    public Task HandleAsync(TopicEventOne @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(TopicEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class TopicEventTwoHandlers : IIntegrationEventHandler<TopicEventTwo>
{
    /// <inheritdoc />
    public Task HandleAsync(TopicEventTwo @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(TopicEventTwoHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

#endregion
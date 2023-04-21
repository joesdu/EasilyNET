using EasilyNET.AutoDependencyInjection.Attributes;
using EasilyNET.RabbitBus.Core;
using System.Text.Json;
using WebApi.Test.Unit.Events;

// ReSharper disable UnusedType.Global

namespace WebApi.Test.Unit.EventHandlers;

/// <summary>
/// HelloWorld消息处理
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class HelloWorldEventHandlers : IIntegrationEventHandler<HelloWorldEvent>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(HelloWorldEvent @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(HelloWorldEventHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// WorkQueues消息处理
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class WorkQueuesEventOneHandlers : IIntegrationEventHandler<WorkQueuesEvent>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(WorkQueuesEvent @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(WorkQueuesEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

#region Fanout(发布/订阅)模式

/// <summary>
/// FanoutOne消息处理
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class FanoutEventOneHandlers : IIntegrationEventHandler<FanoutEventOne>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(FanoutEventOne @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(FanoutEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// FanoutTwo消息处理
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class FanoutEventTwoHandlers : IIntegrationEventHandler<FanoutEventTwo>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(FanoutEventTwo @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(FanoutEventTwoHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

#endregion

#region Routing(路由)模式

/// <summary>
/// FanoutOne消息处理
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class DirectEventOneHandlers : IIntegrationEventHandler<DirectEventOne>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(DirectEventOne @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(DirectEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// FanoutTwo消息处理
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class DirectEventTwoHandlers : IIntegrationEventHandler<DirectEventTwo>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(DirectEventTwo @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(DirectEventTwoHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

#endregion

#region (Topic)主题模式

/// <summary>
/// FanoutOne消息处理
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class TopicEventOneHandlers : IIntegrationEventHandler<TopicEventOne>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(TopicEventOne @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(TopicEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// FanoutTwo消息处理
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public class TopicEventTwoHandlers : IIntegrationEventHandler<TopicEventTwo>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(TopicEventTwo @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(TopicEventTwoHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

#endregion
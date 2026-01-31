using System.Text.Json;
using EasilyNET.RabbitBus.Core.Abstraction;
using WebApi.Test.Unit.Events;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedType.Global

namespace WebApi.Test.Unit.EventHandlers;

/// <inheritdoc />
public class HelloWorldEventHandlers(ILogger<HelloWorldEventHandlers> logger) : IEventHandler<HelloWorldEvent>
{
    /// <inheritdoc />
    public Task HandleAsync(HelloWorldEvent @event)
    {
        logger.LogInformation("[消息处理自:{handler}]-{msg}", nameof(HelloWorldEventHandlers), JsonSerializer.Serialize(@event));
        logger.LogInformation("枚举类型:{EventGender}", @event.Gender);
        @event.SomeMethod();
        return Task.CompletedTask;
    }
}

/// <inheritdoc />
// 现代配置方式：QoS配置通过RabbitModule.cs中的流畅API配置
// 不再需要 [Qos(0, 20)] 属性
public class WorkQueuesEventOneHandlers : IEventHandler<WorkQueuesEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkQueuesEvent @event)
    {
        var threadId = Environment.CurrentManagedThreadId;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Thread-{threadId}] 处理消息: {@event.Summary}");
        await Task.CompletedTask;
    }
}

#region Fanout(发布/订阅)模式

/// <inheritdoc />
public class FanoutEventOneHandlers : IEventHandler<FanoutEventOne>
{
    /// <inheritdoc />
    public Task HandleAsync(FanoutEventOne @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(FanoutEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <inheritdoc />
public class FanoutEventTwoHandlers : IEventHandler<FanoutEventTwo>
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
public class DirectEventOneHandlers : IEventHandler<DirectEventOne>
{
    /// <inheritdoc />
    public Task HandleAsync(DirectEventOne @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(DirectEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <inheritdoc />
public class DirectEventTwoHandlers : IEventHandler<DirectEventTwo>
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
public class TopicEventOneHandlers : IEventHandler<TopicEventOne>
{
    /// <inheritdoc />
    public Task HandleAsync(TopicEventOne @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(TopicEventOneHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

/// <inheritdoc />
public class TopicEventTwoHandlers : IEventHandler<TopicEventTwo>
{
    /// <inheritdoc />
    public Task HandleAsync(TopicEventTwo @event)
    {
        Console.WriteLine($"[消息处理自:{nameof(TopicEventTwoHandlers)}]-{JsonSerializer.Serialize(@event)}");
        return Task.CompletedTask;
    }
}

#endregion
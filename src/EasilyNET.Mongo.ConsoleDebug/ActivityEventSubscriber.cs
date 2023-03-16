using MongoDB.Driver.Core.Events;
using System.Reflection;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local

namespace EasilyNET.Mongo.ConsoleDebug;

/// <summary>
/// 利用IEventSubscriber实现MongoDB语句输出到控制台.
/// </summary>
public sealed class ActivityEventSubscriber : IEventSubscriber
{
    private static readonly HashSet<string> CommandsWithCollectionNameAsValue = new()
    {
        "aggregate",
        "count",
        "distinct",
        "mapReduce",
        "geoSearch",
        "delete",
        "find",
        "killCursors",
        "findAndModify",
        "insert",
        "update",
        "create",
        "drop",
        "createIndexes",
        "listIndexes"
    };

    private readonly InstrumentationOptions _options;
    private readonly ReflectionEventSubscriber _subscriber;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ActivityEventSubscriber() : this(new()) { }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options"></param>
    // ReSharper disable once MemberCanBePrivate.Global
    public ActivityEventSubscriber(InstrumentationOptions options)
    {
        _options = options;
        _subscriber = new(this, bindingFlags: BindingFlags.Instance | BindingFlags.NonPublic);
    }

    /// <summary>
    /// TryGetEventHandler
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="handler"></param>
    /// <returns></returns>
    public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler) => _subscriber.TryGetEventHandler(out handler);

#pragma warning disable IDE0051
    private void Handle(CommandStartedEvent @event)
    {
        if (_options.ShouldStartActivity is not null && !_options.ShouldStartActivity(@event))
            return;
        if (!CommandsWithCollectionNameAsValue.Contains(@event.CommandName))
            return;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss} INF] MongoRequest: {@event.RequestId},Command: ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        var json = @event.Command.Elements;
        //Console.WriteLine($"{json}");
        Console.ForegroundColor = ConsoleColor.White;
    }

    private static bool Predicate(string name) => !string.IsNullOrWhiteSpace(name);

#pragma warning disable CA1822
    private void Handle(CommandSucceededEvent @event)
    {
        if (!CommandsWithCollectionNameAsValue.Contains(@event.CommandName))
            return;
        Console.Write($"[{DateTime.Now:HH:mm:ss} INF] MongoRequest: {@event.RequestId},Status: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Succeeded");
        Console.ForegroundColor = ConsoleColor.White;
    }

    private void Handle(CommandFailedEvent @event)
    {
        if (!CommandsWithCollectionNameAsValue.Contains(@event.CommandName))
            return;
        Console.Write($"[{DateTime.Now:HH:mm:ss} INF] MongoRequest: {@event.RequestId},Status: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Failed");
        Console.ForegroundColor = ConsoleColor.White;
    }
#pragma warning restore CA1822
#pragma warning restore IDE0051
}
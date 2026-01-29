using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using EasilyNET.Mongo.ConsoleDebug.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Events;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local

namespace EasilyNET.Mongo.ConsoleDebug.Subscribers;

/// <summary>
///     <para xml:lang="en">Command context for tracking request state</para>
///     <para xml:lang="zh">用于跟踪请求状态的命令上下文</para>
/// </summary>
/// <param name="StartTime">
///     <para xml:lang="en">The timestamp when the command started</para>
///     <para xml:lang="zh">命令开始时的时间戳</para>
/// </param>
/// <param name="InfoJson">
///     <para xml:lang="en">JSON string containing command information</para>
///     <para xml:lang="zh">包含命令信息的 JSON 字符串</para>
/// </param>
/// <param name="CommandJson">
///     <para xml:lang="en">JSON string containing the command content</para>
///     <para xml:lang="zh">包含命令内容的 JSON 字符串</para>
/// </param>
/// <param name="CollectionName">
///     <para xml:lang="en">The name of the collection being operated on</para>
///     <para xml:lang="zh">正在操作的集合名称</para>
/// </param>
internal sealed record CommandContext(long StartTime, string InfoJson, string CommandJson, string CollectionName);

/// <summary>
///     <para xml:lang="en">Use <see cref="IEventSubscriber" /> to output MongoDB statements to the console, recommended for use in test environments.</para>
///     <para xml:lang="zh">利用 <see cref="IEventSubscriber" /> 实现 MongoDB 语句输出到控制台,推荐在测试环境中使用。</para>
/// </summary>
public sealed class ActivityEventConsoleDebugSubscriber : IEventSubscriber
{
    /// <summary>
    ///     <para xml:lang="en">Maximum number of tracked requests before cleanup</para>
    ///     <para xml:lang="zh">触发清理前的最大跟踪请求数</para>
    /// </summary>
    private const int MaxTrackedRequests = 100;

    /// <summary>
    ///     <para xml:lang="en">Age threshold in seconds for cleaning up stale requests</para>
    ///     <para xml:lang="zh">清理过期请求的时间阈值（秒）</para>
    /// </summary>
    private const int StaleRequestThresholdSeconds = 60;

    private readonly ConsoleDebugInstrumentationOptions _options;

    private readonly ConcurrentDictionary<int, CommandContext> _requestContexts = new();
    private readonly ReflectionEventSubscriber _subscriber;

    /// <summary>
    ///     <para xml:lang="en">Initialize a new instance of the <see cref="ActivityEventConsoleDebugSubscriber" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="ActivityEventConsoleDebugSubscriber" /> 类的新实例。</para>
    /// </summary>
    public ActivityEventConsoleDebugSubscriber() : this(new()) { }

    /// <summary>
    ///     <para xml:lang="en">Initialize a new instance of the <see cref="ActivityEventConsoleDebugSubscriber" /> class with the specified options.</para>
    ///     <para xml:lang="zh">使用指定的选项初始化 <see cref="ActivityEventConsoleDebugSubscriber" /> 类的新实例。</para>
    /// </summary>
    /// <param name="options">
    ///     <para xml:lang="en">Console debug instrumentation options.</para>
    ///     <para xml:lang="zh">控制台调试仪器选项。</para>
    /// </param>
    public ActivityEventConsoleDebugSubscriber(ConsoleDebugInstrumentationOptions options)
    {
        _options = options;
        _subscriber = new(this, bindingFlags: BindingFlags.Instance | BindingFlags.NonPublic);
    }

    /// <summary>
    ///     <para xml:lang="en">Try to get the event handler.</para>
    ///     <para xml:lang="zh">尝试获取事件处理程序。</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type.</para>
    ///     <para xml:lang="zh">事件类型。</para>
    /// </typeparam>
    /// <param name="handler">
    ///     <para xml:lang="en">Event handler.</para>
    ///     <para xml:lang="zh">事件处理程序。</para>
    /// </param>
    public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler) => _subscriber.TryGetEventHandler(out handler);

    /// <summary>
    ///     <para xml:lang="en">Check if the command should be filtered based on options</para>
    ///     <para xml:lang="zh">根据选项检查命令是否应被过滤</para>
    /// </summary>
    private bool ShouldFilterCommand(string commandName, string? collectionName)
    {
        if (!_options.Enable)
        {
            return true;
        }
        if (!CommonExtensions.CommandsWithCollectionNameAsValue.Contains(commandName))
        {
            return true;
        }
        return collectionName is not null && _options.ShouldStartCollection is not null && !_options.ShouldStartCollection(collectionName);
    }

    /// <summary>
    ///     <para xml:lang="en">Try to get collection name from request context</para>
    ///     <para xml:lang="zh">尝试从请求上下文获取集合名称</para>
    /// </summary>
    private bool TryGetCollectionName(int requestId, out string? collectionName)
    {
        if (_requestContexts.TryGetValue(requestId, out var context))
        {
            collectionName = context.CollectionName;
            return true;
        }
        collectionName = null;
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Clean up stale requests that have been tracked for too long</para>
    ///     <para xml:lang="zh">清理跟踪时间过长的过期请求</para>
    /// </summary>
    private void CleanupStaleRequests()
    {
        if (_requestContexts.Count <= MaxTrackedRequests)
        {
            return;
        }
        var currentTimestamp = Stopwatch.GetTimestamp();
        var staleThresholdTicks = StaleRequestThresholdSeconds * Stopwatch.Frequency;
        foreach (var kvp in _requestContexts)
        {
            if (currentTimestamp - kvp.Value.StartTime > staleThresholdTicks)
            {
                _requestContexts.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static void WriteStatus(CommandContext context, int requestId, bool success)
    {
        var endTime = Stopwatch.GetTimestamp();
        var duration = Stopwatch.GetElapsedTime(context.StartTime, endTime).TotalMilliseconds;
        Style durationColor = duration switch
        {
            < 100 => new(new Color(0, 175, 0)),   // 绿色
            < 200 => new(new Color(255, 215, 0)), // 黄色
            _     => new(new Color(175, 0, 0))    // 红色
        };
        var table = new Table
        {
            Border = new RoundedTableBorder()
        };
        table.AddColumn(new TableColumn("Time").Centered());
        table.AddColumn(new TableColumn("Duration (ms)").Centered());
        table.AddColumn(new TableColumn("Status").Centered());
        var rowData = new IRenderable[]
        {
            new Text($"{DateTime.Now:HH:mm:ss.fff}", new(new Color(255, 215, 0))),
            new Text($"{duration:F4}", durationColor),
            new Text(success ? "succeed" : "failed", new(success ? new(0, 175, 0) : new Color(175, 0, 0)))
        };
        table.AddRow(rowData);
        var layout = new Layout("Root")
            .SplitColumns(new Layout(new Panel(new Text(context.CommandJson, new(Color.Purple)))
                {
                    Height = 45,
                    Header = new("Command", Justify.Center)
                }.Collapse().Border(new RoundedBoxBorder()).NoSafeBorder().Expand())
                {
                    MinimumSize = 48,
                    Size = 72
                },
                new Layout(new Rows(new Panel(new Calendar(DateTime.Now)
                {
                    HeaderStyle = new(Color.Blue, decoration: Decoration.Bold),
                    HighlightStyle = new(Color.Pink1, decoration: Decoration.Bold)
                }.AddCalendarEvent(DateTime.Today))
                {
                    Height = 13,
                    Header = new("Calendar", Justify.Center)
                }.Collapse().Border(new RoundedBoxBorder()).NoSafeBorder().Expand(), new Panel(new JsonText(context.InfoJson)
                {
                    BracesStyle = Color.Red,
                    BracketsStyle = Color.Green,
                    CommaStyle = Color.Red,
                    StringStyle = Color.Green,
                    NumberStyle = Color.Blue
                })
                {
                    Height = 13,
                    Header = new("Info", Justify.Center)
                }.Collapse().Border(new RoundedBoxBorder()).NoSafeBorder().Expand(), new Panel(table)
                {
                    Height = 7,
                    Header = new($"Request [#ff5f00]{requestId}[/] Status", Justify.Center)
                }.Collapse().Border(new RoundedBoxBorder()).NoSafeBorder().Expand(), new Panel(new Text("""
                                                                                                          --------------------------------------
                                                                                                        /     Only two things are infinite,      \
                                                                                                        \   the universe and human stupidity.    /
                                                                                                          --------------------------------------
                                                                                                                     ^__^     O   ^__^
                                                                                                             _______/(oo)      o  (oo)\_______
                                                                                                         /\/(       /(__)         (__)\       )\/\
                                                                                                            ||w----||                 ||----w||
                                                                                                            ||     ||                 ||     ||
                                                                                                        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                                                                                        """, new(Color.Orange1)))
                {
                    Height = 12,
                    Header = new("NiuNiu", Justify.Center)
                }.Collapse().Border(new RoundedBoxBorder()).NoSafeBorder().Expand()))
                {
                    Name = "Right",
                    MinimumSize = 46,
                    Size = 46
                });
        AnsiConsole.Write(new Panel(layout)
        {
            Height = 47,
            Border = BoxBorder.Rounded
        }.NoBorder().NoSafeBorder().Expand());
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandStartedEvent @event)
    {
        // 清理过期请求
        CleanupStaleRequests();
        if (@event.Command.Elements.All(c => c.Name != @event.CommandName))
        {
            return;
        }
        var collName = @event.Command.Elements.First(c => c.Name == @event.CommandName).Value.ToString() ?? "N/A";
        if (ShouldFilterCommand(@event.CommandName, collName))
        {
            // 即使被过滤，也需要记录集合名称以便后续事件使用
            _requestContexts.TryAdd(@event.RequestId, new(Stopwatch.GetTimestamp(),
                string.Empty,
                string.Empty,
                collName));
            return;
        }
        var endpoint = @event.ConnectionId?.ServerId?.EndPoint as DnsEndPoint;
        var infoJson = $$"""
                         {
                           "RequestId": {{@event.RequestId}},
                           "Timestamp": "{{@event.Timestamp}}",
                           "Method": "{{@event.CommandName}}",
                           "Database": "{{@event.DatabaseNamespace?.DatabaseName}}",
                           "Collection": "{{collName}}",
                           "ClusterId": {{@event.ConnectionId?.ServerId?.ClusterId.Value}},
                           "Host": "{{endpoint?.Host ?? "N/A"}}",
                           "Port": {{endpoint?.Port}}
                         }
                         """;
        var commandJson = @event.Command.ToJson(new() { Indent = true, OutputMode = JsonOutputMode.Shell });
        _requestContexts.TryAdd(@event.RequestId, new(Stopwatch.GetTimestamp(),
            infoJson,
            commandJson,
            collName));
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandSucceededEvent @event)
    {
        if (!_requestContexts.TryRemove(@event.RequestId, out var context))
        {
            return;
        }

        // 检查是否有有效的命令数据（未被过滤的请求）
        if (string.IsNullOrEmpty(context.CommandJson))
        {
            return;
        }
        if (ShouldFilterCommand(@event.CommandName, context.CollectionName))
        {
            return;
        }
        WriteStatus(context, @event.RequestId, true);
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandFailedEvent @event)
    {
        if (!_requestContexts.TryRemove(@event.RequestId, out var context))
        {
            return;
        }

        // 检查是否有有效的命令数据（未被过滤的请求）
        if (string.IsNullOrEmpty(context.CommandJson))
        {
            return;
        }
        if (ShouldFilterCommand(@event.CommandName, context.CollectionName))
        {
            return;
        }
        WriteStatus(context, @event.RequestId, false);
    }
}
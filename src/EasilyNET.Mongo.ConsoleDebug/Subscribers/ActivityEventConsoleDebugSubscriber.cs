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
///     <para xml:lang="en">Use <see cref="IEventSubscriber" /> to output MongoDB statements to the console, recommended for use in test environments.</para>
///     <para xml:lang="zh">利用 <see cref="IEventSubscriber" /> 实现 MongoDB 语句输出到控制台,推荐在测试环境中使用。</para>
/// </summary>
public sealed class ActivityEventConsoleDebugSubscriber : IEventSubscriber
{
    private static readonly ConcurrentDictionary<int, string> RequestIdWithCollectionName = [];
    private readonly ConsoleDebugInstrumentationOptions _options;
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

    private long StartTime { get; set; }

    private long EndTime { get; set; }

    private string InfoJson { get; set; } = string.Empty;

    private string CommandJson { get; set; } = string.Empty;

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

    private void WritStatus(int requestId, bool success)
    {
        var duration = Stopwatch.GetElapsedTime(StartTime, EndTime).TotalMilliseconds; // 计算耗时，单位为毫秒
        var durationColor = duration switch
        {
            < 100 => "[#00af00]", // 绿色
            < 200 => "[#ffd700]", // 黄色
            _     => "[#af0000]"  // 红色
        };
        var table = new Table
        {
            Border = new RoundedTableBorder()
        };
        table.AddColumn(new TableColumn("Time").Centered());
        table.AddColumn(new TableColumn("Duration (ms)").Centered());
        table.AddColumn(new TableColumn("Status").Centered());
        table.AddRow($"[#ffd700]{DateTime.Now:HH:mm:ss.fff}[/]", $"{durationColor}{duration:F4}[/]", success ? "[#00af00]succeed[/]" : "[#af0000]failed[/]");
        var layout = new Layout("Root")
            .SplitColumns(new Layout(new Panel(new Text(CommandJson, new(Color.Purple)))
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
                }.Collapse().Border(new RoundedBoxBorder()).NoSafeBorder().Expand(), new Panel(new JsonText(InfoJson)
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
        if (RequestIdWithCollectionName.Count > 50) RequestIdWithCollectionName.Clear();
        if (@event.Command.Elements.All(c => c.Name != @event.CommandName)) return;
        StartTime = Stopwatch.GetTimestamp();
        var collName = @event.Command.Elements.First(c => c.Name == @event.CommandName).Value.ToString() ?? "N/A";
        RequestIdWithCollectionName.AddOrUpdate(@event.RequestId, collName, (_, v) => v);
        switch (_options.Enable)
        {
            case true when !CommonExtensions.CommandsWithCollectionNameAsValue.Contains(@event.CommandName):
            case true when _options.ShouldStartCollection is not null && !_options.ShouldStartCollection(collName):
                return;
            case true:
            {
                var endpoint = @event.ConnectionId?.ServerId?.EndPoint as DnsEndPoint;
                // 使用字符串的方式替代序列化
                InfoJson = $$"""
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
                CommandJson = @event.Command.ToJson(new() { Indent = true, OutputMode = JsonOutputMode.Shell });
                break;
            }
        }
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandSucceededEvent @event)
    {
        if (!_options.Enable) return;
        if (_options.ShouldStartCollection is not null)
        {
            var success = RequestIdWithCollectionName.TryGetValue(@event.RequestId, out var collName);
            if (success && !_options.ShouldStartCollection(collName!)) return;
        }
        if (!CommonExtensions.CommandsWithCollectionNameAsValue.Contains(@event.CommandName)) return;
        EndTime = Stopwatch.GetTimestamp();
        WritStatus(@event.RequestId, true);
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandFailedEvent @event)
    {
        EndTime = Stopwatch.GetTimestamp();
        if (!_options.Enable) return;
        if (_options.ShouldStartCollection is not null)
        {
            var success = RequestIdWithCollectionName.TryGetValue(@event.RequestId, out var collName);
            if (success && !_options.ShouldStartCollection(collName!)) return;
        }
        if (!CommonExtensions.CommandsWithCollectionNameAsValue.Contains(@event.CommandName)) return;
        WritStatus(@event.RequestId, false);
    }
}
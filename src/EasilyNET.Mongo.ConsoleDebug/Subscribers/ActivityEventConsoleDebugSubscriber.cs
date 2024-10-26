using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EasilyNET.Mongo.ConsoleDebug.Options;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local

namespace EasilyNET.Mongo.ConsoleDebug.Subscribers;

/// <summary>
/// 利用 <see cref="IEventSubscriber" /> 实现 MongoDB 语句输出到控制台,推荐在测试环境中使用.
/// </summary>
public sealed class ActivityEventConsoleDebugSubscriber : IEventSubscriber
{
    private static readonly ConcurrentDictionary<int, string> RequestIdWithCollectionName = [];
    private readonly ConsoleDebugInstrumentationOptions _options;
    private readonly ReflectionEventSubscriber _subscriber;

    /// <summary>
    /// 初始化 <see cref="ActivityEventConsoleDebugSubscriber" /> 类的新实例.
    /// </summary>
    public ActivityEventConsoleDebugSubscriber() : this(new()) { }

    /// <summary>
    /// 使用指定的选项初始化 <see cref="ActivityEventConsoleDebugSubscriber" /> 类的新实例.
    /// </summary>
    /// <param name="options">控制台调试仪器选项.</param>
    public ActivityEventConsoleDebugSubscriber(ConsoleDebugInstrumentationOptions options)
    {
        _options = options;
        _subscriber = new(this, bindingFlags: BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private string InfoJson { get; set; } = string.Empty;

    private string CommandJson { get; set; } = string.Empty;

    /// <summary>
    /// 尝试获取事件处理程序.
    /// </summary>
    /// <typeparam name="TEvent">事件类型.</typeparam>
    /// <param name="handler">事件处理程序.</param>
    /// <returns>如果找到事件处理程序,则为 <c>true</c>；否则为 <c>false</c>.</returns>
    public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler) => _subscriber.TryGetEventHandler(out handler);

    private void WritStatus(int request_id, bool success)
    {
        var table = new Table
        {
            Border = new RoundedTableBorder()
        };
        table.AddColumn(new TableColumn("RequestId").Centered());
        table.AddColumn(new TableColumn("Time").Centered());
        table.AddColumn(new TableColumn("Status").Centered());
        table.AddRow($"{request_id}", $"[#ffd700]{DateTime.Now:HH:mm:ss.fffff}[/]", success ? "[#00af00]Succeeded[/]" : "[#af0000]Failed[/]");
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
                    Header = new("Request Status", Justify.Center)
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
            Height = 47
        }.NoBorder().NoSafeBorder().Expand());
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandStartedEvent @event)
    {
        if (RequestIdWithCollectionName.Count > 50) RequestIdWithCollectionName.Clear();
        if (@event.Command.Elements.All(c => c.Name != @event.CommandName)) return;
        var coll_name = @event.Command.Elements.First(c => c.Name == @event.CommandName).Value.ToString() ?? "N/A";
        RequestIdWithCollectionName.AddOrUpdate(@event.RequestId, coll_name, (_, v) => v);
        switch (_options.Enable)
        {
            case true when !CommonExtensions.CommandsWithCollectionNameAsValue.Contains(@event.CommandName):
            case true when _options.ShouldStartCollection is not null && !_options.ShouldStartCollection(coll_name):
                return;
            case true:
            {
                // 使用字符串的方式替代序列化
                InfoJson = $$"""
                             {
                               "RequestId": {{@event.RequestId}},
                               "Timestamp": "{{@event.Timestamp:yyyy-MM-dd HH:mm:ss}}",
                               "Method": "{{@event.CommandName}}",
                               "DatabaseName": "{{@event.DatabaseNamespace?.DatabaseName}}",
                               "CollectionName": "{{coll_name}}",
                               "ConnectionInfo": {
                                  "ClusterId": {{@event.ConnectionId?.ServerId?.ClusterId.Value}},
                                  "EndPoint": "{{@event.ConnectionId?.ServerId?.EndPoint}}"
                               }
                             }
                             """;
                CommandJson = @event.Command.ToJson(new() { Indent = true });
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
            var success = RequestIdWithCollectionName.TryGetValue(@event.RequestId, out var coll_name);
            if (success && !_options.ShouldStartCollection(coll_name!)) return;
        }
        if (!CommonExtensions.CommandsWithCollectionNameAsValue.Contains(@event.CommandName)) return;
        WritStatus(@event.RequestId, true);
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandFailedEvent @event)
    {
        if (!_options.Enable) return;
        if (_options.ShouldStartCollection is not null)
        {
            var success = RequestIdWithCollectionName.TryGetValue(@event.RequestId, out var coll_name);
            if (success && !_options.ShouldStartCollection(coll_name!)) return;
        }
        if (!CommonExtensions.CommandsWithCollectionNameAsValue.Contains(@event.CommandName)) return;
        WritStatus(@event.RequestId, false);
    }
}
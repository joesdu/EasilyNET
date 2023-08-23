using MongoDB.Bson;
using MongoDB.Driver.Core.Events;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;
using System.Collections.Concurrent;
using System.Reflection;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local

namespace EasilyNET.Mongo.ConsoleDebug;

/// <summary>
/// 利用IEventSubscriber实现MongoDB语句输出到控制台,推荐测试环境使用.
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

    private static readonly ConcurrentDictionary<int, string> RequestIdWithCollectionName = new();

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

    private string InfoJson { get; set; } = string.Empty;

    private string CommandJson { get; set; } = string.Empty;

    /// <summary>
    /// TryGetEventHandler
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="handler"></param>
    /// <returns></returns>
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

#pragma warning disable IDE0051
    private void Handle(CommandStartedEvent @event)
    {
        if (RequestIdWithCollectionName.Count > 50) RequestIdWithCollectionName.Clear();
        if (@event.Command.Elements.All(c => c.Name != @event.CommandName)) return;
        var coll_name = @event.Command.Elements.First(c => c.Name == @event.CommandName).Value.ToString() ?? "N/A";
        RequestIdWithCollectionName.AddOrUpdate(@event.RequestId, coll_name, (_, v) => v);
        switch (_options.Enable)
        {
            case true when !CommandsWithCollectionNameAsValue.Contains(@event.CommandName):
                return;
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
                               "DatabaseName": "{{@event.DatabaseNamespace.DatabaseName}}",
                               "CollectionName": "{{coll_name}}",
                               "ConnectionInfo": {
                                  "ClusterId": {{@event.ConnectionId.ServerId.ClusterId.Value}},
                                  "EndPoint": "{{@event.ConnectionId.ServerId.EndPoint}}"
                               }
                             }
                             """;
                CommandJson = @event.Command.ToJson(new() { Indent = true });
                //const int CommandMaxLength = 2000;
                //if (CommandJson.Length >= CommandMaxLength) CommandJson = $"{CommandJson[..CommandMaxLength]}\n...\nExcessively long text truncation(命令过长截断)";
                break;
            }
        }
    }

    private void Handle(CommandSucceededEvent @event)
    {
        if (!_options.Enable) return;
        if (_options.ShouldStartCollection is not null)
        {
            var success = RequestIdWithCollectionName.TryGetValue(@event.RequestId, out var coll_name);
            if (success && !_options.ShouldStartCollection(coll_name!)) return;
        }
        if (!CommandsWithCollectionNameAsValue.Contains(@event.CommandName)) return;
        WritStatus(@event.RequestId, true);
    }

    private void Handle(CommandFailedEvent @event)
    {
        if (!_options.Enable) return;
        if (_options.ShouldStartCollection is not null)
        {
            var success = RequestIdWithCollectionName.TryGetValue(@event.RequestId, out var coll_name);
            if (success && !_options.ShouldStartCollection(coll_name!)) return;
        }
        if (!CommandsWithCollectionNameAsValue.Contains(@event.CommandName)) return;
        WritStatus(@event.RequestId, false);
    }
#pragma warning restore IDE0051
}
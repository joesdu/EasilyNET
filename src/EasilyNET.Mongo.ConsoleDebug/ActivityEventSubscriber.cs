using MongoDB.Bson;
using MongoDB.Driver.Core.Events;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;
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
                    MinimumSize = 64,
                    Size = 96
                },
                new Layout(new Rows(new Panel(new Calendar(DateTime.Now)
                {
                    HeaderStyle = Style.Parse("blue bold"),
                    HighlightStyle = Style.Parse("yellow bold")
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
                    Header = new("Mongo Info", Justify.Center)
                }.Collapse().Border(new RoundedBoxBorder()).NoSafeBorder().Expand(), new Panel(table)
                {
                    Height = 7,
                    Header = new("Mongo Request Status", Justify.Center)
                }.Collapse().Border(new RoundedBoxBorder()).NoSafeBorder().Expand(), new Panel(new Text("""
                                                                                                         ________________________________________
                                                                                                        /     Only two things are infinite,      \
                                                                                                        \   the universe and human stupidity.    /
                                                                                                         ----------------------------------------
                                                                                                                     ^__^     O   ^__^
                                                                                                             _______/(oo)      o  (oo)\_______
                                                                                                         /\/(       /(__)         (__)\       )\/\
                                                                                                            ||w----||                 ||----w||
                                                                                                            ||     ||                 ||     ||
                                                                                                        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                                                                                        """, new(Color.Orange1)))
                {
                    Height = 12,
                    Header = new("YongGan NiuNiu", Justify.Center)
                }.Collapse().Border(new RoundedBoxBorder()).NoSafeBorder().Expand()))
                {
                    Name = "Right",
                    MinimumSize = 46,
                    Size = 46
                });
        AnsiConsole.Write(new Panel(layout)
        {
            Height = 47
        }.NoBorder().NoSafeBorder());
        // 解决最后未换行的问题
        Console.WriteLine();
    }

#pragma warning disable IDE0051
    private void Handle(CommandStartedEvent @event)
    {
        if (_options.ShouldStartActivity is not null && !_options.ShouldStartActivity(@event)) return;
        if (!CommandsWithCollectionNameAsValue.Contains(@event.CommandName)) return;
        // 使用字符串的方式替代序列化
        InfoJson = $$"""
                     {
                       "RequestId": {{@event.RequestId}},
                       "Timestamp": "{{@event.Timestamp:yyyy-MM-dd HH:mm:ss}}",
                       "Method": "{{@event.CommandName}}",
                       "DatabaseName": "{{@event.DatabaseNamespace.DatabaseName}}",
                       "CollectionName": "{{@event.Command.Elements.First(c => c.Name == @event.CommandName).Value}}",
                       "ConnectionInfo": {
                          "ClusterId": {{@event.ConnectionId.ServerId.ClusterId.Value}},
                          "EndPoint": "{{@event.ConnectionId.ServerId.EndPoint}}"
                       }
                     }
                     """;
        CommandJson = @event.Command.ToJson(new() { Indent = true });
        if (CommandJson.Length >= 1000) CommandJson = $"{CommandJson[..1000]}\n...\n Excessively long text truncation(文本过长截断)";
    }

    private void Handle(CommandSucceededEvent @event)
    {
        if (!CommandsWithCollectionNameAsValue.Contains(@event.CommandName)) return;
        WritStatus(@event.RequestId, true);
    }

    private void Handle(CommandFailedEvent @event)
    {
        if (!CommandsWithCollectionNameAsValue.Contains(@event.CommandName)) return;
        WritStatus(@event.RequestId, false);
    }
#pragma warning restore IDE0051
}
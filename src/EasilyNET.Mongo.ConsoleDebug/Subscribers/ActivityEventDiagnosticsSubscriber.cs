using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using EasilyNET.Mongo.ConsoleDebug.Options;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;

// ReSharper disable UnusedMember.Local

namespace EasilyNET.Mongo.ConsoleDebug.Subscribers;

/// <summary>
///     <para xml:lang="en">Subscribe to MongoDB driver diagnostic events and convert them to OpenTelemetry activities</para>
///     <para xml:lang="zh">订阅MongoDB驱动程序的诊断事件,并将其转换为OpenTelemetry活动</para>
/// </summary>
public sealed class ActivityEventDiagnosticsSubscriber : IEventSubscriber
{
    private const string ActivityName = "MongoDB.Driver.Core.Events.Command";

    private static readonly AssemblyName AssemblyName = typeof(ActivityEventConsoleDebugSubscriber).Assembly.GetName();
    private static readonly string? ActivitySourceName = AssemblyName.Name ?? string.Empty;
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName, CommonExtensions.GetVersion<ActivityEventConsoleDebugSubscriber>());

    private readonly ConcurrentDictionary<int, Activity> _activityMap = new();

    private readonly DiagnosticsInstrumentationOptions _options;
    private readonly ReflectionEventSubscriber _subscriber;

    /// <summary>
    ///     <para xml:lang="en">Initialize a new instance of the <see cref="ActivityEventDiagnosticsSubscriber" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="ActivityEventDiagnosticsSubscriber" /> 类的新实例。</para>
    /// </summary>
    public ActivityEventDiagnosticsSubscriber() : this(new()) { }

    /// <summary>
    ///     <para xml:lang="en">Initialize a new instance of the <see cref="ActivityEventDiagnosticsSubscriber" /> class with the specified options.</para>
    ///     <para xml:lang="zh">使用指定的选项初始化 <see cref="ActivityEventDiagnosticsSubscriber" /> 类的新实例。</para>
    /// </summary>
    /// <param name="options">
    ///     <para xml:lang="en">Diagnostics instrumentation options.</para>
    ///     <para xml:lang="zh">诊断仪器选项。</para>
    /// </param>
    public ActivityEventDiagnosticsSubscriber(DiagnosticsInstrumentationOptions options)
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
    ///     <para xml:lang="en">Check if command text should be captured based on options and command type</para>
    ///     <para xml:lang="zh">根据选项和命令类型检查是否应捕获命令文本</para>
    /// </summary>
    private bool ShouldCaptureCommandText(string commandName, string collectionName)
    {
        if (!_options.CaptureCommandText)
        {
            return false;
        }
        // 检查是否为 GridFS chunks 的 insert 命令,以避免捕获大量二进制数据
        return !_options.ExcludeGridFSChunks || commandName != "insert" || !collectionName.EndsWith(".chunks", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     <para xml:lang="en">Truncate command text if it exceeds the maximum length</para>
    ///     <para xml:lang="zh">如果命令文本超过最大长度则截断</para>
    /// </summary>
    private string TruncateCommandText(string commandText)
    {
        if (_options.MaxCommandTextLength > 0 && commandText.Length > _options.MaxCommandTextLength)
        {
            return string.Concat(commandText.AsSpan(0, _options.MaxCommandTextLength), "\n... [truncated]");
        }
        return commandText;
    }

    /// <summary>
    ///     <para xml:lang="en">Add endpoint tags to the activity based on endpoint type</para>
    ///     <para xml:lang="zh">根据端点类型向活动添加端点标签</para>
    /// </summary>
    private static void AddEndpointTags(Activity activity, EndPoint? endPoint)
    {
        switch (endPoint)
        {
            case IPEndPoint ipEndPoint:
                activity.AddTag("network.peer.address", ipEndPoint.Address.ToString());
                activity.AddTag("network.peer.port", ipEndPoint.Port.ToString());
                break;
            case DnsEndPoint dnsEndPoint:
                activity.AddTag("server.address", dnsEndPoint.Host);
                activity.AddTag("server.port", dnsEndPoint.Port.ToString());
                break;
        }
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandStartedEvent @event)
    {
        if (_options.ShouldStartActivity is not null && !_options.ShouldStartActivity(@event))
        {
            return;
        }
        // ReSharper disable once ExplicitCallerInfoArgument
        var activity = ActivitySource.StartActivity(ActivityName, ActivityKind.Client);
        if (activity is null)
        {
            return;
        }
        var databaseName = @event.DatabaseNamespace?.DatabaseName;
        if (@event.Command.Elements.All(c => c.Name != @event.CommandName))
        {
            return;
        }
        var collName = @event.Command.Elements.First(c => c.Name == @event.CommandName).Value.ToString() ?? "N/A";
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/database/database-spans.md
        activity.DisplayName = string.IsNullOrEmpty(collName) ? $"{@event.CommandName} {databaseName}" : $"{@event.CommandName} {collName}";
        activity.AddTag("db.system", "mongodb");
        activity.AddTag("db.connection_id", @event.ConnectionId?.ToString());
        activity.AddTag("db.namespace", databaseName);
        activity.AddTag("db.collection.name", collName);
        activity.AddTag("db.operation.name", @event.CommandName);
        activity.AddTag("network.transport", "tcp");
        AddEndpointTags(activity, @event.ConnectionId?.ServerId?.EndPoint);
        if (activity.IsAllDataRequested && ShouldCaptureCommandText(@event.CommandName, collName))
        {
            var commandText = @event.Command.ToJson(new() { Indent = true });
            activity.AddTag("db.query.text", TruncateCommandText(commandText));
        }
        _activityMap.TryAdd(@event.RequestId, activity);
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandSucceededEvent @event)
    {
        if (_activityMap.TryRemove(@event.RequestId, out var activity))
        {
            WithReplacedActivityCurrent(activity, () => activity.Stop());
        }
    }

    [SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
    private void Handle(CommandFailedEvent @event)
    {
        if (_activityMap.TryRemove(@event.RequestId, out var activity))
        {
            WithReplacedActivityCurrent(activity, () =>
            {
                var tags = new ActivityTagsCollection
                {
                    { "exception.type", @event.Failure.GetType().FullName },
                    { "exception.stacktrace", @event.Failure.ToString() }
                };
                if (!string.IsNullOrEmpty(@event.Failure.Message))
                {
                    tags.Add("exception.message", @event.Failure.Message);
                }
                activity.AddEvent(new("exception", DateTimeOffset.UtcNow, tags));
                activity.SetStatus(ActivityStatusCode.Error);
                activity.Stop();
            });
        }
    }

    private static void WithReplacedActivityCurrent(Activity activity, Action action)
    {
        var current = Activity.Current;
        if (activity == current)
        {
            action();
            return;
        }
        try
        {
            Activity.Current = activity;
            action();
        }
        finally
        {
            // it's forbidden to assign stopped activity to Activity.Current
            Activity.Current = current?.IsStopped == true ? null : current;
        }
    }
}
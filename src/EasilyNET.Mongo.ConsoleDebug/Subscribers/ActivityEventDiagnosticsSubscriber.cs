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
        var endPoint = @event.ConnectionId?.ServerId?.EndPoint;
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
        if (activity.IsAllDataRequested && _options.CaptureCommandText)
        {
            activity.AddTag("db.query.text", @event.Command.ToJson(new() { Indent = true }));
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
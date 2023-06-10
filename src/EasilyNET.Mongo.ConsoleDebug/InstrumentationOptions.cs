using MongoDB.Driver.Core.Events;

namespace EasilyNET.Mongo.ConsoleDebug;

/// <summary>
/// 选项
/// </summary>
public sealed class InstrumentationOptions
{
    /// <summary>
    /// 是否开启Activity
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Func<CommandStartedEvent, bool>? ShouldStartActivity { get; set; }
}
using MongoDB.Driver.Core.Events;

namespace EasilyNET.Mongo.ConsoleDebug.Options;

/// <summary>
///     <para xml:lang="en">Diagnostics options</para>
///     <para xml:lang="zh">诊断选项</para>
/// </summary>
public sealed class DiagnosticsInstrumentationOptions
{
    /// <summary>
    ///     <para xml:lang="en">Whether to capture command text</para>
    ///     <para xml:lang="zh">是否捕获命令文本</para>
    /// </summary>
    public bool CaptureCommandText { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Configure the events to listen to</para>
    ///     <para xml:lang="zh">配置需要监听的事件</para>
    /// </summary>
    public Func<CommandStartedEvent, bool>? ShouldStartActivity { get; set; }
}
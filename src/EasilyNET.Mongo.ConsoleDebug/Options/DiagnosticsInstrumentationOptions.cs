using MongoDB.Driver.Core.Events;

namespace EasilyNET.Mongo.ConsoleDebug.Options;

/// <summary>
/// 诊断选项
/// </summary>
public sealed class DiagnosticsInstrumentationOptions
{
    /// <summary>
    /// 是否捕获命令文本
    /// </summary>
    public bool CaptureCommandText { get; set; }

    /// <summary>
    /// 配置需要监听的事件
    /// </summary>
    public Func<CommandStartedEvent, bool>? ShouldStartActivity { get; set; }
}
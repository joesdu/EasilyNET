using MongoDB.Driver.Core.Events;

// ReSharper disable UnusedAutoPropertyAccessor.Global

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
    ///     <para xml:lang="en">Exclude GridFS chunks insert commands from capturing command text to avoid high memory usage from binary data</para>
    ///     <para xml:lang="zh">排除 GridFS chunks 插入命令的命令文本捕获,以避免二进制数据导致的高内存使用</para>
    /// </summary>
    public bool ExcludeGridFSChunks { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Maximum length of command text to capture. Commands exceeding this length will be truncated. Set to 0 for no limit.</para>
    ///     <para xml:lang="zh">捕获的命令文本的最大长度。超过此长度的命令将被截断。设置为 0 表示无限制。</para>
    /// </summary>
    public int MaxCommandTextLength { get; set; } = 1000;

    /// <summary>
    ///     <para xml:lang="en">Configure the events to listen to</para>
    ///     <para xml:lang="zh">配置需要监听的事件</para>
    /// </summary>
    public Func<CommandStartedEvent, bool>? ShouldStartActivity { get; set; }
}
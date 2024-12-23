namespace EasilyNET.Mongo.ConsoleDebug.Options;

/// <summary>
///     <para xml:lang="en">Options</para>
///     <para xml:lang="zh">选项</para>
/// </summary>
public sealed class ConsoleDebugInstrumentationOptions
{
    /// <summary>
    ///     <para xml:lang="en">Whether to enable, default value: <see langword="true" /></para>
    ///     <para xml:lang="zh">是否启用, 默认值: <see langword="true" /></para>
    /// </summary>
    public bool Enable { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Filter which collections need to enable information output</para>
    ///     <para xml:lang="zh">过滤哪些集合需要开启信息输出</para>
    /// </summary>
    public Func<string, bool>? ShouldStartCollection { get; set; }
}
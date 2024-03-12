namespace EasilyNET.Mongo.ConsoleDebug;

/// <summary>
/// 选项
/// </summary>
public sealed class InstrumentationOptions
{
    /// <summary>
    /// 是否启用, 默认值: <see langword="true" />
    /// </summary>
    public bool Enable { get; set; } = true;

    /// <summary>
    /// 过滤哪些集合需要开启信息输出
    /// </summary>
    public Func<string, bool>? ShouldStartCollection { get; set; }
}

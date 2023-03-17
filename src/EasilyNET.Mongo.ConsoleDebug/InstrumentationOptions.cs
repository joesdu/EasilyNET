using MongoDB.Driver.Core.Events;

namespace EasilyNET.Mongo.ConsoleDebug;

/// <summary>
/// 选项
/// </summary>
public sealed class InstrumentationOptions
{
    /// <summary>
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Func<CommandStartedEvent, bool>? ShouldStartActivity { get; set; }

    /// <summary>
    /// JSON输出字段最大长度,默认:200个字符,避免过长(如Base64)影响阅读.
    /// </summary>
    public int FiledMaxLength { get; set; } = 200;
}
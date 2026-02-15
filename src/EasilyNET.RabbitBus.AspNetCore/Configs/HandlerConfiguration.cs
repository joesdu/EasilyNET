namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
///     <para xml:lang="en">Handler configuration with ordering support</para>
///     <para xml:lang="zh">带排序支持的处理器配置</para>
/// </summary>
public sealed class HandlerConfiguration
{
    /// <summary>
    ///     <para xml:lang="en">Handler type</para>
    ///     <para xml:lang="zh">处理器类型</para>
    /// </summary>
    public required Type HandlerType { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Execution order (lower values execute first). Default is 0</para>
    ///     <para xml:lang="zh">执行顺序（值越小越先执行）。默认为0</para>
    /// </summary>
    public int Order { get; init; }
}
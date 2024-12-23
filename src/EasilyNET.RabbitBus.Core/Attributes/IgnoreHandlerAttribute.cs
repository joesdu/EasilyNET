namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
///     <para xml:lang="en">Ignore message handler</para>
///     <para xml:lang="zh">忽略消息处理Handler</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreHandlerAttribute : Attribute;
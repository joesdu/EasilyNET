namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// 忽略消息处理Handler
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreHandlerAttribute : Attribute;
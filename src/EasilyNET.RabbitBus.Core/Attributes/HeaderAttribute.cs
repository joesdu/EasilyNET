// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// 添加RabbitMQ, Headers参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class HeaderAttribute(string key, object value) : RabbitDictionaryAttribute(key, value);

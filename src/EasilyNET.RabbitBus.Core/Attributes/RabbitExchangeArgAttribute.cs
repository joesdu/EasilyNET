// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// RabbitMQ交换机参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RabbitExchangeArgAttribute(string key, object value) : RabbitDictionaryAttribute(key, value);
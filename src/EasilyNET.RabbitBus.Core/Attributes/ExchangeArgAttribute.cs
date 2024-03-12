// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// RabbitMQ Exchange参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExchangeArgAttribute(string key, object value) : RabbitDictionaryAttribute(key, value);

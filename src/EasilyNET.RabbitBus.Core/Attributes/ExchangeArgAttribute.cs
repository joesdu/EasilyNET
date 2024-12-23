// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
///     <para xml:lang="en">RabbitMQ Exchange parameter attribute</para>
///     <para xml:lang="zh">RabbitMQ Exchange参数特性</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExchangeArgAttribute(string key, object value) : RabbitDictionaryAttribute(key, value);
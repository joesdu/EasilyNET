// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
///     <para xml:lang="en">Adds RabbitMQ Headers parameter attribute</para>
///     <para xml:lang="zh">添加RabbitMQ Headers参数特性</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class HeaderAttribute(string key, object value) : RabbitDictionaryAttribute(key, value);
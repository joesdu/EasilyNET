namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
///     <para xml:lang="en">Arguments applied to the queue</para>
///     <para xml:lang="zh">作用于队列的Arguments</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class QueueArgAttribute(string key, object value) : RabbitDictionaryAttribute(key, value);
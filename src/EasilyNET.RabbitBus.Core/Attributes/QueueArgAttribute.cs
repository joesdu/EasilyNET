namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// 作用于队列的Arguments
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
// ReSharper disable once ClassNeverInstantiated.Global
public class QueueArgAttribute(string key, object value) : RabbitDictionaryAttribute(key, value);
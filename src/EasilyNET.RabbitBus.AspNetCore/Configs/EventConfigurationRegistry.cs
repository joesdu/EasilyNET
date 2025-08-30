using EasilyNET.RabbitBus.Core.Abstraction;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
///     <para xml:lang="en">Configuration registry for RabbitMQ events</para>
///     <para xml:lang="zh">RabbitMQ事件配置注册器</para>
/// </summary>
public sealed class EventConfigurationRegistry
{
    private readonly Dictionary<Type, EventConfiguration> _configurations = [];

    /// <summary>
    ///     <para xml:lang="en">Register configuration for an event type</para>
    ///     <para xml:lang="zh">为事件类型注册配置</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="configure">
    ///     <para xml:lang="en">Configuration action</para>
    ///     <para xml:lang="zh">配置操作</para>
    /// </param>
    public void Configure<TEvent>(Action<EventConfiguration> configure) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        var config = _configurations.GetValueOrDefault(eventType) ??
                     new EventConfiguration
                     {
                         EventType = eventType
                     };
        configure(config);
        _configurations[eventType] = config;
    }

    /// <summary>
    ///     <para xml:lang="en">Get configuration for an event type</para>
    ///     <para xml:lang="zh">获取事件类型的配置</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <returns>
    ///     <para xml:lang="en">Event configuration</para>
    ///     <para xml:lang="zh">事件配置</para>
    /// </returns>
    public EventConfiguration? GetConfiguration<TEvent>() where TEvent : IEvent => _configurations.GetValueOrDefault(typeof(TEvent));

    /// <summary>
    ///     <para xml:lang="en">Get configuration for an event type</para>
    ///     <para xml:lang="zh">获取事件类型的配置</para>
    /// </summary>
    /// <param name="eventType">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Event configuration</para>
    ///     <para xml:lang="zh">事件配置</para>
    /// </returns>
    public EventConfiguration? GetConfiguration(Type eventType) => _configurations.GetValueOrDefault(eventType);

    /// <summary>
    ///     <para xml:lang="en">Get all configurations</para>
    ///     <para xml:lang="zh">获取所有配置</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">All event configurations</para>
    ///     <para xml:lang="zh">所有事件配置</para>
    /// </returns>
    public IEnumerable<EventConfiguration> GetAllConfigurations() => _configurations.Values;

    /// <summary>
    ///     <para xml:lang="en">Check if configuration exists for event type</para>
    ///     <para xml:lang="zh">检查是否存在事件类型的配置</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <returns>
    ///     <para xml:lang="en">True if configuration exists</para>
    ///     <para xml:lang="zh">如果配置存在则为true</para>
    /// </returns>
    public bool HasConfiguration<TEvent>() where TEvent : IEvent => _configurations.ContainsKey(typeof(TEvent));
}
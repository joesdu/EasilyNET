using System.Reflection;
using EasilyNET.RabbitBus.AspNetCore.Enums;

namespace EasilyNET.RabbitBus.AspNetCore.Abstractions;

/// <summary>
///     <para xml:lang="en">Interface for managing subscriptions</para>
///     <para xml:lang="zh">管理订阅的接口</para>
/// </summary>
internal interface ISubscriptionsManager
{
    /// <summary>
    ///     <para xml:lang="en">Clears all subscription mappings</para>
    ///     <para xml:lang="zh">清除所有订阅映射</para>
    /// </summary>
    void ClearSubscriptions();

    /// <summary>
    ///     <para xml:lang="en">Adds a subscription</para>
    ///     <para xml:lang="zh">添加一个订阅</para>
    /// </summary>
    /// <param name="eventType">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件的类型</para>
    /// </param>
    /// <param name="handleKind">
    ///     <para xml:lang="en">The kind of event handler</para>
    ///     <para xml:lang="zh">事件处理程序的类型</para>
    /// </param>
    /// <param name="handlerTypes">
    ///     <para xml:lang="en">The type information of the event handlers</para>
    ///     <para xml:lang="zh">事件处理程序的类型信息</para>
    /// </param>
    void AddSubscription(Type eventType, EKindOfHandler handleKind, IList<TypeInfo> handlerTypes);

    /// <summary>
    ///     <para xml:lang="en">Gets the handlers for a specific event</para>
    ///     <para xml:lang="zh">获取特定事件的处理程序</para>
    /// </summary>
    /// <param name="name">
    ///     <para xml:lang="en">The name of the event</para>
    ///     <para xml:lang="zh">事件的名称</para>
    /// </param>
    /// <param name="handleKind">
    ///     <para xml:lang="en">The kind of event handler</para>
    ///     <para xml:lang="zh">事件处理程序的类型</para>
    /// </param>
    IEnumerable<Type> GetHandlersForEvent(string name, EKindOfHandler handleKind);

    /// <summary>
    ///     <para xml:lang="en">Checks if there are subscriptions for a specific event</para>
    ///     <para xml:lang="zh">检查是否有特定事件的订阅</para>
    /// </summary>
    /// <param name="name">
    ///     <para xml:lang="en">The name of the event</para>
    ///     <para xml:lang="zh">事件的名称</para>
    /// </param>
    /// <param name="handleKind">
    ///     <para xml:lang="en">The kind of event handler</para>
    ///     <para xml:lang="zh">事件处理程序的类型</para>
    /// </param>
    bool HasSubscriptionsForEvent(string name, EKindOfHandler handleKind);
}
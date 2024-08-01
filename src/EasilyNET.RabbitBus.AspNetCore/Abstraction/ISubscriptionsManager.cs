using System.Reflection;
using EasilyNET.RabbitBus.AspNetCore.Enums;

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// 订阅管理
/// </summary>
internal interface ISubscriptionsManager
{
    /// <summary>
    /// 清除所有订阅对应关系
    /// </summary>
    void ClearSubscriptions();

    /// <summary>
    /// 添加订阅
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handleKind">事件处理器种类</param>
    /// <param name="handlerTypes">事件处理器类型信息</param>
    void AddSubscription(Type eventType, EKindOfHandler handleKind, IList<TypeInfo> handlerTypes);

    /// <summary>
    /// 获取消息处理程序
    /// </summary>
    /// <param name="name"></param>
    /// <param name="handleKind">事件处理器种类</param>
    /// <returns></returns>
    IEnumerable<Type> GetHandlersForEvent(string name, EKindOfHandler handleKind);

    /// <summary>
    /// 判断订阅者是否存在
    /// </summary>
    /// <param name="name"></param>
    /// <param name="handleKind">事件处理器种类</param>
    /// <returns></returns>
    bool HasSubscriptionsForEvent(string name, EKindOfHandler handleKind);
}
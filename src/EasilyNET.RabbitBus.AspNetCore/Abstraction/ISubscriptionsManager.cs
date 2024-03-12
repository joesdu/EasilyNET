using System.Reflection;

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// 订阅管理
/// </summary>
internal interface ISubscriptionsManager
{
    /// <summary>
    /// 清除所有
    /// </summary>
    void Clear();

    /// <summary>
    /// 添加订阅
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="isDlx">是否是延时队列</param>
    /// <param name="handlerType">事件处理器类型</param>
    void AddSubscription(Type eventType, bool isDlx, IList<TypeInfo> handlerType);

    /// <summary>
    /// 获取消息处理程序
    /// </summary>
    /// <param name="name"></param>
    /// <param name="isDlx">是否是延迟消息</param>
    /// <returns></returns>
    IEnumerable<Type> GetHandlersForEvent(string name, bool isDlx);

    /// <summary>
    /// 判断订阅者是否存在
    /// </summary>
    /// <param name="name"></param>
    /// <param name="isDlx">是否是延迟消息</param>
    /// <returns></returns>
    bool HasSubscriptionsForEvent(string name, bool isDlx);

    /// <summary>
    /// 获取事件Key
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    string GetEventKey(Type type);
}

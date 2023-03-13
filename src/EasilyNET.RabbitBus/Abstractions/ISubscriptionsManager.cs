namespace EasilyNET.RabbitBus.Abstractions;

/// <summary>
/// 订阅管理器接口
/// </summary>
internal interface ISubscriptionsManager
{
    /// <summary>
    /// 清除所有订阅
    /// </summary>
    void Clear();

    /// <summary>
    /// 添加订阅
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handlerType">事件处理器类型</param>
    void AddSubscription(Type eventType, Type handlerType);

    /// <summary>
    /// 获取事件处理程序
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    IEnumerable<Type> GetHandlersForEvent(string eventName);

    /// <summary>
    /// 判断订阅者是否存在
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    bool HasSubscriptionsForEvent(string eventName);

    /// <summary>
    /// 获取事件Key
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    string GetEventKey(Type type);
}
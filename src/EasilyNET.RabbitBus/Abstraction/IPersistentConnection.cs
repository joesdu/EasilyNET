using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.Abstraction;

/// <summary>
/// RabbitMQ链接
/// </summary>
internal abstract class IPersistentConnection
{
    /// <summary>
    /// 是否链接
    /// </summary>
    internal abstract bool IsConnected { get; }

    /// <summary>
    /// 尝试链接
    /// </summary>
    /// <returns></returns>
    internal abstract bool TryConnect();

    /// <summary>
    /// 创建Model
    /// </summary>
    /// <returns></returns>
    internal abstract IModel CreateModel();
}
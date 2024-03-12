using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

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
    internal abstract Task TryConnect();

    /// <summary>
    /// 从池中获取Channel
    /// </summary>
    /// <returns></returns>
    internal abstract Task<IChannel> GetChannel();

    /// <summary>
    /// 归还连接池通道
    /// </summary>
    internal abstract Task ReturnChannel(IChannel channel);
}

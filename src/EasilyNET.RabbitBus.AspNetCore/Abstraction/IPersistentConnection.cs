using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// RabbitMQ链接
/// </summary>
internal interface IPersistentConnection : IDisposable
{
    /// <summary>
    /// 是否链接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 尝试链接
    /// </summary>
    /// <returns></returns>
    Task TryConnect();

    /// <summary>
    /// 从池中获取Channel
    /// </summary>
    /// <returns></returns>
    Task<IChannel> GetChannel();

    /// <summary>
    /// 归还连接池通道
    /// </summary>
    Task ReturnChannel(IChannel channel);
}
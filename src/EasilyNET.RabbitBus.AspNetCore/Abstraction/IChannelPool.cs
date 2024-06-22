using RabbitMQ.Client;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

internal interface IChannelPool : IDisposable
{
    /// <summary>
    /// 从池中获取Channel
    /// </summary>
    /// <returns></returns>
    Task<IChannel> GetChannel();

    /// <summary>
    /// 归还Channel到池或者释放
    /// </summary>
    /// <param name="channel"></param>
    Task ReturnChannel(IChannel channel);
}
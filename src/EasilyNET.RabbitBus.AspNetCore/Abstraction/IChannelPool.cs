using RabbitMQ.Client;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
///     <para xml:lang="en">Interface for a pool of channels</para>
///     <para xml:lang="zh">通道池接口</para>
/// </summary>
internal interface IChannelPool : IDisposable
{
    /// <summary>
    ///     <para xml:lang="en">Retrieves a channel from the pool</para>
    ///     <para xml:lang="zh">从池中获取一个通道</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation. The task result contains the retrieved channel</para>
    ///     <para xml:lang="zh">表示异步操作的任务。任务结果包含检索到的通道</para>
    /// </returns>
    Task<IChannel> GetChannel();

    /// <summary>
    ///     <para xml:lang="en">Returns a channel to the pool or releases it</para>
    ///     <para xml:lang="zh">将通道返回到池中或释放它</para>
    /// </summary>
    /// <param name="channel">
    ///     <para xml:lang="en">The channel to return or release</para>
    ///     <para xml:lang="zh">要返回或释放的通道</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation</para>
    ///     <para xml:lang="zh">表示异步操作的任务</para>
    /// </returns>
    Task ReturnChannel(IChannel channel);
}
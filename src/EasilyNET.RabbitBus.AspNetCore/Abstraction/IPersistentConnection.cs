using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
///     <para xml:lang="en">Interface for a persistent RabbitMQ connection</para>
///     <para xml:lang="zh">持久化 RabbitMQ 连接接口</para>
/// </summary>
internal interface IPersistentConnection : IDisposable
{
    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether the connection is established</para>
    ///     <para xml:lang="zh">获取一个值，指示连接是否已建立</para>
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     <para xml:lang="en">Attempts to establish a connection</para>
    ///     <para xml:lang="zh">尝试建立连接</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation</para>
    ///     <para xml:lang="zh">表示异步操作的任务</para>
    /// </returns>
    Task TryConnect();

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
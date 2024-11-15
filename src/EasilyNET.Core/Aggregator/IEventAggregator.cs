namespace EasilyNET.Core.Aggregator;

/// <summary>
///     <para xml:lang="en">Interface for an event aggregator, which is used to decouple communication between multiple objects.</para>
///     <para xml:lang="zh">事件聚合器接口，用于解耦多个对象之间的通信。</para>
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    ///     <para xml:lang="en">Registers a recipient to receive messages of type <typeparamref name="T" />.</para>
    ///     <para xml:lang="zh">注册一个接收者以接收类型为 <typeparamref name="T" /> 的消息。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of message to subscribe to.</para>
    ///     <para xml:lang="zh">要订阅的消息类型。</para>
    /// </typeparam>
    /// <param name="recipient">
    ///     <para xml:lang="en">The recipient object that will receive the messages.</para>
    ///     <para xml:lang="zh">将接收消息的接收者对象。</para>
    /// </param>
    /// <param name="action">
    ///     <para xml:lang="en">The action to be executed when a message of type <typeparamref name="T" /> is sent.</para>
    ///     <para xml:lang="zh">当发送类型为 <typeparamref name="T" /> 的消息时要执行的操作。</para>
    /// </param>
    void Register<T>(object recipient, Action<T> action) where T : class;

    /// <summary>
    ///     <para xml:lang="en">Unregisters a recipient from receiving messages of type <typeparamref name="T" />.</para>
    ///     <para xml:lang="zh">取消注册接收类型为 <typeparamref name="T" /> 的消息的接收者。</para>
    /// </summary>
    /// <param name="recipient">
    ///     <para xml:lang="en">The recipient object to be unregistered.</para>
    ///     <para xml:lang="zh">要取消注册的接收者对象。</para>
    /// </param>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of message to unsubscribe from.</para>
    ///     <para xml:lang="zh">要取消订阅的消息类型。</para>
    /// </typeparam>
    void Unregister<T>(object recipient) where T : class;

    /// <summary>
    ///     <para xml:lang="en">Sends a message of type <typeparamref name="T" /> to all registered recipients.</para>
    ///     <para xml:lang="zh">向所有注册的接收者发送类型为 <typeparamref name="T" /> 的消息。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">The message to be sent.</para>
    ///     <para xml:lang="zh">要发送的消息。</para>
    /// </param>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of message to send.</para>
    ///     <para xml:lang="zh">要发送的消息类型。</para>
    /// </typeparam>
    void Send<T>(T message) where T : class;
}
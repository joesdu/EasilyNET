using System.Collections.Concurrent;
using System.Reflection;

// ReSharper disable UnusedMember.Global

// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Aggregator;

/// <summary>
///     <para xml:lang="en">A simple event aggregator used to decouple communication between multiple objects, similar to IMessenger in WPF.</para>
///     <para xml:lang="zh">简单的事件发布聚合器，用于解耦多个对象之间的通信，类似 WPF 中的 IMessenger。</para>
/// </summary>
public sealed class SimpleEventAggregator : IEventAggregator, IDisposable
{
    private readonly Lock _lock = new();

    // Changed WeakReference to object for the key of the inner dictionary
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, Delegate>> _recipients = [];
    private bool _disposed;

    /// <summary>
    ///     <para xml:lang="en">Disposes the event aggregator and clears all registered recipients.</para>
    ///     <para xml:lang="zh">释放事件聚合器并清除所有注册的接收者。</para>
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public void Register<TMessage>(object recipient, Action<TMessage> action) where TMessage : class
    {
        // Default to weak reference
        RegisterInternal(recipient, action, false);
    }

    /// <inheritdoc />
    public void Unregister<TMessage>(object recipient) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(recipient);
        var messageType = typeof(TMessage);
        lock (_lock)
        {
            if (!_recipients.TryGetValue(messageType, out var subscribers))
            {
                return;
            }
            var keyToRemove = subscribers.Keys.FirstOrDefault(key =>
            {
                if (key is WeakReference wr)
                {
                    return wr.IsAlive && ReferenceEquals(wr.Target, recipient);
                }
                return ReferenceEquals(key, recipient); // Handles strong references
            });
            if (keyToRemove is not null)
            {
                subscribers.TryRemove(keyToRemove, out _);
            }
            if (subscribers.IsEmpty)
            {
                _recipients.TryRemove(messageType, out _);
            }
        }
    }

    /// <inheritdoc />
    public void Send<TMessage>(TMessage message) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        var messageType = typeof(TMessage);
        List<Action<TMessage>>? actionsToInvoke = null;
        List<object>? deadKeysToRemove = null; // Stores keys (WeakReference or recipient object) to remove
        lock (_lock)
        {
            if (_recipients.TryGetValue(messageType, out var subscribers))
            {
                actionsToInvoke = [];
                // Iterate over a copy of KeyValuePairs for safe removal from subscribers
                foreach (var (key, handlerDelegate) in subscribers.ToList())
                {
                    if (key is WeakReference weakRef)
                    {
                        var target = weakRef.Target;
                        if (!weakRef.IsAlive || target is null) // Check IsAlive and if target is null (collected)
                        {
                            deadKeysToRemove ??= [];
                            deadKeysToRemove.Add(key); // Add the WeakReference itself for removal
                            continue;                  // Skip to next subscriber
                        }
                    }
                    if (handlerDelegate is Action<TMessage> action)
                    {
                        actionsToInvoke.Add(action);
                    }
                    else
                    {
                        // This case should ideally not happen if registration is correct
                        // but good for robustness to remove invalid delegates/keys
                        deadKeysToRemove ??= [];
                        deadKeysToRemove.Add(key);
                    }
                }

                // Cleanup dead references or invalid entries
                if (deadKeysToRemove is not null && deadKeysToRemove.Count > 0)
                {
                    foreach (var deadKey in deadKeysToRemove)
                    {
                        subscribers.TryRemove(deadKey, out _);
                    }
                    if (subscribers.IsEmpty)
                    {
                        _recipients.TryRemove(messageType, out _);
                    }
                }
            }
        }
        if (actionsToInvoke is null)
        {
            return;
        }
        foreach (var action in actionsToInvoke)
        {
            try
            {
                action(message);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
            {
                // Handle cases where the target object is disposed between check and invocation
                // This is more likely with UI elements or other IDisposable recipients
                // Optionally log this occurrence
            }
            // Consider adding more generic catch or specific error handling if one subscriber error shouldn't stop others
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Registers a recipient to receive messages of type <typeparamref name="TMessage" />.</para>
    ///     <para xml:lang="zh">注册一个接收者以接收类型为 <typeparamref name="TMessage" /> 的消息。</para>
    /// </summary>
    /// <typeparam name="TMessage">
    ///     <para xml:lang="en">The type of message to subscribe to.</para>
    ///     <para xml:lang="zh">要订阅的消息类型。</para>
    /// </typeparam>
    /// <param name="recipient">
    ///     <para xml:lang="en">The recipient object that will receive the messages.</para>
    ///     <para xml:lang="zh">将接收消息的接收者对象。</para>
    /// </param>
    /// <param name="action">
    ///     <para xml:lang="en">The action to be executed when a message of type <typeparamref name="TMessage" /> is sent.</para>
    ///     <para xml:lang="zh">当发送类型为 <typeparamref name="TMessage" /> 的消息时要执行的操作。</para>
    /// </param>
    /// <param name="keepSubscriberReferenceAlive">
    ///     <para xml:lang="en">
    ///     If <see langword="true" />, the <see cref="SimpleEventAggregator" /> will keep a strong reference to the subscriber,
    ///     otherwise it will keep a weak reference.
    ///     </para>
    ///     <para xml:lang="zh">如果为 <see langword="true" />，<see cref="SimpleEventAggregator" /> 将保留对订阅者的强引用，否则将保留弱引用。</para>
    /// </param>
    public void Register<TMessage>(object recipient, Action<TMessage> action, bool keepSubscriberReferenceAlive) where TMessage : class
    {
        RegisterInternal(recipient, action, keepSubscriberReferenceAlive);
    }

    private void RegisterInternal<TMessage>(object recipient, Action<TMessage> action, bool keepSubscriberReferenceAlive) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(action);
        var messageType = typeof(TMessage);
        // targetReference is now correctly an object, which can be the recipient itself or a WeakReference
        var targetReference = keepSubscriberReferenceAlive ? recipient : new WeakReference(recipient);
        lock (_lock)
        {
            if (!_recipients.TryGetValue(messageType, out var subscribers))
            {
                subscribers = new();
                _recipients[messageType] = subscribers;
            }
            subscribers[targetReference] = action; // This is now type-correct
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Unregisters a recipient from all messages.</para>
    ///     <para xml:lang="zh">取消注册接收者的所有消息。</para>
    /// </summary>
    /// <param name="recipient">
    ///     <para xml:lang="en">The recipient object to be unregistered.</para>
    ///     <para xml:lang="zh">要取消注册的接收者对象。</para>
    /// </param>
    public void Unregister(object recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        lock (_lock)
        {
            // Iterate over a copy of keys for safe removal from _recipients
            foreach (var messageType in _recipients.Keys.ToList())
            {
                if (!_recipients.TryGetValue(messageType, out var subscribers))
                {
                    continue;
                }
                var keyToRemove = subscribers.Keys.FirstOrDefault(key =>
                {
                    if (key is WeakReference wr)
                    {
                        return wr.IsAlive && ReferenceEquals(wr.Target, recipient);
                    }
                    return ReferenceEquals(key, recipient); // Handles strong references
                });
                if (keyToRemove is not null)
                {
                    subscribers.TryRemove(keyToRemove, out _);
                }
                if (subscribers.IsEmpty)
                {
                    _recipients.TryRemove(messageType, out _);
                }
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            lock (_lock)
            {
                _recipients.Clear();
            }
        }
        _disposed = true;
    }

    /// <summary>
    ///     <para xml:lang="en">Finalizes an instance of the <see cref="SimpleEventAggregator" /> class.</para>
    ///     <para xml:lang="zh">释放 <see cref="SimpleEventAggregator" /> 类的实例。</para>
    /// </summary>
    ~SimpleEventAggregator()
    {
        Dispose(false);
    }
}
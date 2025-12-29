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
    private readonly ConcurrentDictionary<Type, IMessageSubscriptions> _subscriptions = new();
    private bool _disposed;

    /// <summary>
    ///     <para xml:lang="en">Disposes the event aggregator and clears all registered recipients.</para>
    ///     <para xml:lang="zh">释放事件聚合器并清除所有注册的接收者。</para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _subscriptions.Clear();
        _disposed = true;
    }

    /// <inheritdoc />
    public void Register<TMessage>(object recipient, Action<TMessage> action) where TMessage : class
    {
        Register(recipient, action, false);
    }

    /// <inheritdoc />
    public void Unregister<TMessage>(object recipient) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ThrowIfDisposed();
        if (_subscriptions.TryGetValue(typeof(TMessage), out var wrapper))
        {
            wrapper.Unregister(recipient);
        }
    }

    /// <inheritdoc />
    public void Send<TMessage>(TMessage message) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ThrowIfDisposed();
        if (_subscriptions.TryGetValue(typeof(TMessage), out var wrapper))
        {
            ((MessageSubscriptions<TMessage>)wrapper).Send(message);
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
    // ReSharper disable once MemberCanBePrivate.Global
    public void Register<TMessage>(object recipient, Action<TMessage> action, bool keepSubscriberReferenceAlive) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(action);
        ThrowIfDisposed();
        var subscriptions = (MessageSubscriptions<TMessage>)_subscriptions.GetOrAdd(typeof(TMessage), _ => new MessageSubscriptions<TMessage>());
        subscriptions.Add(recipient, action, keepSubscriberReferenceAlive);
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
        ThrowIfDisposed();
        foreach (var wrapper in _subscriptions.Values)
        {
            wrapper.Unregister(recipient);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SimpleEventAggregator));
    }

    private interface IMessageSubscriptions
    {
        void Unregister(object recipient);
    }

    private sealed class MessageSubscriptions<TMessage> : IMessageSubscriptions where TMessage : class
    {
        private readonly Lock _lock = new();
        private readonly List<Subscription> _subscribers = [];

        public void Unregister(object recipient)
        {
            lock (_lock)
            {
                for (var i = _subscribers.Count - 1; i >= 0; i--)
                {
                    if (_subscribers[i].Matches(recipient))
                    {
                        _subscribers.RemoveAt(i);
                    }
                }
            }
        }

        public void Add(object recipient, Action<TMessage> action, bool strong)
        {
            lock (_lock)
            {
                _subscribers.Add(new(recipient, action, strong));
            }
        }

        public void Send(TMessage message)
        {
            List<Subscription> snapshot;
            lock (_lock)
            {
                snapshot = [.. _subscribers];
            }
            var deadSubscribers = snapshot.Where(sub => !sub.Invoke(message)).ToList();
            if (deadSubscribers.Count <= 0)
            {
                return;
            }
            lock (_lock)
            {
                _subscribers.RemoveAll(c => deadSubscribers.Contains(c));
            }
        }

        private sealed class Subscription
        {
            private readonly Action<TMessage>? _action;
            private readonly MethodInfo? _method;
            private readonly object? _strongRecipient;
            private readonly WeakReference? _weakRecipient;

            public Subscription(object recipient, Action<TMessage> action, bool strong)
            {
                if (strong)
                {
                    _strongRecipient = recipient;
                    _action = action;
                }
                else
                {
                    _weakRecipient = new(recipient);
                    // If the action's target is the recipient, we must not hold the action strongly
                    // to avoid a memory leak (Action -> Target -> Recipient).
                    // We store the MethodInfo instead and invoke it via reflection.
                    if (action.Target == recipient)
                    {
                        _method = action.Method;
                    }
                    else
                    {
                        // If the target is not the recipient (e.g. a closure or static method),
                        // we store the action. Note that if the closure captures the recipient,
                        // it will still leak, but we can't easily detect that.
                        _action = action;
                    }
                }
            }

            public bool Matches(object recipient) =>
                _strongRecipient != null
                    ? ReferenceEquals(_strongRecipient, recipient)
                    : _weakRecipient?.Target is { } target && ReferenceEquals(target, recipient);

            public bool Invoke(TMessage message)
            {
                if (_strongRecipient != null)
                {
                    _action!(message);
                    return true;
                }
                var target = _weakRecipient?.Target;
                if (target is null)
                {
                    return false;
                }
                if (_action != null)
                {
                    _action(message);
                    return true;
                }
                if (_method == null)
                {
                    return true;
                }
                try
                {
                    _method.Invoke(target, [message]);
                    return true;
                }
                catch (TargetInvocationException)
                {
                    throw;
                    // If the target method throws, we propagate or swallow?
                    // Usually event aggregators swallow or log.
                    // Here we swallow to avoid breaking other subscribers.
                }
                catch
                {
                    // Ignore other errors
                }
                return true;
            }
        }
    }
}
using System.Collections.Concurrent;

// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Aggregator;

/// <summary>
///     <para xml:lang="en">A simple event aggregator used to decouple communication between multiple objects, similar to IMessenger in WPF.</para>
///     <para xml:lang="zh">简单的事件发布聚合器，用于解耦多个对象之间的通信，类似 WPF 中的 IMessenger。</para>
/// </summary>
public sealed class SimpleEventAggregator : IEventAggregator, IDisposable
{
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
    public void Register<T>(object recipient, Action<T> action) where T : class
    {
        var messageType = typeof(T);
        if (!_recipients.TryGetValue(messageType, out var value))
        {
            value = [];
            _recipients[messageType] = value;
        }
        value[recipient] = action;
    }

    /// <inheritdoc />
    public void Unregister<T>(object recipient) where T : class
    {
        var messageType = typeof(T);
        if (_recipients.TryGetValue(messageType, out var value))
        {
            value.TryRemove(recipient, out _);
        }
    }

    /// <inheritdoc />
    public void Send<T>(T message) where T : class
    {
        var messageType = typeof(T);
        if (!_recipients.TryGetValue(messageType, out var value))
        {
            return;
        }
        foreach (var recipient in value.Values)
        {
            ((Action<T>)recipient)(message);
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
            _recipients.Clear();
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
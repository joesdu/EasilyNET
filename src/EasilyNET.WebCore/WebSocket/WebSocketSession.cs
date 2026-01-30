using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using EasilyNET.Core.Essentials;
using EasilyNET.Core.WebSocket;
using Microsoft.Extensions.Logging;

namespace EasilyNET.WebCore.WebSocket;

internal sealed class WebSocketSession : IWebSocketSession
{
    private readonly CancellationTokenSource _cts = new();
    private readonly WebSocketHandler _handler;
    private readonly ConcurrentDictionary<string, object?> _items = new();
    private readonly ILogger _logger;
    private readonly WebSocketSessionOptions _options;
    private readonly Channel<WebSocketMessage> _sendChannel;
    private readonly System.Net.WebSockets.WebSocket _socket;

    /// <summary>
    /// 最后发送心跳的时间戳。用于确保远端有足够时间响应心跳。
    /// 初始值为 0 表示尚未发送任何心跳。
    /// </summary>
    private long _lastHeartbeatSentTimestamp;

    private long _lastReceiveTimestamp;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="WebSocketSession" /> class with a custom session ID.</para>
    ///     <para xml:lang="zh">使用自定义会话 ID 初始化 <see cref="WebSocketSession" /> 类的新实例。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">The session identifier. If null or empty, a new Ulid will be generated.</para>
    ///     <para xml:lang="zh">会话标识符。如果为 null 或空，将生成新的 Ulid。</para>
    /// </param>
    /// <param name="socket">
    ///     <para xml:lang="en">The WebSocket connection.</para>
    ///     <para xml:lang="zh">WebSocket 连接。</para>
    /// </param>
    /// <param name="handler">
    ///     <para xml:lang="en">The WebSocket handler.</para>
    ///     <para xml:lang="zh">WebSocket 处理程序。</para>
    /// </param>
    /// <param name="options">
    ///     <para xml:lang="en">The session options.</para>
    ///     <para xml:lang="zh">会话选项。</para>
    /// </param>
    /// <param name="logger">
    ///     <para xml:lang="en">The logger.</para>
    ///     <para xml:lang="zh">日志记录器。</para>
    /// </param>
    private WebSocketSession(string? id, System.Net.WebSockets.WebSocket socket, WebSocketHandler handler, WebSocketSessionOptions options, ILogger logger)
    {
        // Use Ulid for globally unique session ID if not provided
        Id = string.IsNullOrEmpty(id) ? Ulid.NewUlid().ToString() : id;
        _socket = socket;
        _handler = handler;
        _options = options;
        _logger = logger;
        var channelOptions = new BoundedChannelOptions(options.SendQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _sendChannel = Channel.CreateBounded<WebSocketMessage>(channelOptions);
        _lastReceiveTimestamp = Stopwatch.GetTimestamp();
        _lastHeartbeatSentTimestamp = 0; // 尚未发送心跳
    }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="WebSocketSession" /> class with an auto-generated Ulid.</para>
    ///     <para xml:lang="zh">使用自动生成的 Ulid 初始化 <see cref="WebSocketSession" /> 类的新实例。</para>
    /// </summary>
    /// <param name="socket">
    ///     <para xml:lang="en">The WebSocket connection.</para>
    ///     <para xml:lang="zh">WebSocket 连接。</para>
    /// </param>
    /// <param name="handler">
    ///     <para xml:lang="en">The WebSocket handler.</para>
    ///     <para xml:lang="zh">WebSocket 处理程序。</para>
    /// </param>
    /// <param name="options">
    ///     <para xml:lang="en">The session options.</para>
    ///     <para xml:lang="zh">会话选项。</para>
    /// </param>
    /// <param name="logger">
    ///     <para xml:lang="en">The logger.</para>
    ///     <para xml:lang="zh">日志记录器。</para>
    /// </param>
    public WebSocketSession(System.Net.WebSockets.WebSocket socket, WebSocketHandler handler, WebSocketSessionOptions options, ILogger logger)
        : this(null, socket, handler, options, logger) { }

    public string Id { get; }

    public WebSocketState State => _socket.State;

    public IDictionary<string, object?> Items => _items;

    public Task SendAsync(ReadOnlyMemory<byte> message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool endOfMessage = true, CancellationToken cancellationToken = default) => SendAsyncInternal(message, messageType, endOfMessage, null, cancellationToken);

    public Task SendTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var byteCount = Encoding.UTF8.GetByteCount(text);
        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        var bytesUsed = Encoding.UTF8.GetBytes(text, rented);
        return SendAsyncInternal(new(rented, 0, bytesUsed), WebSocketMessageType.Text, true, rented, cancellationToken);
    }

    public Task SendBinaryAsync(byte[] bytes, CancellationToken cancellationToken = default) => SendAsync(bytes, WebSocketMessageType.Binary, true, cancellationToken);

    public Task SendBinaryAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) => SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken);

    public async Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken = default)
    {
        // First, signal cancellation so any background loops can observe it and stop.
        try
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // Ignore: the CTS has already been disposed.
        }
        // Then, attempt to close the WebSocket if it is in a state where closing makes sense.
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await _socket.CloseAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(false);
            }
            catch (WebSocketException)
            {
                // Ignore: the socket may have been closed or aborted concurrently.
            }
            catch (ObjectDisposedException)
            {
                // Ignore: the socket was disposed concurrently.
            }
        }
    }

    private async Task SendAsyncInternal(ReadOnlyMemory<byte> message, WebSocketMessageType messageType, bool endOfMessage, byte[]? rentedArray, CancellationToken cancellationToken)
    {
        if (_socket.State != WebSocketState.Open)
        {
            if (rentedArray != null)
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
            throw new WebSocketException(WebSocketError.InvalidState, "WebSocket is not open.");
        }
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var msg = new WebSocketMessage(message, messageType, endOfMessage, tcs, rentedArray);
        try
        {
            await _sendChannel.Writer.WriteAsync(msg, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (rentedArray != null)
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
            throw;
        }
        // await tcs.Task.ConfigureAwait(false); // Optional: wait for actual send
    }

    internal async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var token = linkedCts.Token;
        var sendTask = SendLoopAsync(token);
        var receiveTask = ReceiveLoopAsync(token);
        Task? heartbeatTask = null;
        if (_options.HeartbeatEnabled)
        {
            heartbeatTask = HeartbeatLoopAsync(token);
        }
        var isConnected = false;
        try
        {
            await _handler.OnConnectedAsync(this).ConfigureAwait(false);
            isConnected = true;
            if (heartbeatTask != null)
            {
                await Task.WhenAny(sendTask, receiveTask, heartbeatTask).ConfigureAwait(false);
            }
            else
            {
                await Task.WhenAny(sendTask, receiveTask).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await _handler.OnErrorAsync(this, ex).ConfigureAwait(false);
        }
        finally
        {
            await _cts.CancelAsync();
            if (isConnected)
            {
                await _handler.OnDisconnectedAsync(this).ConfigureAwait(false);
            }

            // Ensure socket is closed
            if (_socket.State is not WebSocketState.Closed and not WebSocketState.Aborted)
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(_options.CloseTimeout);
                    await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Session ended", timeoutCts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Close timeout errors are expected during shutdown - log at debug level
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(ex, "[WebSocketSession:{Id}] Error during close handshake", Id);
                    }
                }
            }
            _socket.Dispose();
            _cts.Dispose();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        // 从 ArrayPool 租借接收缓冲区,避免每次循环分配
        var buffer = ArrayPool<byte>.Shared.Rent(_options.ReceiveBufferSize);
        try
        {
            while (!token.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                ValueWebSocketReceiveResult result;
                // 使用 PooledMemoryStream 减少内存分配
                // 无需线程安全: 此实例仅在当前异步方法内使用，生命周期完全封闭于单个任务
                await using var ms = new PooledMemoryStream();
                do
                {
                    result = await _socket.ReceiveAsync(buffer.AsMemory(0, _options.ReceiveBufferSize), token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (_socket.State == WebSocketState.CloseReceived)
                        {
                            // Echo close
                            await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close", CancellationToken.None).ConfigureAwait(false);
                        }
                        return;
                    }
                    if (result.Count > 0)
                    {
                        ms.Write(buffer.AsSpan(0, result.Count));
                    }
                } while (!result.EndOfMessage);

                // 使用 ToArraySegment 获取内部缓冲区引用，避免额外分配
                // 注意：ArraySegment 引用的是 PooledMemoryStream 的内部缓冲区
                // 在 ms 被 dispose 之前必须完成消息处理
                var segment = ms.ToArraySegment();
                var data = segment.AsMemory();

                // 更新最后接收时间戳
                UpdateLastReceiveTimestamp();
                await _handler.OnMessageAsync(this, new(data, result.MessageType, true)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            // 连接被远端关闭,不视为错误,仅记录
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[WebSocketSession:{Id}] Connection closed prematurely", Id);
            }
        }
        catch (WebSocketException ex)
        {
            await _handler.OnErrorAsync(this, ex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _handler.OnErrorAsync(this, ex).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Updates the last receive timestamp using high-resolution timer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateLastReceiveTimestamp() => Volatile.Write(ref _lastReceiveTimestamp, Stopwatch.GetTimestamp());

    private async Task HeartbeatLoopAsync(CancellationToken token)
    {
        // 使用 PeriodicTimer 替代 Task.Delay,更高效
        using var timer = new PeriodicTimer(_options.HeartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                if (_socket.State != WebSocketState.Open)
                {
                    return;
                }

                // 心跳超时检测：
                // 仅当已发送过心跳后才检查超时，确保远端有机会响应
                // 条件：(距离上次发送心跳) > HeartbeatTimeout AND (距离上次接收) > HeartbeatTimeout
                if (_options.HeartbeatTimeout > TimeSpan.Zero)
                {
                    var lastHeartbeatSent = Volatile.Read(ref _lastHeartbeatSentTimestamp);
                    // 只有在已发送过心跳后才进行超时检查
                    if (lastHeartbeatSent > 0)
                    {
                        var lastReceive = Volatile.Read(ref _lastReceiveTimestamp);
                        var elapsedSinceHeartbeat = Stopwatch.GetElapsedTime(lastHeartbeatSent);
                        var elapsedSinceReceive = Stopwatch.GetElapsedTime(lastReceive);

                        // 只有当发送心跳后超过 HeartbeatTimeout 且期间没有收到任何消息时才判定超时
                        if (elapsedSinceHeartbeat > _options.HeartbeatTimeout && elapsedSinceReceive > _options.HeartbeatTimeout)
                        {
                            if (_logger.IsEnabled(LogLevel.Warning))
                            {
                                _logger.LogWarning("[WebSocketSession:{Id}] Heartbeat timeout: no data received for {ElapsedMs:N0}ms after heartbeat was sent {HeartbeatMs:N0}ms ago",
                                    Id, elapsedSinceReceive.TotalMilliseconds, elapsedSinceHeartbeat.TotalMilliseconds);
                            }
                            // 超时则关闭连接
                            await CloseAsync(WebSocketCloseStatus.ProtocolError, "Heartbeat timeout", CancellationToken.None).ConfigureAwait(false);
                            return;
                        }
                    }
                }

                // 发送心跳消息(如果配置了工厂)
                if (_options.HeartbeatMessageFactory is null)
                {
                    continue;
                }
                try
                {
                    var pingData = _options.HeartbeatMessageFactory();
                    if (_socket.State == WebSocketState.Open)
                    {
                        // 心跳消息通过队列发送，由 SendLoop 统一处理，避免并发发送冲突
                        // 使用 TryWrite 避免在队列满时阻塞心跳循环
                        if (_sendChannel.Writer.TryWrite(new(pingData, WebSocketMessageType.Binary, true)))
                        {
                            // 更新最后发送心跳时间戳
                            Volatile.Write(ref _lastHeartbeatSentTimestamp, Stopwatch.GetTimestamp());
                        }
                        else if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug("[WebSocketSession:{Id}] Heartbeat skipped: send queue full", Id);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(ex, "[WebSocketSession:{Id}] Error sending heartbeat", Id);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
    }

    private async Task SendLoopAsync(CancellationToken token)
    {
        Exception? exception = null;
        try
        {
            while (await _sendChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (_sendChannel.Reader.TryRead(out var message))
                {
                    try
                    {
                        if (_socket.State != WebSocketState.Open)
                        {
                            exception = new WebSocketException(WebSocketError.InvalidState, "WebSocket is not open.");
                            message.CompletionSource?.TrySetException(exception);
                            return;
                        }
                        try
                        {
                            // SendLoop 是唯一的发送者，无需加锁
                            await _socket.SendAsync(message.Data, message.MessageType, message.EndOfMessage, token).ConfigureAwait(false);
                            message.CompletionSource?.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            message.CompletionSource?.TrySetException(ex);
                            throw;
                        }
                    }
                    finally
                    {
                        if (message.RentedArray != null)
                        {
                            ArrayPool<byte>.Shared.Return(message.RentedArray);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected when the provided token is signaled; rethrow if it comes from another source.
            if (!token.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            // Send loop error usually means connection issue; log and let the loop terminate.
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "[WebSocketSession:{Id}] Send loop error", Id);
            }
            exception = ex;
        }
        finally
        {
            while (_sendChannel.Reader.TryRead(out var message))
            {
                if (message.RentedArray != null)
                {
                    ArrayPool<byte>.Shared.Return(message.RentedArray);
                }
                if (exception != null)
                {
                    message.CompletionSource?.TrySetException(exception);
                }
                else
                {
                    message.CompletionSource?.TrySetCanceled(token);
                }
            }
        }
    }
}
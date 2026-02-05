using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using EasilyNET.Core.Essentials;

// ReSharper disable EventNeverSubscribedTo.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.WebSocket;

/// <summary>
///     <para xml:lang="en">A managed WebSocket client that handles reconnection, heartbeats, and message queueing.</para>
///     <para xml:lang="zh">一个托管的 WebSocket 客户端，处理重连、心跳和消息队列。</para>
/// </summary>
public sealed class ManagedWebSocketClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Channel<WebSocketMessage> _sendChannel;
    private readonly Lock _stateLock = new();
    private CancellationTokenSource? _connectionCts;
    private volatile bool _disposed;

    /// <summary>
    /// 最后发送心跳的时间戳。用于确保远端有足够时间响应心跳。
    /// 初始值为 0 表示尚未发送任何心跳。
    /// </summary>
    private long _lastHeartbeatSentTimestamp;

    private long _lastReceiveTimestamp;

    private int _reconnectAttempts;
    private ClientWebSocket? _socket;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="ManagedWebSocketClient" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="ManagedWebSocketClient" /> 类的新实例。</para>
    /// </summary>
    /// <param name="options">
    ///     <para xml:lang="en">The configuration options.</para>
    ///     <para xml:lang="zh">配置选项。</para>
    /// </param>
    public ManagedWebSocketClient(WebSocketClientOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));

        // Create bounded or unbounded channel based on options
        var channelOptions = new BoundedChannelOptions(Options.SendQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _sendChannel = Channel.CreateBounded<WebSocketMessage>(channelOptions);

        // Initialize last receive time to "now" so heartbeat timeout doesn't immediately fire
        // before any connection/receive activity happens.
        _lastReceiveTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the client options.</para>
    ///     <para xml:lang="zh">获取客户端选项。</para>
    /// </summary>
    public WebSocketClientOptions Options { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the current state of the client.</para>
    ///     <para xml:lang="zh">获取客户端的当前状态。</para>
    /// </summary>
    public WebSocketClientState State
    {
        get
        {
            lock (_stateLock)
            {
                return field;
            }
        }
        private set
        {
            lock (_stateLock)
            {
                if (field == value)
                {
                    return;
                }
                var oldState = field;
                field = value;
                OnStateChanged(new(oldState, value));
            }
        }
    } = WebSocketClientState.Disconnected;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        await _disposeCts.CancelAsync().ConfigureAwait(false);
        if (_connectionCts is not null)
        {
            await _connectionCts.CancelAsync().ConfigureAwait(false);
        }
        State = WebSocketClientState.Disposed;

        // Close socket if open
        if (_socket is not null)
        {
            try
            {
                if (_socket.State == WebSocketState.Open)
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Disposing", timeoutCts.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // Disposal errors are expected during shutdown - log at debug level
                Debug.WriteLine($"[ManagedWebSocketClient] Dispose close error: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _socket.Dispose();
            }
        }
        try
        {
            await _connectionLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Lock acquisition timeout during disposal is expected - log at debug level
            Debug.WriteLine($"[ManagedWebSocketClient] Dispose lock timeout: {ex.GetType().Name}: {ex.Message}");
        }
        _disposeCts.Dispose();
        _connectionCts?.Dispose();
        _connectionLock.Dispose();
    }

    /// <summary>
    ///     <para xml:lang="en">Occurs when the connection state changes.</para>
    ///     <para xml:lang="zh">当连接状态发生变化时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketStateChangedEventArgs>? StateChanged;

    /// <summary>
    ///     <para xml:lang="en">Occurs when a message is received.</para>
    ///     <para xml:lang="zh">当收到消息时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    ///     <para xml:lang="en">Occurs when an error occurs.</para>
    ///     <para xml:lang="zh">当发生错误时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    ///     <para xml:lang="en">Occurs when the client is attempting to reconnect.</para>
    ///     <para xml:lang="zh">当客户端尝试重新连接时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketReconnectingEventArgs>? Reconnecting;

    /// <summary>
    ///     <para xml:lang="en">Occurs when the connection is closed.</para>
    ///     <para xml:lang="zh">当连接关闭时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketClosedEventArgs>? Closed;

    /// <summary>
    ///     <para xml:lang="en">Connects to the WebSocket server.</para>
    ///     <para xml:lang="zh">连接到 WebSocket 服务器。</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ManagedWebSocketClient));
        if (Options.ServerUri == null)
        {
            throw new InvalidOperationException("ServerUri is not set.");
        }
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State is WebSocketClientState.Connected or WebSocketClientState.Connecting)
            {
                return;
            }
            State = WebSocketClientState.Connecting;
            Interlocked.Exchange(ref _reconnectAttempts, 0);
            await StartConnectionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            State = WebSocketClientState.Disconnected;
            OnError(new(ex, "ConnectAsync"));
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Disconnects from the WebSocket server.</para>
    ///     <para xml:lang="zh">断开与 WebSocket 服务器的连接。</para>
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_disposed)
        {
            return;
        }
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (State is WebSocketClientState.Disconnected or WebSocketClientState.Disposed)
            {
                return;
            }
            State = WebSocketClientState.Closing;
            if (_connectionCts is not null)
            {
                await _connectionCts.CancelAsync().ConfigureAwait(false);
            }
            if (_socket is { State: WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent })
            {
                try
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Ignore errors during close
                }
            }
            State = WebSocketClientState.Disconnected;
            OnClosed(new(WebSocketCloseStatus.NormalClosure, "Client initiated disconnect", true));
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Sends a message to the WebSocket server.</para>
    ///     <para xml:lang="zh">向 WebSocket 服务器发送消息。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">The message to send.</para>
    ///     <para xml:lang="zh">要发送的消息。</para>
    /// </param>
    /// <param name="messageType">
    ///     <para xml:lang="en">The message type.</para>
    ///     <para xml:lang="zh">消息类型。</para>
    /// </param>
    /// <param name="endOfMessage">
    ///     <para xml:lang="en">Whether this is the end of the message.</para>
    ///     <para xml:lang="zh">是否为消息结尾。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    public Task SendAsync(ReadOnlyMemory<byte> message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool endOfMessage = true, CancellationToken cancellationToken = default) => SendAsyncInternal(message, messageType, endOfMessage, null, cancellationToken);

    private async Task SendAsyncInternal(ReadOnlyMemory<byte> message, WebSocketMessageType messageType, bool endOfMessage, byte[]? rentedArray, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ManagedWebSocketClient));
        // If not connected and not auto-reconnect, throw
        if (State != WebSocketClientState.Connected && !Options.AutoReconnect)
        {
            if (rentedArray != null)
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
            throw new InvalidOperationException("Client is not connected.");
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

        // Wait for the message to be sent if we want to ensure delivery order or catch send errors immediately
        // However, for high performance, we might not want to await the actual send here if we trust the queue.
        // But the user might expect SendAsync to mean "sent to socket".
        // Given the requirement for "High performance sending queue", we usually just enqueue.
        // But if we want to propagate errors, we should await the TCS.
        if (Options.WaitForSendCompletion)
        {
            await tcs.Task.ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Sends a text message.</para>
    ///     <para xml:lang="zh">发送文本消息。</para>
    /// </summary>
    public Task SendTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var byteCount = Encoding.UTF8.GetByteCount(text);
        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        var bytesUsed = Encoding.UTF8.GetBytes(text, rented);
        return SendAsyncInternal(new(rented, 0, bytesUsed), WebSocketMessageType.Text, true, rented, cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Sends a binary message.</para>
    ///     <para xml:lang="zh">发送二进制消息。</para>
    /// </summary>
    /// <param name="bytes">
    ///     <para xml:lang="en">The binary data to send.</para>
    ///     <para xml:lang="zh">要发送的二进制数据。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    public Task SendBinaryAsync(byte[] bytes, CancellationToken cancellationToken = default) => SendAsync(bytes, WebSocketMessageType.Binary, true, cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Sends a binary message from a ReadOnlyMemory.</para>
    ///     <para xml:lang="zh">从 ReadOnlyMemory 发送二进制消息。</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The binary data to send.</para>
    ///     <para xml:lang="zh">要发送的二进制数据。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    public Task SendBinaryAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) => SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken);

    private async Task StartConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connectionCts is not null)
        {
            await _connectionCts.CancelAsync().ConfigureAwait(false);
        }
        _connectionCts?.Dispose();
        _connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, cancellationToken);
        _socket?.Dispose();
        _socket = new();
        Options.ConfigureWebSocket?.Invoke(_socket);
        try
        {
            using var timeoutCts = new CancellationTokenSource(Options.ConnectionTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_connectionCts.Token, timeoutCts.Token);
            if (Options.ServerUri == null)
            {
                throw new InvalidOperationException("ServerUri is null");
            }
            await _socket.ConnectAsync(Options.ServerUri, linkedCts.Token).ConfigureAwait(false);
            State = WebSocketClientState.Connected;

            // Mark the connection as alive at the moment we become connected.
            UpdateLastReceiveTimestamp();

            // Start background loops
            var receiveTask = Task.Factory.StartNew(() => ReceiveLoop(_connectionCts.Token), _connectionCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            _ = receiveTask.ContinueWith(t => OnError(new(t.Exception?.InnerException ?? t.Exception!, "ReceiveLoop background task failed")), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            var sendTask = Task.Factory.StartNew(() => SendLoop(_connectionCts.Token), _connectionCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            _ = sendTask.ContinueWith(t => OnError(new(t.Exception?.InnerException ?? t.Exception!, "SendLoop background task failed")), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            if (Options.HeartbeatEnabled)
            {
                var heartbeatTask = Task.Factory.StartNew(() => HeartbeatLoop(_connectionCts.Token), _connectionCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
                _ = heartbeatTask.ContinueWith(t => OnError(new(t.Exception?.InnerException ?? t.Exception!, "HeartbeatLoop background task failed")), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
        }
        catch (Exception)
        {
            // If connection failed, and we are not cancelling, try to reconnect if enabled
            if (!_disposeCts.IsCancellationRequested && Options.AutoReconnect)
            {
                // We are still in the lock here, so we can't call HandleConnectionLoss directly if it tries to acquire lock.
                // But HandleConnectionLoss is designed to be called from background tasks.
                // Since we are in ConnectAsync, we should probably throw and let the caller handle or start a background reconnect.
                // However, for "AutoReconnect", we usually want it to just work.

                // If this is the initial connection attempt, maybe we should throw? 
                // Or if AutoReconnect is true, we enter reconnect loop?
                // Usually ConnectAsync throws if initial connection fails.
            }
            throw;
        }
    }

    private async Task ReceiveLoop(CancellationToken token)
    {
        // 从 ArrayPool 租借接收缓冲区,避免每次循环分配
        var buffer = ArrayPool<byte>.Shared.Rent(Options.ReceiveBufferSize);
        try
        {
            while (!token.IsCancellationRequested && _socket?.State == WebSocketState.Open)
            {
                ValueWebSocketReceiveResult result;
                // 使用 PooledMemoryStream 减少内存分配
                // 无需线程安全: 此实例仅在当前接收循环内使用，每次循环创建新实例，无跨线程共享
                await using var ms = new PooledMemoryStream();
                do
                {
                    result = await _socket.ReceiveAsync(buffer.AsMemory(0, Options.ReceiveBufferSize), token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // ValueWebSocketReceiveResult 没有 CloseStatus/CloseStatusDescription，需要从 socket 获取
                        await HandleServerClose(_socket.CloseStatus, _socket.CloseStatusDescription).ConfigureAwait(false);
                        return;
                    }
                    if (result.Count > 0)
                    {
                        ms.Write(buffer.AsSpan(0, result.Count));
                    }
                } while (!result.EndOfMessage);

                // 获取完整消息数据
                var data = ms.ToArray();

                // Any successfully received message indicates the connection is alive.
                UpdateLastReceiveTimestamp();

                // 检查是否为心跳响应消息（pong），如果是则不触发 MessageReceived 事件
                // 只有当消息类型与心跳类型匹配且内容匹配时才过滤，避免误过滤业务消息
                if (IsHeartbeatResponse(data, result.MessageType))
                {
                    continue;
                }
                OnMessageReceived(new(data, result.MessageType, true));
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            // 连接被远端关闭,不视为错误
            await HandleConnectionLoss(ex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnError(new(ex, "ReceiveLoop"));
            await HandleConnectionLoss(ex).ConfigureAwait(false);
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

    /// <summary>
    ///     <para xml:lang="en">Checks if the received data is a heartbeat response message.</para>
    ///     <para xml:lang="zh">检查接收到的数据是否为心跳响应消息。</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The received data.</para>
    ///     <para xml:lang="zh">接收到的数据。</para>
    /// </param>
    /// <param name="messageType">
    ///     <para xml:lang="en">The WebSocket message type.</para>
    ///     <para xml:lang="zh">WebSocket 消息类型。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the data matches the heartbeat response pattern and message type; otherwise, false.</para>
    ///     <para xml:lang="zh">如果数据和消息类型都匹配心跳响应模式则返回 true；否则返回 false。</para>
    /// </returns>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     The heartbeat response is only filtered when:
    ///     1. HeartbeatResponseMessage is not empty (filtering is enabled)
    ///     2. The message type matches HeartbeatMessageType (prevents filtering business messages with same content but different type)
    ///     3. The message content exactly matches HeartbeatResponseMessage
    ///     </para>
    ///     <para xml:lang="zh">
    ///     心跳响应仅在以下条件都满足时才被过滤：
    ///     1. HeartbeatResponseMessage 不为空（启用过滤）
    ///     2. 消息类型与 HeartbeatMessageType 匹配（防止过滤内容相同但类型不同的业务消息）
    ///     3. 消息内容与 HeartbeatResponseMessage 完全匹配
    ///     </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsHeartbeatResponse(byte[] data, WebSocketMessageType messageType)
    {
        var expectedResponse = Options.HeartbeatResponseMessage;
        // 只有当消息类型匹配心跳类型且内容匹配时才认为是心跳响应
        // 这样可以避免误过滤内容恰好是 "pong" 的业务消息（如 Text 类型的 "pong" 不会被过滤，如果心跳类型是 Binary）
        return !expectedResponse.IsEmpty &&
               messageType == Options.HeartbeatMessageType &&
               data.AsSpan().SequenceEqual(expectedResponse.Span);
    }

    private async Task SendLoop(CancellationToken token)
    {
        try
        {
            while (await _sendChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (_sendChannel.Reader.TryRead(out var message))
                {
                    try
                    {
                        if (_socket?.State == WebSocketState.Open)
                        {
                            // SendLoop 是唯一的发送者，无需加锁
                            await _socket.SendAsync(message.Data, message.MessageType, message.EndOfMessage, token).ConfigureAwait(false);
                            message.CompletionSource?.TrySetResult(true);
                        }
                        else
                        {
                            // If socket is not open, we might want to requeue or fail
                            // For now, fail the task
                            message.CompletionSource?.TrySetException(new WebSocketException("WebSocket is not open."));
                        }
                    }
                    catch (Exception ex)
                    {
                        message.CompletionSource?.TrySetException(ex);
                        OnError(new(ex, "SendLoop processing message"));
                        // If send fails, it might be a connection issue
                        if (_socket?.State == WebSocketState.Open)
                        {
                            continue;
                        }
                        await HandleConnectionLoss(ex).ConfigureAwait(false);
                        FailPendingSends(ex);
                        return; // Exit loop, let reconnection handle restart
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
        catch (OperationCanceledException oce)
        {
            // Normal cancellation
            FailPendingSends(oce);
        }
        catch (Exception ex)
        {
            OnError(new(ex, "SendLoop"));
            FailPendingSends(ex);
        }
    }

    private void FailPendingSends(Exception exception)
    {
        while (_sendChannel.Reader.TryRead(out var pendingMessage))
        {
            if (pendingMessage.RentedArray != null)
            {
                ArrayPool<byte>.Shared.Return(pendingMessage.RentedArray);
            }
            if (pendingMessage.CompletionSource is null)
            {
                continue;
            }
            if (exception is OperationCanceledException)
            {
                pendingMessage.CompletionSource.TrySetCanceled();
            }
            else
            {
                pendingMessage.CompletionSource.TrySetException(exception);
            }
        }
    }

    private async Task HeartbeatLoop(CancellationToken token)
    {
        // 使用 PeriodicTimer 按心跳间隔发送心跳
        using var timer = new PeriodicTimer(Options.HeartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                if (_socket?.State != WebSocketState.Open)
                {
                    return; // 当 socket 关闭时退出心跳循环，避免浪费资源
                }

                // 心跳超时检测：
                // 仅当已发送过心跳后才检查超时，确保远端有机会响应
                // 条件：发送心跳后超过 HeartbeatTimeout 时间，且在此期间没有收到任何消息
                if (Options.HeartbeatTimeout > TimeSpan.Zero)
                {
                    var lastHeartbeatSent = Volatile.Read(ref _lastHeartbeatSentTimestamp);
                    // 只有在已发送过心跳后才进行超时检查
                    if (lastHeartbeatSent > 0)
                    {
                        var lastReceive = Volatile.Read(ref _lastReceiveTimestamp);
                        var elapsedSinceHeartbeat = Stopwatch.GetElapsedTime(lastHeartbeatSent);

                        // 超时条件：
                        // 1. 发送心跳后已超过 HeartbeatTimeout 时间
                        // 2. 上次收到消息的时间早于上次发送心跳的时间（即发送心跳后没有收到任何消息）
                        if (elapsedSinceHeartbeat > Options.HeartbeatTimeout && lastReceive < lastHeartbeatSent)
                        {
                            var elapsedSinceReceive = Stopwatch.GetElapsedTime(lastReceive);
                            var ex = new TimeoutException($"WebSocket heartbeat timeout: no response for {elapsedSinceHeartbeat.TotalMilliseconds:N0}ms after heartbeat sent (last receive was {elapsedSinceReceive.TotalMilliseconds:N0}ms ago).");
                            OnError(new(ex, "HeartbeatLoop timeout"));
                            await HandleConnectionLoss(ex).ConfigureAwait(false);
                            return;
                        }
                    }
                }
                try
                {
                    // 心跳消息通过队列发送，由 SendLoop 统一处理，避免并发发送冲突
                    var pingData = Options.HeartbeatMessageFactory?.Invoke() ?? ReadOnlyMemory<byte>.Empty;
                    // 使用 TryWrite 避免在队列满时阻塞心跳循环
                    // 使用可配置的消息类型发送心跳
                    if (_sendChannel.Writer.TryWrite(new(pingData, Options.HeartbeatMessageType, true)))
                    {
                        // 更新最后发送心跳时间戳
                        Volatile.Write(ref _lastHeartbeatSentTimestamp, Stopwatch.GetTimestamp());
                    }
                    // 如果队列满了，跳过此次心跳，等待下一个周期
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation during heartbeat send
                    return;
                }
                catch (Exception ex)
                {
                    OnError(new(ex, "HeartbeatLoop sending ping"));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
    }

    private async Task HandleServerClose(WebSocketCloseStatus? closeStatus, string? closeStatusDescription)
    {
        State = WebSocketClientState.Disconnected;
        OnClosed(new(closeStatus, closeStatusDescription, false));
        if (Options.AutoReconnect && !_disposeCts.IsCancellationRequested)
        {
            await ReconnectAsync().ConfigureAwait(false);
        }
    }

    private async Task HandleConnectionLoss(Exception _)
    {
        // If the client is being disposed, do not attempt to reconnect.
        if (_disposeCts.IsCancellationRequested)
        {
            return;
        }

        // Serialize the decision to start reconnecting to avoid race conditions
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (State == WebSocketClientState.Reconnecting || _disposeCts.IsCancellationRequested)
            {
                // Another caller already initiated reconnection or we are disposing
                return;
            }

            // Transition to a state that allows reconnection; ReconnectAsync will perform the actual reconnect.
            State = WebSocketClientState.Reconnecting;
        }
        finally
        {
            _connectionLock.Release();
        }
        if (Options.AutoReconnect)
        {
            await ReconnectAsync().ConfigureAwait(false);
        }
    }

    private async Task ReconnectAsync()
    {
        // Acquire lock to ensure single reconnection process
        try
        {
            await _connectionLock.WaitAsync(_disposeCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        try
        {
            if (State == WebSocketClientState.Connected || _disposeCts.IsCancellationRequested)
            {
                return;
            }
            State = WebSocketClientState.Reconnecting;
            while (Options.MaxReconnectAttempts == -1 || Volatile.Read(ref _reconnectAttempts) < Options.MaxReconnectAttempts)
            {
                if (_disposeCts.IsCancellationRequested)
                {
                    return;
                }
                var attempts = Interlocked.Increment(ref _reconnectAttempts);

                // Calculate delay using TimeSpan
                var delay = CalculateReconnectDelay(attempts);
                var args = new WebSocketReconnectingEventArgs(attempts, delay, null);
                OnReconnecting(args);
                if (args.Cancel)
                {
                    State = WebSocketClientState.Disconnected;
                    return;
                }
                await Task.Delay(delay, _disposeCts.Token).ConfigureAwait(false);
                try
                {
                    await StartConnectionAsync(_disposeCts.Token).ConfigureAwait(false);
                    // If successful
                    Interlocked.Exchange(ref _reconnectAttempts, 0);
                    return;
                }
                catch (Exception ex)
                {
                    OnError(new(ex, $"Reconnection attempt {attempts} failed"));
                }
            }

            // Failed to reconnect after max attempts
            State = WebSocketClientState.Disconnected;
            OnClosed(new(null, "Failed to reconnect after maximum attempts", false));
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Calculates the reconnection delay based on attempt number and backoff strategy.</para>
    ///     <para xml:lang="zh">根据尝试次数和退避策略计算重连延迟。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Adds ±20% jitter to prevent thundering herd problem when multiple clients reconnect simultaneously.</para>
    ///     <para xml:lang="zh">添加 ±20% 的抖动以防止多个客户端同时重连时的雷群效应。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TimeSpan CalculateReconnectDelay(int attempts)
    {
        double delayMs;
        if (!Options.UseExponentialBackoff)
        {
            delayMs = Options.ReconnectDelay.TotalMilliseconds;
        }
        else
        {
            delayMs = Options.ReconnectDelay.TotalMilliseconds * Math.Pow(2, attempts - 1);
            delayMs = Math.Min(delayMs, Options.MaxReconnectDelay.TotalMilliseconds);
        }

        // Add jitter: ±20% to prevent thundering herd
        var jitterFactor = 0.8 + (Random.Shared.NextDouble() * 0.4); // Range: 0.8 to 1.2
        delayMs *= jitterFactor;

        // Ensure we don't exceed max delay after jitter
        return TimeSpan.FromMilliseconds(Math.Min(delayMs, Options.MaxReconnectDelay.TotalMilliseconds));
    }

    private void OnStateChanged(WebSocketStateChangedEventArgs e)
    {
        try
        {
            StateChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            // Subscriber exception should not crash the client
            Debug.WriteLine($"[ManagedWebSocketClient] StateChanged handler error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnMessageReceived(WebSocketMessageReceivedEventArgs e)
    {
        try
        {
            MessageReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            // Subscriber exception should not crash the receive loop
            Debug.WriteLine($"[ManagedWebSocketClient] MessageReceived handler error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnError(WebSocketErrorEventArgs e)
    {
        try
        {
            Error?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            // Subscriber exception should not crash the client
            Debug.WriteLine($"[ManagedWebSocketClient] Error handler error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnReconnecting(WebSocketReconnectingEventArgs e)
    {
        try
        {
            Reconnecting?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            // Subscriber exception should not crash the reconnect loop
            Debug.WriteLine($"[ManagedWebSocketClient] Reconnecting handler error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnClosed(WebSocketClosedEventArgs e)
    {
        try
        {
            Closed?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            // Subscriber exception should not crash the client
            Debug.WriteLine($"[ManagedWebSocketClient] Closed handler error: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
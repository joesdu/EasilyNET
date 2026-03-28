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

    // Object-lifetime cancellation. Once cancelled, the client is shutting down permanently.
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Channel<WebSocketMessage> _sendChannel;

    // Use int with Interlocked.Exchange to guarantee atomic check-and-set across concurrent callers.
    private int _disposedFlag;

    /// <summary>
    /// 最后发送心跳的时间戳。用于确保远端有足够时间响应心跳。
    /// 初始值为 0 表示尚未发送任何心跳。
    /// </summary>
    private long _lastHeartbeatSentTimestamp;

    private long _lastReceiveTimestamp;

    /// <summary>
    /// 用于标记用户主动调用了 DisconnectAsync，阻止自动重连。
    /// </summary>
    private volatile bool _manualDisconnect;

    private int _reconnectAttempts;

    // Current active connection generation, including both the socket and its cancellation scope.
    private ConnectionSession? _session;

    // State is read frequently and can change from multiple asynchronous paths.
    // Use atomic operations instead of a dedicated lock to reduce contention and cognitive load.
    private int _state = (int)WebSocketClientState.Disconnected;

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
    public WebSocketClientState State => (WebSocketClientState)Volatile.Read(ref _state);

    private bool IsDisposed => Volatile.Read(ref _disposedFlag) != 0;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Guarantee exactly-once disposal even when called concurrently.
        if (Interlocked.Exchange(ref _disposedFlag, 1) != 0)
        {
            return;
        }

        // 1. Signal all background loops to stop
        await _disposeCts.CancelAsync().ConfigureAwait(false);
        await CancelConnectionAsync().ConfigureAwait(false);

        // 2. Acquire lock to ensure no concurrent connect/disconnect/reconnect is running.
        // Since all CTS tokens are already cancelled, background loops will exit soon and release the lock.
        // However, the lock may also be held while executing user callbacks (for example ConfigureWebSocket),
        // so disposal must never wait indefinitely here.
        var lockAcquired = false;
        WebSocketStateChangedEventArgs? disposedStateChanged = null;
        var initialDisposeLockTimeout = NormalizeDisposeLockTimeout(Options.DisposeLockTimeout);
        var disposeLockGracePeriod = NormalizeDisposeLockTimeout(Options.DisposeLockTimeoutGracePeriod);
        try
        {
            lockAcquired = await _connectionLock.WaitAsync(initialDisposeLockTimeout).ConfigureAwait(false);
            if (!lockAcquired && disposeLockGracePeriod > TimeSpan.Zero)
            {
                Debug.WriteLine($"[ManagedWebSocketClient] Dispose lock timed out after {initialDisposeLockTimeout.TotalSeconds:N1}s — waiting up to an additional {disposeLockGracePeriod.TotalSeconds:N1}s before falling back to best-effort cleanup.");
                lockAcquired = await _connectionLock.WaitAsync(disposeLockGracePeriod).ConfigureAwait(false);
            }
            if (!lockAcquired)
            {
                Debug.WriteLine($"[ManagedWebSocketClient] Dispose lock unavailable after {(initialDisposeLockTimeout + disposeLockGracePeriod).TotalSeconds:N1}s total wait — continuing with best-effort cleanup and skipping contested resource disposal.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ManagedWebSocketClient] Dispose lock error: {ex.GetType().Name}: {ex.Message}");
        }
        try
        {
            disposedStateChanged = TryUpdateState(WebSocketClientState.Disposed);

            // 3. Close socket only while holding the lock to prevent races with concurrent operations.
            if (lockAcquired && _session is not null)
            {
                try
                {
                    if (_session.Socket.State == WebSocketState.Open)
                    {
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await _session.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Disposing", timeoutCts.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ManagedWebSocketClient] Dispose close error: {ex.GetType().Name}: {ex.Message}");
                }
                finally
                {
                    _session.Dispose();
                }
            }

            // 4. Fail any remaining queued sends
            FailPendingSends(new ObjectDisposedException(nameof(ManagedWebSocketClient)));
        }
        finally
        {
            if (lockAcquired)
            {
                _connectionLock.Release();
                _disposeCts.Dispose();
                _connectionLock.Dispose();
            }
            else
            {
                Debug.WriteLine("[ManagedWebSocketClient] Dispose completed without owning the connection lock; CTS/semaphore disposal was skipped to avoid racing with in-flight operations.");
            }
            PublishStateChanged(disposedStateChanged);
        }
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
        ObjectDisposedException.ThrowIf(IsDisposed, typeof(ManagedWebSocketClient));
        Options.Validate();
        WebSocketStateChangedEventArgs? connectingStateChanged;
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State is WebSocketClientState.Connected or WebSocketClientState.Connecting)
            {
                return;
            }
            _manualDisconnect = false;
            connectingStateChanged = TryUpdateState(WebSocketClientState.Connecting);
            Interlocked.Exchange(ref _reconnectAttempts, 0);
        }
        finally
        {
            _connectionLock.Release();
        }
        PublishStateChanged(connectingStateChanged);
        try
        {
            await StartConnectionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SetState(WebSocketClientState.Disconnected);
            OnError(new(ex, "ConnectAsync"));
            throw;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Disconnects from the WebSocket server.</para>
    ///     <para xml:lang="zh">断开与 WebSocket 服务器的连接。</para>
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (IsDisposed)
        {
            return;
        }
        WebSocketStateChangedEventArgs? closingStateChanged;
        WebSocketStateChangedEventArgs? disconnectedStateChanged;
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (State is WebSocketClientState.Disconnected or WebSocketClientState.Disposed)
            {
                return;
            }
            _manualDisconnect = true;
            closingStateChanged = TryUpdateState(WebSocketClientState.Closing);
            await CancelConnectionAsync().ConfigureAwait(false);
            if (_session is not null)
            {
                if (_session.Socket.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
                {
                    try
                    {
                        await _session.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Ignore errors during close
                    }
                }
                _session.Dispose();
                _session = null;
            }
            disconnectedStateChanged = TryUpdateState(WebSocketClientState.Disconnected);
        }
        finally
        {
            _connectionLock.Release();
        }
        PublishStateChanged(closingStateChanged);
        PublishStateChanged(disconnectedStateChanged);
        OnClosed(new(WebSocketCloseStatus.NormalClosure, "Client initiated disconnect", true));
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
        ObjectDisposedException.ThrowIf(IsDisposed, typeof(ManagedWebSocketClient));
        if (State != WebSocketClientState.Connected)
        {
            if (rentedArray != null)
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
            throw new InvalidOperationException("Client is not connected.");
        }
        // Only allocate TCS when the caller will actually await it; avoids a heap allocation
        // per send in fire-and-forget mode (WaitForSendCompletion = false).
        var tcs = Options.WaitForSendCompletion ? new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously) : null;
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
        if (tcs is not null)
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
        ConnectionSession session;
        ConnectionSession? previousSession;
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnectionStartAborted() || State != WebSocketClientState.Connecting)
            {
                throw new TaskCanceledException("Connection attempt aborted before socket initialization.");
            }
            previousSession = _session;
            session = CreateSession(cancellationToken);
            _session = session;
        }
        finally
        {
            _connectionLock.Release();
        }
        if (previousSession is not null)
        {
            await previousSession.CancelAsync().ConfigureAwait(false);
        }
        previousSession?.Dispose();
        try
        {
            Options.ConfigureWebSocket?.Invoke(session.Socket);
            // Guard against DisconnectAsync / DisposeAsync being called synchronously inside ConfigureWebSocket.
            // In that case the session token is already cancelled and the socket is already disposed,
            // so attempting ConnectAsync would throw ObjectDisposedException rather than the expected TaskCanceledException.
            if (IsConnectionStartAborted() || session.Token.IsCancellationRequested)
            {
                throw new TaskCanceledException("Connection attempt aborted inside ConfigureWebSocket callback.");
            }
            using var timeoutCts = new CancellationTokenSource(Options.ConnectionTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(session.Token, timeoutCts.Token);
            // Validate ServerUri on every connect/reconnect to avoid NullReferenceException if it was reset.
            var serverUri = Options.ServerUri ?? throw new InvalidOperationException("WebSocket connection failed because Options.ServerUri is null. Ensure ManagedWebSocketClientOptions.ServerUri is configured before connecting or reconnecting.");
            await session.Socket.ConnectAsync(serverUri, linkedCts.Token).ConfigureAwait(false);
            await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            WebSocketStateChangedEventArgs? connectedStateChanged;
            try
            {
                if (IsConnectionStartAborted() || !IsCurrentSession(session))
                {
                    throw new TaskCanceledException("Connection attempt aborted after socket connect.");
                }
                connectedStateChanged = TryUpdateState(WebSocketClientState.Connected);
            }
            finally
            {
                _connectionLock.Release();
            }
            PublishStateChanged(connectedStateChanged);

            // Mark the connection as alive at the moment we become connected.
            UpdateLastReceiveTimestamp();

            // Start background loops
            // ReSharper disable AccessToDisposedClosure
            var receiveTask = Task.Factory.StartNew(() => ReceiveLoop(session), session.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            _ = receiveTask.ContinueWith(t => OnError(new(t.Exception?.InnerException ?? t.Exception!, "ReceiveLoop background task failed")), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            var sendTask = Task.Factory.StartNew(() => SendLoop(session), session.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            _ = sendTask.ContinueWith(t => OnError(new(t.Exception?.InnerException ?? t.Exception!, "SendLoop background task failed")), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            if (Options.HeartbeatEnabled)
            {
                var heartbeatTask = Task.Factory.StartNew(() => HeartbeatLoop(session), session.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
                _ = heartbeatTask.ContinueWith(t => OnError(new(t.Exception?.InnerException ?? t.Exception!, "HeartbeatLoop background task failed")), TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
            // ReSharper restore AccessToDisposedClosure
        }
        catch (Exception)
        {
            // Dispose the newly allocated session to release the socket and linked CTS.
            // _session references this same object, but it will be overwritten on the next successful connect;
            // callers that observe the exception must call ConnectAsync again.
            session.Dispose();
            SetState(WebSocketClientState.Disconnected);
            throw;
        }
    }

    private async Task ReceiveLoop(ConnectionSession session)
    {
        // 从 ArrayPool 租借接收缓冲区,避免每次循环分配
        var buffer = ArrayPool<byte>.Shared.Rent(Options.ReceiveBufferSize);
        var token = session.Token;
        try
        {
            while (!token.IsCancellationRequested && IsSessionOpen(session))
            {
                ValueWebSocketReceiveResult result;
                // 使用 PooledMemoryStream 减少内存分配
                // 无需线程安全: 此实例仅在当前接收循环内使用，每次循环创建新实例，无跨线程共享
                await using var ms = new PooledMemoryStream();
                do
                {
                    result = await session.Socket.ReceiveAsync(buffer.AsMemory(0, Options.ReceiveBufferSize), token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // ValueWebSocketReceiveResult 没有 CloseStatus/CloseStatusDescription，需要从 socket 获取
                        await HandleServerClose(session, session.Socket.CloseStatus, session.Socket.CloseStatusDescription).ConfigureAwait(false);
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
            await HandleConnectionLoss(session, ex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnError(new(ex, "ReceiveLoop"));
            await HandleConnectionLoss(session, ex).ConfigureAwait(false);
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
    ///     2. The message type matches HeartbeatResponseMessageType (allows response type to differ from send type)
    ///     3. The message content exactly matches HeartbeatResponseMessage
    ///     </para>
    ///     <para xml:lang="zh">
    ///     心跳响应仅在以下条件都满足时才被过滤：
    ///     1. HeartbeatResponseMessage 不为空（启用过滤）
    ///     2. 消息类型与 HeartbeatResponseMessageType 匹配（允许响应类型与发送类型不同）
    ///     3. 消息内容与 HeartbeatResponseMessage 完全匹配
    ///     </para>
    /// </remarks>
    private bool IsHeartbeatResponse(byte[] data, WebSocketMessageType messageType)
    {
        var expectedResponse = Options.HeartbeatResponseMessage;
        // 使用独立的 HeartbeatResponseMessageType（而非发送侧的 HeartbeatMessageType），
        // 支持服务端以不同消息类型（如 Text）响应心跳（如 Binary）的场景。
        return !expectedResponse.IsEmpty &&
               messageType == Options.HeartbeatResponseMessageType &&
               data.AsSpan().SequenceEqual(expectedResponse.Span);
    }

    private async Task SendLoop(ConnectionSession session)
    {
        var token = session.Token;
        try
        {
            while (await _sendChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (!token.IsCancellationRequested && _sendChannel.Reader.TryRead(out var message))
                {
                    try
                    {
                        if (IsSessionOpen(session))
                        {
                            // SendLoop 是唯一的发送者，无需加锁
                            await session.Socket.SendAsync(message.Data, message.MessageType, message.EndOfMessage, token).ConfigureAwait(false);
                            message.CompletionSource?.TrySetResult(true);
                        }
                        else
                        {
                            // Socket is closing (transitioning to reconnect). Stop draining the queue so
                            // the new SendLoop can pick up remaining messages after reconnection.
                            // The one message already dequeued cannot be put back, so fail it gracefully.
                            message.CompletionSource?.TrySetException(new WebSocketException("WebSocket connection closed; message dropped during reconnection."));
                            return; // let OperationCanceledException / reconnect take over
                        }
                    }
                    catch (Exception ex)
                    {
                        message.CompletionSource?.TrySetException(ex);
                        OnError(new(ex, "SendLoop processing message"));
                        // If send fails, it might be a connection issue
                        if (IsSessionOpen(session))
                        {
                            continue;
                        }
                        await HandleConnectionLoss(session, ex).ConfigureAwait(false);
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
        catch (OperationCanceledException)
        {
            // Distinguish between reconnect-triggered cancellation (new SendLoop will take over)
            // and disconnect/dispose cancellation (pending sends must be completed to avoid callers hanging).
            if (IsShutdownOrDisconnected())
            {
                FailPendingSends(new OperationCanceledException("SendLoop cancelled due to disconnect or dispose."));
            }
            // Otherwise (reconnecting) — do NOT drain the queue; a new SendLoop will take over after reconnect
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

    private async Task HeartbeatLoop(ConnectionSession session)
    {
        // 使用 PeriodicTimer 按心跳间隔发送心跳
        using var timer = new PeriodicTimer(Options.HeartbeatInterval);
        var token = session.Token;
        try
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                if (!IsSessionOpen(session))
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
                            await HandleConnectionLoss(session, ex).ConfigureAwait(false);
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
                        // 仅在心跳成功入队后更新时间戳，避免队列满时产生虚假超时判断
                        Volatile.Write(ref _lastHeartbeatSentTimestamp, Stopwatch.GetTimestamp());
                    }
                    // Queue full — heartbeat skipped; do NOT update timestamp to prevent false timeout detection.
                    // This is a transient backpressure condition and is intentionally not surfaced via OnError.
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

    private async Task HandleServerClose(ConnectionSession session, WebSocketCloseStatus? closeStatus, string? closeStatusDescription)
    {
        if (!IsCurrentSession(session))
        {
            return;
        }
        SetState(WebSocketClientState.Disconnected);
        if (Options.AutoReconnect && !_disposeCts.IsCancellationRequested)
        {
            // Do NOT fire OnClosed here — the client is about to attempt reconnection.
            // OnClosed will be fired only if reconnection ultimately fails (or is cancelled).
            await ReconnectAsync().ConfigureAwait(false);
        }
        else
        {
            OnClosed(new(closeStatus, closeStatusDescription, false));
        }
    }

    private async Task HandleConnectionLoss(ConnectionSession session, Exception ex)
    {
        if (!IsCurrentSession(session))
        {
            return;
        }

        // If the client is being disposed, do not attempt to reconnect.
        if (_disposeCts.IsCancellationRequested)
        {
            return;
        }

        // Serialize the decision to start reconnecting to avoid race conditions
        WebSocketStateChangedEventArgs? stateChanged;
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (State == WebSocketClientState.Reconnecting || _disposeCts.IsCancellationRequested)
            {
                // Another caller already initiated reconnection or we are disposing
                return;
            }
            stateChanged = Options.AutoReconnect ? TryUpdateState(WebSocketClientState.Reconnecting) : TryUpdateState(WebSocketClientState.Disconnected);
        }
        finally
        {
            _connectionLock.Release();
        }
        PublishStateChanged(stateChanged);
        if (!Options.AutoReconnect)
        {
            OnClosed(new(null, ex.Message, false));
            return;
        }
        await ReconnectAsync().ConfigureAwait(false);
    }

    private async Task ReconnectAsync()
    {
        // Brief lock to check state
        WebSocketStateChangedEventArgs? reconnectingStateChanged;
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
            if (ShouldAbortReconnect())
            {
                return;
            }
            reconnectingStateChanged = TryUpdateState(WebSocketClientState.Reconnecting);
        }
        finally
        {
            _connectionLock.Release();
        }
        PublishStateChanged(reconnectingStateChanged);

        // Retry loop WITHOUT holding the lock
        Exception? lastException = null;
        while (Options.MaxReconnectAttempts == -1 || Volatile.Read(ref _reconnectAttempts) < Options.MaxReconnectAttempts)
        {
            if (ShouldAbortReconnect())
            {
                return;
            }
            var attempts = Interlocked.Increment(ref _reconnectAttempts);

            // Calculate delay using TimeSpan
            var delay = CalculateReconnectDelay(attempts);
            var args = new WebSocketReconnectingEventArgs(attempts, delay, lastException);
            OnReconnecting(args);
            if (args.Cancel)
            {
                SetState(WebSocketClientState.Disconnected);
                FailPendingSends(new OperationCanceledException("Reconnection cancelled."));
                return;
            }
            try
            {
                await Task.Delay(delay, _disposeCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Brief lock for actual connection attempt pre-check only.
            // StartConnectionAsync manages its own internal lock boundaries so that user callbacks
            // (for example ConfigureWebSocket) never execute while _connectionLock is held.
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
                if (ShouldAbortReconnect())
                {
                    return;
                }
            }
            finally
            {
                _connectionLock.Release();
            }
            try
            {
                await StartConnectionAsync(_disposeCts.Token).ConfigureAwait(false);
                Interlocked.Exchange(ref _reconnectAttempts, 0);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                OnError(new(ex, $"Reconnection attempt {attempts} failed"));
            }
        }

        // Failed to reconnect after max attempts
        SetState(WebSocketClientState.Disconnected);
        FailPendingSends(new OperationCanceledException("Failed to reconnect after maximum attempts."));
        OnClosed(new(null, "Failed to reconnect after maximum attempts", false));
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
    private bool ShouldAbortReconnect() => _disposeCts.IsCancellationRequested || _manualDisconnect || State == WebSocketClientState.Connected;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetState(WebSocketClientState newState) => PublishStateChanged(TryUpdateState(newState));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ConnectionSession CreateSession(CancellationToken cancellationToken) => new(new(), CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, cancellationToken));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsCurrentSession(ConnectionSession session) => ReferenceEquals(Volatile.Read(ref _session), session);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSessionOpen(ConnectionSession session) => session.Socket.State == WebSocketState.Open;

    private async ValueTask CancelConnectionAsync()
    {
        if (_session is not null)
        {
            await _session.CancelAsync().ConfigureAwait(false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsConnectionStartAborted() => IsDisposed || _disposeCts.IsCancellationRequested || _manualDisconnect;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsShutdownOrDisconnected() => _disposeCts.IsCancellationRequested || _manualDisconnect || State is WebSocketClientState.Disconnected or WebSocketClientState.Disposed;

    private WebSocketStateChangedEventArgs? TryUpdateState(WebSocketClientState newState)
    {
        while (true)
        {
            var current = Volatile.Read(ref _state);
            if (current == (int)newState)
            {
                return null;
            }
            var original = Interlocked.CompareExchange(ref _state, (int)newState, current);
            if (original == current)
            {
                return new((WebSocketClientState)current, newState);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PublishStateChanged(WebSocketStateChangedEventArgs? args)
    {
        if (args is not null)
        {
            OnStateChanged(args);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TimeSpan NormalizeDisposeLockTimeout(TimeSpan timeout) => timeout > TimeSpan.Zero ? timeout : TimeSpan.Zero;

    private sealed class ConnectionSession(ClientWebSocket socket, CancellationTokenSource cancellationSource) : IDisposable
    {
        public ClientWebSocket Socket { get; } = socket;

        private CancellationTokenSource CancellationSource { get; } = cancellationSource;

        public CancellationToken Token => CancellationSource.Token;

        public void Dispose()
        {
            CancellationSource.Dispose();
            Socket.Dispose();
        }

        public async ValueTask CancelAsync() => await CancellationSource.CancelAsync().ConfigureAwait(false);
    }
}
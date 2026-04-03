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
    private readonly Task _persistentSendLoopTask; // 持久单一 SendLoop
    private readonly Channel<WebSocketMessage> _sendChannel;

    // Use int with Interlocked.Exchange to guarantee atomic check-and-set across concurrent callers.
    private int _disposedFlag;

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
        _persistentSendLoopTask = Task.Run(PersistentSendLoop, _disposeCts.Token);
        _ = ObserveBackgroundTask(_persistentSendLoopTask, nameof(PersistentSendLoop));
    }

    private bool IsDisposed => Volatile.Read(ref _disposedFlag) != 0;

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
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await Task.WhenAny(_persistentSendLoopTask, Task.Delay(TimeSpan.FromSeconds(3), timeoutCts.Token)).ConfigureAwait(false);
        }
        finally
        {
            await timeoutCts.CancelAsync().ConfigureAwait(false);
        }

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
            lockAcquired = await _connectionLock.WaitAsync(initialDisposeLockTimeout, timeoutCts.Token).ConfigureAwait(false);
            if (!lockAcquired && disposeLockGracePeriod > TimeSpan.Zero)
            {
                Debug.WriteLine($"[ManagedWebSocketClient] Dispose lock timed out after {initialDisposeLockTimeout.TotalSeconds:N1}s — waiting up to an additional {disposeLockGracePeriod.TotalSeconds:N1}s before falling back to best-effort cleanup.");
                lockAcquired = await _connectionLock.WaitAsync(disposeLockGracePeriod, timeoutCts.Token).ConfigureAwait(false);
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
            if (lockAcquired && _session?.Socket.State is WebSocketState.Open)
            {
                try
                {
                    await _session.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Disposing", timeoutCts.Token).ConfigureAwait(false);
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
                // Only dispose shared synchronization primitives when we actually acquired the lock.
                // Disposing them while concurrent operations may be waiting on _connectionLock or
                // reading _disposeCts.Token would cause ObjectDisposedException / unpredictable races.
                _disposeCts.Dispose();
                _connectionLock.Dispose();
            }

            // 未获取到锁时，对 session 做 best-effort 清理
            // Skip CTS/semaphore disposal to avoid racing with in-flight operations.
            else if (!lockAcquired && _session is not null)
            {
                try
                {
                    _session.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ManagedWebSocketClient] Best-effort session disposal error: {ex.GetType().Name}: {ex.Message}");
                }
            }
            PublishStateChanged(disposedStateChanged);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Occurs when the connection is closed.</para>
    ///     <para xml:lang="zh">当连接关闭时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketClosedEventArgs>? Closed;

    /// <summary>
    ///     <para xml:lang="en">Occurs when an error occurs.</para>
    ///     <para xml:lang="zh">当发生错误时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    ///     <para xml:lang="en">Occurs when a message is received.</para>
    ///     <para xml:lang="zh">当收到消息时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    ///     <para xml:lang="en">Occurs when the client is attempting to reconnect.</para>
    ///     <para xml:lang="zh">当客户端尝试重新连接时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketReconnectingEventArgs>? Reconnecting;

    /// <summary>
    ///     <para xml:lang="en">Occurs when the connection state changes.</para>
    ///     <para xml:lang="zh">当连接状态发生变化时发生。</para>
    /// </summary>
    public event EventHandler<WebSocketStateChangedEventArgs>? StateChanged;

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
        WebSocketStateChangedEventArgs? connectingStateChanged;
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State is WebSocketClientState.Connected or WebSocketClientState.Connecting)
            {
                return;
            }
            Options.Validate();
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
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsDisposed)
        {
            return;
        }
        // Set _manualDisconnect before waiting on the lock so that any concurrent
        // HandleDisconnectAsync that acquires the lock first sees the flag and
        // skips reconnect / reports initiatedByClient correctly.
        _manualDisconnect = true;
        // 使用超时防止 ConfigureWebSocket 等回调长时间持有锁导致无限挂起
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Options.ConnectionTimeout);
        WebSocketStateChangedEventArgs? closingStateChanged;
        WebSocketStateChangedEventArgs? disconnectedStateChanged;
        try
        {
            await _connectionLock.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch
        {
            // 未能获取锁完成断开操作，还原标志以免永久阻止自动重连
            _manualDisconnect = false;
            throw;
        }
        try
        {
            if (State is WebSocketClientState.Disconnected or WebSocketClientState.Disposed)
            {
                // _manualDisconnect was set above; reset it because we are not actually disconnecting.
                _manualDisconnect = false;
                return;
            }
            closingStateChanged = TryUpdateState(WebSocketClientState.Closing);
            await CancelConnectionAsync().ConfigureAwait(false);
            if (_session is not null)
            {
                if (_session.Socket.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
                {
                    try
                    {
                        await _session.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", timeoutCts.Token).ConfigureAwait(false);
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
            if (IsConnectionStartAborted() || State is not (WebSocketClientState.Connecting or WebSocketClientState.Reconnecting))
            {
                throw new OperationCanceledException("Connection attempt aborted before socket initialization.");
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
            Options.ApplyTo(session.Socket);
            Options.ConfigureWebSocket?.Invoke(session.Socket);
            // Guard against DisconnectAsync / DisposeAsync being called synchronously inside ConfigureWebSocket.
            // In that case the session token is already cancelled and the socket is already disposed,
            // so attempting ConnectAsync would throw ObjectDisposedException rather than the expected TaskCanceledException.
            if (IsConnectionStartAborted() || session.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException("Connection attempt aborted inside ConfigureWebSocket callback.");
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
                    throw new OperationCanceledException("Connection attempt aborted after socket connect.");
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

            // Extract token to a local so that CA2025 is not triggered:
            // session is IDisposable, but CancellationToken is a value type copied here before any await.
            var sessionToken = session.Token;
            // ReSharper disable once AccessToDisposedClosure
            _ = ObserveBackgroundTask(Task.Run(() => ReceiveLoop(session), sessionToken), nameof(ReceiveLoop));
            if (Options.HeartbeatEnabled)
            {
                // ReSharper disable once AccessToDisposedClosure
                _ = ObserveBackgroundTask(Task.Run(() => HeartbeatLoop(session), sessionToken), nameof(HeartbeatLoop));
            }
        }
        catch (Exception)
        {
            // Dispose the newly allocated session to release the socket and linked CTS.
            // 同时将 _session 置空，避免后续 CancelConnectionAsync / DisposeAsync 访问已释放的对象。
            // 连接状态由调用方负责回退：ConnectAsync 失败时切回 Disconnected，
            // ReconnectAsync 失败时保持 Reconnecting 以继续后续重试。
            session.Dispose();
            if (ReferenceEquals(Volatile.Read(ref _session), session))
            {
                Volatile.Write(ref _session, null);
            }
            throw;
        }
    }

    private async Task PersistentSendLoop()
    {
        var token = _disposeCts.Token;
        try
        {
            while (await _sendChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (!token.IsCancellationRequested && _sendChannel.Reader.TryRead(out var message))
                {
                    var currentSession = Volatile.Read(ref _session);
                    if (currentSession is null || !IsSessionOpen(currentSession))
                    {
                        message.CompletionSource?.TrySetException(new InvalidOperationException("WebSocket connection is not open."));
                        if (message.RentedArray != null)
                        {
                            ArrayPool<byte>.Shared.Return(message.RentedArray);
                        }
                        continue;
                    }
                    try
                    {
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, currentSession.Token);
                        await currentSession.Socket.SendAsync(message.Data, message.MessageType, message.EndOfMessage, linkedCts.Token).ConfigureAwait(false);
                        message.CompletionSource?.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        message.CompletionSource?.TrySetException(ex);
                        OnError(new(ex, "PersistentSendLoop"));
                        if (IsSessionOpen(currentSession))
                        {
                            continue;
                        }
                        // 先清空旧队列再触发重连，避免重连成功后 FailPendingSends 误杀用户新消息。
                        // 使用 break 而非 return，确保外层 WaitToReadAsync 循环存活，重连后可继续消费。
                        FailPendingSends(ex);
                        await HandleConnectionLoss(currentSession, ex).ConfigureAwait(false);
                        break;
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
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            OnError(new(ex, "PersistentSendLoop"));
            FailPendingSends(ex);
        }
    }

    private async Task ReceiveLoop(ConnectionSession session)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(Options.ReceiveBufferSize);
        var token = session.Token;
        var maxSize = Options.MaxMessageSize;
        try
        {
            while (!token.IsCancellationRequested && IsSessionOpen(session))
            {
                var result = await session.Socket.ReceiveAsync(buffer.AsMemory(0, Options.ReceiveBufferSize), token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleServerClose(session, session.Socket.CloseStatus, session.Socket.CloseStatusDescription).ConfigureAwait(false);
                    return;
                }
                byte[] rentedMessageArray;
                int messageLength;
                if (result.EndOfMessage)
                {
                    // 单帧快速路径
                    if (maxSize > 0 && result.Count > maxSize)
                    {
                        var ex = new InvalidOperationException($"Message size ({result.Count}) exceeded MaxMessageSize ({maxSize}).");
                        OnError(new(ex, "ReceiveLoop"));
                        await HandleConnectionLoss(session, ex).ConfigureAwait(false);
                        return;
                    }
                    rentedMessageArray = ArrayPool<byte>.Shared.Rent(result.Count);
                    messageLength = result.Count;
                    buffer.AsSpan(0, result.Count).CopyTo(rentedMessageArray);
                }
                else
                {
                    // 多帧路径（优化：仅一次最终分配）
                    await using var ms = new PooledMemoryStream();
                    do
                    {
                        if (result.Count > 0)
                        {
                            if (maxSize > 0 && (ulong)ms.Length + (ulong)result.Count > (ulong)maxSize)
                            {
                                var ex = new InvalidOperationException($"Message size exceeded MaxMessageSize ({maxSize}).");
                                OnError(new(ex, "ReceiveLoop"));
                                await HandleConnectionLoss(session, ex).ConfigureAwait(false);
                                return;
                            }
                            ms.Write(buffer.AsSpan(0, result.Count));
                        }
                        if (result.EndOfMessage)
                        {
                            break;
                        }
                        result = await session.Socket.ReceiveAsync(buffer.AsMemory(0, Options.ReceiveBufferSize), token).ConfigureAwait(false);
                        if (result.MessageType != WebSocketMessageType.Close)
                        {
                            continue;
                        }
                        await HandleServerClose(session, session.Socket.CloseStatus, session.Socket.CloseStatusDescription).ConfigureAwait(false);
                        return;
                    } while (true);
                    var finalLength = (int)ms.Length;
                    rentedMessageArray = ArrayPool<byte>.Shared.Rent(finalLength);
                    messageLength = finalLength;
                    ms.GetSpan().CopyTo(rentedMessageArray);
                }
                UpdateLastReceiveTimestamp();

                // 心跳响应过滤
                if (IsHeartbeatResponse(rentedMessageArray.AsSpan(0, messageLength), result.MessageType))
                {
                    ArrayPool<byte>.Shared.Return(rentedMessageArray);
                    continue;
                }
                using var args = new WebSocketMessageReceivedEventArgs(new(rentedMessageArray, 0, messageLength),
                    result.MessageType,
                    true,
                    rentedMessageArray);
                OnMessageReceived(args); // EN: client disposes buffer after all subscribers return; Data is only valid during callback. / ZH: 事件返回后由客户端统一 Dispose，Data 仅在回调期间有效
            }
        }
        catch (OperationCanceledException) { }
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
    private bool IsHeartbeatResponse(ReadOnlySpan<byte> data, WebSocketMessageType messageType)
    {
        var expectedResponse = Options.HeartbeatResponseMessage;
        // 使用独立的 HeartbeatResponseMessageType（而非发送侧的 HeartbeatMessageType），
        // 支持服务端以不同消息类型（如 Text）响应心跳（如 Binary）的场景。
        return !expectedResponse.IsEmpty &&
               messageType == Options.HeartbeatResponseMessageType &&
               data.SequenceEqual(expectedResponse.Span);
    }

    private void FailPendingSends(Exception exception)
    {
        while (_sendChannel.Reader.TryRead(out var msg))
        {
            if (msg.RentedArray is not null)
            {
                ArrayPool<byte>.Shared.Return(msg.RentedArray);
            }
            if (msg.CompletionSource is null)
            {
                continue;
            }
            if (exception is OperationCanceledException operationCanceledException)
            {
                msg.CompletionSource.TrySetCanceled(operationCanceledException.CancellationToken);
                continue;
            }
            msg.CompletionSource.TrySetException(exception);
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

                // 先发送心跳，入队失败（队列满）则跳过本次 tick
                var pingData = Options.HeartbeatMessageFactory?.Invoke() ?? ReadOnlyMemory<byte>.Empty;

                // 仅当需要超时检测时才分配 TCS，以绑定超时计时起点到"实际发送完成"而非"入队时间"
                var pingTcs = Options.HeartbeatTimeout > TimeSpan.Zero
                                  ? new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
                                  : null;
                if (!_sendChannel.Writer.TryWrite(new(pingData, Options.HeartbeatMessageType, true, pingTcs)))
                {
                    // Queue full — heartbeat skipped; do NOT start timeout tracking to prevent false timeout.
                    // This is a transient backpressure condition and is intentionally not surfaced via OnError.
                    pingTcs?.TrySetCanceled(token);
                    continue;
                }
                if (Options.HeartbeatTimeout <= TimeSpan.Zero)
                {
                    continue;
                }

                // 等待 SendLoop 真正将心跳帧发送到 socket 后，再开始超时计时，
                // 避免队列积压时超时判断偏早引发误重连。
                try
                {
                    await pingTcs!.Task.WaitAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    // 发送失败（如连接已关闭）：SendLoop 已通过 OnError 上报，此处跳过超时检查避免误重连
                    continue;
                }

                // 记录本次心跳实际发送完成时刻（局部变量，避免跨 tick 的共享状态竞态）
                var sentTimestamp = Stopwatch.GetTimestamp();

                // 等待 HeartbeatTimeout 后再做超时判断：
                // 确保 ReceiveLoop 有足够时间将 _lastReceiveTimestamp 更新到本次心跳之后，
                // 从而彻底消除"超时检查与消息接收并发"引起的误判。
                try
                {
                    await Task.Delay(Options.HeartbeatTimeout, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                // 等待结束后再读取 lastReceive：
                // 若在 HeartbeatTimeout 内收到任何消息，lastReceive >= sentTimestamp → 连接正常
                var lastReceive = Volatile.Read(ref _lastReceiveTimestamp);
                if (lastReceive >= sentTimestamp)
                {
                    continue;
                }

                // 二次校验：让调度器有机会处理任何待处理的接收操作，再次确认超时状态，降低竞态误判
                await Task.Yield();
                if (Volatile.Read(ref _lastReceiveTimestamp) >= sentTimestamp)
                {
                    continue;
                }
                var elapsedSinceHeartbeat = Stopwatch.GetElapsedTime(sentTimestamp);
                var elapsedSinceReceive = Stopwatch.GetElapsedTime(lastReceive);
                var ex = new TimeoutException($"WebSocket heartbeat timeout: no response for {elapsedSinceHeartbeat.TotalMilliseconds:N0}ms after heartbeat sent (last receive was {elapsedSinceReceive.TotalMilliseconds:N0}ms ago).");
                OnError(new(ex, "HeartbeatLoop timeout"));
                await HandleConnectionLoss(session, ex).ConfigureAwait(false);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
    }

    private async Task HandleServerClose(ConnectionSession session, WebSocketCloseStatus? closeStatus, string? closeStatusDescription)
    {
        await HandleDisconnectAsync(session, closeStatus, closeStatusDescription, null).ConfigureAwait(false);
    }

    private async Task HandleConnectionLoss(ConnectionSession session, Exception ex)
    {
        await HandleDisconnectAsync(session, null, ex.Message, ex).ConfigureAwait(false);
    }

    private async Task HandleDisconnectAsync(ConnectionSession session, WebSocketCloseStatus? closeStatus, string? closeStatusDescription, Exception? exception)
    {
        if (!IsCurrentSession(session) || _disposeCts.IsCancellationRequested)
        {
            return;
        }
        bool shouldReconnect;
        WebSocketStateChangedEventArgs? stateChanged;
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!IsCurrentSession(session) || _disposeCts.IsCancellationRequested)
            {
                return;
            }
            shouldReconnect = Options.AutoReconnect && !_manualDisconnect;
            if (shouldReconnect && State == WebSocketClientState.Reconnecting)
            {
                return;
            }
            stateChanged = TryUpdateState(shouldReconnect ? WebSocketClientState.Reconnecting : WebSocketClientState.Disconnected);
        }
        finally
        {
            _connectionLock.Release();
        }
        PublishStateChanged(stateChanged);
        if (!shouldReconnect)
        {
            OnClosed(new(closeStatus, closeStatusDescription ?? exception?.Message, false));
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

    private ConnectionSession CreateSession(CancellationToken cancellationToken)
    {
        var socket = new ClientWebSocket();
        CancellationTokenSource? cts = null;
        try
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, cancellationToken);
            return new(socket, cts);
        }
        catch
        {
            cts?.Dispose();
            socket.Dispose();
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsCurrentSession(ConnectionSession session) => ReferenceEquals(Volatile.Read(ref _session), session);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSessionOpen(ConnectionSession session) => session.Socket.State == WebSocketState.Open;

    /// <summary>
    /// 观察后台任务的所有终止状态（包括 Canceled），避免产生 UnobservedTaskException。
    /// </summary>
    private async Task ObserveBackgroundTask(Task task, string context)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during disconnect/dispose/reconnect
        }
        catch (Exception ex)
        {
            OnError(new(ex, $"{context} background task failed"));
        }
    }

    private async ValueTask CancelConnectionAsync()
    {
        if (_session is not null)
        {
            try
            {
                await _session.CancelAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // Session already disposed (e.g., after a failed connection attempt)
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsConnectionStartAborted() => IsDisposed || _disposeCts.IsCancellationRequested || _manualDisconnect;

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
        private CancellationTokenSource CancellationSource { get; } = cancellationSource;

        public ClientWebSocket Socket { get; } = socket;

        public CancellationToken Token => CancellationSource.Token;

        public void Dispose()
        {
            CancellationSource.Dispose();
            Socket.Dispose();
        }

        public async ValueTask CancelAsync() => await CancellationSource.CancelAsync().ConfigureAwait(false);
    }
}
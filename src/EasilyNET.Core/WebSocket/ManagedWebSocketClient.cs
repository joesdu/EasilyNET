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
    private readonly Task _persistentDispatchLoopTask; // 持久单一消息分发循环（解耦接收与用户回调）
    private readonly Task _persistentSendLoopTask;     // 持久单一 SendLoop
    private readonly Channel<WebSocketMessageReceivedEventArgs> _receiveChannel;
    private readonly Channel<WebSocketMessage> _sendChannel;

    // 确保单个断开/关闭事件在一次连接生命周期内最多触发一次 Closed。连接成功时重置为 0。
    private int _closedRaisedFlag;

    // Use int with Interlocked.Exchange to guarantee atomic check-and-set across concurrent callers.
    private int _disposedFlag;

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

        // Decouple message receiving from user-callback dispatch so a slow handler can never stall the
        // receive loop (which would otherwise freeze heartbeat liveness tracking and trigger a false timeout).
        var receiveChannelOptions = new BoundedChannelOptions(Options.ReceiveDispatchQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _receiveChannel = Channel.CreateBounded<WebSocketMessageReceivedEventArgs>(receiveChannelOptions);
        _persistentSendLoopTask = Task.Run(PersistentSendLoop, _disposeCts.Token);
        _ = ObserveBackgroundTask(_persistentSendLoopTask, nameof(PersistentSendLoop));
        _persistentDispatchLoopTask = Task.Run(PersistentDispatchLoop, _disposeCts.Token);
        _ = ObserveBackgroundTask(_persistentDispatchLoopTask, nameof(PersistentDispatchLoop));
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

        // 等待持久后台循环（SendLoop + DispatchLoop）优雅退出。
        // DispatchLoop 退出时会在其 finally 中清空接收队列并归还所有未投递缓冲区。
        var backgroundLoops = Task.WhenAll(_persistentSendLoopTask, _persistentDispatchLoopTask);
        var completedTask = await Task.WhenAny(backgroundLoops, Task.Delay(3000)).ConfigureAwait(false);
        if (completedTask == backgroundLoops)
        {
            try
            {
                await backgroundLoops.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected: the persistent background loops are cancelled by _disposeCts during disposal.
            }
        }
        else
        {
            Debug.WriteLine("[ManagedWebSocketClient] Persistent background loops did not exit within 3 seconds during disposal; continuing with best-effort cleanup.");
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

            // 3. Close/dispose the current session only while holding the lock to prevent races with concurrent operations.
            if (lockAcquired && _session is not null)
            {
                var session = _session;
                try
                {
                    if (session.Socket.State is WebSocketState.Open)
                    {
                        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await session.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Disposing", timeoutCts.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ManagedWebSocketClient] Dispose close error: {ex.GetType().Name}: {ex.Message}");
                }
                finally
                {
                    session.Dispose();
                    _session = null;
                }
            }

            // 4. Fail any remaining queued sends
            FailPendingSends(new ObjectDisposedException(nameof(ManagedWebSocketClient)));
        }
        finally
        {
            switch (lockAcquired)
            {
                case true:
                    _connectionLock.Release();
                    // Only dispose shared synchronization primitives when we actually acquired the lock.
                    // Disposing them while concurrent operations may be waiting on _connectionLock or
                    // reading _disposeCts.Token would cause ObjectDisposedException / unpredictable races.
                    _disposeCts.Dispose();
                    _connectionLock.Dispose();
                    break;
                // 未获取到锁时，对 session 做 best-effort 清理
                // Skip CTS/semaphore disposal to avoid racing with in-flight operations.
                case false when _session is not null:
                    try
                    {
                        _session.Dispose();
                        _session = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ManagedWebSocketClient] Best-effort session disposal error: {ex.GetType().Name}: {ex.Message}");
                    }
                    break;
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
    /// <remarks>
    ///     <para xml:lang="en">
    ///     This event is synchronous. Do not use <c>async</c> lambdas with it unless you copy
    ///     <see cref="WebSocketMessageReceivedEventArgs.Data" /> first. For asynchronous processing, prefer <see cref="MessageReceivedAsync" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     此事件是同步事件。除非先复制 <see cref="WebSocketMessageReceivedEventArgs.Data" />，否则不要在此事件上使用 <c>async</c> lambda。异步处理请优先使用
    ///     <see cref="MessageReceivedAsync" />。
    ///     </para>
    /// </remarks>
    public event EventHandler<WebSocketMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    ///     <para xml:lang="en">Occurs when a message is received and allows asynchronous handlers to finish before the pooled receive buffer is returned.</para>
    ///     <para xml:lang="zh">当收到消息时发生，并允许异步处理器在池化接收缓冲区归还前完成处理。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     Prefer this event when message processing needs <c>await</c>. The <see cref="WebSocketMessageReceivedEventArgs.Data" />
    ///     buffer remains valid until every subscribed async handler completes.
    ///     </para>
    ///     <para xml:lang="zh">当消息处理需要 <c>await</c> 时优先使用此事件。直到所有已订阅的异步处理器完成前，<see cref="WebSocketMessageReceivedEventArgs.Data" /> 缓冲区都会保持有效。</para>
    /// </remarks>
    public event Func<ManagedWebSocketClient, WebSocketMessageReceivedEventArgs, ValueTask>? MessageReceivedAsync;

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
        // 按上界一次性租用，省去 GetByteCount 的额外扫描；实际写入长度以 GetBytes 返回值为准。
        var rented = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(text.Length));
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
                // 新的连接生命周期开始：允许后续断开再次触发一次 Closed。
                Interlocked.Exchange(ref _closedRaisedFlag, 0);
            }
            finally
            {
                _connectionLock.Release();
            }
            PublishStateChanged(connectedStateChanged);

            // Extract token to a local so that CA2025 is not triggered:
            // session is IDisposable, but CancellationToken is a value type copied here before any await.
            var sessionToken = session.Token;
            // ReSharper disable once AccessToDisposedClosure
            _ = ObserveBackgroundTask(Task.Run(() => ReceiveLoop(session), sessionToken), nameof(ReceiveLoop));
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
                        await currentSession.Socket.SendAsync(message.Data, message.MessageType, message.EndOfMessage, currentSession.Token).ConfigureAwait(false);
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
        var bufferSize = Options.ReceiveBufferSize;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var token = session.Token;
        var maxSize = Options.MaxMessageSize;
        try
        {
            while (!token.IsCancellationRequested && IsSessionOpen(session))
            {
                var result = await session.Socket.ReceiveAsync(buffer.AsMemory(0, bufferSize), token).ConfigureAwait(false);
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
                        result = await session.Socket.ReceiveAsync(buffer.AsMemory(0, bufferSize), token).ConfigureAwait(false);
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
                var args = new WebSocketMessageReceivedEventArgs(new(rentedMessageArray, 0, messageLength),
                    result.MessageType,
                    true,
                    rentedMessageArray);
                // 入队给专门的分发循环处理；分发循环负责调用订阅者并在完成后归还缓冲区。
                // 这样慢处理器不会阻塞接收循环。队列满时此处会产生背压（等待空间）。
                try
                {
                    await _receiveChannel.Writer.WriteAsync(args, token).ConfigureAwait(false);
                }
                catch
                {
                    // 入队失败（取消等）：归还缓冲区，避免泄漏；异常交由外层统一处理。
                    args.Dispose();
                    throw;
                }
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
    /// 持久消息分发循环：从接收队列读取消息事件，按顺序投递给同步/异步订阅者，
    /// 完成后归还池化缓冲区。与 <see cref="ReceiveLoop" /> 解耦，确保用户回调耗时不会阻塞接收与心跳活性检测。
    /// </summary>
    private async Task PersistentDispatchLoop()
    {
        var token = _disposeCts.Token;
        try
        {
            while (await _receiveChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (!token.IsCancellationRequested && _receiveChannel.Reader.TryRead(out var args))
                {
                    try
                    {
                        // EN: Data is only valid during the callback; buffer is returned right after subscribers complete.
                        // ZH: Data 仅在回调期间有效；订阅者完成后立即归还缓冲区。
                        OnMessageReceived(args);
                        await OnMessageReceivedAsync(args).ConfigureAwait(false);
                    }
                    finally
                    {
                        args.Dispose();
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            OnError(new(ex, nameof(PersistentDispatchLoop)));
        }
        finally
        {
            // 归还队列中所有尚未投递的缓冲区，避免在关闭/取消时泄漏。
            DrainReceiveChannel();
        }
    }

    /// <summary>
    /// 清空接收队列并归还其中所有未投递消息的池化缓冲区。仅由分发循环（唯一读取者）调用。
    /// </summary>
    private void DrainReceiveChannel()
    {
        while (_receiveChannel.Reader.TryRead(out var args))
        {
            args.Dispose();
        }
    }

    private void FailPendingSends(Exception exception)
    {
        var droppedFireAndForgetMessages = false;
        while (_sendChannel.Reader.TryRead(out var msg))
        {
            if (msg.RentedArray is not null)
            {
                ArrayPool<byte>.Shared.Return(msg.RentedArray);
            }
            if (msg.CompletionSource is null)
            {
                droppedFireAndForgetMessages = true;
                continue;
            }
            if (exception is OperationCanceledException operationCanceledException)
            {
                msg.CompletionSource.TrySetCanceled(operationCanceledException.CancellationToken);
                continue;
            }
            msg.CompletionSource.TrySetException(exception);
        }
        if (droppedFireAndForgetMessages)
        {
            OnError(new(exception, "Queued fire-and-forget WebSocket message was dropped before send completion."));
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
            await DisposeSessionIfCurrentAsync(session).ConfigureAwait(false);
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
                await DisposeCurrentSessionAsync().ConfigureAwait(false);
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
        await DisposeCurrentSessionAsync().ConfigureAwait(false);
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
    private bool ShouldAbortReconnect() => _disposeCts.IsCancellationRequested || _manualDisconnect || State is WebSocketClientState.Connected or WebSocketClientState.Connecting;

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

    private async ValueTask OnMessageReceivedAsync(WebSocketMessageReceivedEventArgs e)
    {
        var handlers = MessageReceivedAsync;
        if (handlers is null)
        {
            return;
        }
        var invocationList = handlers.GetInvocationList();
        // 单订阅者快速路径：避免高频接收场景下逐条消息分配/遍历 Delegate[] 的开销。
        if (invocationList.Length == 1)
        {
            try
            {
                await handlers(this, e).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ManagedWebSocketClient] MessageReceivedAsync handler error: {ex.GetType().Name}: {ex.Message}");
                OnError(new(ex, "MessageReceivedAsync handler"));
            }
            return;
        }
        foreach (var d in invocationList)
        {
            var handler = (Func<ManagedWebSocketClient, WebSocketMessageReceivedEventArgs, ValueTask>)d;
            try
            {
                await handler(this, e).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ManagedWebSocketClient] MessageReceivedAsync handler error: {ex.GetType().Name}: {ex.Message}");
                OnError(new(ex, "MessageReceivedAsync handler"));
            }
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
        // 一次连接生命周期内只触发一次 Closed：避免用户主动断开与后台重连耗尽等竞态路径重复上报。
        // 标志在每次成功连接（进入 Connected）时重置，因此后续的关闭仍能正常触发。
        if (Interlocked.Exchange(ref _closedRaisedFlag, 1) != 0)
        {
            return;
        }
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

    private async ValueTask DisposeCurrentSessionAsync()
    {
        ConnectionSession? session;
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            session = _session;
            _session = null;
        }
        finally
        {
            _connectionLock.Release();
        }
        if (session is not null)
        {
            await DisposeSessionAsync(session).ConfigureAwait(false);
        }
    }

    private async ValueTask DisposeSessionIfCurrentAsync(ConnectionSession session)
    {
        var shouldDispose = false;
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (IsCurrentSession(session))
            {
                _session = null;
                shouldDispose = true;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
        if (shouldDispose)
        {
            await DisposeSessionAsync(session).ConfigureAwait(false);
        }
    }

    private static async ValueTask DisposeSessionAsync(ConnectionSession session)
    {
        try
        {
            await session.CancelAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // Session already disposed by a concurrent cleanup path.
        }
        session.Dispose();
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
            // Disposed 是终止态：一旦进入便不可离开，防止并发的 Connect/Reconnect 失败回退把状态改回 Disconnected。
            if (current == (int)WebSocketClientState.Disposed && newState != WebSocketClientState.Disposed)
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
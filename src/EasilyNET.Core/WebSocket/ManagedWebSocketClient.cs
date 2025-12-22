using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

// ReSharper disable EventNeverSubscribedTo.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.WebSocket;

/// <summary>
///     <para xml:lang="en">A managed WebSocket client that handles reconnection, heartbeats, and message queueing.</para>
///     <para xml:lang="zh">一个托管的 WebSocket 客户端，处理重连、心跳和消息队列。</para>
/// </summary>
public sealed class ManagedWebSocketClient : IDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Channel<WebSocketMessage> _sendChannel;
    private CancellationTokenSource? _connectionCts;
    private bool _disposed;
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
        get;
        private set
        {
            if (field == value)
            {
                return;
            }
            var oldState = field;
            field = value;
            OnStateChanged(new(oldState, value));
        }
    } = WebSocketClientState.Disconnected;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _disposeCts.Cancel();
        _connectionCts?.Cancel();

        // Close socket if open
        if (_socket is not null)
        {
            try
            {
                if (_socket.State == WebSocketState.Open)
                {
                    // Fire and forget close
                    _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None);
                }
                _socket.Dispose();
            }
            catch
            {
                // Ignore
            }
        }
        _connectionLock.Dispose();
        _disposeCts.Dispose();
        _connectionCts?.Dispose();
        State = WebSocketClientState.Disposed;
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
            _reconnectAttempts = 0;
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
    public async Task SendAsync(ReadOnlyMemory<byte> message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool endOfMessage = true, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ManagedWebSocketClient));
        // If not connected and not auto-reconnect, throw
        if (State != WebSocketClientState.Connected && !Options.AutoReconnect)
        {
            throw new InvalidOperationException("Client is not connected.");
        }
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var msg = new WebSocketMessage(message, messageType, endOfMessage, tcs);
        await _sendChannel.Writer.WriteAsync(msg, cancellationToken).ConfigureAwait(false);

        // Wait for the message to be sent if we want to ensure delivery order or catch send errors immediately
        // However, for high performance, we might not want to await the actual send here if we trust the queue.
        // But the user might expect SendAsync to mean "sent to socket".
        // Given the requirement for "High performance sending queue", we usually just enqueue.
        // But if we want to propagate errors, we should await the TCS.
        if (Options.KeepMessageOrder)
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
        var bytes = Encoding.UTF8.GetBytes(text);
        return SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Sends a binary message.</para>
    ///     <para xml:lang="zh">发送二进制消息。</para>
    /// </summary>
    public Task SendBinaryAsync(byte[] bytes, CancellationToken cancellationToken = default) => SendAsync(bytes, WebSocketMessageType.Binary, true, cancellationToken);

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
            using var timeoutCts = new CancellationTokenSource(Options.ConnectionTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_connectionCts.Token, timeoutCts.Token);
            if (Options.ServerUri == null)
            {
                throw new InvalidOperationException("ServerUri is null");
            }
            await _socket.ConnectAsync(Options.ServerUri, linkedCts.Token).ConfigureAwait(false);
            State = WebSocketClientState.Connected;

            // Start background loops
            _ = Task.Factory.StartNew(() => ReceiveLoop(_connectionCts.Token), _connectionCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            _ = Task.Factory.StartNew(() => SendLoop(_connectionCts.Token), _connectionCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            if (Options.HeartbeatEnabled)
            {
                _ = Task.Factory.StartNew(() => HeartbeatLoop(_connectionCts.Token), _connectionCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
        var buffer = new byte[Options.ReceiveBufferSize];
        try
        {
            while (!token.IsCancellationRequested && _socket?.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                using var ms = new MemoryStream();
                do
                {
                    result = await _socket.ReceiveAsync(new(buffer), token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await HandleServerClose(result.CloseStatus, result.CloseStatusDescription).ConfigureAwait(false);
                        return;
                    }
                    await ms.WriteAsync(buffer.AsMemory(0, result.Count), token).ConfigureAwait(false);
                } while (!result.EndOfMessage);
                ms.Seek(0, SeekOrigin.Begin);
                var data = ms.ToArray();
                OnMessageReceived(new(data, result.MessageType, true));
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            OnError(new(ex, "ReceiveLoop"));
            await HandleConnectionLoss(ex).ConfigureAwait(false);
        }
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
                        return; // Exit loop, let reconnection handle restart
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            OnError(new(ex, "SendLoop"));
        }
    }

    private async Task HeartbeatLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(Options.HeartbeatIntervalMs, token).ConfigureAwait(false);
                if (_socket?.State != WebSocketState.Open)
                {
                    continue;
                }
                try
                {
                    // Send ping
                    // ClientWebSocket doesn't have a direct Ping method exposed in .NET Standard 2.0/2.1 easily without using reflection or specific frames.
                    // However, .NET 6+ might. But usually sending a small binary message or a specific ping frame if supported.
                    // Standard ClientWebSocket does not expose Ping/Pong control frames directly.
                    // Many implementations send a specific application-level heartbeat message.
                    // Or we can send a 0-byte binary message if the server supports it as ping.
                    var pingData = Options.HeartbeatMessageFactory?.Invoke() ?? Array.Empty<byte>();
                    // We use the send channel to ensure thread safety and ordering
                    // But heartbeat usually needs to bypass the queue if the queue is full? 
                    // Or just put it in queue.
                    await SendAsync(pingData, WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
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
        if (State == WebSocketClientState.Reconnecting || _disposeCts.IsCancellationRequested)
        {
            return;
        }

        // Ensure we transition to a state that allows reconnection
        State = WebSocketClientState.Disconnected;
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
            if (State == WebSocketClientState.Reconnecting || State == WebSocketClientState.Connected || _disposeCts.IsCancellationRequested)
            {
                return;
            }
            State = WebSocketClientState.Reconnecting;
            while (Options.MaxReconnectAttempts == -1 || _reconnectAttempts < Options.MaxReconnectAttempts)
            {
                if (_disposeCts.IsCancellationRequested)
                {
                    return;
                }
                _reconnectAttempts++;

                // Calculate delay
                var delay = Options.ReconnectDelayMs;
                if (Options.UseExponentialBackoff)
                {
                    delay = (int)Math.Min(Options.ReconnectDelayMs * Math.Pow(2, _reconnectAttempts - 1), Options.MaxReconnectDelayMs);
                }
                var args = new WebSocketReconnectingEventArgs(_reconnectAttempts, TimeSpan.FromMilliseconds(delay), null);
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
                    _reconnectAttempts = 0;
                    return;
                }
                catch (Exception ex)
                {
                    OnError(new(ex, $"Reconnection attempt {_reconnectAttempts} failed"));
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

    private void OnStateChanged(WebSocketStateChangedEventArgs e) => StateChanged?.Invoke(this, e);
    private void OnMessageReceived(WebSocketMessageReceivedEventArgs e) => MessageReceived?.Invoke(this, e);
    private void OnError(WebSocketErrorEventArgs e) => Error?.Invoke(this, e);
    private void OnReconnecting(WebSocketReconnectingEventArgs e) => Reconnecting?.Invoke(this, e);
    private void OnClosed(WebSocketClosedEventArgs e) => Closed?.Invoke(this, e);
}
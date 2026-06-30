using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
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
    private readonly Channel<WebSocketMessage> _receiveChannel;
    private readonly Channel<WebSocketMessage> _sendChannel;
    private readonly System.Net.WebSockets.WebSocket _socket;

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
        // 将消息接收与用户回调分发解耦：接收循环只负责入队，由独立的分发循环调用 OnMessageAsync。
        // 这样慢处理器不会阻塞接收循环。
        if (options.ReceiveDispatchQueueCapacity <= 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(options.ReceiveDispatchQueueCapacity), $"{nameof(options.ReceiveDispatchQueueCapacity)} must be greater than zero.");
        }
        var receiveChannelOptions = new BoundedChannelOptions(options.ReceiveDispatchQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        };
        _receiveChannel = Channel.CreateBounded<WebSocketMessage>(receiveChannelOptions);
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
        // 按上界一次性租用，省去 GetByteCount 的额外扫描；实际写入长度以 GetBytes 返回值为准。
        var rented = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(text.Length));
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
                // CloseAsync 会等待对端的关闭确认；对不响应的客户端必须设置超时，否则可能无限挂起。
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_options.CloseTimeout);
                await _socket.CloseAsync(closeStatus, statusDescription, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore: the close handshake timed out or was cancelled.
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
        // 发送为即发即弃（入队后即返回），不再分配从不被 await 的 TaskCompletionSource。
        // 入队失败由 WriteAsync 直接抛出；实际发送错误由 SendLoop 记录日志。
        var msg = new WebSocketMessage(message, messageType, endOfMessage, null, rentedArray);
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
    }

    internal async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var token = linkedCts.Token;
        var isConnected = false;
        Task? sendTask = null;
        Task? receiveTask = null;
        Task? dispatchTask = null;
        try
        {
            // 先完成连接回调，确保 OnConnectedAsync 在任何 OnMessageAsync 之前执行，
            // 避免处理器在初始化（如鉴权、状态准备）完成前就收到消息。
            await _handler.OnConnectedAsync(this).ConfigureAwait(false);
            isConnected = true;
            sendTask = SendLoopAsync(token);
            dispatchTask = DispatchLoopAsync(token);
            receiveTask = ReceiveLoopAsync(token);
            await Task.WhenAny(sendTask, receiveTask, dispatchTask).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _handler.OnErrorAsync(this, ex).ConfigureAwait(false);
        }
        finally
        {
            // 通知所有循环停止
            await _cts.CancelAsync().ConfigureAwait(false);
            // 完成两条通道，确保 SendLoop / DispatchLoop 不会挂起在 WaitToReadAsync
            _sendChannel.Writer.TryComplete();
            _receiveChannel.Writer.TryComplete();
            // 等待所有已启动的循环结束，避免未观察到的异常
            var startedTasks = new[] { sendTask, receiveTask, dispatchTask }
                               .Where(static t => t is not null)
                               .Cast<Task>()
                               .ToArray();
            if (startedTasks.Length > 0)
            {
                try
                {
                    await Task.WhenAll(startedTasks).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // 取消是正常行为
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(ex, "[WebSocketSession:{Id}] Error during task cleanup", Id);
                    }
                }
            }
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
        var bufferSize = _options.ReceiveBufferSize;
        var maxSize = _options.MaxMessageSize;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            while (!token.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                // 第一帧接收
                var result = await _socket.ReceiveAsync(buffer.AsMemory(0, bufferSize), token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (_socket.State == WebSocketState.CloseReceived)
                    {
                        await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close", CancellationToken.None).ConfigureAwait(false);
                    }
                    return;
                }
                ReadOnlyMemory<byte> data;
                if (result.EndOfMessage)
                {
                    // 单帧快速路径：直接复制缓冲区，无需 PooledMemoryStream
                    if (maxSize > 0 && result.Count > maxSize)
                    {
                        await CloseForOversizedMessageAsync(result.Count).ConfigureAwait(false);
                        return;
                    }
                    data = buffer.AsSpan(0, result.Count).ToArray();
                }
                else
                {
                    // 多帧路径：使用 PooledMemoryStream 拼接
                    await using var ms = new PooledMemoryStream();
                    if (result.Count > 0)
                    {
                        ms.Write(buffer.AsSpan(0, result.Count));
                    }
                    do
                    {
                        result = await _socket.ReceiveAsync(buffer.AsMemory(0, bufferSize), token).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            if (_socket.State == WebSocketState.CloseReceived)
                            {
                                await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close", CancellationToken.None).ConfigureAwait(false);
                            }
                            return;
                        }
                        if (result.Count > 0)
                        {
                            // 在写入前累加校验，防止恶意客户端通过超大消息（或永不结束的分片）耗尽内存
                            if (maxSize > 0 && (ulong)ms.Length + (ulong)result.Count > (ulong)maxSize)
                            {
                                await CloseForOversizedMessageAsync(ms.Length + result.Count).ConfigureAwait(false);
                                return;
                            }
                            ms.Write(buffer.AsSpan(0, result.Count));
                        }
                    } while (!result.EndOfMessage);
                    data = ms.ToArray();
                }

                // 入队给分发循环处理；队列满时此处产生背压。data 为独立的托管数组，无池化生命周期约束。
                await _receiveChannel.Writer.WriteAsync(new(data, result.MessageType, true), token).ConfigureAwait(false);
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
    /// 关闭因超过 <see cref="WebSocketSessionOptions.MaxMessageSize" /> 的连接，并记录告警日志。
    /// </summary>
    private async Task CloseForOversizedMessageAsync(long size)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("[WebSocketSession:{Id}] Message size ({Size}) exceeded MaxMessageSize ({Max}); closing connection", Id, size, _options.MaxMessageSize);
        }
        await CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message too big", CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// 持久消息分发循环：从接收队列按顺序读取消息并投递给 <see cref="WebSocketHandler.OnMessageAsync" />，
    /// 与 <see cref="ReceiveLoopAsync" /> 解耦，确保用户处理器耗时不会阻塞接收循环。
    /// </summary>
    private async Task DispatchLoopAsync(CancellationToken token)
    {
        try
        {
            while (await _receiveChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (!token.IsCancellationRequested && _receiveChannel.Reader.TryRead(out var message))
                {
                    try
                    {
                        await _handler.OnMessageAsync(this, message).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await _handler.OnErrorAsync(this, ex).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 取消是正常行为
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
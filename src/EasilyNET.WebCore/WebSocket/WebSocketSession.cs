using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using EasilyNET.Core.WebSocket;

namespace EasilyNET.WebCore.WebSocket;

internal sealed class WebSocketSession : IWebSocketSession
{
    private const int ReceiveBufferSize = 1024 * 4;
    private readonly CancellationTokenSource _cts = new();
    private readonly WebSocketHandler _handler;
    private readonly Channel<WebSocketMessage> _sendChannel;
    private readonly System.Net.WebSockets.WebSocket _socket;

    public WebSocketSession(string id, System.Net.WebSockets.WebSocket socket, WebSocketHandler handler, WebSocketSessionOptions options)
    {
        Id = id;
        _socket = socket;
        _handler = handler;
        var channelOptions = new BoundedChannelOptions(options.SendQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _sendChannel = Channel.CreateBounded<WebSocketMessage>(channelOptions);
    }

    public string Id { get; }

    public WebSocketState State => _socket.State;

    public Task SendAsync(ReadOnlyMemory<byte> message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool endOfMessage = true, CancellationToken cancellationToken = default) => SendAsyncInternal(message, messageType, endOfMessage, cancellationToken, null);

    public Task SendTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var byteCount = Encoding.UTF8.GetByteCount(text);
        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        var bytesUsed = Encoding.UTF8.GetBytes(text, rented);
        return SendAsyncInternal(new(rented, 0, bytesUsed), WebSocketMessageType.Text, true, cancellationToken, rented);
    }

    public Task SendBinaryAsync(byte[] bytes, CancellationToken cancellationToken = default) => SendAsync(bytes, WebSocketMessageType.Binary, true, cancellationToken);

    public async Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken = default)
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await _socket.CloseAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(false);
        }
        try
        {
            await _cts.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
            // Ignore
        }
    }

    private async Task SendAsyncInternal(ReadOnlyMemory<byte> message, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken, byte[]? rentedArray)
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
        var isConnected = false;
        try
        {
            await _handler.OnConnectedAsync(this).ConfigureAwait(false);
            isConnected = true;
            await Task.WhenAny(sendTask, receiveTask).ConfigureAwait(false);
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
            if (_socket.State != WebSocketState.Closed && _socket.State != WebSocketState.Aborted)
            {
                try
                {
                    await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore
                }
            }
            _socket.Dispose();
            _cts.Dispose();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);
        try
        {
            while (!token.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                ValueWebSocketReceiveResult result;
                using var ms = new MemoryStream();
                do
                {
                    result = await _socket.ReceiveAsync(new Memory<byte>(buffer), token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (_socket.State == WebSocketState.CloseReceived)
                        {
                            // Echo close
                            await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close", CancellationToken.None).ConfigureAwait(false);
                        }
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);
                ms.Seek(0, SeekOrigin.Begin);
                var data = ms.ToArray();
                await _handler.OnMessageAsync(this, new(data, result.MessageType, true)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (WebSocketException ex) // Connection lost
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
            await Console.Error.WriteLineAsync($"[WebSocketSession:{Id}] Send loop error: {ex}");
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
                    message.CompletionSource?.TrySetCanceled();
                }
            }
        }
    }
}
using System.IO.Pipes;
using EasilyNET.Ipc.Interfaces;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Transports;

/// <summary>
/// Provides a transport mechanism for inter-process communication (IPC) using named pipes.
/// </summary>
/// <remarks>
/// This class supports both client and server roles for named pipe communication.  It allows
/// asynchronous operations for connecting, reading, and writing data over the pipe. Use the <see cref="IsConnected" />
/// property to check the connection status before performing operations.
/// </remarks>
public class NamedPipeTransport : IIpcTransport
{
    private readonly NamedPipeClientStream? _clientStream;
    private readonly bool _isServer;
    private readonly ILogger? _logger;
    private readonly NamedPipeServerStream? _serverStream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedPipeTransport" /> class for communication over a named pipe.
    /// </summary>
    /// <remarks>
    /// When <paramref name="isServer" /> is <see langword="true" />, the instance creates a named pipe
    /// server stream. When <paramref name="isServer" /> is <see langword="false" />, the instance creates a named pipe
    /// client stream.
    /// </remarks>
    /// <param name="pipeName">The name of the pipe to use for communication. This must match the pipe name used by the other endpoint.</param>
    /// <param name="isServer">
    /// A value indicating whether this instance operates as a server (<see langword="true" />) or as a client (
    /// <see
    ///     langword="false" />
    /// ).
    /// </param>
    /// <param name="maxServerInstances">
    /// The maximum number of server instances allowed for the named pipe. This parameter is only applicable when
    /// <paramref name="isServer" /> is <see langword="true" />. Defaults to 1.
    /// </param>
    /// <param name="logger">
    /// An optional <see cref="ILogger" /> instance for logging diagnostic information. If <see langword="null" />, no
    /// logging will be performed.
    /// </param>
    public NamedPipeTransport(string pipeName, bool isServer, int maxServerInstances = 1, ILogger? logger = null)
    {
        _isServer = isServer;
        _logger = logger;
        if (_isServer)
        {
            _serverStream = new(pipeName, PipeDirection.InOut, maxServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }
        else
        {
            _clientStream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }
    }

    /// <inheritdoc />
    public bool IsConnected => _isServer ? _serverStream?.IsConnected ?? false : _clientStream?.IsConnected ?? false;

    /// <inheritdoc />
    public async Task WaitForConnectionAsync(CancellationToken cancellationToken)
    {
        if (!_isServer || _serverStream == null)
        {
            throw new InvalidOperationException("仅服务端支持等待连接");
        }
        await _serverStream.WaitForConnectionAsync(cancellationToken);
        _logger?.LogDebug("命名管道客户端已连接");
    }

    /// <inheritdoc />
    public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (_isServer || _clientStream == null)
        {
            throw new InvalidOperationException("仅客户端支持连接操作");
        }
        await _clientStream.ConnectAsync((int)timeout.TotalMilliseconds, cancellationToken);
        _logger?.LogDebug("命名管道连接成功");
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadAsync(CancellationToken cancellationToken)
    {
        Stream? stream = _isServer ? _serverStream : _clientStream;
        if (stream is null or PipeStream { IsConnected: false })
        {
            throw new InvalidOperationException("管道未连接");
        }
        var lengthBuffer = new byte[4];
        var bytesRead = await stream.ReadAsync(lengthBuffer.AsMemory(0, 4), cancellationToken);
        if (bytesRead != 4)
        {
            throw new IOException("读取数据长度失败");
        }
        var length = BitConverter.ToInt32(lengthBuffer, 0);
        var buffer = new byte[length];
        bytesRead = await stream.ReadAsync(buffer.AsMemory(0, length), cancellationToken);
        return bytesRead != length ? throw new IOException("读取数据失败") : buffer;
    }

    /// <inheritdoc />
    public async Task WriteAsync(byte[] data, CancellationToken cancellationToken)
    {
        Stream? stream = _isServer ? _serverStream : _clientStream;
        if (stream is null or PipeStream { IsConnected: false })
        {
            throw new InvalidOperationException("管道未连接");
        }
        var lengthBuffer = BitConverter.GetBytes(data.Length);
        await stream.WriteAsync(lengthBuffer.AsMemory(0, lengthBuffer.Length), cancellationToken);
        await stream.WriteAsync(data.AsMemory(0, data.Length), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        switch (_isServer)
        {
            case true when _serverStream is { IsConnected: true }:
                _serverStream.Disconnect();
                _logger?.LogDebug("命名管道服务端已断开连接");
                break;
            case false when _clientStream is { IsConnected: true }:
                _clientStream.Close();
                _logger?.LogDebug("命名管道客户端已断开连接");
                break;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _serverStream?.Dispose();
        _clientStream?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
using System.Net.Sockets;
using EasilyNET.Ipc.Interfaces;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Transports;

/// <summary>
/// Provides a transport mechanism for inter-process communication (IPC) using Unix domain sockets.
/// </summary>
/// <remarks>
/// This class supports both server and client roles for communication over Unix domain sockets.  The
/// server can accept connections from multiple clients, while the client can connect to a server. Use this class to
/// send and receive data between processes on the same machine.
/// </remarks>
public class UnixSocketTransport : IIpcTransport
{
    private readonly Socket? _clientSocket;
    private readonly bool _isServer;
    private readonly ILogger? _logger;
    private readonly Socket? _serverSocket;
    private readonly string _socketPath;
    private Socket? _connectedSocket;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnixSocketTransport" /> class for communication over a Unix domain
    /// socket.
    /// </summary>
    /// <remarks>
    /// When operating as a server (<paramref name="isServer" /> is <see langword="true" />), the
    /// constructor will delete any existing file at the specified <paramref name="socketPath" /> to ensure the socket
    /// can be created. The server socket will then be bound to the path and start listening for connections.  When
    /// operating as a client (<paramref name="isServer" /> is <see langword="false" />), the constructor initializes a
    /// client socket but does not connect it.
    /// </remarks>
    /// <param name="socketPath">The file system path of the Unix domain socket. This must be a valid path and cannot be null or empty.</param>
    /// <param name="isServer">
    /// A value indicating whether the instance operates as a server.  If <see langword="true" />, the instance will
    /// create and listen on the specified socket; otherwise, it will act as a client.
    /// </param>
    /// <param name="maxServerInstances">
    /// The maximum number of simultaneous connections the server can accept. This parameter is only applicable when
    /// <paramref name="isServer" /> is <see langword="true" />. Defaults to 1.
    /// </param>
    /// <param name="logger">An optional <see cref="ILogger" /> instance for logging diagnostic information. Can be <see langword="null" />.</param>
    public UnixSocketTransport(string socketPath, bool isServer, int maxServerInstances = 1, ILogger? logger = null)
    {
        _socketPath = socketPath;
        _isServer = isServer;
        _logger = logger;
        if (_isServer)
        {
            if (File.Exists(_socketPath))
            {
                File.Delete(_socketPath);
            }
            _serverSocket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(_socketPath);
            _serverSocket.Bind(endpoint);
            _serverSocket.Listen(maxServerInstances1);
        }
        else
        {
            _clientSocket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        }
    }

    /// <inheritdoc />
    public bool IsConnected => _isServer ? _connectedSocket?.Connected ?? false : _clientSocket?.Connected ?? false;

    /// <inheritdoc />
    public async Task WaitForConnectionAsync(CancellationToken cancellationToken)
    {
        if (!_isServer || _serverSocket == null)
        {
            throw new InvalidOperationException("仅服务端支持等待连接");
        }
        _connectedSocket?.Dispose();
        _connectedSocket = await _serverSocket.AcceptAsync(cancellationToken);
        _logger?.LogDebug("Unix 域套接字客户端已连接");
    }

    /// <inheritdoc />
    public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (_isServer || _clientSocket == null)
        {
            throw new InvalidOperationException("仅客户端支持连接操作");
        }
        var endpoint = new UnixDomainSocketEndPoint(_socketPath);
        using var cts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
        await _clientSocket.ConnectAsync(endpoint, linkedCts.Token);
        _logger?.LogDebug("Unix 域套接字连接成功");
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadAsync(CancellationToken cancellationToken)
    {
        var socket = _isServer ? _connectedSocket : _clientSocket;
        if (socket is not { Connected: true })
        {
            throw new InvalidOperationException("套接字未连接");
        }
        var lengthBuffer = new byte[4];
        var bytesRead = await socket.ReceiveAsync(lengthBuffer, SocketFlags.None, cancellationToken);
        if (bytesRead != 4)
        {
            throw new IOException("读取数据长度失败");
        }
        var length = BitConverter.ToInt32(lengthBuffer, 0);
        var buffer = new byte[length];
        bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
        return bytesRead != length ? throw new IOException("读取数据失败") : buffer;
    }

    /// <inheritdoc />
    public async Task WriteAsync(byte[] data, CancellationToken cancellationToken)
    {
        var socket = _isServer ? _connectedSocket : _clientSocket;
        if (socket is not { Connected: true })
        {
            throw new InvalidOperationException("套接字未连接");
        }

        var lengthBuffer = BitConverter.GetBytes(data.Length);
        await socket.SendAsync(lengthBuffer, SocketFlags.None, cancellationToken);
        await socket.SendAsync(data, SocketFlags.None, cancellationToken);
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        switch (_isServer)
        {
            case true when _connectedSocket is { Connected: true }:
                _connectedSocket.Shutdown(SocketShutdown.Both);
                _connectedSocket.Close();
                _logger?.LogDebug("Unix 域套接字服务端已断开连接");
                break;
            case false when _clientSocket is { Connected: true }:
                _clientSocket.Shutdown(SocketShutdown.Both);
                _clientSocket.Close();
                _logger?.LogDebug("Unix 域套接字客户端已断开连接");
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
        _connectedSocket?.Dispose();
        _serverSocket?.Dispose();
        _clientSocket?.Dispose();
        if (_isServer && File.Exists(_socketPath))
        {
            File.Delete(_socketPath);
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
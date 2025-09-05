using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;

namespace EasilyNET.Consensus.Raft.Rpc;

/// <summary>
/// StreamJsonRpc Raft 服务器
/// </summary>
public class StreamJsonRpcRaftServer : IDisposable
{
    private readonly ConcurrentBag<IDisposable> _connections = [];
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly int _port;
    private readonly RaftNode _raftNode;
    private CancellationTokenSource? _cancellationTokenSource;
    private TcpListener? _listener;
    private JoinableTask? _listeningTask;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="raftNode">Raft节点</param>
    /// <param name="port">监听端口</param>
    /// <param name="joinableTaskFactory">可连接任务工厂</param>
    public StreamJsonRpcRaftServer(RaftNode raftNode, int port, JoinableTaskFactory joinableTaskFactory)
    {
        _raftNode = raftNode;
        _port = port;
        _joinableTaskFactory = joinableTaskFactory;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _ = StopAsync();
    }

    /// <summary>
    /// 启动服务器
    /// </summary>
    public async Task StartAsync()
    {
        _listener = new(IPAddress.Any, _port);
        _listener.Start();
        _cancellationTokenSource = new();
        _listeningTask = _joinableTaskFactory.RunAsync(ListenForConnectionsAsync);
        await Task.CompletedTask; // 确保方法是异步的
    }

    /// <summary>
    /// 监听连接
    /// </summary>
    private async Task ListenForConnectionsAsync()
    {
        while (!_cancellationTokenSource!.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                _ = _joinableTaskFactory.RunAsync(() => HandleClientAsync(client, _cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接受连接失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 处理客户端连接
    /// </summary>
    /// <param name="client">TCP客户端</param>
    /// <param name="serverToken">服务器取消标记</param>
    private async Task HandleClientAsync(TcpClient client, CancellationToken serverToken)
    {
        CancellationTokenSource? linkedCts = null;
        try
        {
            var stream = client.GetStream();
            var formatter = new SystemTextJsonFormatter
            {
                JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            };
            var handler = new HeaderDelimitedMessageHandler(stream, stream, formatter);
            var rpc = new JsonRpc(handler);
            rpc.AddLocalRpcTarget(new RaftRpcTarget(_raftNode));
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverToken);
            rpc.Disconnected += (_, _) =>
            {
                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    linkedCts?.Cancel();
                }
                catch
                {
                    // ignored
                }
            };
            rpc.StartListening();
            _connections.Add(rpc);

            // 等待连接断开
            while (client.Connected && !linkedCts.IsCancellationRequested)
            {
                await Task.Delay(500, linkedCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理客户端连接失败: {ex.Message}");
        }
        finally
        {
            try
            {
                linkedCts?.Dispose();
            }
            catch
            {
                // ignored
            }
            try
            {
                client.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public async Task StopAsync()
    {
        if (_cancellationTokenSource != null)
        {
            try
            {
                await _cancellationTokenSource.CancelAsync();
            }
            catch
            {
                await _cancellationTokenSource.CancelAsync();
            }
            if (_listeningTask != null)
            {
                try
                {
                    await _listeningTask;
                }
                catch
                {
                    // ignored
                }
            }
        }
        try
        {
            _listener?.Stop();
        }
        catch
        {
            // ignored
        }

        // 释放所有 jsonrpc 连接
        foreach (var disp in _connections)
        {
            try
            {
                disp.Dispose();
            }
            catch
            {
                // ignored
            }
        }
        _connections.Clear();
    }
}
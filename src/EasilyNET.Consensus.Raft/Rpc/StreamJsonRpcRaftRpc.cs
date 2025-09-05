using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;
using EasilyNET.Consensus.Raft.Discovery;
using EasilyNET.Consensus.Raft.Protocols;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;

namespace EasilyNET.Consensus.Raft.Rpc;

/// <summary>
/// 基于StreamJsonRpc的Raft RPC实现，内置自动重连与重试
/// </summary>
public class StreamJsonRpcRaftRpc : IRaftRpc, IDisposable
{
    private const int DefaultInvokeTimeoutMs = 5000; // 单次调用超时
    private const int MaxRetry = 3;                  // 最大重试次数（包含首次尝试）
    private const int BaseBackoffMs = 100;           // 指数退避基准

    private readonly string _currentNodeId;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, ConnectionState> _rpcClients;
    private readonly ConcurrentDictionary<string, StreamJsonRpcRaftServer> _servers;
    private readonly IServiceDiscovery _serviceDiscovery;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="currentNodeId">当前节点ID</param>
    /// <param name="serviceDiscovery">服务发现实例</param>
    public StreamJsonRpcRaftRpc(string currentNodeId, IServiceDiscovery serviceDiscovery)
    {
        _currentNodeId = currentNodeId;
        _serviceDiscovery = serviceDiscovery;
        _rpcClients = new();
        _servers = new();
        _joinableTaskFactory = new(new JoinableTaskContext());
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _ = StopAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 请求投票（自动重试/重连）
    /// </summary>
    public async Task<VoteResponse> RequestVoteAsync(string targetNodeId, VoteRequest request)
    {
        if (targetNodeId == _currentNodeId)
        {
            throw new InvalidOperationException("不能向自己发送投票请求");
        }
        return await InvokeWithRetryAsync(targetNodeId, async rpc =>
                   {
                       using var cts = new CancellationTokenSource(DefaultInvokeTimeoutMs);
                       return await rpc.InvokeAsync<VoteResponse>("RequestVoteAsync", new object?[] { request }, cts.Token);
                   },
                   new() { Term = 0, VoteGranted = false });
    }

    /// <summary>
    /// 追加日志条目（自动重试/重连）
    /// </summary>
    public async Task<AppendEntriesResponse> AppendEntriesAsync(string targetNodeId, AppendEntriesRequest request)
    {
        if (targetNodeId == _currentNodeId)
        {
            throw new InvalidOperationException("不能向自己发送追加请求");
        }
        return await InvokeWithRetryAsync(targetNodeId, async rpc =>
                   {
                       using var cts = new CancellationTokenSource(DefaultInvokeTimeoutMs);
                       return await rpc.InvokeAsync<AppendEntriesResponse>("AppendEntriesAsync", new object?[] { request }, cts.Token);
                   },
                   new() { Term = 0, Success = false });
    }

    /// <summary>
    /// 添加节点，仅当 nodeId 等于当前节点时才启动本地服务器。
    /// </summary>
    public async Task AddNodeAsync(string nodeId, RaftNode raftNode)
    {
        if (nodeId != _currentNodeId)
        {
            return;
        }
        lock (_lock)
        {
            if (_servers.ContainsKey(nodeId))
            {
                return; // 已为本节点创建服务器
            }
        }
        var address = await _serviceDiscovery.GetNodeAddressAsync(nodeId) ?? throw new InvalidOperationException($"无法通过服务发现获取节点 {nodeId} 的地址");
        lock (_lock)
        {
            _servers[nodeId] = new(raftNode, address.Port, _joinableTaskFactory);
        }
    }

    /// <summary>
    /// 启动所有连接（仅启动本地服务器监听）
    /// </summary>
    public async Task StartAsync()
    {
        var startTasks = _servers.Values.Select(server => server.StartAsync()).ToArray();
        await Task.WhenAll(startTasks);
    }

    /// <summary>
    /// 停止所有连接
    /// </summary>
    public async Task StopAsync()
    {
        // 停止所有客户端
        foreach (var kv in _rpcClients)
        {
            kv.Value.Dispose();
        }
        _rpcClients.Clear();

        // 停止所有服务器
        var stopTasks = _servers.Values.Select(server => server.StopAsync()).ToArray();
        await Task.WhenAll(stopTasks);
        _servers.Clear();
    }

    // ----------------- 内部实现 -----------------

    private async Task<T> InvokeWithRetryAsync<T>(string targetNodeId, Func<JsonRpc, Task<T>> invoker, T onFailureResult)
    {
        var attempt = 0;
        Exception? lastEx = null;
        while (attempt < MaxRetry)
        {
            attempt++;
            try
            {
                var state = await GetOrCreateConnectionAsync(targetNodeId);
                if (state.Rpc is null)
                {
                    throw new InvalidOperationException("RPC 未初始化");
                }
                return await invoker(state.Rpc);
            }
            catch (Exception ex)
            {
                lastEx = ex;
                Console.WriteLine($"RPC调用失败 {targetNodeId} (第{attempt}次): {ex.Message}");
                await ForceReconnectAsync(targetNodeId);
                if (attempt < MaxRetry)
                {
                    await Task.Delay(BaseBackoffMs * (int)Math.Pow(2, attempt - 1));
                }
            }
        }
        Console.WriteLine($"RPC调用失败(已达最大重试次数) {targetNodeId}: {lastEx?.Message}");
        return onFailureResult;
    }

    private async Task<ConnectionState> GetOrCreateConnectionAsync(string targetNodeId)
    {
        if (!_rpcClients.TryGetValue(targetNodeId, out var state))
        {
            var address = await _serviceDiscovery.GetNodeAddressAsync(targetNodeId) ?? throw new InvalidOperationException($"无法获取节点 {targetNodeId} 的地址");
            state = new() { TargetNodeId = targetNodeId, Address = address };
            _rpcClients[targetNodeId] = state;
        }
        await state.Sync.WaitAsync();
        try
        {
            if (state.IsConnected)
            {
                return state;
            }

            // 地址可能变化，尝试通过服务发现刷新
            var address = await _serviceDiscovery.GetNodeAddressAsync(targetNodeId) ?? state.Address;
            state.Address = address;

            // 清理旧连接
            try
            {
                state.Rpc?.Dispose();
            }
            catch
            {
                // ignored
            }
            try
            {
                state.Client?.Dispose();
            }
            catch
            {
                // ignored
            }
            state.Rpc = null;
            state.Client = null;

            // 建立新连接
            var client = new TcpClient();
            try
            {
                using var cts = new CancellationTokenSource(DefaultInvokeTimeoutMs);
#if NET8_0_OR_GREATER
                await client.ConnectAsync(address.Host, address.Port, cts.Token);
#else
                await client.ConnectAsync(address.Host, address.Port);
#endif
            }
            catch
            {
                client.Dispose();
                throw;
            }
            try
            {
                client.NoDelay = true;
                try
                {
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                }
                catch
                {
                    /* best-effort */
                }
                var stream = client.GetStream();
                var formatter = new SystemTextJsonFormatter
                {
                    JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                };
                var handler = new HeaderDelimitedMessageHandler(stream, stream, formatter);
                var rpcClient = new JsonRpc(handler);
                rpcClient.Disconnected += (_, _) =>
                {
                    _rpcClients.TryGetValue(targetNodeId, out var st);
                    st?.Sync.Wait();
                    try
                    {
                        st?.Rpc?.Dispose();
                        st?.Client?.Dispose();
                        if (st == null)
                        {
                            return;
                        }
                        st.Rpc = null;
                        st.Client = null;
                    }
                    finally
                    {
                        st?.Sync.Release();
                    }
                };
                rpcClient.StartListening();
                state.Client = client;
                state.Rpc = rpcClient;
                state.FailureCount = 0;
                return state;
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }
        finally
        {
            state.Sync.Release();
        }
    }

    private async Task ForceReconnectAsync(string targetNodeId)
    {
        if (_rpcClients.TryGetValue(targetNodeId, out var state))
        {
            await state.Sync.WaitAsync();
            try
            {
                try
                {
                    state.Rpc?.Dispose();
                }
                catch
                {
                    // ignored
                }
                try
                {
                    state.Client?.Dispose();
                }
                catch
                {
                    // ignored
                }
                state.Rpc = null;
                state.Client = null;
            }
            finally
            {
                state.Sync.Release();
            }
        }
    }

    private sealed class ConnectionState : IDisposable
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public required string TargetNodeId { get; init; }

        public required NodeAddress Address { get; set; }

        public TcpClient? Client { get; set; }

        public JsonRpc? Rpc { get; set; }

        public SemaphoreSlim Sync { get; } = new(1, 1);

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public int FailureCount { get; set; }

        public bool IsConnected => Client is { Connected: true } && Rpc != null;

        public void Dispose()
        {
            try
            {
                Rpc?.Dispose();
            }
            catch
            {
                /* ignored */
            }
            try
            {
                Client?.Dispose();
            }
            catch
            {
                /* ignored */
            }
            Sync.Dispose();
        }
    }
}
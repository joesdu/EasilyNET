using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using StreamJsonRpc;
using Microsoft.VisualStudio.Threading;

namespace EasilyNET.Consensus.Raft;

/// <summary>
/// 基于StreamJsonRpc的Raft RPC实现
/// </summary>
public class StreamJsonRpcRaftRpc : IRaftRpc, IDisposable
{
    private readonly string _currentNodeId;
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ConcurrentDictionary<string, JsonRpc> _rpcClients;
    private readonly ConcurrentDictionary<string, StreamJsonRpcRaftServer> _servers;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly object _lock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="currentNodeId">当前节点ID</param>
    /// <param name="serviceDiscovery">服务发现实例</param>
    public StreamJsonRpcRaftRpc(string currentNodeId, IServiceDiscovery serviceDiscovery)
    {
        _currentNodeId = currentNodeId;
        _serviceDiscovery = serviceDiscovery;
        _rpcClients = new ConcurrentDictionary<string, JsonRpc>();
        _servers = new ConcurrentDictionary<string, StreamJsonRpcRaftServer>();
        _joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
    }

    /// <summary>
    /// 添加节点
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="raftNode">Raft节点</param>
    public async Task AddNodeAsync(string nodeId, RaftNode raftNode)
    {
        lock (_lock)
        {
            if (_rpcClients.ContainsKey(nodeId) || _servers.ContainsKey(nodeId))
            {
                return; // 节点已存在
            }
        }

        // 通过服务发现获取地址
        var address = await _serviceDiscovery.GetNodeAddressAsync(nodeId);
        if (address == null)
        {
            throw new InvalidOperationException($"无法通过服务发现获取节点 {nodeId} 的地址");
        }

        lock (_lock)
        {
            _servers[nodeId] = new StreamJsonRpcRaftServer(raftNode, address.Port, _joinableTaskFactory);
        }
    }

    /// <summary>
    /// 启动所有连接
    /// </summary>
    public async Task StartAsync()
    {
        // 启动所有服务器
        var startTasks = _servers.Values.Select(server => server.StartAsync()).ToArray();
        await Task.WhenAll(startTasks);
    }

    /// <summary>
    /// 请求投票
    /// </summary>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <param name="request">投票请求</param>
    /// <returns>投票响应</returns>
    public async Task<VoteResponse> RequestVoteAsync(string targetNodeId, VoteRequest request)
    {
        if (targetNodeId == _currentNodeId)
        {
            throw new InvalidOperationException("不能向自己发送投票请求");
        }

        var rpcClient = await GetOrCreateRpcClientAsync(targetNodeId);
        try
        {
            var response = await rpcClient.InvokeAsync<VoteResponse>("RequestVoteAsync", request);
            return response ?? new VoteResponse { Term = 0, VoteGranted = false };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RPC调用失败 {targetNodeId}: {ex.Message}");
            // 清理失败的连接
            _rpcClients.TryRemove(targetNodeId, out _);
            return new VoteResponse { Term = 0, VoteGranted = false };
        }
    }

    /// <summary>
    /// 追加日志条目
    /// </summary>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <param name="request">追加请求</param>
    /// <returns>追加响应</returns>
    public async Task<AppendEntriesResponse> AppendEntriesAsync(string targetNodeId, AppendEntriesRequest request)
    {
        if (targetNodeId == _currentNodeId)
        {
            throw new InvalidOperationException("不能向自己发送追加请求");
        }

        var rpcClient = await GetOrCreateRpcClientAsync(targetNodeId);
        try
        {
            var response = await rpcClient.InvokeAsync<AppendEntriesResponse>("AppendEntriesAsync", request);
            return response ?? new AppendEntriesResponse { Term = 0, Success = false };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RPC调用失败 {targetNodeId}: {ex.Message}");
            // 清理失败的连接
            _rpcClients.TryRemove(targetNodeId, out _);
            return new AppendEntriesResponse { Term = 0, Success = false };
        }
    }

    /// <summary>
    /// 获取或创建RPC客户端
    /// </summary>
    /// <param name="targetNodeId">目标节点ID</param>
    /// <returns>RPC客户端</returns>
    private async Task<JsonRpc> GetOrCreateRpcClientAsync(string targetNodeId)
    {
        if (_rpcClients.TryGetValue(targetNodeId, out var existingClient))
        {
            return existingClient;
        }

        // 获取目标节点地址
        var address = await _serviceDiscovery.GetNodeAddressAsync(targetNodeId);
        if (address == null)
        {
            throw new InvalidOperationException($"无法获取节点 {targetNodeId} 的地址");
        }

        // 创建TCP连接
        var client = new TcpClient();
        await client.ConnectAsync(address.Host, address.Port);

        var stream = client.GetStream();
        var rpcClient = new JsonRpc(stream);
        rpcClient.Disconnected += (sender, e) =>
        {
            _rpcClients.TryRemove(targetNodeId, out _);
            client.Dispose();
        };

        // 启动RPC客户端
        rpcClient.StartListening();

        // 缓存客户端
        _rpcClients[targetNodeId] = rpcClient;

        return rpcClient;
    }

    /// <summary>
    /// 停止所有连接
    /// </summary>
    public async Task StopAsync()
    {
        // 停止所有客户端
        var disposeTasks = _rpcClients.Values.Select(client =>
        {
            client.Dispose();
            return Task.CompletedTask;
        }).ToArray();
        await Task.WhenAll(disposeTasks);
        _rpcClients.Clear();

        // 停止所有服务器
        var stopTasks = _servers.Values.Select(server => server.StopAsync()).ToArray();
        await Task.WhenAll(stopTasks);
        _servers.Clear();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _ = StopAsync();
    }
}

/// <summary>
/// StreamJsonRpc Raft 服务器
/// </summary>
public class StreamJsonRpcRaftServer : IDisposable
{
    private readonly RaftNode _raftNode;
    private readonly int _port;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private TcpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
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
    /// 启动服务器
    /// </summary>
    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        _cancellationTokenSource = new CancellationTokenSource();
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
                _ = _joinableTaskFactory.RunAsync(() => HandleClientAsync(client));
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
    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            var rpc = new JsonRpc(stream);
            rpc.AddLocalRpcTarget(new RaftRpcTarget(_raftNode));

            // 启动RPC服务器
            rpc.StartListening();

            // 等待连接断开
            while (client.Connected && !_cancellationTokenSource!.IsCancellationRequested)
            {
                await Task.Delay(1000, _cancellationTokenSource.Token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理客户端连接失败: {ex.Message}");
        }
        finally
        {
            client.Dispose();
        }
    }

    /// <summary>
    /// 停止服务器
    /// </summary>
    public async Task StopAsync()
    {
        if (_cancellationTokenSource != null)
        {
            await _joinableTaskFactory.RunAsync(async () =>
            {
                await _cancellationTokenSource.CancelAsync();
                if (_listeningTask != null)
                {
                    await _listeningTask;
                }
            });
        }

        _listener?.Stop();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _ = StopAsync();
    }
}

/// <summary>
/// Raft RPC 目标对象
/// </summary>
public class RaftRpcTarget
{
    private readonly RaftNode _raftNode;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="raftNode">Raft节点</param>
    public RaftRpcTarget(RaftNode raftNode)
    {
        _raftNode = raftNode;
    }

    /// <summary>
    /// 处理投票请求
    /// </summary>
    /// <param name="request">投票请求</param>
    /// <returns>投票响应</returns>
    public async Task<VoteResponse> RequestVoteAsync(VoteRequest request)
    {
        return await _raftNode.HandleRequestVote(request);
    }

    /// <summary>
    /// 处理追加日志请求
    /// </summary>
    /// <param name="request">追加请求</param>
    /// <returns>追加响应</returns>
    public async Task<AppendEntriesResponse> AppendEntriesAsync(AppendEntriesRequest request)
    {
        return await _raftNode.HandleAppendEntries(request);
    }
}

/// <summary>
/// StreamJsonRpc Raft 集群管理器
/// </summary>
public class StreamJsonRpcRaftCluster : IDisposable
{
    private readonly ConcurrentDictionary<string, RaftNode> _nodes;
    private readonly ConcurrentDictionary<string, StreamJsonRpcRaftRpc> _rpcs;
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly JoinableTaskFactory _joinableTaskFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="nodeIds">节点ID列表</param>
    /// <param name="serviceDiscovery">服务发现实例</param>
    public StreamJsonRpcRaftCluster(List<string> nodeIds, IServiceDiscovery serviceDiscovery)
    {
        _nodes = new ConcurrentDictionary<string, RaftNode>();
        _rpcs = new ConcurrentDictionary<string, StreamJsonRpcRaftRpc>();
        _serviceDiscovery = serviceDiscovery;
        _joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());

        _joinableTaskFactory.RunAsync(() => InitializeClusterAsync(nodeIds)).Join();
    }

    /// <summary>
    /// 初始化集群
    /// </summary>
    /// <param name="nodeIds">节点ID列表</param>
    private async Task InitializeClusterAsync(List<string> nodeIds)
    {
        foreach (var nodeId in nodeIds)
        {
            var config = new RaftConfig(nodeId, nodeIds);
            var rpc = new StreamJsonRpcRaftRpc(nodeId, _serviceDiscovery);
            var node = new RaftNode(config, rpc);

            _nodes[nodeId] = node;
            _rpcs[nodeId] = rpc;

            // 添加所有节点到RPC
            foreach (var otherNodeId in nodeIds)
            {
                await rpc.AddNodeAsync(otherNodeId, node);
            }
        }
    }

    /// <summary>
    /// 启动集群
    /// </summary>
    public async Task StartAsync()
    {
        // 启动所有RPC连接
        var startTasks = _rpcs.Values.Select(rpc => rpc.StartAsync()).ToArray();
        await Task.WhenAll(startTasks);

        // 启动所有节点
        foreach (var node in _nodes.Values)
        {
            node.Start();
        }
    }

    /// <summary>
    /// 停止集群
    /// </summary>
    public async Task StopAsync()
    {
        // 停止所有节点
        foreach (var node in _nodes.Values)
        {
            node.Stop();
        }

        // 停止所有RPC连接
        var stopTasks = _rpcs.Values.Select(rpc => rpc.StopAsync()).ToArray();
        await Task.WhenAll(stopTasks);
    }

    /// <summary>
    /// 获取节点
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <returns>Raft节点</returns>
    public RaftNode? GetNode(string nodeId)
    {
        _nodes.TryGetValue(nodeId, out var node);
        return node;
    }

    /// <summary>
    /// 获取所有节点
    /// </summary>
    /// <returns>节点字典</returns>
    public ConcurrentDictionary<string, RaftNode> GetNodes()
    {
        return _nodes;
    }

    /// <summary>
    /// 获取领导者节点
    /// </summary>
    /// <returns>领导者节点</returns>
    public RaftNode? GetLeader()
    {
        return _nodes.Values.FirstOrDefault(n => n.State == RaftState.Leader);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _ = StopAsync();
    }
}
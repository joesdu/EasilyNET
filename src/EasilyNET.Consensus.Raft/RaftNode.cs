using System.Collections.Concurrent;
using System.Timers;

namespace EasilyNET.Consensus.Raft;

/// <summary>
/// Raft 节点实现
/// </summary>
public class RaftNode : IDisposable
{
    private readonly RaftConfig _config;
    private readonly IRaftRpc _rpc;
    private readonly System.Timers.Timer _electionTimer;
    private readonly System.Timers.Timer _heartbeatTimer;
    private readonly ConcurrentDictionary<string, int> _nextIndex;
    private readonly ConcurrentDictionary<string, int> _matchIndex;

    /// <summary>
    /// 当前任期号
    /// </summary>
    public int CurrentTerm { get; private set; }

    /// <summary>
    /// 当前节点状态
    /// </summary>
    public RaftState State { get; private set; }

    /// <summary>
    /// 为哪个候选者投票
    /// </summary>
    public string? VotedFor { get; private set; }

    /// <summary>
    /// 日志条目列表
    /// </summary>
    public List<LogEntry> Log { get; } = new();

    /// <summary>
    /// 已提交的最高日志条目索引
    /// </summary>
    public long CommitIndex { get; private set; }

    /// <summary>
    /// 最后应用的日志条目索引
    /// </summary>
    public long LastApplied { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">Raft 配置</param>
    /// <param name="rpc">RPC 接口</param>
    public RaftNode(RaftConfig config, IRaftRpc rpc)
    {
        _config = config;
        _rpc = rpc;
        State = RaftState.Follower;
        CurrentTerm = 0;
        CommitIndex = 0;
        LastApplied = 0;

        _nextIndex = new ConcurrentDictionary<string, int>();
        _matchIndex = new ConcurrentDictionary<string, int>();

        // 初始化选举定时器
        _electionTimer = new System.Timers.Timer(GetRandomElectionTimeout());
        _electionTimer.Elapsed += OnElectionTimeout;
        _electionTimer.AutoReset = false;

        // 初始化心跳定时器
        _heartbeatTimer = new System.Timers.Timer(_config.HeartbeatIntervalMs);
        _heartbeatTimer.Elapsed += OnHeartbeatTimeout;
        _heartbeatTimer.AutoReset = true;

        // 添加初始日志条目
        Log.Add(new LogEntry(0, 0, null));
    }

    /// <summary>
    /// 启动节点
    /// </summary>
    public void Start()
    {
        _electionTimer.Start();
    }

    /// <summary>
    /// 停止节点
    /// </summary>
    public void Stop()
    {
        _electionTimer.Stop();
        _heartbeatTimer.Stop();
    }

    /// <summary>
    /// 处理投票请求
    /// </summary>
    /// <param name="request">投票请求</param>
    /// <returns>投票响应</returns>
    public async Task<VoteResponse> HandleRequestVote(VoteRequest request)
    {
        var response = new VoteResponse { Term = CurrentTerm };

        if (request.Term < CurrentTerm)
        {
            response.VoteGranted = false;
            return response;
        }

        if (request.Term > CurrentTerm)
        {
            BecomeFollower(request.Term);
        }

        if ((VotedFor == null || VotedFor == request.CandidateId) &&
            IsLogUpToDate(request.LastLogIndex, request.LastLogTerm))
        {
            VotedFor = request.CandidateId;
            response.VoteGranted = true;
            ResetElectionTimer();
        }
        else
        {
            response.VoteGranted = false;
        }

        return response;
    }

    /// <summary>
    /// 处理追加日志条目请求
    /// </summary>
    /// <param name="request">追加请求</param>
    /// <returns>追加响应</returns>
    public async Task<AppendEntriesResponse> HandleAppendEntries(AppendEntriesRequest request)
    {
        var response = new AppendEntriesResponse { Term = CurrentTerm };

        if (request.Term < CurrentTerm)
        {
            response.Success = false;
            return response;
        }

        if (request.Term > CurrentTerm)
        {
            BecomeFollower(request.Term);
        }

        ResetElectionTimer();

        if (request.PrevLogIndex > 0 &&
            !IsLogConsistent(request.PrevLogIndex, request.PrevLogTerm))
        {
            response.Success = false;
            return response;
        }

        // 追加新的日志条目
        foreach (var entry in request.Entries)
        {
            if (entry.Index > Log.Count - 1)
            {
                Log.Add(entry);
            }
            else if (Log[(int)entry.Index].Term != entry.Term)
            {
                Log.RemoveRange((int)entry.Index, Log.Count - (int)entry.Index);
                Log.Add(entry);
            }
        }

        if (request.LeaderCommit > CommitIndex)
        {
            CommitIndex = Math.Min(request.LeaderCommit, Log.Count - 1);
            ApplyCommittedEntries();
        }

        response.Success = true;
        return response;
    }

    /// <summary>
    /// 成为跟随者
    /// </summary>
    /// <param name="term">新任期</param>
    private void BecomeFollower(int term)
    {
        State = RaftState.Follower;
        CurrentTerm = term;
        VotedFor = null;
        _heartbeatTimer.Stop();
        ResetElectionTimer();
    }

    /// <summary>
    /// 成为候选者
    /// </summary>
    private async Task BecomeCandidateAsync()
    {
        State = RaftState.Candidate;
        CurrentTerm++;
        VotedFor = _config.NodeId;
        ResetElectionTimer();

        // 发送投票请求
        await SendVoteRequests();
    }

    /// <summary>
    /// 发送投票请求
    /// </summary>
    private async Task SendVoteRequests()
    {
        var votes = 1; // 给自己投票
        var majority = (_config.ClusterNodes.Count / 2) + 1;

        var request = new VoteRequest
        {
            Term = CurrentTerm,
            CandidateId = _config.NodeId,
            LastLogIndex = Log.Count - 1,
            LastLogTerm = Log.Count > 0 ? Log.Last().Term : 0
        };

        var tasks = new List<Task<VoteResponse>>();

        foreach (var node in _config.ClusterNodes.Where(n => n != _config.NodeId))
        {
            tasks.Add(_rpc.RequestVoteAsync(node, request));
        }

        try
        {
            var responses = await Task.WhenAll(tasks);

            foreach (var response in responses)
            {
                if (response.Term > CurrentTerm)
                {
                    BecomeFollower(response.Term);
                    return;
                }

                if (response.VoteGranted)
                {
                    votes++;
                    if (votes >= majority && State == RaftState.Candidate)
                    {
                        BecomeLeader();
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 处理异常
            Console.WriteLine($"投票请求异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 成为领导者
    /// </summary>
    private void BecomeLeader()
    {
        State = RaftState.Leader;
        _electionTimer.Stop();
        _heartbeatTimer.Start();

        // 初始化 nextIndex 和 matchIndex
        foreach (var node in _config.ClusterNodes.Where(n => n != _config.NodeId))
        {
            _nextIndex[node] = Log.Count;
            _matchIndex[node] = 0;
        }
    }

    /// <summary>
    /// 重置选举定时器
    /// </summary>
    private void ResetElectionTimer()
    {
        _electionTimer.Stop();
        _electionTimer.Interval = GetRandomElectionTimeout();
        _electionTimer.Start();
    }

    /// <summary>
    /// 获取随机选举超时时间
    /// </summary>
    /// <returns>超时时间（毫秒）</returns>
    private double GetRandomElectionTimeout()
    {
        var random = new Random();
        return _config.ElectionTimeoutMs + random.Next(0, _config.ElectionTimeoutMs);
    }

    /// <summary>
    /// 检查日志是否是最新的（安全性保证）
    /// </summary>
    /// <param name="lastLogIndex">最后日志索引</param>
    /// <param name="lastLogTerm">最后日志任期</param>
    /// <returns>是否最新</returns>
    private bool IsLogUpToDate(long lastLogIndex, int lastLogTerm)
    {
        var lastEntry = Log.Last();
        return lastLogTerm > lastEntry.Term ||
               (lastLogTerm == lastEntry.Term && lastLogIndex >= lastEntry.Index);
    }

    /// <summary>
    /// 验证日志一致性（日志匹配特性）
    /// </summary>
    /// <param name="prevLogIndex">前一个日志索引</param>
    /// <param name="prevLogTerm">前一个日志任期</param>
    /// <returns>是否一致</returns>
    private bool IsLogConsistent(long prevLogIndex, int prevLogTerm)
    {
        if (prevLogIndex == 0)
        {
            return true; // 第一个条目总是匹配
        }

        if (prevLogIndex >= Log.Count)
        {
            return false; // 日志太短
        }

        return Log[(int)prevLogIndex].Term == prevLogTerm;
    }

    /// <summary>
    /// 应用已提交的日志条目到状态机
    /// </summary>
    private void ApplyCommittedEntries()
    {
        while (LastApplied < CommitIndex)
        {
            LastApplied++;
            var entry = Log[(int)LastApplied];

            if (entry.Command != null)
            {
                // 在这里应用命令到状态机
                // 例如：ApplyToStateMachine(entry.Command);
                OnEntryApplied?.Invoke(this, entry);
            }
        }
    }

    /// <summary>
    /// 日志条目应用事件
    /// </summary>
    public event EventHandler<LogEntry>? OnEntryApplied;

    /// <summary>
    /// 选举超时事件处理
    /// </summary>
    private async void OnElectionTimeout(object? sender, ElapsedEventArgs e)
    {
        if (State != RaftState.Leader)
        {
            await BecomeCandidateAsync();
        }
    }

    /// <summary>
    /// 心跳超时事件处理
    /// </summary>
    private async void OnHeartbeatTimeout(object? sender, ElapsedEventArgs e)
    {
        if (State == RaftState.Leader)
        {
            // 发送心跳
            await SendHeartbeat();
        }
    }

    /// <summary>
    /// 追加日志条目（客户端请求）
    /// </summary>
    /// <param name="command">命令数据</param>
    /// <returns>是否成功</returns>
    public async Task<bool> AppendLog(byte[] command)
    {
        if (State != RaftState.Leader)
        {
            return false;
        }

        var entry = new LogEntry(CurrentTerm, Log.Count, command);
        Log.Add(entry);

        // 复制到其他节点
        return await ReplicateLog(entry);
    }

    /// <summary>
    /// 复制日志条目到其他节点
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>是否成功</returns>
    private async Task<bool> ReplicateLog(LogEntry entry)
    {
        var successCount = 1; // 自己
        var majority = (_config.ClusterNodes.Count / 2) + 1;

        var tasks = new List<Task<bool>>();

        foreach (var node in _config.ClusterNodes.Where(n => n != _config.NodeId))
        {
            tasks.Add(ReplicateToNode(node, entry));
        }

        try
        {
            var results = await Task.WhenAll(tasks);
            successCount += results.Count(r => r);

            if (successCount >= majority)
            {
                CommitIndex = entry.Index;
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志复制异常: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 复制日志条目到指定节点
    /// </summary>
    /// <param name="nodeId">节点ID</param>
    /// <param name="entry">日志条目</param>
    /// <returns>是否成功</returns>
    private async Task<bool> ReplicateToNode(string nodeId, LogEntry entry)
    {
        var nextIndex = _nextIndex.GetOrAdd(nodeId, Log.Count);
        var prevLogIndex = nextIndex - 1;
        var prevLogTerm = prevLogIndex >= 0 ? Log[prevLogIndex].Term : 0;

        var request = new AppendEntriesRequest
        {
            Term = CurrentTerm,
            LeaderId = _config.NodeId,
            PrevLogIndex = prevLogIndex,
            PrevLogTerm = prevLogTerm,
            Entries = new List<LogEntry> { entry },
            LeaderCommit = CommitIndex
        };

        try
        {
            var response = await _rpc.AppendEntriesAsync(nodeId, request);

            if (response.Success)
            {
                _nextIndex[nodeId] = (int)entry.Index + 1;
                _matchIndex[nodeId] = (int)entry.Index;
                return true;
            }
            else
            {
                // 减少 nextIndex 并重试
                _nextIndex[nodeId] = Math.Max(1, _nextIndex[nodeId] - 1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"复制到节点 {nodeId} 异常: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 发送心跳
    /// </summary>
    private async Task SendHeartbeat()
    {
        foreach (var node in _config.ClusterNodes.Where(n => n != _config.NodeId))
        {
            var request = new AppendEntriesRequest
            {
                Term = CurrentTerm,
                LeaderId = _config.NodeId,
                PrevLogIndex = Log.Count - 1,
                PrevLogTerm = Log.Count > 0 ? Log.Last().Term : 0,
                Entries = new List<LogEntry>(),
                LeaderCommit = CommitIndex
            };

            try
            {
                await _rpc.AppendEntriesAsync(node, request);
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"发送心跳异常: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _electionTimer.Dispose();
        _heartbeatTimer.Dispose();
    }
}
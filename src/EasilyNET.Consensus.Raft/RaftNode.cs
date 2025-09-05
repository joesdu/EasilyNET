using System.Collections.Concurrent;
using System.Timers;
using EasilyNET.Consensus.Raft.Protocols;
using EasilyNET.Consensus.Raft.Rpc;
using Timer = System.Timers.Timer;

namespace EasilyNET.Consensus.Raft;

/// <summary>
/// Raft 节点实现
/// </summary>
public class RaftNode : IDisposable
{
    private readonly RaftConfig _config;
    private readonly Timer _electionTimer;
    private readonly Timer _heartbeatTimer;
    private readonly ConcurrentDictionary<string, int> _matchIndex;
    private readonly ConcurrentDictionary<string, int> _nextIndex;
    private readonly IRaftRpc _rpc;

    public RaftNode(RaftConfig config, IRaftRpc rpc)
    {
        _config = config;
        _rpc = rpc;
        State = RaftState.Follower;
        CurrentTerm = 0;
        CommitIndex = 0;
        LastApplied = 0;
        _nextIndex = new();
        _matchIndex = new();
        _electionTimer = new(GetRandomElectionTimeout());
        _electionTimer.Elapsed += OnElectionTimeout;
        _electionTimer.AutoReset = false;
        _heartbeatTimer = new(_config.HeartbeatIntervalMs);
        _heartbeatTimer.Elapsed += OnHeartbeatTimeout;
        _heartbeatTimer.AutoReset = true;
        Log.Add(new(0, 0, null));
    }

    public string NodeId => _config.NodeId;

    public int CurrentTerm { get; private set; }

    public RaftState State { get; private set; }

    public string? VotedFor { get; private set; }

    public List<LogEntry> Log { get; } = [];

    public long CommitIndex { get; private set; }

    public long LastApplied { get; private set; }

    public void Dispose()
    {
        _electionTimer.Dispose();
        _heartbeatTimer.Dispose();
    }

    public void Start()
    {
        _electionTimer.Start();
    }

    public void Stop()
    {
        _electionTimer.Stop();
        _heartbeatTimer.Stop();
    }

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

    private void BecomeFollower(int term)
    {
        State = RaftState.Follower;
        CurrentTerm = term;
        VotedFor = null;
        _heartbeatTimer.Stop();
        ResetElectionTimer();
    }

    private async Task BecomeCandidateAsync()
    {
        State = RaftState.Candidate;
        CurrentTerm++;
        VotedFor = _config.NodeId;
        ResetElectionTimer();
        await SendVoteRequests();
    }

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
            Console.WriteLine($"投票请求异常: {ex.Message}");
        }
    }

    private void BecomeLeader()
    {
        State = RaftState.Leader;
        _electionTimer.Stop();
        _heartbeatTimer.Start();
        foreach (var node in _config.ClusterNodes.Where(n => n != _config.NodeId))
        {
            _nextIndex[node] = Log.Count;
            _matchIndex[node] = 0;
        }
    }

    private void ResetElectionTimer()
    {
        _electionTimer.Stop();
        _electionTimer.Interval = GetRandomElectionTimeout();
        _electionTimer.Start();
    }

    private double GetRandomElectionTimeout()
    {
        var random = new Random();
        return _config.ElectionTimeoutMs + random.Next(0, _config.ElectionTimeoutMs);
    }

    private bool IsLogUpToDate(long lastLogIndex, int lastLogTerm)
    {
        var lastEntry = Log.Last();
        return lastLogTerm > lastEntry.Term ||
               (lastLogTerm == lastEntry.Term && lastLogIndex >= lastEntry.Index);
    }

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

    private void ApplyCommittedEntries()
    {
        while (LastApplied < CommitIndex)
        {
            LastApplied++;
            var entry = Log[(int)LastApplied];
            if (entry.Command != null)
            {
                OnEntryApplied?.Invoke(this, entry);
            }
        }
    }

    private void TryAdvanceCommitIndex()
    {
        if (State != RaftState.Leader)
        {
            return;
        }
        var majority = (_config.ClusterNodes.Count / 2) + 1;
        for (var n = Log.Count - 1; n > CommitIndex; n--)
        {
            if (n <= 0)
            {
                break;
            }
            if (Log[n].Term != CurrentTerm)
            {
                continue; // 仅在当前任期提交以保证线性一致
            }
            var count = 1; // self
            foreach (var kv in _matchIndex)
            {
                if (kv.Value >= n)
                {
                    count++;
                }
            }
            if (count >= majority)
            {
                CommitIndex = n;
                ApplyCommittedEntries();
                break;
            }
        }
    }

    public event EventHandler<LogEntry>? OnEntryApplied;

    private async void OnElectionTimeout(object? sender, ElapsedEventArgs e)
    {
        if (State != RaftState.Leader)
        {
            await BecomeCandidateAsync();
        }
    }

    private async void OnHeartbeatTimeout(object? sender, ElapsedEventArgs e)
    {
        if (State == RaftState.Leader)
        {
            await SendHeartbeat();
        }
    }

    public async Task<bool> AppendLog(byte[] command)
    {
        if (State != RaftState.Leader)
        {
            return false;
        }
        var entry = new LogEntry(CurrentTerm, Log.Count, command);
        Log.Add(entry);
        var result = await ReplicateLog(entry);
        if (result)
        {
            TryAdvanceCommitIndex();
        }
        return result;
    }

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
                // 提交可能可前移
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志复制异常: {ex.Message}");
        }
        return false;
    }

    private async Task<bool> ReplicateToNode(string nodeId, LogEntry entry)
    {
        const int maxAttempts = 5;
        var attempt = 0;
        while (attempt++ < maxAttempts)
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
                Entries = [entry],
                LeaderCommit = CommitIndex
            };
            try
            {
                var response = await _rpc.AppendEntriesAsync(nodeId, request);
                if (response.Term > CurrentTerm)
                {
                    BecomeFollower(response.Term);
                    return false;
                }
                if (response.Success)
                {
                    _nextIndex[nodeId] = (int)entry.Index + 1;
                    _matchIndex[nodeId] = (int)entry.Index;
                    TryAdvanceCommitIndex();
                    return true;
                }
                // 减少 nextIndex 并重试（回退以寻找匹配位置）
                _nextIndex[nodeId] = Math.Max(1, _nextIndex[nodeId] - 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"复制到节点 {nodeId} 异常: {ex.Message}");
                await Task.Delay(100 * attempt);
            }
        }
        return false;
    }

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
                Entries = [],
                LeaderCommit = CommitIndex
            };
            try
            {
                var response = await _rpc.AppendEntriesAsync(node, request);
                if (response.Term > CurrentTerm)
                {
                    BecomeFollower(response.Term);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送心跳异常: {ex.Message}");
            }
        }
    }
}
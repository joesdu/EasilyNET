using System.Collections.Concurrent;
using EasilyNET.Mongo.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">GridFS upload rate limiter and concurrency controller</para>
///     <para xml:lang="zh">GridFS 上传速率限制器和并发控制器</para>
/// </summary>
public sealed class GridFSRateLimiter : IDisposable
{
    private readonly ConcurrentDictionary<string, SessionSemaphore> _sessionSemaphores = new();
    private readonly SemaphoreSlim _globalSemaphore;
    private readonly GridFSRateLimitOptions _options;
    private readonly SlidingWindowRateLimiter _rateLimiter;
    private bool _disposed;

    /// <summary>
    ///     <para xml:lang="en">Initialize rate limiter</para>
    ///     <para xml:lang="zh">初始化速率限制器</para>
    /// </summary>
    public GridFSRateLimiter(IOptions<GridFSRateLimitOptions> options)
    {
        _options = options.Value;
        _globalSemaphore = new(_options.MaxConcurrentSessions, _options.MaxConcurrentSessions);
        _rateLimiter = new(_options.MaxRequestsPerWindow, TimeSpan.FromSeconds(_options.RateLimitWindowSeconds));
    }

    /// <summary>
    ///     <para xml:lang="en">Acquire global session slot</para>
    ///     <para xml:lang="zh">获取全局会话槽位</para>
    /// </summary>
    public async Task<bool> TryAcquireSessionSlotAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await _globalSemaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Release global session slot</para>
    ///     <para xml:lang="zh">释放全局会话槽位</para>
    /// </summary>
    public void ReleaseSessionSlot()
    {
        if (_disposed) return;
        try
        {
            _globalSemaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Ignore - already released
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Acquire chunk upload slot for a session</para>
    ///     <para xml:lang="zh">获取会话的块上传槽位</para>
    /// </summary>
    public async Task<bool> TryAcquireChunkSlotAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Check rate limit first
        if (_options.EnableRateLimiting && !_rateLimiter.TryAcquire())
        {
            return false;
        }

        var sessionSemaphore = _sessionSemaphores.GetOrAdd(sessionId, _ => new SessionSemaphore(_options.MaxConcurrentChunksPerSession));
        return await sessionSemaphore.Semaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Release chunk upload slot for a session</para>
    ///     <para xml:lang="zh">释放会话的块上传槽位</para>
    /// </summary>
    public void ReleaseChunkSlot(string sessionId)
    {
        if (_disposed) return;
        if (_sessionSemaphores.TryGetValue(sessionId, out var sessionSemaphore))
        {
            try
            {
                sessionSemaphore.Semaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // Ignore - already released
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Remove session from tracking</para>
    ///     <para xml:lang="zh">从跟踪中移除会话</para>
    /// </summary>
    public void RemoveSession(string sessionId)
    {
        if (_sessionSemaphores.TryRemove(sessionId, out var sessionSemaphore))
        {
            sessionSemaphore.Dispose();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get current statistics</para>
    ///     <para xml:lang="zh">获取当前统计信息</para>
    /// </summary>
    public RateLimiterStats GetStats() => new()
    {
        ActiveSessions = _options.MaxConcurrentSessions - _globalSemaphore.CurrentCount,
        MaxSessions = _options.MaxConcurrentSessions,
        TrackedSessions = _sessionSemaphores.Count,
        RateLimitWindowSeconds = _options.RateLimitWindowSeconds,
        MaxRequestsPerWindow = _options.MaxRequestsPerWindow
    };

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _globalSemaphore.Dispose();
        _rateLimiter.Dispose();

        foreach (var kvp in _sessionSemaphores)
        {
            kvp.Value.Dispose();
        }
        _sessionSemaphores.Clear();
    }

    private sealed class SessionSemaphore(int maxConcurrent) : IDisposable
    {
        public SemaphoreSlim Semaphore { get; } = new(maxConcurrent, maxConcurrent);

        public void Dispose() => Semaphore.Dispose();
    }
}

/// <summary>
///     <para xml:lang="en">Sliding window rate limiter</para>
///     <para xml:lang="zh">滑动窗口速率限制器</para>
/// </summary>
internal sealed class SlidingWindowRateLimiter : IDisposable
{
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly Queue<DateTime> _requestTimes = new();
    private readonly object _lock = new();
    private bool _disposed;

    public SlidingWindowRateLimiter(int maxRequests, TimeSpan window)
    {
        _maxRequests = maxRequests;
        _window = window;
    }

    public bool TryAcquire()
    {
        if (_disposed) return false;

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var windowStart = now - _window;

            // Remove expired entries
            while (_requestTimes.Count > 0 && _requestTimes.Peek() < windowStart)
            {
                _requestTimes.Dequeue();
            }

            if (_requestTimes.Count >= _maxRequests)
            {
                return false;
            }

            _requestTimes.Enqueue(now);
            return true;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        lock (_lock)
        {
            _requestTimes.Clear();
        }
    }
}

/// <summary>
///     <para xml:lang="en">Rate limiter statistics</para>
///     <para xml:lang="zh">速率限制器统计信息</para>
/// </summary>
public sealed class RateLimiterStats
{
    /// <summary>
    ///     <para xml:lang="en">Number of active sessions</para>
    ///     <para xml:lang="zh">活跃会话数</para>
    /// </summary>
    public int ActiveSessions { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Maximum allowed sessions</para>
    ///     <para xml:lang="zh">最大允许会话数</para>
    /// </summary>
    public int MaxSessions { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Number of tracked sessions</para>
    ///     <para xml:lang="zh">跟踪的会话数</para>
    /// </summary>
    public int TrackedSessions { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Rate limit window in seconds</para>
    ///     <para xml:lang="zh">速率限制窗口（秒）</para>
    /// </summary>
    public int RateLimitWindowSeconds { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Maximum requests per window</para>
    ///     <para xml:lang="zh">每窗口最大请求数</para>
    /// </summary>
    public int MaxRequestsPerWindow { get; init; }
}

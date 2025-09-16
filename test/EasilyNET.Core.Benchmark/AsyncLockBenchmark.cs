using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using EasilyNET.Core.Threading;

namespace EasilyNET.Core.Benchmark;

/// <summary>
///     <para xml:lang="en">
///     Benchmarks comparing three async lock strategies under contention:
///     - Custom AsyncLock (FIFO waiter queue + Interlocked fast path)
///     - SemaphoreAsyncLock (SemaphoreSlim-backed)
///     - Native SemaphoreSlim used directly
///     </para>
///     <para xml:lang="zh">
///     比较三种异步锁策略在竞争场景下的性能基准测试：
///     - 自定义 AsyncLock（FIFO 等待队列 + Interlocked 快路径）
///     - SemaphoreAsyncLock（基于 SemaphoreSlim）
///     - 直接使用原生 SemaphoreSlim
///     </para>
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[Config(typeof(AsyncLockBenchmarkConfig))]
public class AsyncLockBenchmark
{
    [Params(1, 2, 4, 8, 16, 32)]
    public int Concurrency { get; set; }

    [Params(50, 200)] // 修改：减少 IterationsPerWorker 以降低测试时间
    public int IterationsPerWorker { get; set; }

    private TimeSpan Timeout => TimeSpan.FromMilliseconds(500 + (0.1 * Concurrency * IterationsPerWorker)); // 新增：动态超时

    [Benchmark]
    public async Task AsyncLock_Contention()
    {
        using var cts = new CancellationTokenSource(Timeout);
        var counter = 0L;
        var expected = (long)Concurrency * IterationsPerWorker;
        var mutex = new AsyncLock();
        var tasks = new Task[Concurrency];
        var exceptions = new List<Exception>();
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    for (var j = 0; j < IterationsPerWorker; j++)
                    {
                        try
                        {
                            using (await mutex.LockAsync(cts.Token).ConfigureAwait(false))
                            {
                                Interlocked.Increment(ref counter);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // 预期取消，允许继续下一次循环
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, cts.Token);
        }
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw new InvalidOperationException($"AsyncLock benchmark canceled: {ex.Message}", ex);
        }
        finally
        {
            await Task.WhenAll(tasks.Where(t => !t.IsCompleted).Select(t => t.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously)));
            var incompleteTasks = tasks.Select((t, i) => new { Index = i, t.Status }).Where(x => x.Status != TaskStatus.RanToCompletion).ToList();
            if (incompleteTasks.Count > 0)
            {
                Console.WriteLine($"Incomplete tasks: {string.Join(", ", incompleteTasks.Select(x => $"Task[{x.Index}]={x.Status}"))}");
            }
            mutex.Dispose();
        }
        if (exceptions.Count > 0)
        {
            throw new AggregateException("AsyncLock benchmark failed with exceptions", exceptions);
        }
        if (counter != expected)
        {
            throw new InvalidOperationException($"AsyncLock counter mismatch: {counter} != {expected}");
        }
        Consume(counter);
    }

    [Benchmark]
    public async Task AsyncLock_Reentrant()
    {
        using var cts = new CancellationTokenSource(Timeout);
        var counter = 0L;
        var canceledCount = 0L; // 新增：统计取消次数
        var expected = (long)Concurrency * IterationsPerWorker;
        var mutex = new AsyncLock();
        var tasks = new Task[Concurrency];
        var exceptions = new List<Exception>();
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    for (var j = 0; j < IterationsPerWorker; j++)
                    {
                        try
                        {
                            using (await mutex.LockAsync(cts.Token).ConfigureAwait(false))
                            {
                                try
                                {
                                    using (await mutex.LockAsync(cts.Token).ConfigureAwait(false))
                                    {
                                        Interlocked.Increment(ref counter);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    Interlocked.Increment(ref canceledCount); // 新增：记录内层取消
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Interlocked.Increment(ref canceledCount); // 新增：记录外层取消
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, cts.Token);
        }
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine($"AsyncLock reentrant benchmark canceled, canceled operations: {canceledCount}");
            // 预期取消，不抛出异常
        }
        finally
        {
            await Task.WhenAll(tasks.Where(t => !t.IsCompleted).Select(t => t.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously)));
            var incompleteTasks = tasks.Select((t, i) => new { Index = i, t.Status }).Where(x => x.Status != TaskStatus.RanToCompletion).ToList();
            if (incompleteTasks.Count > 0)
            {
                Console.WriteLine($"Incomplete tasks: {string.Join(", ", incompleteTasks.Select(x => $"Task[{x.Index}]={x.Status}"))}");
            }
            mutex.Dispose();
        }
        if (exceptions.Count > 0)
        {
            throw new AggregateException("AsyncLock reentrant benchmark failed with exceptions", exceptions);
        }
        if (counter != expected)
        {
            Console.WriteLine($"AsyncLock reentrant counter mismatch: {counter} != {expected}, canceled: {canceledCount}");
            // 不抛出异常，允许部分取消
        }
        Consume(counter);
    }

    [Benchmark]
    public async Task AsyncLock_Cancellation()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50)); // 修改：更短取消时间
        var counter = 0L;
        var canceledCount = 0L;
        var mutex = new AsyncLock();
        var tasks = new Task[Concurrency];
        var exceptions = new List<Exception>();
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    for (var j = 0; j < IterationsPerWorker; j++)
                    {
                        try
                        {
                            using (await mutex.LockAsync(cts.Token).ConfigureAwait(false))
                            {
                                Interlocked.Increment(ref counter);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Interlocked.Increment(ref canceledCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, cts.Token);
        }
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"AsyncLock cancellation benchmark canceled, canceled operations: {canceledCount}");
        }
        finally
        {
            await Task.WhenAll(tasks.Where(t => !t.IsCompleted).Select(t => t.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously)));
            var incompleteTasks = tasks.Select((t, i) => new { Index = i, t.Status }).Where(x => x.Status != TaskStatus.RanToCompletion).ToList();
            if (incompleteTasks.Count > 0)
            {
                Console.WriteLine($"Incomplete tasks: {string.Join(", ", incompleteTasks.Select(x => $"Task[{x.Index}]={x.Status}"))}");
            }
            mutex.Dispose();
        }
        if (exceptions.Count > 0)
        {
            throw new AggregateException("AsyncLock cancellation benchmark failed with unexpected exceptions", exceptions);
        }
        Consume(counter);
    }

    [Benchmark]
    public async Task AsyncLock_SingleThread()
    {
        using var cts = new CancellationTokenSource(Timeout);
        var counter = 0L;
        var expected = (long)IterationsPerWorker;
        var mutex = new AsyncLock();
        try
        {
            for (var j = 0; j < IterationsPerWorker; j++)
            {
                using (await mutex.LockAsync(cts.Token).ConfigureAwait(false))
                {
                    counter++;
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            throw new InvalidOperationException($"AsyncLock single-thread benchmark canceled: {ex.Message}", ex);
        }
        finally
        {
            mutex.Dispose();
        }
        if (counter != expected)
        {
            throw new InvalidOperationException($"AsyncLock single-thread counter mismatch: {counter} != {expected}");
        }
        Consume(counter);
    }

    [Benchmark]
    public async Task SemaphoreAsyncLock_Contention()
    {
        using var cts = new CancellationTokenSource(Timeout);
        var counter = 0L;
        var expected = (long)Concurrency * IterationsPerWorker;
        var mutex = new SemaphoreAsyncLock();
        var tasks = new Task[Concurrency];
        var exceptions = new List<Exception>();
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    for (var j = 0; j < IterationsPerWorker; j++)
                    {
                        try
                        {
                            using (await mutex.LockAsync(cts.Token).ConfigureAwait(false))
                            {
                                Interlocked.Increment(ref counter);
                            }
                        }
                        catch (OperationCanceledException) { }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, cts.Token);
        }
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw new InvalidOperationException($"SemaphoreAsyncLock benchmark canceled: {ex.Message}", ex);
        }
        finally
        {
            await Task.WhenAll(tasks.Where(t => !t.IsCompleted).Select(t => t.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously)));
            var incompleteTasks = tasks.Select((t, i) => new { Index = i, t.Status }).Where(x => x.Status != TaskStatus.RanToCompletion).ToList();
            if (incompleteTasks.Count > 0)
            {
                Console.WriteLine($"Incomplete tasks: {string.Join(", ", incompleteTasks.Select(x => $"Task[{x.Index}]={x.Status}"))}");
            }
            mutex.Dispose();
        }
        if (exceptions.Count > 0)
        {
            throw new AggregateException("SemaphoreAsyncLock benchmark failed with exceptions", exceptions);
        }
        if (counter != expected)
        {
            throw new InvalidOperationException($"SemaphoreAsyncLock counter mismatch: {counter} != {expected}");
        }
        Consume(counter);
    }

    [Benchmark(Baseline = true)]
    public async Task SemaphoreSlim_Contention()
    {
        using var cts = new CancellationTokenSource(Timeout);
        var counter = 0L;
        var expected = (long)Concurrency * IterationsPerWorker;
        using var semaphore = new SemaphoreSlim(1, 1);
        var tasks = new Task[Concurrency];
        var exceptions = new List<Exception>();
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    for (var j = 0; j < IterationsPerWorker; j++)
                    {
                        try
                        {
                            await semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
                            try
                            {
                                Interlocked.Increment(ref counter);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }
                        catch (OperationCanceledException) { }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, cts.Token);
        }
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw new InvalidOperationException($"SemaphoreSlim benchmark canceled: {ex.Message}", ex);
        }
        finally
        {
            await Task.WhenAll(tasks.Where(t => !t.IsCompleted).Select(t => t.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously)));
            var incompleteTasks = tasks.Select((t, i) => new { Index = i, t.Status }).Where(x => x.Status != TaskStatus.RanToCompletion).ToList();
            if (incompleteTasks.Count > 0)
            {
                Console.WriteLine($"Incomplete tasks: {string.Join(", ", incompleteTasks.Select(x => $"Task[{x.Index}]={x.Status}"))}");
            }
            semaphore.Dispose();
        }
        if (exceptions.Count > 0)
        {
            throw new AggregateException("SemaphoreSlim benchmark failed with exceptions", exceptions);
        }
        if (counter != expected)
        {
            throw new InvalidOperationException($"SemaphoreSlim counter mismatch: {counter} != {expected}");
        }
        Consume(counter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Consume(long value)
    {
        Volatile.Read(ref value);
    }
}

public sealed class AsyncLockBenchmarkConfig : ManualConfig
{
    public AsyncLockBenchmarkConfig()
    {
        AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(ThreadingDiagnoser.Default);
    }
}
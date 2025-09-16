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

/*
AsyncLockBenchmark.SemaphoreAsyncLock_Contention: Job-JWQWGO(Toolchain=InProcessEmitToolchain) [Concurrency=32, IterationsPerWorker=200]
Runtime = ; GC =
Mean = 4.253 ms, StdErr = 0.023 ms (0.53%), N = 30, StdDev = 0.124 ms
Min = 4.002 ms, Q1 = 4.149 ms, Median = 4.261 ms, Q3 = 4.339 ms, Max = 4.497 ms
IQR = 0.190 ms, LowerFence = 3.863 ms, UpperFence = 4.625 ms
ConfidenceInterval = [4.171 ms; 4.336 ms] (CI 99.9%), Margin = 0.083 ms (1.95% of Mean)
Skewness = -0.22, Kurtosis = 2.25, MValue = 2
-------------------- Histogram --------------------
[3.950 ms ; 4.066 ms) | @@
[4.066 ms ; 4.171 ms) | @@@@@@@
[4.171 ms ; 4.348 ms) | @@@@@@@@@@@@@@@
[4.348 ms ; 4.455 ms) | @@@@@
[4.455 ms ; 4.549 ms) | @
---------------------------------------------------

// * Summary *

BenchmarkDotNet v0.15.2, Windows 11 (10.0.27943.1)
Unknown processor
.NET SDK 10.0.100-rc.1.25451.107
  [Host] : .NET 10.0.0 (10.0.25.45207), X64 RyuJIT AVX2

Toolchain=InProcessEmitToolchain

| Method                        | Concurrency | IterationsPerWorker | Mean           | Error        | StdDev        | Ratio | RatioSD | Rank | Completed Work Items | Lock Contentions | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------------------------------ |------------ |-------------------- |---------------:|-------------:|--------------:|------:|--------:|-----:|---------------------:|-----------------:|---------:|--------:|----------:|------------:|
| AsyncLock_SingleThread        | 1           | 50                  |       951.1 ns |     11.56 ns |      10.25 ns |  0.37 |    0.01 |    1 |               0.0000 |                - |   0.0219 |       - |     352 B |        0.31 |
| AsyncLock_Cancellation        | 1           | 50                  |     2,124.6 ns |     26.76 ns |      20.89 ns |  0.82 |    0.01 |    2 |               1.0009 |           0.0000 |   0.0763 |       - |    1248 B |        1.11 |
| AsyncLock_Contention          | 1           | 50                  |     2,191.7 ns |     40.77 ns |      36.14 ns |  0.84 |    0.02 |    2 |               1.0005 |           0.0001 |   0.0763 |       - |    1248 B |        1.11 |
| SemaphoreSlim_Contention      | 1           | 50                  |     2,597.9 ns |     26.01 ns |      24.33 ns |  1.00 |    0.01 |    3 |               1.0004 |           0.0000 |   0.0687 |       - |    1128 B |        1.00 |
| SemaphoreAsyncLock_Contention | 1           | 50                  |     2,658.3 ns |     34.72 ns |      30.78 ns |  1.02 |    0.01 |    3 |               1.0003 |           0.0001 |   0.0763 |       - |    1240 B |        1.10 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 1           | 200                 |     3,515.8 ns |     14.06 ns |      12.46 ns |  0.34 |    0.02 |    1 |               0.0000 |                - |   0.0191 |       - |     352 B |        0.30 |
| AsyncLock_Contention          | 1           | 200                 |     5,228.8 ns |     38.36 ns |      34.01 ns |  0.51 |    0.02 |    2 |               1.0002 |           0.0003 |   0.0763 |       - |    1248 B |        1.08 |
| AsyncLock_Cancellation        | 1           | 200                 |     5,309.3 ns |     78.27 ns |     112.25 ns |  0.52 |    0.03 |    2 |               1.0006 |           0.0002 |   0.0763 |       - |    1248 B |        1.08 |
| SemaphoreSlim_Contention      | 1           | 200                 |    10,275.8 ns |    203.71 ns |     495.85 ns |  1.00 |    0.07 |    3 |               1.0003 |           0.0361 |   0.0610 |       - |    1156 B |        1.00 |
| SemaphoreAsyncLock_Contention | 1           | 200                 |    11,637.1 ns |    237.53 ns |     681.52 ns |  1.14 |    0.09 |    4 |               1.0003 |           0.0889 |   0.0763 |       - |    1290 B |        1.12 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 2           | 50                  |       969.2 ns |      7.56 ns |       7.07 ns |  0.02 |    0.00 |    1 |               0.0000 |                - |   0.0210 |       - |     352 B |        0.01 |
| AsyncLock_Cancellation        | 2           | 50                  |    34,827.1 ns |    454.65 ns |     425.28 ns |  0.57 |    0.01 |    2 |              60.1407 |           0.0052 |   0.8545 |       - |   13720 B |        0.42 |
| AsyncLock_Contention          | 2           | 50                  |    35,597.9 ns |    685.19 ns |     841.47 ns |  0.58 |    0.02 |    2 |              62.6229 |           0.0026 |   0.8545 |       - |   14221 B |        0.43 |
| SemaphoreSlim_Contention      | 2           | 50                  |    61,219.7 ns |  1,199.25 ns |   1,231.54 ns |  1.00 |    0.03 |    3 |              83.7426 |           0.0172 |   2.0752 |       - |   32768 B |        1.00 |
| SemaphoreAsyncLock_Contention | 2           | 50                  |    67,540.2 ns |  1,347.77 ns |   2,564.26 ns |  1.10 |    0.05 |    4 |              84.4452 |           0.0076 |   2.6855 |       - |   43032 B |        1.31 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 2           | 200                 |     3,516.9 ns |     31.35 ns |      26.18 ns |  0.01 |    0.00 |    1 |               0.0000 |                - |   0.0191 |       - |     352 B |       0.002 |
| AsyncLock_Cancellation        | 2           | 200                 |   135,116.2 ns |  2,665.20 ns |   3,736.24 ns |  0.55 |    0.02 |    2 |             287.8821 |           0.0103 |   3.6621 |       - |   59436 B |       0.421 |
| AsyncLock_Contention          | 2           | 200                 |   135,171.7 ns |  2,639.99 ns |   4,337.58 ns |  0.55 |    0.02 |    2 |             249.9175 |           0.0115 |   3.1738 |       - |   51817 B |       0.367 |
| SemaphoreSlim_Contention      | 2           | 200                 |   244,998.4 ns |  4,749.81 ns |   4,442.97 ns |  1.00 |    0.02 |    3 |             372.4443 |           0.0281 |   9.0332 |       - |  141327 B |       1.000 |
| SemaphoreAsyncLock_Contention | 2           | 200                 |   260,659.5 ns |  5,210.21 ns |  14,174.75 ns |  1.06 |    0.06 |    3 |             379.9858 |           0.0190 |  11.7188 |       - |  189523 B |       1.341 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 4           | 50                  |       943.3 ns |      7.13 ns |       6.67 ns | 0.007 |    0.00 |    1 |               0.0000 |                - |   0.0210 |       - |     352 B |       0.006 |
| AsyncLock_Contention          | 4           | 50                  |   102,694.6 ns |  2,052.73 ns |   3,648.73 ns | 0.785 |    0.04 |    2 |             182.7277 |           0.0079 |   2.4414 |       - |   38661 B |       0.649 |
| AsyncLock_Cancellation        | 4           | 50                  |   103,482.0 ns |  2,069.63 ns |   2,125.36 ns | 0.791 |    0.03 |    2 |             184.8300 |           0.0077 |   2.4414 |       - |   39089 B |       0.656 |
| SemaphoreSlim_Contention      | 4           | 50                  |   131,063.5 ns |  2,600.16 ns |   4,688.62 ns | 1.001 |    0.05 |    3 |             155.0359 |           0.0483 |   3.6621 |       - |   59577 B |       1.000 |
| SemaphoreAsyncLock_Contention | 4           | 50                  |   148,243.2 ns |  2,933.51 ns |   4,652.86 ns | 1.132 |    0.05 |    4 |             189.9160 |           0.0212 |   6.1035 |       - |   95296 B |       1.600 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 4           | 200                 |     3,488.6 ns |     24.81 ns |      21.99 ns | 0.007 |    0.00 |    1 |               0.0000 |                - |   0.0191 |       - |     352 B |       0.001 |
| AsyncLock_Cancellation        | 4           | 200                 |   393,822.1 ns |  7,821.78 ns |  15,439.44 ns | 0.766 |    0.03 |    2 |             768.5635 |           0.0190 |   9.7656 |       - |  155853 B |       0.527 |
| AsyncLock_Contention          | 4           | 200                 |   396,411.9 ns |  7,845.50 ns |   9,339.51 ns | 0.771 |    0.02 |    2 |             765.7583 |           0.0215 |   9.7656 |       - |  155286 B |       0.525 |
| SemaphoreSlim_Contention      | 4           | 200                 |   514,189.1 ns | 10,067.11 ns |   9,887.25 ns | 1.000 |    0.03 |    3 |             783.3789 |           0.0674 |  18.5547 |       - |  295992 B |       1.000 |
| SemaphoreAsyncLock_Contention | 4           | 200                 |   559,675.9 ns | 11,019.13 ns |  22,509.16 ns | 1.089 |    0.05 |    4 |             782.2539 |           0.0391 |  24.4141 |       - |  389035 B |       1.314 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 8           | 50                  |       995.3 ns |     19.67 ns |      23.42 ns | 0.004 |    0.00 |    1 |               0.0000 |                - |   0.0210 |       - |     352 B |       0.002 |
| AsyncLock_Contention          | 8           | 50                  |   209,379.4 ns |  4,161.19 ns |   8,777.36 ns | 0.755 |    0.04 |    2 |             393.4272 |           0.0112 |   5.1270 |       - |   81793 B |       0.550 |
| AsyncLock_Cancellation        | 8           | 50                  |   213,001.6 ns |  4,234.69 ns |  11,944.02 ns | 0.768 |    0.05 |    2 |             319.0764 |           0.0129 |   4.1504 |       - |   66674 B |       0.448 |
| SemaphoreSlim_Contention      | 8           | 50                  |   277,951.2 ns |  5,540.91 ns |  11,565.94 ns | 1.002 |    0.06 |    3 |             391.3096 |           0.0493 |   9.2773 |       - |  148831 B |       1.000 |
| SemaphoreAsyncLock_Contention | 8           | 50                  |   312,748.0 ns |  6,243.15 ns |  13,439.05 ns | 1.127 |    0.07 |    4 |             388.8447 |           0.0200 |  12.2070 |       - |  193762 B |       1.302 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 8           | 200                 |     3,493.5 ns |     13.96 ns |      11.65 ns | 0.003 |    0.00 |    1 |               0.0000 |                - |   0.0191 |       - |     352 B |       0.001 |
| AsyncLock_Cancellation        | 8           | 200                 |   797,181.2 ns | 15,575.78 ns |  22,338.32 ns | 0.777 |    0.03 |    2 |            1576.9160 |           0.0195 |  19.5313 |       - |  318488 B |       0.536 |
| AsyncLock_Contention          | 8           | 200                 |   813,259.4 ns | 16,104.49 ns |  22,576.24 ns | 0.793 |    0.03 |    2 |            1579.9053 |           0.0371 |  19.5313 |       - |  319105 B |       0.537 |
| SemaphoreSlim_Contention      | 8           | 200                 | 1,026,682.7 ns | 19,740.10 ns |  23,499.19 ns | 1.001 |    0.03 |    3 |            1575.0176 |           0.1035 |  37.1094 |       - |  593945 B |       1.000 |
| SemaphoreAsyncLock_Contention | 8           | 200                 | 1,079,439.0 ns | 21,473.56 ns |  32,140.62 ns | 1.052 |    0.04 |    3 |            1578.0156 |           0.0898 |  48.8281 |       - |  783590 B |       1.319 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 16          | 50                  |       937.5 ns |      4.82 ns |       4.51 ns | 0.002 |    0.00 |    1 |               0.0000 |                - |   0.0219 |       - |     352 B |       0.001 |
| AsyncLock_Contention          | 16          | 50                  |   415,313.8 ns |  7,737.94 ns |  14,533.74 ns | 0.781 |    0.03 |    2 |             797.2441 |           0.0117 |  10.2539 |       - |  164530 B |       0.541 |
| AsyncLock_Cancellation        | 16          | 50                  |   415,330.8 ns |  7,985.87 ns |  10,931.15 ns | 0.781 |    0.03 |    2 |             802.6875 |           0.0146 |  10.2539 |       - |  165638 B |       0.545 |
| SemaphoreSlim_Contention      | 16          | 50                  |   532,236.5 ns | 10,623.37 ns |  13,435.16 ns | 1.001 |    0.04 |    3 |             802.2129 |           0.0459 |  19.5313 |       - |  303917 B |       1.000 |
| SemaphoreAsyncLock_Contention | 16          | 50                  |   543,377.8 ns | 10,287.70 ns |  11,007.72 ns | 1.022 |    0.03 |    3 |             798.1416 |           0.0605 |  25.3906 |  0.9766 |  396412 B |       1.304 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 16          | 200                 |     3,502.5 ns |     25.38 ns |      22.49 ns | 0.002 |    0.00 |    1 |               0.0000 |                - |   0.0191 |       - |     352 B |       0.000 |
| AsyncLock_Cancellation        | 16          | 200                 | 1,611,508.8 ns | 31,878.48 ns |  39,149.64 ns | 0.796 |    0.02 |    2 |            3131.9453 |           0.0234 |  39.0625 |       - |  631443 B |       0.527 |
| AsyncLock_Contention          | 16          | 200                 | 1,621,219.4 ns | 32,336.67 ns |  66,780.83 ns | 0.801 |    0.03 |    2 |            3136.9238 |           0.0391 |  39.0625 |       - |  632459 B |       0.527 |
| SemaphoreSlim_Contention      | 16          | 200                 | 2,024,395.8 ns | 31,963.37 ns |  28,334.69 ns | 1.000 |    0.02 |    3 |            3183.2031 |           0.1484 |  74.2188 |       - | 1199228 B |       1.000 |
| SemaphoreAsyncLock_Contention | 16          | 200                 | 2,213,955.0 ns | 43,963.09 ns |  91,767.35 ns | 1.094 |    0.05 |    4 |            3181.5391 |           0.1641 |  97.6563 |  3.9063 | 1578597 B |       1.316 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 32          | 50                  |       927.6 ns |      5.92 ns |       5.25 ns | 0.001 |    0.00 |    1 |               0.0000 |                - |   0.0219 |       - |     352 B |       0.001 |
| AsyncLock_Contention          | 32          | 50                  |   843,130.8 ns | 16,804.18 ns |  27,135.62 ns | 0.766 |    0.03 |    2 |            1612.6846 |           0.1084 |  20.5078 |  0.9766 |  331589 B |       0.546 |
| AsyncLock_Cancellation        | 32          | 50                  |   845,989.9 ns | 16,367.22 ns |  21,849.77 ns | 0.769 |    0.03 |    2 |            1607.1035 |           0.0791 |  20.5078 |  0.9766 |  330441 B |       0.544 |
| SemaphoreSlim_Contention      | 32          | 50                  | 1,101,875.1 ns | 21,990.64 ns |  36,741.41 ns | 1.001 |    0.05 |    3 |            1605.8535 |           0.1602 |  37.1094 |  1.9531 |  607212 B |       1.000 |
| SemaphoreAsyncLock_Contention | 32          | 50                  | 1,131,921.4 ns | 22,499.71 ns |  28,454.93 ns | 1.028 |    0.04 |    3 |            1600.4453 |           0.1738 |  50.7813 |  3.9063 |  793539 B |       1.307 |
|                               |             |                     |                |              |               |       |         |      |                      |                  |          |         |           |             |
| AsyncLock_SingleThread        | 32          | 200                 |     3,512.0 ns |     49.70 ns |      46.49 ns | 0.001 |    0.00 |    1 |               0.0000 |                - |   0.0191 |       - |     352 B |       0.000 |
| AsyncLock_Cancellation        | 32          | 200                 | 3,199,314.7 ns | 63,880.26 ns | 106,729.58 ns | 0.771 |    0.03 |    2 |            6352.9063 |           0.1055 |  78.1250 |  3.9063 | 1279591 B |       0.531 |
| AsyncLock_Contention          | 32          | 200                 | 3,232,832.2 ns | 62,375.66 ns |  85,380.52 ns | 0.779 |    0.03 |    2 |            6354.5391 |           0.0820 |  78.1250 |  3.9063 | 1279962 B |       0.531 |
| SemaphoreSlim_Contention      | 32          | 200                 | 4,151,598.4 ns | 80,982.26 ns |  86,650.11 ns | 1.000 |    0.03 |    3 |            6402.7109 |           0.3047 | 148.4375 |  7.8125 | 2410922 B |       1.000 |
| SemaphoreAsyncLock_Contention | 32          | 200                 | 4,253,427.5 ns | 82,810.09 ns | 123,946.25 ns | 1.025 |    0.04 |    3 |            6387.7656 |           0.2656 | 195.3125 | 15.6250 | 3168134 B |       1.314 |

// * Warnings *
MultimodalDistribution
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain -> It seems that the distribution can have several modes (mValue = 3.2)

// * Hints *
Outliers
  AsyncLockBenchmark.AsyncLock_SingleThread: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (976.91 ns)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 3 outliers were removed (2.23 us..2.31 us)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 2 outliers were removed (2.52 us, 2.71 us)
  AsyncLockBenchmark.SemaphoreAsyncLock_Contention: Toolchain=InProcessEmitToolchain -> 1 outlier  was  removed (2.78 us)
  AsyncLockBenchmark.AsyncLock_SingleThread: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (3.57 us)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 1 outlier  was  removed (5.33 us)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 5 outliers were removed (5.79 us..6.56 us)
  AsyncLockBenchmark.SemaphoreSlim_Contention: Toolchain=InProcessEmitToolchain      -> 3 outliers were removed, 4 outliers were detected (9.16 us, 11.49 us..12.60 us)
  AsyncLockBenchmark.SemaphoreAsyncLock_Contention: Toolchain=InProcessEmitToolchain -> 5 outliers were removed, 6 outliers were detected (9.97 us, 13.39 us..15.93 us)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 2 outliers were detected (32.55 us, 34.10 us)
  AsyncLockBenchmark.SemaphoreAsyncLock_Contention: Toolchain=InProcessEmitToolchain -> 1 outlier  was  removed, 3 outliers were detected (60.02 us, 62.05 us, 77.71 us)
  AsyncLockBenchmark.AsyncLock_SingleThread: Toolchain=InProcessEmitToolchain        -> 2 outliers were removed (3.60 us, 3.66 us)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 2 outliers were detected (123.10 us, 126.82 us)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 1 outlier  was  detected (123.80 us)
  AsyncLockBenchmark.SemaphoreSlim_Contention: Toolchain=InProcessEmitToolchain      -> 1 outlier  was  removed (259.67 us)
  AsyncLockBenchmark.SemaphoreAsyncLock_Contention: Toolchain=InProcessEmitToolchain -> 3 outliers were detected (207.36 us..224.74 us)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 2 outliers were removed, 3 outliers were detected (93.67 us, 114.16 us, 116.55 us)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 2 outliers were removed (108.49 us, 111.03 us)
  AsyncLockBenchmark.AsyncLock_SingleThread: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (3.66 us)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed, 2 outliers were detected (357.22 us, 447.74 us)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 2 outliers were removed (428.48 us, 429.33 us)
  AsyncLockBenchmark.AsyncLock_SingleThread: Toolchain=InProcessEmitToolchain        -> 2 outliers were removed, 4 outliers were detected (945.76 ns, 953.52 ns, 1.05 us, 1.13 us)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 2 outliers were detected (166.53 us, 182.51 us)
  AsyncLockBenchmark.SemaphoreSlim_Contention: Toolchain=InProcessEmitToolchain      -> 1 outlier  was  removed (318.56 us)
  AsyncLockBenchmark.SemaphoreAsyncLock_Contention: Toolchain=InProcessEmitToolchain -> 1 outlier  was  removed, 2 outliers were detected (274.90 us, 367.51 us)
  AsyncLockBenchmark.AsyncLock_SingleThread: Toolchain=InProcessEmitToolchain        -> 2 outliers were removed, 3 outliers were detected (3.47 us, 3.53 us, 3.54 us)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (895.71 us)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 1 outlier  was  removed (923.97 us)
  AsyncLockBenchmark.SemaphoreAsyncLock_Contention: Toolchain=InProcessEmitToolchain -> 1 outlier  was  removed (1.19 ms)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 3 outliers were removed (463.65 us..465.88 us)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 2 outliers were removed (454.91 us, 455.90 us)
  AsyncLockBenchmark.SemaphoreSlim_Contention: Toolchain=InProcessEmitToolchain      -> 1 outlier  was  removed (573.35 us)
  AsyncLockBenchmark.SemaphoreAsyncLock_Contention: Toolchain=InProcessEmitToolchain -> 1 outlier  was  detected (521.07 us)
  AsyncLockBenchmark.AsyncLock_SingleThread: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (3.63 us)
  AsyncLockBenchmark.SemaphoreSlim_Contention: Toolchain=InProcessEmitToolchain      -> 1 outlier  was  removed (2.19 ms)
  AsyncLockBenchmark.AsyncLock_SingleThread: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (942.99 ns)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 1 outlier  was  removed (933.12 us)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (926.01 us)
  AsyncLockBenchmark.AsyncLock_Cancellation: Toolchain=InProcessEmitToolchain        -> 1 outlier  was  removed (3.52 ms)
  AsyncLockBenchmark.AsyncLock_Contention: Toolchain=InProcessEmitToolchain          -> 1 outlier  was  removed (3.55 ms)
  AsyncLockBenchmark.SemaphoreAsyncLock_Contention: Toolchain=InProcessEmitToolchain -> 1 outlier  was  removed (4.64 ms)

// * Legends *
  Concurrency          : Value of the 'Concurrency' parameter
  IterationsPerWorker  : Value of the 'IterationsPerWorker' parameter
  Mean                 : Arithmetic mean of all measurements
  Error                : Half of 99.9% confidence interval
  StdDev               : Standard deviation of all measurements
  Ratio                : Mean of the ratio distribution ([Current]/[Baseline])
  RatioSD              : Standard deviation of the ratio distribution ([Current]/[Baseline])
  Rank                 : Relative position of current benchmark mean among all benchmarks (Arabic style)
  Completed Work Items : The number of work items that have been processed in ThreadPool (per single operation)
  Lock Contentions     : The number of times there was contention upon trying to take a Monitor's lock (per single operation)
  Gen0                 : GC Generation 0 collects per 1000 operations
  Gen1                 : GC Generation 1 collects per 1000 operations
  Allocated            : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Alloc Ratio          : Allocated memory ratio distribution ([Current]/[Baseline])
  1 ns                 : 1 Nanosecond (0.000000001 sec)

// * Diagnostic Output - MemoryDiagnoser *

// * Diagnostic Output - ThreadingDiagnoser *


// ***** BenchmarkRunner: End *****
Run time: 00:24:30 (1470.52 sec), executed benchmarks: 60

Global total time: 00:24:31 (1471.56 sec), executed benchmarks: 60
// * Artifacts cleanup *
Artifacts cleanup is finished
 */
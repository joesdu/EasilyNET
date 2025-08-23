using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using EasilyNET.Core.Threading;

namespace EasilyNET.Core.Benchmark;

/// <summary>
/// Benchmarks comparing three async lock strategies under contention:
/// - Custom AsyncLock (FIFO waiter queue + Interlocked fast path)
/// - SemaphoreAsyncLock (SemaphoreSlim-backed)
/// - Native SemaphoreSlim used directly
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

    // Per-worker critical-section entries. Tune to keep total runtime reasonable across all jobs.
    [Params(5_000)]
    public int IterationsPerWorker { get; set; }

    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    [Benchmark]
    public async Task AsyncLock_Contention()
    {
        using var cts = new CancellationTokenSource(Timeout);
        var counter = 0;
        var expected = Concurrency * IterationsPerWorker;
        var mutex = new AsyncLock();
        var tasks = new Task[Concurrency];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                for (var j = 0; j < IterationsPerWorker; j++)
                {
                    using (await mutex.LockAsync(cts.Token).ConfigureAwait(false))
                    {
                        counter++;
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
            throw new InvalidOperationException("AsyncLock benchmark canceled (possible deadlock or starvation)");
        }
        finally
        {
            mutex.Dispose();
        }
        if (counter != expected)
        {
            throw new InvalidOperationException($"AsyncLock counter mismatch: {counter} != {expected}");
        }
        Consume(counter);
    }

    [Benchmark]
    public async Task SemaphoreAsyncLock_Contention()
    {
        using var cts = new CancellationTokenSource(Timeout);
        var counter = 0;
        var expected = Concurrency * IterationsPerWorker;
        var mutex = new SemaphoreAsyncLock();
        var tasks = new Task[Concurrency];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                for (var j = 0; j < IterationsPerWorker; j++)
                {
                    using (await mutex.LockAsync(cts.Token).ConfigureAwait(false))
                    {
                        counter++;
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
            throw new InvalidOperationException("SemaphoreAsyncLock benchmark canceled (possible deadlock or starvation)");
        }
        finally
        {
            mutex.Dispose();
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
        var counter = 0;
        var expected = Concurrency * IterationsPerWorker;
        using var semaphore = new SemaphoreSlim(1, 1);
        var tasks = new Task[Concurrency];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                for (var j = 0; j < IterationsPerWorker; j++)
                {
                    await semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
                    try
                    {
                        counter++;
                    }
                    finally
                    {
                        semaphore.Release();
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
            throw new InvalidOperationException("SemaphoreSlim benchmark canceled (possible deadlock or starvation)");
        }
        if (counter != expected)
        {
            throw new InvalidOperationException($"SemaphoreSlim counter mismatch: {counter} != {expected}");
        }
        Consume(counter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Consume(int value)
    {
        // Prevents JIT from optimizing away the counter.
        if (value == int.MinValue)
            Console.WriteLine();
    }
}

public sealed class AsyncLockBenchmarkConfig : ManualConfig
{
    public AsyncLockBenchmarkConfig()
    {
        // Run benchmarks in-process to avoid file copy/locking issues when BDN generates child projects
        AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(ThreadingDiagnoser.Default);
    }
}
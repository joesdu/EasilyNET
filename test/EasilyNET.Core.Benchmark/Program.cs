using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace EasilyNET.Core.Benchmark;

// ReSharper disable ClassNeverInstantiated.Global
/// <summary>
/// </summary>
[Config(typeof(Config))]
public class AsyncSemaphoreBenchmark
{
    // private readonly AsyncSemaphore _semaphore = new(1);
    //
    // private readonly AsyncSemaphoreCas _semaphoreCas = new();
    //
    // [Benchmark]
    // public async Task Asynchronously()
    // {
    //     var task = _semaphore.WaitAsync();
    //     _semaphore.Release();
    //     await task;
    // }
    //
    // [Benchmark]
    // public async Task AsynchronouslyCAS()
    // {
    //     var task = _semaphore.WaitAsync();
    //     _semaphore.Release();
    //     await task;
    // }

    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
}

/// <summary>
/// </summary>
public static class Program
{
    /// <summary>
    /// </summary>
    public static void Main()
    {
        _ = BenchmarkRunner.Run<AsyncSemaphoreBenchmark>();
    }
}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using EasilyNET.Core.Threading;

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

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<AsyncSemaphoreBenchmark>();
    }
}
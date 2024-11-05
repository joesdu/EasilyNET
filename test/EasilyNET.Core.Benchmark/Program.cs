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
public class AsyncStrictNextBenchmark
{
    private readonly Random _random = new();

    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default);
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }

    // [Benchmark]
    // public void TestStrictNext()
    // {
    //     for (int i = 0; i < 5000; i++)
    //     {
    //         _random.StrictNext();
    //     }
    // }
    //
    // [Benchmark]
    // public void TestStrictNext2()
    // {
    //     for (int i = 0; i < 5000; i++)
    //     {
    //         _random.StrictNext2();
    //     }
    // }
}

/// <summary>
/// </summary>
public static class Program
{
    /// <summary>
    /// </summary>
    public static void Main()
    {
        _ = BenchmarkRunner.Run<AsyncStrictNextBenchmark>();
    }
}
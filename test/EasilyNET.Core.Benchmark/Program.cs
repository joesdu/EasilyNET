using BenchmarkDotNet.Running;

namespace EasilyNET.Core.Benchmark;

/// <summary>
/// </summary>
public static class Program
{
    /// <summary>
    /// </summary>
    public static void Main()
    {
        //BenchmarkRunner.Run<UlidBenchmark>();
        //BenchmarkRunner.Run<PooledMemoryStreamBenchmark>();
        BenchmarkRunner.Run<ObjectIdCompatBenchmark>();
        //BenchmarkRunner.Run<AsyncLockBenchmark>();
    }
}
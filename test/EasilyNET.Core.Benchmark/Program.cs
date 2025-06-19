using BenchmarkDotNet.Running;

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

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
        BenchmarkRunner.Run<SnowIdBenchmark>();
    }
}
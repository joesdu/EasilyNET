using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using EasilyNET.Core.Essentials;

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace EasilyNET.Core.Benchmark;

[Config(typeof(UlidBenchmarkConfig))]
public class UlidBenchmark
{
    private static readonly string UlidString = Ulid.NewUlid().ToString();
    private static readonly byte[] UlidBytes = Ulid.NewUlid().ToByteArray();
    private static readonly Ulid UlidInstance = Ulid.NewUlid();

    [Benchmark]
    public static Ulid NewUlid() => Ulid.NewUlid();

    [Benchmark]
    public static string UlidToString() => UlidInstance.ToString();

    [Benchmark]
    public static Ulid ParseFromString() => Ulid.Parse(UlidString);

    [Benchmark]
    public static byte[] ToByteArray() => UlidInstance.ToByteArray();

    [Benchmark]
    public static Ulid ParseFromBytes() => new(UlidBytes);
}

public class UlidBenchmarkConfig : ManualConfig
{
    public UlidBenchmarkConfig()
    {
        AddJob(Job.Default);
        AddDiagnoser(MemoryDiagnoser.Default);
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
        _ = BenchmarkRunner.Run<UlidBenchmark>();
    }
}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using EasilyNET.Core.Essentials;

// ReSharper disable ClassNeverInstantiated.Global

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
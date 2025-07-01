using BenchmarkDotNet.Attributes;
using EasilyNET.Core.Essentials;
using MongoDB.Bson;

namespace EasilyNET.Core.Benchmark;

/// <summary>
/// SnowId vs ObjectId
/// </summary>
[MemoryDiagnoser]
public class SnowIdBenchmark
{
    private static readonly string snowIdString = SnowId.GenerateNewId().ToString();
    private static readonly string objectIdString = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// GenerateNewId
    /// </summary>
    [Benchmark]
    public static SnowId GenerateNewSnowId() => SnowId.GenerateNewId();

    /// <summary>
    /// ObjectId
    /// </summary>
    [Benchmark]
    public static ObjectId GenerateNewObjectId() => ObjectId.GenerateNewId();

    /// <summary>
    /// SnowIdToString
    /// </summary>
    [Benchmark]
    public static string SnowIdToString() => SnowId.GenerateNewId().ToString();

    /// <summary>
    /// ObjectIdToString
    /// </summary>
    [Benchmark]
    public static string ObjectIdToString() => ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// StringToSnowId
    /// </summary>
    [Benchmark]
    public static SnowId StringToSnowId() => SnowId.Parse(snowIdString);

    /// <summary>
    /// StringToObjectId
    /// </summary>
    [Benchmark]
    public static ObjectId StringToObjectId() => ObjectId.Parse(objectIdString);
}

/*
 * // * Summary *
 *
 * BenchmarkDotNet v0.15.2, Windows 11 (10.0.27871.1000)
 * 13th Gen Intel Core i7-13700 2.10GHz, 1 CPU, 24 logical and 16 physical cores
 * .NET SDK 10.0.100-preview.5.25277.114
 *   [Host]     : .NET 10.0.0 (10.0.25.27814), X64 RyuJIT AVX2
 *   DefaultJob : .NET 10.0.0 (10.0.25.27814), X64 RyuJIT AVX2
 *
 *
 * | Method              | Mean     | Error    | StdDev   | Gen0   | Allocated |
 * |-------------------- |---------:|---------:|---------:|-------:|----------:|
 * | GenerateNewSnowId   | 30.63 ns | 0.408 ns | 0.362 ns |      - |         - |
 * | GenerateNewObjectId | 30.78 ns | 0.449 ns | 0.375 ns |      - |         - |
 * | SnowIdToString      | 45.30 ns | 0.955 ns | 0.981 ns | 0.0046 |      72 B |
 * | ObjectIdToString    | 45.23 ns | 0.506 ns | 0.474 ns | 0.0092 |     144 B |
 * | StringToSnowId      | 22.21 ns | 0.069 ns | 0.065 ns |      - |         - |
 * | StringToObjectId    | 21.52 ns | 0.302 ns | 0.267 ns | 0.0025 |      40 B |
 *
 * // * Hints *
 * Outliers
 *   SnowIdBenchmark.GenerateNewSnowId: Default   -> 1 outlier  was  removed (33.83 ns)
 *   SnowIdBenchmark.GenerateNewObjectId: Default -> 2 outliers were removed (34.04 ns, 34.25 ns)
 *   SnowIdBenchmark.StringToObjectId: Default    -> 1 outlier  was  removed (24.26 ns)
 *
 * // * Legends *
 *   Mean      : Arithmetic mean of all measurements
 *   Error     : Half of 99.9% confidence interval
 *   StdDev    : Standard deviation of all measurements
 *   Gen0      : GC Generation 0 collects per 1000 operations
 *   Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
 *   1 ns      : 1 Nanosecond (0.000000001 sec)
 *
 * // * Diagnostic Output - MemoryDiagnoser *
 *
 *
 * // ***** BenchmarkRunner: End *****
 * Run time: 00:01:30 (90.25 sec), executed benchmarks: 6
 *
 * Global total time: 00:01:38 (98.79 sec), executed benchmarks: 6
 */
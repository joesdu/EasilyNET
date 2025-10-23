using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using EasilyNET.Core.Essentials;
using Microsoft.VSDiagnostics;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。

namespace EasilyNET.Core.Benchmark;

[CPUUsageDiagnoser]
[Config(typeof(PooledMemoryStreamBenchmarkConfig))]
public class PooledMemoryStreamBenchmark
{
    private byte[] _data;
    private MemoryStream _ms;
    private PooledMemoryStream _pms;

    [Params(1024, 1024 * 1024)]
    // ReSharper disable once UnassignedField.Global
    public int Size;

    [GlobalSetup]
    public void Setup()
    {
        _data = [.. Enumerable.Range(0, Size).Select(i => (byte)(i % 256))];
        _ms = new();
        _pms = new();
    }

    [Benchmark]
    public void Write_MemoryStream()
    {
        _ms.Position = 0;
        _ms.SetLength(0);
        _ms.Write(_data, 0, _data.Length);
    }

    [Benchmark]
    public void Write_PooledMemoryStream()
    {
        _pms.Position = 0;
        _pms.SetLength(0);
        _pms.Write(_data, 0, _data.Length);
    }

    [Benchmark]
    public byte[] ToArray_MemoryStream()
    {
        _ms.Position = 0;
        _ms.SetLength(_data.Length);
        _ms.Write(_data, 0, _data.Length);
        return _ms.ToArray();
    }

    [Benchmark]
    public byte[] ToArray_PooledMemoryStream()
    {
        _pms.Position = 0;
        _pms.SetLength(_data.Length);
        _pms.Write(_data, 0, _data.Length);
        return [.. _pms];
    }
}

public class PooledMemoryStreamBenchmarkConfig : ManualConfig
{
    public PooledMemoryStreamBenchmarkConfig()
    {
        AddJob(Job.Default);
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using EasilyNET.Core.Essentials;
using MongoDB.Bson;

namespace EasilyNET.Core.Benchmark;

/// <summary>
/// ObjectIdCompat vs MongoDB.Bson.ObjectId - comprehensive benchmark
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ObjectIdCompatBenchmark
{
    private static readonly ObjectIdCompat compatSample = ObjectIdCompat.GenerateNewId();
    private static ObjectId mongoSample = ObjectId.GenerateNewId();

    private static readonly string compatString = compatSample.ToString();
    private static readonly string mongoString = mongoSample.ToString();

    private static readonly byte[] compatBytes = compatSample.ToByteArray();
    private static readonly byte[] mongoBytes = mongoSample.ToByteArray();

    [Params(1, 1000)]
    public int N;

    // Generate --------------------------------------------------------------

    [Benchmark]
    [BenchmarkCategory("Generate")]
    public ObjectIdCompat GenerateNew_ObjectIdCompat()
    {
        ObjectIdCompat id = default;
        for (var i = 0; i < N; i++)
            id = ObjectIdCompat.GenerateNewId();
        return id;
    }

    [Benchmark]
    [BenchmarkCategory("Generate")]
    public ObjectId GenerateNew_ObjectId()
    {
        ObjectId id = default;
        for (var i = 0; i < N; i++)
            id = ObjectId.GenerateNewId();
        return id;
    }

    [Benchmark]
    [BenchmarkCategory("GenerateWithTimestamp")]
    public ObjectIdCompat GenerateWithTimestamp_ObjectIdCompat()
    {
        var now = DateTime.UtcNow;
        ObjectIdCompat id = default;
        for (var i = 0; i < N; i++)
            id = ObjectIdCompat.GenerateNewId(now);
        return id;
    }

    [Benchmark]
    [BenchmarkCategory("GenerateWithTimestamp")]
    public ObjectId GenerateWithTimestamp_ObjectId()
    {
        var now = DateTime.UtcNow;
        ObjectId id = default;
        for (var i = 0; i < N; i++)
            id = ObjectId.GenerateNewId(now);
        return id;
    }

    // ToString --------------------------------------------------------------

    [Benchmark]
    [BenchmarkCategory("ToString")]
    public string ToString_ObjectIdCompat()
    {
        var s = string.Empty;
        for (var i = 0; i < N; i++)
            s = ObjectIdCompat.GenerateNewId().ToString();
        return s;
    }

    [Benchmark]
    [BenchmarkCategory("ToString")]
    public string ToString_ObjectId()
    {
        var s = string.Empty;
        for (var i = 0; i < N; i++)
            s = ObjectId.GenerateNewId().ToString();
        return s;
    }

    // Parse / TryParse ------------------------------------------------------

    [Benchmark]
    [BenchmarkCategory("Parse")]
    public ObjectIdCompat Parse_String_ObjectIdCompat()
    {
        ObjectIdCompat id = default;
        for (var i = 0; i < N; i++)
            id = ObjectIdCompat.Parse(compatString);
        return id;
    }

    [Benchmark]
    [BenchmarkCategory("Parse")]
    public ObjectId Parse_String_ObjectId()
    {
        ObjectId id = default;
        for (var i = 0; i < N; i++)
            id = ObjectId.Parse(mongoString);
        return id;
    }

    [Benchmark]
    [BenchmarkCategory("TryParse")]
    public bool TryParse_Success_ObjectIdCompat()
    {
        var ok = false;
        for (var i = 0; i < N; i++)
            ok = ObjectIdCompat.TryParse(compatString, out _);
        return ok;
    }

    [Benchmark]
    [BenchmarkCategory("TryParse")]
    public bool TryParse_Success_ObjectId()
    {
        var ok = false;
        for (var i = 0; i < N; i++)
            ok = ObjectId.TryParse(mongoString, out _);
        return ok;
    }

    [Benchmark]
    [BenchmarkCategory("TryParse")]
    public bool TryParse_Fail_ObjectIdCompat()
    {
        const string bad = "zzzzzzzzzzzzzzzzzzzzzzzz"; // invalid hex
        var ok = true;
        for (var i = 0; i < N; i++)
            ok &= ObjectIdCompat.TryParse(bad, out _);
        return ok;
    }

    [Benchmark]
    [BenchmarkCategory("TryParse")]
    public bool TryParse_Fail_ObjectId()
    {
        const string bad = "zzzzzzzzzzzzzzzzzzzzzzzz"; // invalid hex
        var ok = true;
        for (var i = 0; i < N; i++)
            ok &= ObjectId.TryParse(bad, out _);
        return ok;
    }

    // Bytes -----------------------------------------------------------------

    [Benchmark]
    [BenchmarkCategory("Bytes")]
    public ObjectIdCompat FromBytes_ObjectIdCompat()
    {
        ObjectIdCompat id = default;
        for (var i = 0; i < N; i++)
            id = new(compatBytes);
        return id;
    }

    [Benchmark]
    [BenchmarkCategory("Bytes")]
    public ObjectId FromBytes_ObjectId()
    {
        ObjectId id = default;
        for (var i = 0; i < N; i++)
            id = new(mongoBytes);
        return id;
    }

    [Benchmark]
    [BenchmarkCategory("Bytes")]
    public byte[] ToBytes_ObjectIdCompat()
    {
        var arr = Array.Empty<byte>();
        for (var i = 0; i < N; i++)
            arr = compatSample.ToByteArray();
        return arr;
    }

    [Benchmark]
    [BenchmarkCategory("Bytes")]
    public byte[] ToBytes_ObjectId()
    {
        var arr = Array.Empty<byte>();
        for (var i = 0; i < N; i++)
            arr = mongoSample.ToByteArray();
        return arr;
    }

    [Benchmark]
    [BenchmarkCategory("Bytes")]
    public void ToBytes_Span_ObjectIdCompat()
    {
        Span<byte> dst = stackalloc byte[12];
        for (var i = 0; i < N; i++)
            compatSample.ToByteArray(dst);
    }

    // Properties ------------------------------------------------------------

    [Benchmark]
    [BenchmarkCategory("Properties")]
    public (int, DateTime) ReadProps_ObjectIdCompat()
    {
        var ts = 0;
        DateTime dt = default;
        for (var i = 0; i < N; i++)
        {
            ts = compatSample.Timestamp;
            dt = compatSample.CreationTime;
        }
        return (ts, dt);
    }

    [Benchmark]
    [BenchmarkCategory("Properties")]
    public (int, DateTime) ReadProps_ObjectId()
    {
        var ts = 0;
        DateTime dt = default;
        for (var i = 0; i < N; i++)
        {
            ts = mongoSample.Timestamp;
            dt = mongoSample.CreationTime;
        }
        return (ts, dt);
    }

    // Equality / Compare ----------------------------------------------------

    [Benchmark]
    [BenchmarkCategory("Equality")]
    public (bool, int) EqualsAndCompare_ObjectIdCompat()
    {
        var a = compatSample;
        var b = ObjectIdCompat.GenerateNewId();
        var eq = false;
        var cmp = 0;
        for (var i = 0; i < N; i++)
        {
            eq = a.Equals(b);
            cmp = a.CompareTo(b);
        }
        return (eq, cmp);
    }

    [Benchmark]
    [BenchmarkCategory("Equality")]
    public (bool, int) EqualsAndCompare_ObjectId()
    {
        var a = mongoSample;
        var b = ObjectId.GenerateNewId();
        var eq = false;
        var cmp = 0;
        for (var i = 0; i < N; i++)
        {
            eq = a.Equals(b);
            cmp = a.CompareTo(b);
        }
        return (eq, cmp);
    }

    [Benchmark]
    [BenchmarkCategory("HashCode")]
    public int GetHashCode_ObjectIdCompat()
    {
        var h = 0;
        var id = compatSample;
        for (var i = 0; i < N; i++)
            h = id.GetHashCode();
        return h;
    }

    [Benchmark]
    [BenchmarkCategory("HashCode")]
    public int GetHashCode_ObjectId()
    {
        var h = 0;
        var id = mongoSample;
        for (var i = 0; i < N; i++)
            h = id.GetHashCode();
        return h;
    }

    // RoundTrip -------------------------------------------------------------

    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public bool RoundTrip_String_ObjectIdCompat()
    {
        var ok = false;
        for (var i = 0; i < N; i++)
        {
            var id = ObjectIdCompat.GenerateNewId();
            var s = id.ToString();
            var back = ObjectIdCompat.Parse(s);
            ok = id.Equals(back);
        }
        return ok;
    }

    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public bool RoundTrip_String_ObjectId()
    {
        var ok = false;
        for (var i = 0; i < N; i++)
        {
            var id = ObjectId.GenerateNewId();
            var s = id.ToString();
            var back = ObjectId.Parse(s);
            ok = id.Equals(back);
        }
        return ok;
    }

    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public bool RoundTrip_Bytes_ObjectIdCompat()
    {
        var ok = false;
        for (var i = 0; i < N; i++)
        {
            var id = ObjectIdCompat.GenerateNewId();
            var bytes = id.ToByteArray();
            var back = new ObjectIdCompat(bytes);
            ok = id.Equals(back);
        }
        return ok;
    }

    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public bool RoundTrip_Bytes_ObjectId()
    {
        var ok = false;
        for (var i = 0; i < N; i++)
        {
            var id = ObjectId.GenerateNewId();
            var bytes = id.ToByteArray();
            var back = new ObjectId(bytes);
            ok = id.Equals(back);
        }
        return ok;
    }
}
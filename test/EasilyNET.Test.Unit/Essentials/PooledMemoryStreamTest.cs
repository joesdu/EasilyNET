using EasilyNET.Core.Essentials;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasilyNET.Test.Unit.Essentials;

[TestClass]
public class PooledMemoryStreamTest
{
    [TestMethod]
    public void WriteAndRead_ShouldBeConsistent()
    {
        var data = Enumerable.Range(0, 1000).Select(i => (byte)(i % 256)).ToArray();
        using var stream = new PooledMemoryStream();
        stream.Write(data, 0, data.Length);
        stream.Position = 0;
        var read = new byte[data.Length];
        var bytesRead = stream.Read(read, 0, read.Length);
        Assert.AreEqual(data.Length, bytesRead);
        CollectionAssert.AreEqual(data, read);
    }

    [TestMethod]
    public void ToArray_ShouldReturnCorrectData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new PooledMemoryStream(data);
        var arr = stream.ToArray();
        CollectionAssert.AreEqual(data, arr);
    }

    [TestMethod]
    public void SeekAndSetLength_ShouldWorkCorrectly()
    {
        using var stream = new PooledMemoryStream();
        stream.SetLength(100);
        Assert.AreEqual(100, stream.Length);
        stream.Seek(10, SeekOrigin.Begin);
        Assert.AreEqual(10, stream.Position);
        stream.Seek(-5, SeekOrigin.Current);
        Assert.AreEqual(5, stream.Position);
        stream.Seek(0, SeekOrigin.End);
        Assert.AreEqual(100, stream.Position);
    }

    [TestMethod]
    public void Dispose_ShouldReleaseBuffer()
    {
        var stream = new PooledMemoryStream();
        stream.Write([1, 2, 3], 0, 3);
        stream.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => stream.Write([1], 0, 1));
    }

    [TestMethod]
    public void Expansion_ShouldNotLoseData()
    {
        var data = Enumerable.Range(0, 10000).Select(i => (byte)(i % 256)).ToArray();
        using var stream = new PooledMemoryStream();
        stream.Write(data, 0, data.Length);
        var arr = stream.ToArray();
        CollectionAssert.AreEqual(data, arr);
    }

    [TestMethod]
    public void SpanWriteAndRead_ShouldWork()
    {
        var data = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();
        using var stream = new PooledMemoryStream();
        stream.Write(data);
        stream.Position = 0;
        var read = new byte[data.Length];
        var bytesRead = stream.Read(read);
        Assert.AreEqual(data.Length, bytesRead);
        CollectionAssert.AreEqual(data, read);
    }
}
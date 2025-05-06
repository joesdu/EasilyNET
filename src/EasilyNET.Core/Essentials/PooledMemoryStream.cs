using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Essentials;

/// <summary>
///     <para xml:lang="en">Pooled memory stream</para>
///     <para xml:lang="zh">池化内存流</para>
/// </summary>
public sealed class PooledMemoryStream : Stream, IEnumerable<byte>
{
    private const float OverExpansionFactor = 2;
    private readonly ArrayPool<byte> _pool;

    private byte[] _data = [];
    private bool _isDisposed;
    private int _length;

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    public PooledMemoryStream() : this(ArrayPool<byte>.Shared) { }

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The buffer</para>
    ///     <para xml:lang="zh">缓冲区</para>
    /// </param>
    public PooledMemoryStream(byte[] buffer) : this(ArrayPool<byte>.Shared, buffer.Length)
    {
        Buffer.BlockCopy(buffer, 0, _data, 0, buffer.Length);
    }

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="arrayPool">
    ///     <para xml:lang="en">The array pool</para>
    ///     <para xml:lang="zh">数组池</para>
    /// </param>
    /// <param name="capacity">
    ///     <para xml:lang="en">The capacity</para>
    ///     <para xml:lang="zh">容量</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the array pool is null</para>
    ///     <para xml:lang="zh">当数组池为空时抛出</para>
    /// </exception>
    public PooledMemoryStream(ArrayPool<byte> arrayPool, int capacity = 0)
    {
        _pool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));
        if (capacity > 0)
        {
            _data = _pool.Rent(capacity);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets whether the stream can be read</para>
    ///     <para xml:lang="zh">是否可读</para>
    /// </summary>
    public override bool CanRead => true;

    /// <summary>
    ///     <para xml:lang="en">Gets whether the stream supports seeking</para>
    ///     <para xml:lang="zh">是否可查找</para>
    /// </summary>
    public override bool CanSeek => true;

    /// <summary>
    ///     <para xml:lang="en">Gets whether the stream can be written to</para>
    ///     <para xml:lang="zh">是否可写</para>
    /// </summary>
    public override bool CanWrite => true;

    /// <summary>
    ///     <para xml:lang="en">Gets the length of the stream</para>
    ///     <para xml:lang="zh">长度</para>
    /// </summary>
    public override long Length => _length;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the position within the stream</para>
    ///     <para xml:lang="zh">位置</para>
    /// </summary>
    public override long Position { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Gets the total capacity of the stream</para>
    ///     <para xml:lang="zh">总量</para>
    /// </summary>
    public long Capacity => _data.Length;

    /// <summary>
    ///     <para xml:lang="en">Returns an enumerator that iterates through the collection</para>
    ///     <para xml:lang="zh">返回一个枚举器，用于遍历集合</para>
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     <para xml:lang="en">Gets an enumerator</para>
    ///     <para xml:lang="zh">获取枚举器</para>
    /// </summary>
    public IEnumerator<byte> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
        {
            yield return _data[i];
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Finalizer (destructor)</para>
    ///     <para xml:lang="zh">终结器（析构函数）</para>
    /// </summary>
    ~PooledMemoryStream()
    {
        Dispose(true);
    }

    /// <summary>
    ///     <para xml:lang="en">Converts to an ArraySegment</para>
    ///     <para xml:lang="zh">转换为 ArraySegment</para>
    /// </summary>
    public ArraySegment<byte> ToArraySegment() => new(_data, 0, (int)Length);

    /// <summary>
    ///     <para xml:lang="en">Flushes the stream</para>
    ///     <para xml:lang="zh">刷新数据</para>
    /// </summary>
    public override void Flush()
    {
        AssertNotDisposed();
    }

    /// <summary>
    ///     <para xml:lang="en">Reads bytes into a buffer</para>
    ///     <para xml:lang="zh">读取到字节数组</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The buffer to read into</para>
    ///     <para xml:lang="zh">要读取的缓冲区</para>
    /// </param>
    /// <param name="offset">
    ///     <para xml:lang="en">The zero-based byte offset in the buffer at which to begin storing the data read from the stream</para>
    ///     <para xml:lang="zh">缓冲区中开始存储从流中读取的数据的零字节偏移量</para>
    /// </param>
    /// <param name="count">
    ///     <para xml:lang="en">The maximum number of bytes to read</para>
    ///     <para xml:lang="zh">要读取的最大字节数</para>
    /// </param>
    public override int Read(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        if (count == 0)
        {
            return 0;
        }
        var available = Math.Min(count, Length - Position);
        Array.Copy(_data, Position, buffer, offset, available);
        Position += available;
        return (int)available;
    }

    /// <summary>
    ///     <para xml:lang="en">Changes the position within the stream</para>
    ///     <para xml:lang="zh">改变游标位置</para>
    /// </summary>
    /// <param name="offset">
    ///     <para xml:lang="en">A byte offset relative to the origin parameter</para>
    ///     <para xml:lang="zh">相对于 origin 参数的字节偏移量</para>
    /// </param>
    /// <param name="origin">
    ///     <para xml:lang="en">A value of type SeekOrigin indicating the reference point used to obtain the new position</para>
    ///     <para xml:lang="zh">SeekOrigin 类型的值，指示用于获取新位置的参考点</para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when the offset is out of range</para>
    ///     <para xml:lang="zh">当偏移量超出范围时抛出</para>
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        AssertNotDisposed();
        switch (origin)
        {
            case SeekOrigin.Current:
                if (Position + offset < 0 || Position + offset > Capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                Position += offset;
                _length = (int)Math.Max(Position, _length);
                return Position;
            case SeekOrigin.Begin:
                if (offset < 0 || offset > Capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                Position = offset;
                _length = (int)Math.Max(Position, _length);
                return Position;
            case SeekOrigin.End:
                if (Length + offset < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                if (Length + offset > Capacity)
                {
                    SetCapacity((int)(Length + offset));
                }
                Position = Length + offset;
                _length = (int)Math.Max(Position, _length);
                return Position;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Sets the length of the stream</para>
    ///     <para xml:lang="zh">设置内容长度</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The desired length of the stream in bytes</para>
    ///     <para xml:lang="zh">流的所需长度（以字节为单位）</para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when the value is negative</para>
    ///     <para xml:lang="zh">当值为负数时抛出</para>
    /// </exception>
    public override void SetLength(long value)
    {
        AssertNotDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        if (value > Capacity)
        {
            SetCapacity((int)value);
        }
        _length = (int)value;
        if (Position > Length)
        {
            Position = Length;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Writes bytes to the stream</para>
    ///     <para xml:lang="zh">写入到字节数组</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The buffer to write from</para>
    ///     <para xml:lang="zh">要写入的缓冲区</para>
    /// </param>
    /// <param name="offset">
    ///     <para xml:lang="en">The zero-based byte offset in the buffer at which to begin writing bytes to the stream</para>
    ///     <para xml:lang="zh">缓冲区中开始将字节写入流的零字节偏移量</para>
    /// </param>
    /// <param name="count">
    ///     <para xml:lang="en">The number of bytes to write to the stream</para>
    ///     <para xml:lang="zh">要写入流的字节数</para>
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        if (count == 0)
        {
            return;
        }
        if (Capacity - Position < count)
        {
            SetCapacity((int)(OverExpansionFactor * (Position + count)));
        }
        Array.Copy(buffer, offset, _data, Position, count);
        Position += count;
        _length = (int)Math.Max(Position, _length);
    }

    /// <summary>
    ///     <para xml:lang="en">Writes the stream to another stream</para>
    ///     <para xml:lang="zh">写入到另一个流</para>
    /// </summary>
    /// <param name="stream">
    ///     <para xml:lang="en">The stream to write to</para>
    ///     <para xml:lang="zh">要写入的流</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the stream is null</para>
    ///     <para xml:lang="zh">当流为空时抛出</para>
    /// </exception>
    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        AssertNotDisposed();
        stream.Write(_data, 0, (int)Length);
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the byte array of the stream</para>
    ///     <para xml:lang="zh">获取流的字节数组</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">The byte array of the stream</para>
    ///     <para xml:lang="zh">流的字节数组</para>
    /// </returns>
    public byte[] GetBuffer()
    {
        AssertNotDisposed();
        if (_data.Length == Length)
        {
            return _data;
        }
        var buffer = new byte[Length];
        Buffer.BlockCopy(_data, 0, buffer, 0, buffer.Length);
        return buffer;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the byte array of the stream</para>
    ///     <para xml:lang="zh">获取流的字节数组</para>
    /// </summary>
    public byte[] ToArray() => GetBuffer();

    /// <summary>
    ///     <para xml:lang="en">Disposes the stream</para>
    ///     <para xml:lang="zh">释放流</para>
    /// </summary>
    /// <param name="disposing">
    ///     <para xml:lang="en">Whether to dispose managed resources</para>
    ///     <para xml:lang="zh">是否释放托管资源</para>
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isDisposed = true;
            Position = 0;
            _length = 0;
            _pool.Return(_data);
            _data = null!;
        }
        base.Dispose(disposing);
    }

    private void SetCapacity(int newCapacity)
    {
        var newData = _pool.Rent(newCapacity);
        Array.Copy(_data, 0, newData, 0, Position);
        _pool.Return(_data);
        _data = newData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, typeof(PooledMemoryStream));

    /// <summary>
    ///     <para xml:lang="en">Gets a span of bytes</para>
    ///     <para xml:lang="zh">获取 Span</para>
    /// </summary>
    public Span<byte> GetSpan() => _data.AsSpan(0, _length);

    /// <summary>
    ///     <para xml:lang="en">Gets a memory of bytes</para>
    ///     <para xml:lang="zh">获取 Memory&lt;<see cref="byte" />&gt;</para>
    /// </summary>
    public Memory<byte> GetMemory() => _data.AsMemory(0, _length);
}
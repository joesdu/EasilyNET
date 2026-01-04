using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Essentials;

/// <summary>
///     <para xml:lang="en">
///     High-performance pooled memory stream that uses ArrayPool to minimize allocations.
///     This class is NOT thread-safe. Callers must provide external synchronization if accessed from multiple threads.
///     This follows the same pattern as MemoryStream and other .NET stream types.
///     </para>
///     <para xml:lang="zh">
///     使用 ArrayPool 最小化内存分配的高性能池化内存流。
///     此类不是线程安全的。如果从多个线程访问，调用者必须提供外部同步。
///     这遵循与 MemoryStream 及其他 .NET 流类型相同的模式。
///     </para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">
///     Thread-safety note: Like MemoryStream, FileStream, and other .NET streams, this class is designed for
///     single-threaded use. The IBufferWriter&lt;byte&gt; pattern (GetSpan → Write → Advance) fundamentally
///     cannot be made thread-safe because Span/Memory references become invalid if the underlying buffer is resized.
///     If you need thread-safe access, wrap the stream operations with external locks.
///     </para>
///     <para xml:lang="zh">
///     线程安全说明：与 MemoryStream、FileStream 及其他 .NET 流一样，此类设计用于单线程使用。
///     IBufferWriter&lt;byte&gt; 模式（GetSpan → Write → Advance）从根本上无法实现线程安全，
///     因为如果底层缓冲区调整大小，Span/Memory 引用将变为无效。
///     如果需要线程安全访问，请使用外部锁包装流操作。
///     </para>
/// </remarks>
public sealed class PooledMemoryStream : Stream, IEnumerable<byte>
{
    private const int DefaultCapacity = 256;
    private const float OverExpansionFactor = 2;
    private readonly ArrayPool<byte> _pool;
    private byte[] _data;
    private bool _isDisposed;
    private int _length;

    private long _position;
    private int _returned; // 0: not returned, 1: returned

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    public PooledMemoryStream() : this(ArrayPool<byte>.Shared, DefaultCapacity) { }

    /// <summary>
    ///     <para xml:lang="en">Constructor with initial data</para>
    ///     <para xml:lang="zh">使用初始数据的构造函数</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The initial buffer data to copy</para>
    ///     <para xml:lang="zh">要复制的初始缓冲区数据</para>
    /// </param>
    public PooledMemoryStream(byte[] buffer) : this(ArrayPool<byte>.Shared, buffer.Length)
    {
        Buffer.BlockCopy(buffer, 0, _data, 0, buffer.Length);
        _length = buffer.Length;
    }

    /// <summary>
    ///     <para xml:lang="en">Constructor with custom array pool and capacity</para>
    ///     <para xml:lang="zh">使用自定义数组池和容量的构造函数</para>
    /// </summary>
    /// <param name="arrayPool">
    ///     <para xml:lang="en">The array pool to use for buffer allocation</para>
    ///     <para xml:lang="zh">用于缓冲区分配的数组池</para>
    /// </param>
    /// <param name="capacity">
    ///     <para xml:lang="en">The initial capacity (minimum buffer size)</para>
    ///     <para xml:lang="zh">初始容量（最小缓冲区大小）</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the array pool is null</para>
    ///     <para xml:lang="zh">当数组池为空时抛出</para>
    /// </exception>
    public PooledMemoryStream(ArrayPool<byte> arrayPool, int capacity = 0)
    {
        _pool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));
        _data = _pool.Rent(capacity > 0 ? capacity : DefaultCapacity);
    }

    /// <summary>
    ///     <para xml:lang="en">Gets whether the stream can be read</para>
    ///     <para xml:lang="zh">是否可读</para>
    /// </summary>
    public override bool CanRead => !_isDisposed;

    /// <summary>
    ///     <para xml:lang="en">Gets whether the stream supports seeking</para>
    ///     <para xml:lang="zh">是否可查找</para>
    /// </summary>
    public override bool CanSeek => !_isDisposed;

    /// <summary>
    ///     <para xml:lang="en">Gets whether the stream can be written to</para>
    ///     <para xml:lang="zh">是否可写</para>
    /// </summary>
    public override bool CanWrite => !_isDisposed;

    /// <summary>
    ///     <para xml:lang="en">Gets the length of the stream (number of bytes written)</para>
    ///     <para xml:lang="zh">获取流的长度（已写入的字节数）</para>
    /// </summary>
    public override long Length => _length;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the position within the stream</para>
    ///     <para xml:lang="zh">获取或设置流中的位置</para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when the value is negative or exceeds int.MaxValue</para>
    ///     <para xml:lang="zh">当值为负数或超过 int.MaxValue 时抛出</para>
    /// </exception>
    public override long Position
    {
        get => _position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);
            _position = value;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the total capacity of the underlying buffer</para>
    ///     <para xml:lang="zh">获取底层缓冲区的总容量</para>
    /// </summary>
    public long Capacity => _data.Length;

    /// <summary>
    ///     <para xml:lang="en">Returns an enumerator that iterates through the collection</para>
    ///     <para xml:lang="zh">返回一个枚举器，用于遍历集合</para>
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     <para xml:lang="en">Gets an enumerator to iterate over bytes in the stream</para>
    ///     <para xml:lang="zh">获取用于遍历流中字节的枚举器</para>
    /// </summary>
    public IEnumerator<byte> GetEnumerator()
    {
        for (var i = 0; i < _length; i++)
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
        Dispose(false);
    }

    /// <summary>
    ///     <para xml:lang="en">Converts to an ArraySegment (exposes internal buffer, use with caution)</para>
    ///     <para xml:lang="zh">转换为 ArraySegment（暴露内部缓冲区，请谨慎使用）</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     Warning: The returned ArraySegment references the internal buffer directly.
    ///     Do not use after Dispose() is called or after any operation that may resize the buffer (Write, SetLength, etc.).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     警告：返回的 ArraySegment 直接引用内部缓冲区。
    ///     请勿在调用 Dispose() 后或任何可能调整缓冲区大小的操作（Write、SetLength 等）后使用。
    ///     </para>
    /// </remarks>
    public ArraySegment<byte> ToArraySegment()
    {
        AssertNotDisposed();
        return new(_data, 0, _length);
    }

    /// <summary>
    ///     <para xml:lang="en">Flushes the stream (no-op for memory stream)</para>
    ///     <para xml:lang="zh">刷新流（对于内存流无操作）</para>
    /// </summary>
    public override void Flush()
    {
        AssertNotDisposed();
    }

    /// <summary>
    ///     <para xml:lang="en">Reads bytes into a buffer</para>
    ///     <para xml:lang="zh">将字节读取到缓冲区</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The buffer to read into</para>
    ///     <para xml:lang="zh">要读入的缓冲区</para>
    /// </param>
    /// <param name="offset">
    ///     <para xml:lang="en">The zero-based byte offset in the buffer at which to begin storing data</para>
    ///     <para xml:lang="zh">缓冲区中开始存储数据的从零开始的字节偏移量</para>
    /// </param>
    /// <param name="count">
    ///     <para xml:lang="en">The maximum number of bytes to read</para>
    ///     <para xml:lang="zh">要读取的最大字节数</para>
    /// </param>
    public override int Read(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        if ((uint)offset > (uint)buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if ((uint)count > (uint)(buffer.Length - offset))
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        if (count == 0)
        {
            return 0;
        }
        var available = Math.Min(count, _length - (int)_position);
        if (available <= 0)
        {
            return 0;
        }
        Buffer.BlockCopy(_data, (int)_position, buffer, offset, available);
        _position += available;
        return available;
    }

    /// <summary>
    ///     <para xml:lang="en">Reads bytes into a span</para>
    ///     <para xml:lang="zh">将字节读取到 Span</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The span to read into</para>
    ///     <para xml:lang="zh">要读入的 Span</para>
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(Span<byte> buffer)
    {
        AssertNotDisposed();
        var available = Math.Min(buffer.Length, _length - (int)_position);
        if (available <= 0)
        {
            return 0;
        }
        _data.AsSpan((int)_position, available).CopyTo(buffer);
        _position += available;
        return available;
    }

    /// <summary>
    ///     <para xml:lang="en">Reads a single byte from the stream</para>
    ///     <para xml:lang="zh">从流中读取一个字节</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">The byte read, or -1 if at end of stream</para>
    ///     <para xml:lang="zh">读取的字节，如果在流末尾则返回 -1</para>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int ReadByte()
    {
        AssertNotDisposed();
        if (_position >= _length)
        {
            return -1;
        }
        var b = _data[(int)_position];
        _position++;
        return b;
    }

    /// <summary>
    ///     <para xml:lang="en">Changes the position within the stream</para>
    ///     <para xml:lang="zh">改变流中的位置</para>
    /// </summary>
    /// <param name="offset">
    ///     <para xml:lang="en">A byte offset relative to the origin parameter</para>
    ///     <para xml:lang="zh">相对于 origin 参数的字节偏移量</para>
    /// </param>
    /// <param name="origin">
    ///     <para xml:lang="en">A value indicating the reference point for the new position</para>
    ///     <para xml:lang="zh">指示新位置参考点的值</para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when the resulting position is out of range</para>
    ///     <para xml:lang="zh">当结果位置超出范围时抛出</para>
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Seek(long offset, SeekOrigin origin)
    {
        AssertNotDisposed();
        var newPos = origin switch
        {
            SeekOrigin.Begin   => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End     => _length + offset,
            _                  => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        if (newPos is < 0 or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        _position = newPos;
        return _position;
    }

    /// <summary>
    ///     <para xml:lang="en">Sets the length of the stream</para>
    ///     <para xml:lang="zh">设置流的长度</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The desired length of the stream in bytes</para>
    ///     <para xml:lang="zh">流的所需长度（以字节为单位）</para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when the value is negative or exceeds int.MaxValue</para>
    ///     <para xml:lang="zh">当值为负数或超过 int.MaxValue 时抛出</para>
    /// </exception>
    public override void SetLength(long value)
    {
        AssertNotDisposed();
        if (value is < 0 or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (value > _data.Length)
        {
            SetCapacity((int)value);
        }
        _length = (int)value;
        if (_position > _length)
        {
            _position = _length;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Writes bytes to the stream</para>
    ///     <para xml:lang="zh">将字节写入流</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The buffer containing data to write</para>
    ///     <para xml:lang="zh">包含要写入数据的缓冲区</para>
    /// </param>
    /// <param name="offset">
    ///     <para xml:lang="en">The zero-based byte offset in buffer from which to begin writing</para>
    ///     <para xml:lang="zh">缓冲区中开始写入的从零开始的字节偏移量</para>
    /// </param>
    /// <param name="count">
    ///     <para xml:lang="en">The number of bytes to write</para>
    ///     <para xml:lang="zh">要写入的字节数</para>
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(byte[] buffer, int offset, int count)
    {
        AssertNotDisposed();
        if ((uint)offset > (uint)buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if ((uint)count > (uint)(buffer.Length - offset))
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        if (count == 0)
        {
            return;
        }
        EnsureCapacity(_position + count);
        Buffer.BlockCopy(buffer, offset, _data, (int)_position, count);
        _position += count;
        if (_position > _length)
        {
            _length = (int)_position;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Writes bytes from a span to the stream</para>
    ///     <para xml:lang="zh">从 Span 写入字节到流</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The span containing data to write</para>
    ///     <para xml:lang="zh">包含要写入数据的 Span</para>
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        AssertNotDisposed();
        EnsureCapacity(_position + buffer.Length);
        buffer.CopyTo(_data.AsSpan((int)_position));
        _position += buffer.Length;
        if (_position > _length)
        {
            _length = (int)_position;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Writes a single byte to the stream</para>
    ///     <para xml:lang="zh">向流写入一个字节</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void WriteByte(byte value)
    {
        AssertNotDisposed();
        EnsureCapacity(_position + 1);
        _data[(int)_position] = value;
        _position++;
        if (_position > _length)
        {
            _length = (int)_position;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously writes the stream contents to another stream</para>
    ///     <para xml:lang="zh">异步将流内容写入另一个流</para>
    /// </summary>
    /// <param name="stream">
    ///     <para xml:lang="en">The destination stream</para>
    ///     <para xml:lang="zh">目标流</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the stream is null</para>
    ///     <para xml:lang="zh">当流为空时抛出</para>
    /// </exception>
    public async Task WriteToAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        AssertNotDisposed();
        var remaining = _length - (int)_position;
        if (remaining <= 0)
        {
            return;
        }
        await stream.WriteAsync(_data.AsMemory((int)_position, remaining)).ConfigureAwait(false);
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously writes the stream contents to another stream with cancellation support</para>
    ///     <para xml:lang="zh">异步将流内容写入另一个流，支持取消</para>
    /// </summary>
    /// <param name="stream">
    ///     <para xml:lang="en">The destination stream</para>
    ///     <para xml:lang="zh">目标流</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消标记</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the stream is null</para>
    ///     <para xml:lang="zh">当流为空时抛出</para>
    /// </exception>
    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        AssertNotDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        var remaining = _length - (int)_position;
        if (remaining <= 0)
        {
            return;
        }
        await stream.WriteAsync(_data.AsMemory((int)_position, remaining), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     <para xml:lang="en">Synchronously writes the stream contents to another stream</para>
    ///     <para xml:lang="zh">同步将流内容写入另一个流</para>
    /// </summary>
    /// <param name="stream">
    ///     <para xml:lang="en">The destination stream</para>
    ///     <para xml:lang="zh">目标流</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the stream is null</para>
    ///     <para xml:lang="zh">当流为空时抛出</para>
    /// </exception>
    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        AssertNotDisposed();
        var remaining = _length - (int)_position;
        if (remaining <= 0)
        {
            return;
        }
        stream.Write(_data, (int)_position, remaining);
    }

    /// <summary>
    ///     <para xml:lang="en">Creates a copy of the stream data as a new byte array</para>
    ///     <para xml:lang="zh">将流数据复制为新的字节数组</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A new byte array containing a copy of the stream data</para>
    ///     <para xml:lang="zh">包含流数据副本的新字节数组</para>
    /// </returns>
    public byte[] GetBuffer()
    {
        AssertNotDisposed();
        var buffer = new byte[_length];
        Buffer.BlockCopy(_data, 0, buffer, 0, _length);
        return buffer;
    }

    /// <summary>
    ///     <para xml:lang="en">Creates a copy of the stream data as a new byte array</para>
    ///     <para xml:lang="zh">将流数据复制为新的字节数组</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A new byte array containing a copy of the stream data</para>
    ///     <para xml:lang="zh">包含流数据副本的新字节数组</para>
    /// </returns>
    public byte[] ToArray() => GetBuffer();

    /// <summary>
    ///     <para xml:lang="en">Asynchronously reads bytes into a buffer</para>
    ///     <para xml:lang="zh">异步将字节读取到缓冲区</para>
    /// </summary>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <summary>
    ///     <para xml:lang="en">Asynchronously reads bytes into a memory buffer</para>
    ///     <para xml:lang="zh">异步将字节读取到内存缓冲区</para>
    /// </summary>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        AssertNotDisposed();
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }
        var available = Math.Min(buffer.Length, _length - (int)_position);
        if (available <= 0)
        {
            return ValueTask.FromResult(0);
        }
        _data.AsMemory((int)_position, available).CopyTo(buffer);
        _position += available;
        return ValueTask.FromResult(available);
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously writes bytes from a buffer</para>
    ///     <para xml:lang="zh">异步从缓冲区写入字节</para>
    /// </summary>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <summary>
    ///     <para xml:lang="en">Asynchronously writes bytes from a memory buffer</para>
    ///     <para xml:lang="zh">异步从内存缓冲区写入字节</para>
    /// </summary>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        AssertNotDisposed();
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }
        if (buffer.Length == 0)
        {
            return ValueTask.CompletedTask;
        }
        EnsureCapacity(_position + buffer.Length);
        buffer.CopyTo(_data.AsMemory((int)_position));
        _position += buffer.Length;
        if (_position > _length)
        {
            _length = (int)_position;
        }
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     <para xml:lang="en">Disposes the stream and returns the buffer to the pool</para>
    ///     <para xml:lang="zh">释放流并将缓冲区返回到池</para>
    /// </summary>
    /// <param name="disposing">
    ///     <para xml:lang="en">Whether to dispose managed resources</para>
    ///     <para xml:lang="zh">是否释放托管资源</para>
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            if (Interlocked.Exchange(ref _returned, 1) == 0)
            {
                _pool.Return(_data);
                _data = [];
            }
        }
        base.Dispose(disposing);
    }

    private void SetCapacity(int newCapacity)
    {
        var newData = _pool.Rent(newCapacity);
        Buffer.BlockCopy(_data, 0, newData, 0, _length);
        if (Interlocked.Exchange(ref _returned, 1) == 0)
        {
            _pool.Return(_data);
        }
        _data = newData;
        _returned = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(long required)
    {
        if (required <= _data.Length)
        {
            return;
        }
        var newCapacity = (int)Math.Max(required, _data.Length * OverExpansionFactor);
        SetCapacity(newCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(PooledMemoryStream));
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets a span view of the current stream data.
    ///     Warning: The returned span references the internal buffer directly. Do not use after any operation
    ///     that may resize the buffer (Write, SetLength, etc.) or after Dispose().
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取当前流数据的 Span 视图。
    ///     警告：返回的 Span 直接引用内部缓冲区。请勿在任何可能调整缓冲区大小的操作
    ///     （Write、SetLength 等）后或 Dispose() 后使用。
    ///     </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan()
    {
        AssertNotDisposed();
        return _data.AsSpan(0, _length);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets a memory view of the current stream data.
    ///     Warning: The returned Memory references the internal buffer directly. Do not use after any operation
    ///     that may resize the buffer (Write, SetLength, etc.) or after Dispose().
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取当前流数据的 Memory 视图。
    ///     警告：返回的 Memory 直接引用内部缓冲区。请勿在任何可能调整缓冲区大小的操作
    ///     （Write、SetLength 等）后或 Dispose() 后使用。
    ///     </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetMemory()
    {
        AssertNotDisposed();
        return _data.AsMemory(0, _length);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets a read-only span view of the current stream data.
    ///     Warning: The returned span references the internal buffer directly. Do not use after any operation
    ///     that may resize the buffer (Write, SetLength, etc.) or after Dispose().
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取当前流数据的只读 Span 视图。
    ///     警告：返回的 Span 直接引用内部缓冲区。请勿在任何可能调整缓冲区大小的操作
    ///     （Write、SetLength 等）后或 Dispose() 后使用。
    ///     </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetReadOnlySpan()
    {
        AssertNotDisposed();
        return _data.AsSpan(0, _length);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets a read-only memory view of the current stream data.
    ///     Warning: The returned Memory references the internal buffer directly. Do not use after any operation
    ///     that may resize the buffer (Write, SetLength, etc.) or after Dispose().
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取当前流数据的只读 Memory 视图。
    ///     警告：返回的 Memory 直接引用内部缓冲区。请勿在任何可能调整缓冲区大小的操作
    ///     （Write、SetLength 等）后或 Dispose() 后使用。
    ///     </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> GetReadOnlyMemory()
    {
        AssertNotDisposed();
        return _data.AsMemory(0, _length);
    }

    /// <summary>
    ///     <para xml:lang="en">Clears and resets the stream for reuse without reallocating the buffer</para>
    ///     <para xml:lang="zh">清空并重置流以便复用，不重新分配缓冲区</para>
    /// </summary>
    public void Clear()
    {
        AssertNotDisposed();
        _position = 0;
        _length = 0;
    }

    /// <summary>
    ///     <para xml:lang="en">Copies the contents of this stream to another stream</para>
    ///     <para xml:lang="zh">将此流的内容复制到另一个流</para>
    /// </summary>
    /// <param name="destination">
    ///     <para xml:lang="en">The destination stream</para>
    ///     <para xml:lang="zh">目标流</para>
    /// </param>
    /// <param name="bufferSize">
    ///     <para xml:lang="en">The buffer size (ignored, direct copy is used for efficiency)</para>
    ///     <para xml:lang="zh">缓冲区大小（已忽略，为提高效率使用直接复制）</para>
    /// </param>
    public override void CopyTo(Stream destination, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(destination);
        AssertNotDisposed();
        var remaining = _length - (int)_position;
        if (remaining <= 0)
        {
            return;
        }
        destination.Write(_data, (int)_position, remaining);
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously copies the contents of this stream to another stream</para>
    ///     <para xml:lang="zh">异步将此流的内容复制到另一个流</para>
    /// </summary>
    /// <param name="destination">
    ///     <para xml:lang="en">The destination stream</para>
    ///     <para xml:lang="zh">目标流</para>
    /// </param>
    /// <param name="bufferSize">
    ///     <para xml:lang="en">The buffer size (ignored, direct copy is used for efficiency)</para>
    ///     <para xml:lang="zh">缓冲区大小（已忽略，为提高效率使用直接复制）</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消标记</para>
    /// </param>
    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        AssertNotDisposed();
        var remaining = _length - (int)_position;
        if (remaining <= 0)
        {
            return;
        }
        await destination.WriteAsync(_data.AsMemory((int)_position, remaining), cancellationToken).ConfigureAwait(false);
    }
}
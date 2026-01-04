using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Essentials;

/// <summary>
///     <para xml:lang="en">
///     Pooled memory stream with optional thread-safety support.
///     When thread-safe mode is enabled, all operations are protected by a lock.
///     When disabled (default), provides maximum performance for single-threaded scenarios.
///     </para>
///     <para xml:lang="zh">
///     支持可选线程安全的池化内存流。
///     启用线程安全模式时，所有操作都受锁保护。
///     禁用时(默认)，为单线程场景提供最佳性能。
///     </para>
/// </summary>
public sealed class PooledMemoryStream : Stream, IEnumerable<byte>
{
    private const int DefaultCapacity = 256;
    private const float OverExpansionFactor = 2;
    private readonly Lock? _lock;
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
    ///     <para xml:lang="en">Constructor with thread-safety option</para>
    ///     <para xml:lang="zh">带线程安全选项的构造函数</para>
    /// </summary>
    /// <param name="threadSafe">
    ///     <para xml:lang="en">Whether to enable thread-safe mode</para>
    ///     <para xml:lang="zh">是否启用线程安全模式</para>
    /// </param>
    public PooledMemoryStream(bool threadSafe) : this(ArrayPool<byte>.Shared, DefaultCapacity, threadSafe) { }

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The buffer</para>
    ///     <para xml:lang="zh">缓冲区</para>
    /// </param>
    /// <param name="threadSafe">
    ///     <para xml:lang="en">Whether to enable thread-safe mode</para>
    ///     <para xml:lang="zh">是否启用线程安全模式</para>
    /// </param>
    public PooledMemoryStream(byte[] buffer, bool threadSafe = false) : this(ArrayPool<byte>.Shared, buffer.Length, threadSafe)
    {
        Buffer.BlockCopy(buffer, 0, _data, 0, buffer.Length);
        _length = buffer.Length;
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
    /// <param name="threadSafe">
    ///     <para xml:lang="en">Whether to enable thread-safe mode (default: false for maximum performance)</para>
    ///     <para xml:lang="zh">是否启用线程安全模式（默认：false 以获得最佳性能）</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the array pool is null</para>
    ///     <para xml:lang="zh">当数组池为空时抛出</para>
    /// </exception>
    public PooledMemoryStream(ArrayPool<byte> arrayPool, int capacity = 0, bool threadSafe = false)
    {
        _pool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));
        _data = _pool.Rent(capacity > 0 ? capacity : DefaultCapacity);
        _lock = threadSafe ? new Lock() : null;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets whether thread-safe mode is enabled</para>
    ///     <para xml:lang="zh">获取是否启用了线程安全模式</para>
    /// </summary>
    public bool IsThreadSafe => _lock is not null;

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
    ///     <para xml:lang="en">Gets the length of the stream</para>
    ///     <para xml:lang="zh">长度</para>
    /// </summary>
    public override long Length
    {
        get
        {
            if (_lock is null)
                return _length;
            using (_lock.EnterScope())
            {
                return _length;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the position within the stream</para>
    ///     <para xml:lang="zh">位置</para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when the value is negative or exceeds int.MaxValue</para>
    ///     <para xml:lang="zh">当值为负数或超过 int.MaxValue 时抛出</para>
    /// </exception>
    public override long Position
    {
        get
        {
            if (_lock is null)
                return _position;
            using (_lock.EnterScope())
            {
                return _position;
            }
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);
            if (_lock is null)
            {
                _position = value;
                return;
            }
            using (_lock.EnterScope())
            {
                _position = value;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the total capacity of the stream</para>
    ///     <para xml:lang="zh">总量</para>
    /// </summary>
    public long Capacity
    {
        get
        {
            if (_lock is null)
                return _data.Length;
            using (_lock.EnterScope())
            {
                return _data.Length;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Returns an enumerator that iterates through the collection</para>
    ///     <para xml:lang="zh">返回一个枚举器，用于遍历集合</para>
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     <para xml:lang="en">Gets an enumerator. In thread-safe mode, iterates over a snapshot copy.</para>
    ///     <para xml:lang="zh">获取枚举器。在线程安全模式下，遍历快照副本。</para>
    /// </summary>
    public IEnumerator<byte> GetEnumerator()
    {
        if (_lock is null)
        {
            for (var i = 0; i < _length; i++)
            {
                yield return _data[i];
            }
        }
        else
        {
            // Take a snapshot for thread-safe enumeration
            byte[] snapshot;
            int len;
            using (_lock.EnterScope())
            {
                len = _length;
                snapshot = new byte[len];
                Buffer.BlockCopy(_data, 0, snapshot, 0, len);
            }
            for (var i = 0; i < len; i++)
            {
                yield return snapshot[i];
            }
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
    ///     <para xml:lang="en">Warning: The returned ArraySegment references the internal buffer. Do not use after Dispose or buffer resize. In thread-safe mode, external synchronization is required.</para>
    ///     <para xml:lang="zh">警告：返回的 ArraySegment 引用内部缓冲区。请勿在 Dispose 或缓冲区扩容后使用。在线程安全模式下需要外部同步。</para>
    /// </remarks>
    public ArraySegment<byte> ToArraySegment()
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            return new(_data, 0, _length);
        }
        using (_lock.EnterScope())
        {
            return new(_data, 0, _length);
        }
    }

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
        if (_lock is null)
        {
            return ReadCore(buffer, offset, count);
        }
        using (_lock.EnterScope())
        {
            return ReadCore(buffer, offset, count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadCore(byte[] buffer, int offset, int count)
    {
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
    ///     <para xml:lang="zh">读取到字节数组</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The span to read into</para>
    ///     <para xml:lang="zh">要读取的缓冲区</para>
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(Span<byte> buffer)
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            return ReadSpanCore(buffer);
        }
        using (_lock.EnterScope())
        {
            return ReadSpanCore(buffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadSpanCore(Span<byte> buffer)
    {
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
    ///     <para xml:lang="zh">读取一个字节</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int ReadByte()
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            return ReadByteCore();
        }
        using (_lock.EnterScope())
        {
            return ReadByteCore();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadByteCore()
    {
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
        if (_lock is null)
        {
            return SeekCore(offset, origin);
        }
        using (_lock.EnterScope())
        {
            return SeekCore(offset, origin);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long SeekCore(long offset, SeekOrigin origin)
    {
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
        if (value is < 0 or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (_lock is null)
        {
            SetLengthCore(value);
            return;
        }
        using (_lock.EnterScope())
        {
            SetLengthCore(value);
        }
    }

    private void SetLengthCore(long value)
    {
        if (value > _data.Length)
        {
            SetCapacityCore((int)value);
        }
        _length = (int)value;
        if (_position > _length)
        {
            _position = _length;
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
        if (_lock is null)
        {
            WriteCore(buffer, offset, count);
            return;
        }
        using (_lock.EnterScope())
        {
            WriteCore(buffer, offset, count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteCore(byte[] buffer, int offset, int count)
    {
        EnsureCapacityCore(_position + count);
        Buffer.BlockCopy(buffer, offset, _data, (int)_position, count);
        _position += count;
        if (_position > _length)
        {
            _length = (int)_position;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Writes bytes to the stream</para>
    ///     <para xml:lang="zh">写入到字节数组</para>
    /// </summary>
    /// <param name="buffer">
    ///     <para xml:lang="en">The span to write from</para>
    ///     <para xml:lang="zh">要写入的缓冲区</para>
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            WriteSpanCore(buffer);
            return;
        }
        using (_lock.EnterScope())
        {
            WriteSpanCore(buffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteSpanCore(ReadOnlySpan<byte> buffer)
    {
        EnsureCapacityCore(_position + buffer.Length);
        buffer.CopyTo(_data.AsSpan((int)_position));
        _position += buffer.Length;
        if (_position > _length)
        {
            _length = (int)_position;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Writes a single byte to the stream</para>
    ///     <para xml:lang="zh">写入一个字节</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void WriteByte(byte value)
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            WriteByteCore(value);
            return;
        }
        using (_lock.EnterScope())
        {
            WriteByteCore(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteByteCore(byte value)
    {
        EnsureCapacityCore(_position + 1);
        _data[(int)_position] = value;
        _position++;
        if (_position > _length)
        {
            _length = (int)_position;
        }
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
    public async Task WriteToAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        AssertNotDisposed();
        // Capture state under lock, then write outside
        ReadOnlyMemory<byte> data;
        if (_lock is null)
        {
            var remaining = _length - (int)_position;
            if (remaining <= 0)
                return;
            data = _data.AsMemory((int)_position, remaining);
        }
        else
        {
            using (_lock.EnterScope())
            {
                var remaining = _length - (int)_position;
                if (remaining <= 0)
                    return;
                data = _data.AsMemory((int)_position, remaining);
            }
        }
        await stream.WriteAsync(data).ConfigureAwait(false);
    }

    /// <summary>
    ///     <para xml:lang="en">Writes the stream to another stream</para>
    ///     <para xml:lang="zh">写入到另一个流</para>
    /// </summary>
    /// <param name="stream">
    ///     <para xml:lang="en">The stream to write to</para>
    ///     <para xml:lang="zh">要写入的流</para>
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
        // Capture state under lock, then write outside
        ReadOnlyMemory<byte> data;
        if (_lock is null)
        {
            var remaining = _length - (int)_position;
            if (remaining <= 0)
                return;
            data = _data.AsMemory((int)_position, remaining);
        }
        else
        {
            using (_lock.EnterScope())
            {
                var remaining = _length - (int)_position;
                if (remaining <= 0)
                    return;
                data = _data.AsMemory((int)_position, remaining);
            }
        }
        await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
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
        if (_lock is null)
        {
            var remaining = _length - (int)_position;
            if (remaining <= 0)
                return;
            stream.Write(_data, (int)_position, remaining);
            return;
        }
        // Capture data under lock
        byte[] dataCopy;
        int start, len;
        using (_lock.EnterScope())
        {
            var remaining = _length - (int)_position;
            if (remaining <= 0)
                return;
            start = (int)_position;
            len = remaining;
            dataCopy = _data;
        }
        stream.Write(dataCopy, start, len);
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
        if (_lock is null)
        {
            var buffer = new byte[_length];
            Buffer.BlockCopy(_data, 0, buffer, 0, _length);
            return buffer;
        }
        using (_lock.EnterScope())
        {
            var buffer = new byte[_length];
            Buffer.BlockCopy(_data, 0, buffer, 0, _length);
            return buffer;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the byte array of the stream</para>
    ///     <para xml:lang="zh">获取流的字节数组</para>
    /// </summary>
    public byte[] ToArray() => GetBuffer();

    /// <summary>
    ///     <para xml:lang="en">Asynchronous read into a byte array</para>
    ///     <para xml:lang="zh">异步读取到字节数组</para>
    /// </summary>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        // Delegate to Memory-based overload for optimal behavior
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <summary>
    ///     <para xml:lang="en">Asynchronous read into a memory buffer</para>
    ///     <para xml:lang="zh">异步读取到内存缓冲区</para>
    /// </summary>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        AssertNotDisposed();
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }
        if (_lock is null)
        {
            return ValueTask.FromResult(ReadMemoryCore(buffer));
        }
        using (_lock.EnterScope())
        {
            return ValueTask.FromResult(ReadMemoryCore(buffer));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadMemoryCore(Memory<byte> buffer)
    {
        var available = Math.Min(buffer.Length, _length - (int)_position);
        if (available <= 0)
        {
            return 0;
        }
        _data.AsMemory((int)_position, available).CopyTo(buffer);
        _position += available;
        return available;
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronous write from a byte array</para>
    ///     <para xml:lang="zh">从字节数组异步写入</para>
    /// </summary>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <summary>
    ///     <para xml:lang="en">Asynchronous write from a memory buffer</para>
    ///     <para xml:lang="zh">从内存缓冲区异步写入</para>
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
        if (_lock is null)
        {
            WriteMemoryCore(buffer);
            return ValueTask.CompletedTask;
        }
        using (_lock.EnterScope())
        {
            WriteMemoryCore(buffer);
        }
        return ValueTask.CompletedTask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteMemoryCore(ReadOnlyMemory<byte> buffer)
    {
        EnsureCapacityCore(_position + buffer.Length);
        buffer.CopyTo(_data.AsMemory((int)_position));
        _position += buffer.Length;
        if (_position > _length)
        {
            _length = (int)_position;
        }
    }

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

    private void SetCapacityCore(int newCapacity)
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
    private void EnsureCapacityCore(long required)
    {
        if (required <= _data.Length)
        {
            return;
        }
        var newCapacity = (int)Math.Max(required, _data.Length * OverExpansionFactor);
        SetCapacityCore(newCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(PooledMemoryStream));
    }

    /// <summary>
    ///     <para xml:lang="en">Gets a span of bytes. Warning: In thread-safe mode, external synchronization is required when using the returned span.</para>
    ///     <para xml:lang="zh">获取 Span。警告：在线程安全模式下，使用返回的 Span 时需要外部同步。</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan()
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            return _data.AsSpan(0, _length);
        }
        using (_lock.EnterScope())
        {
            return _data.AsSpan(0, _length);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets a memory of bytes. Warning: In thread-safe mode, external synchronization is required when using the returned Memory.</para>
    ///     <para xml:lang="zh">获取 Memory&lt;<see cref="byte" />&gt;。警告：在线程安全模式下，使用返回的 Memory 时需要外部同步。</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetMemory()
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            return _data.AsMemory(0, _length);
        }
        using (_lock.EnterScope())
        {
            return _data.AsMemory(0, _length);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets a read-only span of bytes. Warning: In thread-safe mode, external synchronization is required when using the returned span.</para>
    ///     <para xml:lang="zh">获取只读 Span。警告：在线程安全模式下，使用返回的 Span 时需要外部同步。</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetReadOnlySpan()
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            return _data.AsSpan(0, _length);
        }
        using (_lock.EnterScope())
        {
            return _data.AsSpan(0, _length);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets a read-only memory of bytes. Warning: In thread-safe mode, external synchronization is required when using the returned Memory.</para>
    ///     <para xml:lang="zh">获取只读 Memory。警告：在线程安全模式下，使用返回的 Memory 时需要外部同步。</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> GetReadOnlyMemory()
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            return _data.AsMemory(0, _length);
        }
        using (_lock.EnterScope())
        {
            return _data.AsMemory(0, _length);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Clears and resets the stream for reuse</para>
    ///     <para xml:lang="zh">清空并重置流以便复用</para>
    /// </summary>
    public void Clear()
    {
        AssertNotDisposed();
        if (_lock is null)
        {
            _position = 0;
            _length = 0;
            return;
        }
        using (_lock.EnterScope())
        {
            _position = 0;
            _length = 0;
        }
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
    ///     <para xml:lang="en">The buffer size (ignored, direct copy is used)</para>
    ///     <para xml:lang="zh">缓冲区大小（已忽略，使用直接复制）</para>
    /// </param>
    public override void CopyTo(Stream destination, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(destination);
        AssertNotDisposed();
        if (_lock is null)
        {
            var remaining = _length - (int)_position;
            if (remaining <= 0)
                return;
            destination.Write(_data, (int)_position, remaining);
            return;
        }
        // Capture data under lock
        byte[] dataCopy;
        int start, len;
        using (_lock.EnterScope())
        {
            var remaining = _length - (int)_position;
            if (remaining <= 0)
                return;
            start = (int)_position;
            len = remaining;
            dataCopy = _data;
        }
        destination.Write(dataCopy, start, len);
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
    ///     <para xml:lang="en">The buffer size (ignored, direct copy is used)</para>
    ///     <para xml:lang="zh">缓冲区大小（已忽略，使用直接复制）</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消标记</para>
    /// </param>
    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        AssertNotDisposed();
        // Capture state under lock, then write outside
        ReadOnlyMemory<byte> data;
        if (_lock is null)
        {
            var remaining = _length - (int)_position;
            if (remaining <= 0)
                return;
            data = _data.AsMemory((int)_position, remaining);
        }
        else
        {
            using (_lock.EnterScope())
            {
                var remaining = _length - (int)_position;
                if (remaining <= 0)
                    return;
                data = _data.AsMemory((int)_position, remaining);
            }
        }
        await destination.WriteAsync(data, cancellationToken).ConfigureAwait(false);
    }
}
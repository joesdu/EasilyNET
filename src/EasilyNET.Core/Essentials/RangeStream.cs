namespace EasilyNET.Core.Essentials;

/// <summary>
///     <para xml:lang="en">Range stream that limits read length</para>
///     <para xml:lang="zh">限制读取长度的范围流</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">
///     The current position of the base stream is treated as the start of the range.
///     Avoid changing the base stream position outside this wrapper.
///     </para>
///     <para xml:lang="zh">
///     基础流的当前位置会被视为范围起点。
///     避免在外部修改基础流的位置。
///     </para>
/// </remarks>
// ReSharper disable once UnusedType.Global
public sealed class RangeStream : Stream
{
    private readonly long _baseStartPosition;
    private readonly Stream _baseStream;
    private readonly bool _leaveOpen;
    private bool _disposed;
    private long _position;

    /// <summary>
    ///     <para xml:lang="en">Create a range stream</para>
    ///     <para xml:lang="zh">创建范围流</para>
    /// </summary>
    /// <param name="baseStream">
    ///     <para xml:lang="en">The underlying stream</para>
    ///     <para xml:lang="zh">基础流</para>
    /// </param>
    /// <param name="maxLength">
    ///     <para xml:lang="en">Maximum readable length from the current position</para>
    ///     <para xml:lang="zh">从当前位置开始可读取的最大长度</para>
    /// </param>
    /// <param name="leaveOpen">
    ///     <para xml:lang="en">Whether to leave the base stream open when disposing</para>
    ///     <para xml:lang="zh">释放时是否保留基础流</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when base stream is null</para>
    ///     <para xml:lang="zh">当基础流为空时抛出</para>
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <para xml:lang="en">Thrown when maxLength is negative</para>
    ///     <para xml:lang="zh">当 maxLength 为负数时抛出</para>
    /// </exception>
    public RangeStream(Stream baseStream, long maxLength, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(baseStream);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
        _baseStream = baseStream;
        Length = maxLength;
        _leaveOpen = leaveOpen;
        if (baseStream.CanSeek)
        {
            _baseStartPosition = baseStream.Position;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets whether the stream can be read</para>
    ///     <para xml:lang="zh">是否可读</para>
    /// </summary>
    public override bool CanRead => !_disposed && _baseStream.CanRead;

    /// <summary>
    ///     <para xml:lang="en">Gets whether the stream supports seeking</para>
    ///     <para xml:lang="zh">是否可查找</para>
    /// </summary>
    public override bool CanSeek => !_disposed && _baseStream.CanSeek;

    /// <summary>
    ///     <para xml:lang="en">Gets whether the stream can be written to</para>
    ///     <para xml:lang="zh">是否可写</para>
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    ///     <para xml:lang="en">Gets the maximum readable length</para>
    ///     <para xml:lang="zh">获取最大可读长度</para>
    /// </summary>
    public override long Length { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the position within the range</para>
    ///     <para xml:lang="zh">获取或设置范围内的位置</para>
    /// </summary>
    public override long Position
    {
        get
        {
            EnsureNotDisposed();
            return !CanSeek ? throw new NotSupportedException("Stream does not support seeking") : _position;
        }
        set
        {
            EnsureNotDisposed();
            if (!CanSeek)
            {
                throw new NotSupportedException("Stream does not support seeking");
            }
            if (value < 0 || value > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Position must be within the range of the stream");
            }
            _baseStream.Position = _baseStartPosition + value;
            _position = value;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Reads bytes into a buffer</para>
    ///     <para xml:lang="zh">将字节读取到缓冲区</para>
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        EnsureNotDisposed();
        if (count == 0)
        {
            return 0;
        }
        var remainingBytes = Length - _position;
        if (remainingBytes <= 0)
        {
            return 0;
        }
        var bytesToRead = (int)Math.Min(count, remainingBytes);
        var bytesRead = _baseStream.Read(buffer, offset, bytesToRead);
        _position += bytesRead;
        return bytesRead;
    }

    /// <summary>
    ///     <para xml:lang="en">Reads bytes into a span</para>
    ///     <para xml:lang="zh">将字节读取到 Span</para>
    /// </summary>
    public override int Read(Span<byte> buffer)
    {
        EnsureNotDisposed();
        if (buffer.Length == 0)
        {
            return 0;
        }
        var remainingBytes = Length - _position;
        if (remainingBytes <= 0)
        {
            return 0;
        }
        var bytesToRead = (int)Math.Min(buffer.Length, remainingBytes);
        var bytesRead = _baseStream.Read(buffer[..bytesToRead]);
        _position += bytesRead;
        return bytesRead;
    }

    /// <summary>
    ///     <para xml:lang="en">Reads a single byte from the stream</para>
    ///     <para xml:lang="zh">从流中读取一个字节</para>
    /// </summary>
    public override int ReadByte()
    {
        EnsureNotDisposed();
        if (Length - _position <= 0)
        {
            return -1;
        }
        var value = _baseStream.ReadByte();
        if (value >= 0)
        {
            _position++;
        }
        return value;
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously reads bytes into a buffer</para>
    ///     <para xml:lang="zh">异步将字节读取到缓冲区</para>
    /// </summary>
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        if (count == 0)
        {
            return 0;
        }
        var remainingBytes = Length - _position;
        if (remainingBytes <= 0)
        {
            return 0;
        }
        var bytesToRead = (int)Math.Min(count, remainingBytes);
        var bytesRead = await _baseStream.ReadAsync(buffer.AsMemory(offset, bytesToRead), cancellationToken).ConfigureAwait(false);
        _position += bytesRead;
        return bytesRead;
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously reads bytes into a memory buffer</para>
    ///     <para xml:lang="zh">异步将字节读取到内存缓冲区</para>
    /// </summary>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        if (buffer.Length == 0)
        {
            return 0;
        }
        var remainingBytes = Length - _position;
        if (remainingBytes <= 0)
        {
            return 0;
        }
        var bytesToRead = (int)Math.Min(buffer.Length, remainingBytes);
        var bytesRead = await _baseStream.ReadAsync(buffer[..bytesToRead], cancellationToken).ConfigureAwait(false);
        _position += bytesRead;
        return bytesRead;
    }

    /// <summary>
    ///     <para xml:lang="en">Flushes the stream</para>
    ///     <para xml:lang="zh">刷新流</para>
    /// </summary>
    public override void Flush()
    {
        EnsureNotDisposed();
        _baseStream.Flush();
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously flushes the stream</para>
    ///     <para xml:lang="zh">异步刷新流</para>
    /// </summary>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        return _baseStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Changes the position within the range</para>
    ///     <para xml:lang="zh">改变范围内的位置</para>
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
        EnsureNotDisposed();
        if (!CanSeek)
        {
            throw new NotSupportedException("Stream does not support seeking");
        }
        var newPosition = origin switch
        {
            SeekOrigin.Begin   => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End     => Length + offset,
            _                  => throw new ArgumentException("Invalid seek origin", nameof(origin))
        };
        if (newPosition < 0 || newPosition > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Position must be within the range of the stream");
        }
        _baseStream.Position = _baseStartPosition + newPosition;
        _position = newPosition;
        return _position;
    }

    /// <summary>
    ///     <para xml:lang="en">Setting length is not supported</para>
    ///     <para xml:lang="zh">不支持设置长度</para>
    /// </summary>
    public override void SetLength(long value) => throw new NotSupportedException("Setting length is not supported for range streams");

    /// <summary>
    ///     <para xml:lang="en">Writing is not supported</para>
    ///     <para xml:lang="zh">不支持写入</para>
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Writing is not supported for range streams");

    /// <summary>
    ///     <para xml:lang="en">Writing is not supported</para>
    ///     <para xml:lang="zh">不支持写入</para>
    /// </summary>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException("Writing is not supported for range streams");

    /// <summary>
    ///     <para xml:lang="en">Writing is not supported</para>
    ///     <para xml:lang="zh">不支持写入</para>
    /// </summary>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException("Writing is not supported for range streams");

    /// <summary>
    ///     <para xml:lang="en">Writing is not supported</para>
    ///     <para xml:lang="zh">不支持写入</para>
    /// </summary>
    public override void WriteByte(byte value) => throw new NotSupportedException("Writing is not supported for range streams");

    /// <summary>
    ///     <para xml:lang="en">Releases resources used by the stream</para>
    ///     <para xml:lang="zh">释放流占用的资源</para>
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            base.Dispose(disposing);
            return;
        }
        _disposed = true;
        if (disposing && !_leaveOpen)
        {
            _baseStream.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously releases resources used by the stream</para>
    ///     <para xml:lang="zh">异步释放流占用的资源</para>
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            await base.DisposeAsync().ConfigureAwait(false);
            return;
        }
        _disposed = true;
        if (!_leaveOpen)
        {
            await _baseStream.DisposeAsync().ConfigureAwait(false);
        }
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(RangeStream));
    }
}
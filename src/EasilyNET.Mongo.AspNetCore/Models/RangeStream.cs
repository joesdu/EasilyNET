namespace EasilyNET.Mongo.AspNetCore.Models;

/// <summary>
///     <para xml:lang="en">Range stream that limits read length</para>
///     <para xml:lang="zh">限制读取长度的范围流</para>
/// </summary>
internal sealed class RangeStream(Stream baseStream, long maxLength) : Stream
{
    private readonly Stream _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
    private long _position;

    public override bool CanRead => _baseStream.CanRead;

    public override bool CanSeek => _baseStream.CanSeek;

    public override bool CanWrite => false;

    public override long Length { get; } = maxLength;

    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Position must be within the range of the stream");
            }
            // 计算在基础流中的实际位置
            var baseStreamInitialPosition = _baseStream.Position - _position;
            var newBasePosition = baseStreamInitialPosition + value;
            _baseStream.Position = newBasePosition;
            _position = value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
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

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var remainingBytes = Length - _position;
        if (remainingBytes <= 0)
        {
            return 0;
        }
        var bytesToRead = (int)Math.Min(count, remainingBytes);
        var bytesRead = await _baseStream.ReadAsync(buffer.AsMemory(offset, bytesToRead), cancellationToken);
        _position += bytesRead;
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var remainingBytes = Length - _position;
        if (remainingBytes <= 0)
        {
            return 0;
        }
        var bytesToRead = (int)Math.Min(buffer.Length, remainingBytes);
        try
        {
            var bytesRead = await _baseStream.ReadAsync(buffer[..bytesToRead], cancellationToken);
            _position += bytesRead;
            return bytesRead;
        }
        catch (OperationCanceledException)
        {
            // 客户端中止连接是正常行为,返回 0 表示流结束
            return 0;
        }
    }

    public override void Flush() => _baseStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => _baseStream.FlushAsync(cancellationToken);

    public override long Seek(long offset, SeekOrigin origin)
    {
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
        if (newPosition < 0)
        {
            throw new IOException("Cannot seek to a negative position");
        }
        if (newPosition > Length)
        {
            throw new IOException("Cannot seek beyond the end of the stream");
        }
        Position = newPosition;
        return _position;
    }

    public override void SetLength(long value) => throw new NotSupportedException("Setting length is not supported for range streams");

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Writing is not supported for range streams");

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException("Writing is not supported for range streams");

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException("Writing is not supported for range streams");

    public override void WriteByte(byte value) => throw new NotSupportedException("Writing is not supported for range streams");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseStream.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _baseStream.DisposeAsync();
        await base.DisposeAsync();
    }
}
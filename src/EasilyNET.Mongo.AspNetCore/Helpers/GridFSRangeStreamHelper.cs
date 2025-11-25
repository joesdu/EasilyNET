using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">GridFS range stream helper for video/audio streaming scenarios</para>
///     <para xml:lang="zh">GridFS 范围流辅助类,用于视频/音频流场景</para>
/// </summary>
public static class GridFSRangeStreamHelper
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Downloads a range of bytes from a GridFS file. Supports HTTP Range header for video/audio streaming.
    ///     </para>
    ///     <para xml:lang="zh">从 GridFS 文件中下载指定范围的字节。支持 HTTP Range 头,用于视频/音频流传输。</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS 存储桶</para>
    /// </param>
    /// <param name="id">
    ///     <para xml:lang="en">File ObjectId</para>
    ///     <para xml:lang="zh">文件 ObjectId</para>
    /// </param>
    /// <param name="startByte">
    ///     <para xml:lang="en">Start byte position (inclusive)</para>
    ///     <para xml:lang="zh">起始字节位置(包含)</para>
    /// </param>
    /// <param name="endByte">
    ///     <para xml:lang="en">End byte position (inclusive), null for end of file</para>
    ///     <para xml:lang="zh">结束字节位置(包含),null 表示文件末尾</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Range stream with file info</para>
    ///     <para xml:lang="zh">范围流及文件信息</para>
    /// </returns>
    public static async Task<(Stream Stream, long TotalLength, long RangeStart, long RangeEnd, GridFSFileInfo FileInfo)> DownloadRangeAsync(
        IGridFSBucket bucket,
        ObjectId id,
        long startByte,
        long? endByte = null,
        CancellationToken cancellationToken = default)
    {
        // 获取文件信息
        var fileInfo = await (await bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, id), cancellationToken: cancellationToken))
                           .FirstOrDefaultAsync(cancellationToken) ??
                       throw new FileNotFoundException($"File with ID {id} not found");
        var totalLength = fileInfo.Length;
        var actualStart = Math.Max(0, startByte);
        var actualEnd = endByte.HasValue ? Math.Min(endByte.Value, totalLength - 1) : totalLength - 1;
        if (actualStart >= totalLength)
        {
            throw new ArgumentOutOfRangeException(nameof(startByte), "Start byte is beyond file length");
        }
        // 打开可定位的下载流
        var fullStream = await bucket.OpenDownloadStreamAsync(id, new() { Seekable = true }, cancellationToken);
        // 定位到起始位置
        fullStream.Seek(actualStart, SeekOrigin.Begin);
        // 创建范围限制流
        var rangeLength = (actualEnd - actualStart) + 1;
        var rangeStream = new RangeStream(fullStream, rangeLength);
        return (rangeStream, totalLength, actualStart, actualEnd, fileInfo);
    }

    /// <summary>
    ///     <para xml:lang="en">Range stream that limits read length</para>
    ///     <para xml:lang="zh">限制读取长度的范围流</para>
    /// </summary>
    private sealed class RangeStream(Stream baseStream, long maxLength) : Stream
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
}
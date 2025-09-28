using System.Text;
using EasilyNET.Core.Essentials;

// ReSharper disable InvertIf

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Stream Extensions</para>
///     <para xml:lang="zh">流扩展</para>
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Open file with shared read and write</para>
    ///     <para xml:lang="zh">共享读写打开文件</para>
    /// </summary>
    /// <param name="file">
    ///     <para xml:lang="en">File info</para>
    ///     <para xml:lang="zh">文件信息</para>
    /// </param>
    public static FileStream ShareReadWrite(this FileInfo file) => file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

    /// <param name="stream">
    ///     <para xml:lang="en">Input stream</para>
    ///     <para xml:lang="zh">输入流</para>
    /// </param>
    extension(Stream stream)
    {
        /// <summary>
        ///     <para xml:lang="en">Convert stream to memory stream</para>
        ///     <para xml:lang="zh">将流转换为内存流</para>
        /// </summary>
        public PooledMemoryStream SaveAsMemoryStream()
        {
            if (stream is PooledMemoryStream pooledMemoryStream)
            {
                return pooledMemoryStream;
            }
            stream.Seek(0, SeekOrigin.Begin);
            var ms = new PooledMemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }

        /// <summary>
        ///     <para xml:lang="en">Convert to byte array asynchronously</para>
        ///     <para xml:lang="zh">异步转成字节数组</para>
        /// </summary>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation token</para>
        ///     <para xml:lang="zh">取消令牌</para>
        /// </param>
        public async Task<byte[]> ToArrayAsync(CancellationToken cancellationToken = default)
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }
            stream.Position = 0L;
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, 81920, cancellationToken).ConfigureAwait(false);
            return ms.ToArray();
        }

        /// <summary>
        ///     <para xml:lang="en">Convert to byte array</para>
        ///     <para xml:lang="zh">转成字节数组</para>
        /// </summary>
        public byte[] ToArray()
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }
            stream.Position = 0;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Shuffle code, randomly add a few empty bytes at the end of the stream, use with caution for important data, may cause stream
        ///     corruption
        ///     </para>
        ///     <para xml:lang="zh">流洗码，在流的末端随机增加几个空字节，重要数据请谨慎使用，可能造成流损坏</para>
        /// </summary>
        public void ShuffleCode()
        {
            if (stream is not { CanWrite: true, CanSeek: true })
            {
                return;
            }
            var position = stream.Position;
            stream.Position = stream.Length;
            var buffer = new byte[RandomExtensions.StrictNext(1, 20)];
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
            stream.Position = position;
        }
    }

    /// <param name="stream">
    ///     <para xml:lang="en">Input stream</para>
    ///     <para xml:lang="zh">输入流</para>
    /// </param>
    extension(StreamReader stream)
    {
        /// <summary>
        ///     <para xml:lang="en">Read all lines</para>
        ///     <para xml:lang="zh">读取所有行</para>
        /// </summary>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after reading</para>
        ///     <para xml:lang="zh">读取完毕后关闭流</para>
        /// </param>
        public List<string> ReadAllLines(bool closeAfter = true)
        {
            var stringList = new List<string>();
            while (stream.ReadLine() is { } str)
            {
                stringList.Add(str);
            }
            if (closeAfter)
            {
                stream.Close();
                stream.Dispose();
            }
            return stringList;
        }

        /// <summary>
        ///     <para xml:lang="en">Read all lines asynchronously</para>
        ///     <para xml:lang="zh">异步读取所有行</para>
        /// </summary>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after reading</para>
        ///     <para xml:lang="zh">读取完毕后关闭流</para>
        /// </param>
        public async Task<List<string>> ReadAllLinesAsync(bool closeAfter = true)
        {
            var stringList = new List<string>();
            while (await stream.ReadLineAsync().ConfigureAwait(false) is { } str)
            {
                stringList.Add(str);
            }
            if (closeAfter)
            {
                stream.Dispose();
            }
            return stringList;
        }
    }

    /// <param name="stream">
    ///     <para xml:lang="en">Input stream</para>
    ///     <para xml:lang="zh">输入流</para>
    /// </param>
    extension(FileStream stream)
    {
        /// <summary>
        ///     <para xml:lang="en">Read all lines</para>
        ///     <para xml:lang="zh">读取所有行</para>
        /// </summary>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding</para>
        ///     <para xml:lang="zh">编码</para>
        /// </param>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after reading</para>
        ///     <para xml:lang="zh">读取完毕后关闭流</para>
        /// </param>
        public List<string> ReadAllLines(Encoding encoding, bool closeAfter = true)
        {
            using var sr = new StreamReader(stream, encoding);
            var stringList = new List<string>();
            while (sr.ReadLine() is { } str)
            {
                stringList.Add(str);
            }
            if (closeAfter)
            {
                stream.Close();
                stream.Dispose();
            }
            return stringList;
        }

        /// <summary>
        ///     <para xml:lang="en">Read all text</para>
        ///     <para xml:lang="zh">读取所有文本</para>
        /// </summary>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding</para>
        ///     <para xml:lang="zh">编码</para>
        /// </param>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after reading</para>
        ///     <para xml:lang="zh">读取完毕后关闭流</para>
        /// </param>
        public string ReadAllText(Encoding encoding, bool closeAfter = true)
        {
            using var sr = new StreamReader(stream, encoding);
            var text = sr.ReadToEnd();
            if (closeAfter)
            {
                stream.Close();
                stream.Dispose();
            }
            return text;
        }

        /// <summary>
        ///     <para xml:lang="en">Write all text</para>
        ///     <para xml:lang="zh">写入所有文本</para>
        /// </summary>
        /// <param name="content">
        ///     <para xml:lang="en">Content</para>
        ///     <para xml:lang="zh">内容</para>
        /// </param>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding</para>
        ///     <para xml:lang="zh">编码</para>
        /// </param>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after writing</para>
        ///     <para xml:lang="zh">写入完毕后关闭流</para>
        /// </param>
        public void WriteAllText(string content, Encoding encoding, bool closeAfter = true)
        {
            using var sw = new StreamWriter(stream, encoding);
            stream.SetLength(0);
            sw.Write(content);
            if (closeAfter)
            {
                stream.Close();
                stream.Dispose();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Write all lines</para>
        ///     <para xml:lang="zh">写入所有文本行</para>
        /// </summary>
        /// <param name="lines">
        ///     <para xml:lang="en">Lines content</para>
        ///     <para xml:lang="zh">行内容</para>
        /// </param>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding</para>
        ///     <para xml:lang="zh">编码</para>
        /// </param>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after writing</para>
        ///     <para xml:lang="zh">写入完毕后关闭流</para>
        /// </param>
        public void WriteAllLines(IEnumerable<string> lines, Encoding encoding, bool closeAfter = true)
        {
            using var sw = new StreamWriter(stream, encoding);
            stream.SetLength(0);
            foreach (var line in lines)
            {
                sw.WriteLine(line);
            }
            sw.Flush();
            if (closeAfter)
            {
                stream.Close();
                stream.Dispose();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Read all lines asynchronously</para>
        ///     <para xml:lang="zh">异步读取所有行</para>
        /// </summary>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding</para>
        ///     <para xml:lang="zh">编码</para>
        /// </param>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after reading</para>
        ///     <para xml:lang="zh">读取完毕后关闭流</para>
        /// </param>
        public async Task<List<string>> ReadAllLinesAsync(Encoding encoding, bool closeAfter = true)
        {
            using var sr = new StreamReader(stream, encoding);
            var stringList = new List<string>();
            while (await sr.ReadLineAsync().ConfigureAwait(false) is { } str)
            {
                stringList.Add(str);
            }
            if (closeAfter)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
            return stringList;
        }

        /// <summary>
        ///     <para xml:lang="en">Read all text asynchronously</para>
        ///     <para xml:lang="zh">异步读取所有文本</para>
        /// </summary>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding</para>
        ///     <para xml:lang="zh">编码</para>
        /// </param>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after reading</para>
        ///     <para xml:lang="zh">读取完毕后关闭流</para>
        /// </param>
        public async Task<string> ReadAllTextAsync(Encoding encoding, bool closeAfter = true)
        {
            using var sr = new StreamReader(stream, encoding);
            var text = await sr.ReadToEndAsync().ConfigureAwait(false);
            if (closeAfter)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
            return text;
        }

        /// <summary>
        ///     <para xml:lang="en">Write all text asynchronously</para>
        ///     <para xml:lang="zh">异步写入所有文本</para>
        /// </summary>
        /// <param name="content">
        ///     <para xml:lang="en">Content</para>
        ///     <para xml:lang="zh">内容</para>
        /// </param>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding</para>
        ///     <para xml:lang="zh">编码</para>
        /// </param>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after writing</para>
        ///     <para xml:lang="zh">写入完毕后关闭流</para>
        /// </param>
        public async Task WriteAllTextAsync(string content, Encoding encoding, bool closeAfter = true)
        {
            await using var sw = new StreamWriter(stream, encoding);
            stream.SetLength(0);
            await sw.WriteAsync(content).ConfigureAwait(false);
            await sw.FlushAsync().ConfigureAwait(false);
            if (closeAfter)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Write all lines asynchronously</para>
        ///     <para xml:lang="zh">异步写入所有文本行</para>
        /// </summary>
        /// <param name="lines">
        ///     <para xml:lang="en">Lines content</para>
        ///     <para xml:lang="zh">行内容</para>
        /// </param>
        /// <param name="encoding">
        ///     <para xml:lang="en">Encoding</para>
        ///     <para xml:lang="zh">编码</para>
        /// </param>
        /// <param name="closeAfter">
        ///     <para xml:lang="en">Close stream after writing</para>
        ///     <para xml:lang="zh">写入完毕后关闭流</para>
        /// </param>
        public async Task WriteAllLinesAsync(IEnumerable<string> lines, Encoding encoding, bool closeAfter = true)
        {
            await using var sw = new StreamWriter(stream, encoding);
            stream.SetLength(0);
            foreach (var line in lines)
            {
                await sw.WriteLineAsync(line).ConfigureAwait(false);
            }
            await sw.FlushAsync().ConfigureAwait(false);
            if (closeAfter)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
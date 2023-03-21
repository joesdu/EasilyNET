using EasilyNET.Core.Systems;
using System.Text;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.BaseType;

/// <summary>
/// 流扩展
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// 将流转换为内存流
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static PooledMemoryStream SaveAsMemoryStream(this Stream stream)
    {
        if (stream is PooledMemoryStream pooledMemoryStream)
        {
            return pooledMemoryStream;
        }
        stream.Seek(0, SeekOrigin.Begin);
        var ms = new PooledMemoryStream();
        stream.CopyTo(ms);
        return ms;
    }

    /// <summary>
    /// 转成byte数组
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static byte[] ToArray(this Stream stream)
    {
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        _ = stream.Read(bytes, 0, bytes.Length);
        // 设置当前流的位置为流的开始
        stream.Seek(0, SeekOrigin.Begin);
        return bytes;
    }

    /// <summary>
    /// 流洗码，在流的末端随即增加几个空字节，重要数据请谨慎使用，可能造成流损坏
    /// </summary>
    /// <param name="stream"></param>
    public static void ShuffleCode(this Stream stream)
    {
        if (stream is not { CanWrite: true, CanSeek: true }) return;
        var position = stream.Position;
        stream.Position = stream.Length;
        for (var i = 0; i < new Random().Next(1, 20); i++)
        {
            stream.WriteByte(0);
        }
        stream.Flush();
        stream.Position = position;
    }

    /// <summary>
    /// 读取所有行
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static List<string> ReadAllLines(this StreamReader stream, bool closeAfter = true)
    {
        var stringList = new List<string>();
        string str;
        while (!string.IsNullOrWhiteSpace(str = stream.ReadLine() ?? string.Empty))
        {
            stringList.Add(str);
        }
        if (!closeAfter) return stringList;
        stream.Close();
        stream.Dispose();
        return stringList;
    }

    /// <summary>
    /// 读取所有行
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="encoding"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static List<string> ReadAllLines(this FileStream stream, Encoding encoding, bool closeAfter = true)
    {
        var stringList = new List<string>();
        string str;
        var sr = new StreamReader(stream, encoding);
        while (!string.IsNullOrWhiteSpace(str = sr.ReadLine() ?? string.Empty))
        {
            stringList.Add(str);
        }
        if (!closeAfter) return stringList;
        sr.Close();
        sr.Dispose();
        stream.Close();
        stream.Dispose();
        return stringList;
    }

    /// <summary>
    /// 读取所有文本
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="encoding"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static string ReadAllText(this FileStream stream, Encoding encoding, bool closeAfter = true)
    {
        var sr = new StreamReader(stream, encoding);
        var text = sr.ReadToEnd();
        if (!closeAfter) return text;
        sr.Close();
        sr.Dispose();
        stream.Close();
        stream.Dispose();
        return text;
    }

    /// <summary>
    /// 写入所有文本
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="content"></param>
    /// <param name="encoding"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static void WriteAllText(this FileStream stream, string content, Encoding encoding, bool closeAfter = true)
    {
        var sw = new StreamWriter(stream, encoding);
        stream.SetLength(0);
        sw.Write(content);
        if (!closeAfter) return;
        sw.Close();
        sw.Dispose();
        stream.Close();
        stream.Dispose();
    }

    /// <summary>
    /// 写入所有文本行
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="lines"></param>
    /// <param name="encoding"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static void WriteAllLines(this FileStream stream, IEnumerable<string> lines, Encoding encoding, bool closeAfter = true)
    {
        var sw = new StreamWriter(stream, encoding);
        stream.SetLength(0);
        foreach (var line in lines)
        {
            sw.WriteLine(line);
        }
        sw.Flush();
        if (!closeAfter) return;
        sw.Close();
        sw.Dispose();
        stream.Close();
        stream.Dispose();
    }

    /// <summary>
    /// 共享读写打开文件
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static FileStream ShareReadWrite(this FileInfo file) => file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

    /// <summary>
    /// 读取所有行
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static async Task<List<string>> ReadAllLinesAsync(this StreamReader stream, bool closeAfter = true)
    {
        var stringList = new List<string>();
        string str;
        while (!string.IsNullOrWhiteSpace(str = await stream.ReadLineAsync().ConfigureAwait(false) ?? string.Empty))
        {
            stringList.Add(str);
        }
        if (!closeAfter) return stringList;
        stream.Close();
        stream.Dispose();
        return stringList;
    }

    /// <summary>
    /// 读取所有行
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="encoding"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static async Task<List<string>> ReadAllLinesAsync(this FileStream stream, Encoding encoding, bool closeAfter = true)
    {
        var stringList = new List<string>();
        string str;
        var sr = new StreamReader(stream, encoding);
        while (!string.IsNullOrWhiteSpace(str = await sr.ReadLineAsync().ConfigureAwait(false) ?? string.Empty))
        {
            stringList.Add(str);
        }
        if (!closeAfter) return stringList;
        sr.Close();
        sr.Dispose();
        stream.Close();
        await stream.DisposeAsync().ConfigureAwait(false);
        return stringList;
    }

    /// <summary>
    /// 读取所有文本
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="encoding"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static async Task<string> ReadAllTextAsync(this FileStream stream, Encoding encoding, bool closeAfter = true)
    {
        var sr = new StreamReader(stream, encoding);
        var text = await sr.ReadToEndAsync().ConfigureAwait(false);
        if (!closeAfter) return text;
        sr.Close();
        sr.Dispose();
        stream.Close();
        await stream.DisposeAsync().ConfigureAwait(false);
        return text;
    }

    /// <summary>
    /// 写入所有文本
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="content"></param>
    /// <param name="encoding"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static async Task WriteAllTextAsync(this FileStream stream, string content, Encoding encoding, bool closeAfter = true)
    {
        var sw = new StreamWriter(stream, encoding);
        stream.SetLength(0);
        await sw.WriteAsync(content).ConfigureAwait(false);
        await sw.FlushAsync().ConfigureAwait(false);
        if (closeAfter)
        {
            sw.Close();
            stream.Close();
            await sw.DisposeAsync().ConfigureAwait(false);
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 写入所有文本行
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="lines"></param>
    /// <param name="encoding"></param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns></returns>
    public static async Task WriteAllLinesAsync(this FileStream stream, IEnumerable<string> lines, Encoding encoding, bool closeAfter = true)
    {
        var sw = new StreamWriter(stream, encoding);
        stream.SetLength(0);
        foreach (var line in lines)
        {
            await sw.WriteLineAsync(line).ConfigureAwait(false);
        }
        await sw.FlushAsync().ConfigureAwait(false);
        if (closeAfter)
        {
            sw.Close();
            stream.Close();
            await sw.DisposeAsync().ConfigureAwait(false);
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<byte[]> ToArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        _ = await stream.ReadAsync(bytes, cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);
        return bytes;
    }
}
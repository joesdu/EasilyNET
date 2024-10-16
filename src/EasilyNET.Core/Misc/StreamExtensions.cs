using System.Text;
using EasilyNET.Core.System;

// ReSharper disable InvertIf

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// 流扩展
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// 将流转换为内存流
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <returns>内存流</returns>
    public static PooledMemoryStream SaveAsMemoryStream(this Stream stream)
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
    /// 转成byte数组
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <returns>字节数组</returns>
    public static byte[] ToArray(this Stream stream)
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
    /// 流洗码，在流的末端随即增加几个空字节，重要数据请谨慎使用，可能造成流损坏
    /// </summary>
    /// <param name="stream">输入流</param>
    public static void ShuffleCode(this Stream stream)
    {
        if (stream is not { CanWrite: true, CanSeek: true }) return;
        var position = stream.Position;
        stream.Position = stream.Length;
        var random = new Random();
        var buffer = new byte[random.Next(1, 20)];
        stream.Write(buffer, 0, buffer.Length);
        stream.Flush();
        stream.Position = position;
    }

    /// <summary>
    /// 读取所有行
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns>所有行的列表</returns>
    public static List<string> ReadAllLines(this StreamReader stream, bool closeAfter = true)
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
    /// 读取所有行
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="encoding">编码</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns>所有行的列表</returns>
    public static List<string> ReadAllLines(this FileStream stream, Encoding encoding, bool closeAfter = true)
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
    /// 读取所有文本
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="encoding">编码</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns>所有文本</returns>
    public static string ReadAllText(this FileStream stream, Encoding encoding, bool closeAfter = true)
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
    /// 写入所有文本
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="content">内容</param>
    /// <param name="encoding">编码</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    public static void WriteAllText(this FileStream stream, string content, Encoding encoding, bool closeAfter = true)
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
    /// 写入所有文本行
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="lines">行内容</param>
    /// <param name="encoding">编码</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    public static void WriteAllLines(this FileStream stream, IEnumerable<string> lines, Encoding encoding, bool closeAfter = true)
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
    /// 共享读写打开文件
    /// </summary>
    /// <param name="file">文件信息</param>
    /// <returns>文件流</returns>
    public static FileStream ShareReadWrite(this FileInfo file) => file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

    /// <summary>
    /// 读取所有行
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns>所有行的列表</returns>
    public static async Task<List<string>> ReadAllLinesAsync(this StreamReader stream, bool closeAfter = true)
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

    /// <summary>
    /// 读取所有行
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="encoding">编码</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns>所有行的列表</returns>
    public static async Task<List<string>> ReadAllLinesAsync(this FileStream stream, Encoding encoding, bool closeAfter = true)
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
    /// 读取所有文本
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="encoding">编码</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    /// <returns>所有文本</returns>
    public static async Task<string> ReadAllTextAsync(this FileStream stream, Encoding encoding, bool closeAfter = true)
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
    /// 写入所有文本
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="content">内容</param>
    /// <param name="encoding">编码</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    public static async Task WriteAllTextAsync(this FileStream stream, string content, Encoding encoding, bool closeAfter = true)
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
    /// 写入所有文本行
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="lines">行内容</param>
    /// <param name="encoding">编码</param>
    /// <param name="closeAfter">读取完毕后关闭流</param>
    public static async Task WriteAllLinesAsync(this FileStream stream, IEnumerable<string> lines, Encoding encoding, bool closeAfter = true)
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

    /// <summary>
    /// 变数组
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字节数组</returns>
    public static async Task<byte[]> ToArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
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
}
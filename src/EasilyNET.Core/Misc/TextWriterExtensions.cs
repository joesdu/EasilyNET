using System.Text;
using EasilyNET.Core.Threading;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// <see cref="Console.Out" /> 扩展方法
/// </summary>
public static class TextWriterExtensions
{
    private static readonly AsyncLock _lock = new();
    private static string _lastOutput = string.Empty;
    private static string _clearLine = new(' ', Console.WindowWidth);
    private static int _lastWindowWidth = Console.WindowWidth;

    /// <summary>
    /// 线程安全的控制台在同一行输出消息
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    ///  Console.Out.SafeWriteOutput("Hello World!");
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="msg"></param>
    public static async Task SafeWriteOutput(this TextWriter writer, string msg)
    {
        using (await _lock.LockAsync())
        {
            UpdateClearLine();
            if (_lastOutput != msg)
            {
                ClearCurrentLine();
                await writer.WriteAsync(msg);
                _lastOutput = msg;
            }
        }
    }

    /// <summary>
    /// 线程安全的清除当前行
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    ///  Console.Out.SafeClearCurrentLine();
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <returns></returns>
    public static async Task SafeClearCurrentLine(this TextWriter _)
    {
        using (await _lock.LockAsync())
        {
            UpdateClearLine();
            ClearCurrentLine();
        }
    }

    /// <summary>
    /// 线程安全的清除上一行,并将光标移动到改行行首
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    ///  Console.Out.SafeClearPreviousLine();
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <returns></returns>
    public static async Task SafeClearPreviousLine(this TextWriter _)
    {
        using (await _lock.LockAsync())
        {
            UpdateClearLine();
            ClearPreviousLine();
        }
    }

    private static void UpdateClearLine()
    {
        if (Console.WindowWidth == _lastWindowWidth) return;
        _lastWindowWidth = Console.WindowWidth;
        _clearLine = new(' ', _lastWindowWidth);
    }

    private static bool IsConsoleCursorPositionSupported()
    {
        try
        {
            // 尝试设置光标位置
            var (Left, Top) = Console.GetCursorPosition();
            Console.SetCursorPosition(Left, Top);
            return true;
        }
        catch (IOException)
        {
            // 捕获到 IOException，说明不支持
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            // 捕获到 PlatformNotSupportedException，说明不支持
            return false;
        }
    }

    private static void ClearPreviousLine()
    {
        if (!IsConsoleCursorPositionSupported())
        {
            Console.WriteLine();
            return;
        }
        try
        {
            if (Console.CursorTop <= 0) return;
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(_clearLine);
            Console.SetCursorPosition(0, Console.CursorTop);
        }
        catch (IOException ex)
        {
            // Log the exception or handle it as needed
            Console.WriteLine($"IOException: {ex.Message}");
        }
    }

    private static void ClearCurrentLine()
    {
        if (!IsConsoleCursorPositionSupported())
        {
            Console.WriteLine();
            return;
        }
        try
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(_clearLine);
            Console.SetCursorPosition(0, Console.CursorTop);
        }
        catch (IOException ex)
        {
            // Log the exception or handle it as needed
            Console.WriteLine($"IOException: {ex.Message}");
        }
    }

    /// <summary>
    /// 在控制台输出进度条,用于某些时候需要显示进度的场景
    /// </summary>
    /// <param name="writer"><see cref="TextWriter" />Writer</param>
    /// <param name="progressPercentage">进度</param>
    /// <param name="message">消息</param>
    /// <param name="totalWidth">进度条整体宽度,包含消息部分</param>
    /// <param name="completedChar">完成部分填充字符</param>
    /// <param name="incompleteChar">未完成部分填充字符</param>
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    /// for (var progress = 0d; progress <= 100.00; progress += 0.1)
    ///   {
    ///      await Console.Out.ShowProgressBar(progress, "Processing...", Console.WindowWidth);
    ///      await Task.Delay(100);
    ///   }
    /// Output:
    ///   [==========---------------------] 5.1% Processing... 
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// <returns></returns>
    public static async Task ShowProgressBar(this TextWriter writer, double progressPercentage, string message = "", int totalWidth = -1, char completedChar = '=', char incompleteChar = '-')
    {
        if (progressPercentage < 0) progressPercentage = 0;
        if (progressPercentage > 100) progressPercentage = 100;
        var progressText = $"{progressPercentage / 100.0:P1}".PadLeft(7, (char)32);

        // 使用 UTF-8 编码计算消息的字节长度
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var progressTextBytes = Encoding.UTF8.GetBytes(progressText);
        var extraWidth = progressTextBytes.Length + messageBytes.Length + 5; // 计算额外字符的宽度，包括边界和百分比信息
        try
        {
            // 确保 totalWidth 不为负数
            if (totalWidth is -1)
            {
                totalWidth = Math.Max(0, Console.WindowWidth - extraWidth);
                // 当 totalWidth 为 -1 并且最大宽度大于 100 时，将 totalWidth 设置为 100
                if (totalWidth > 100)
                {
                    totalWidth >>= 1; // 使用位运算除以 2
                }
                if (totalWidth < 100)
                {
                    totalWidth = 100;
                }
            }
            else
            {
                totalWidth = Math.Max(0, totalWidth - extraWidth);
            }
        }
        catch (Exception)
        {
            // 如果 Console.WindowWidth 抛出异常说明当前环境不支持,则将 totalWidth 设置为 100
            totalWidth = Math.Max(0, 100 - extraWidth);
        }
        var progressBarWidth = (int)(progressPercentage * totalWidth) / 100;
        var isCompleted = Math.Abs(progressPercentage - 100) <= 0.000001;
        if (isCompleted) progressBarWidth = totalWidth; // 确保在 100% 时填满进度条
        var outputLength = totalWidth + extraWidth;
        var outputBytes = outputLength <= 256 ? stackalloc byte[outputLength] : new byte[outputLength];
        outputBytes[0] = 91; // ASCII for '['
        for (var i = 1; i <= progressBarWidth; i++)
        {
            outputBytes[i] = (byte)completedChar;
        }
        for (var i = progressBarWidth + 1; i <= totalWidth; i++)
        {
            outputBytes[i] = (byte)incompleteChar;
        }
        outputBytes[totalWidth + 1] = 93; // ASCII for ']'
        outputBytes[totalWidth + 2] = 32; // ASCII for ' '
        progressTextBytes.CopyTo(outputBytes[(totalWidth + 3)..]);
        outputBytes[totalWidth + 3 + progressTextBytes.Length] = 32; // ASCII for ' '
        messageBytes.CopyTo(outputBytes[(totalWidth + 4 + progressTextBytes.Length)..]);
        var output = Encoding.UTF8.GetString(outputBytes);
        await writer.SafeWriteOutput(output);
        // 当进度为 100% 时，输出换行
        if (isCompleted) Console.WriteLine();
    }
}
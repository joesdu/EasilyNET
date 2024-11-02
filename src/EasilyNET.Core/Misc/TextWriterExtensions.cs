using System.Runtime.InteropServices;
using System.Text;
using EasilyNET.Core.Commons;
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
    private static readonly AsyncLock _asyncLock = new();
    private static readonly Lock _syncLock = new();

    private static string _lastOutput = string.Empty;
    private static bool? _ansiSupported;
    private static bool? _cursorPosSupported;
    private static bool? _windowSizeSupported;

    private static char[] _clearBuffer = new char[256];

    private static void ClearBuffer(int length)
    {
        if (length > _clearBuffer.Length)
        {
            Array.Resize(ref _clearBuffer, length);
        }
        Array.Fill(_clearBuffer, ' ', 0, length);
    }

    /// <summary>
    /// 线程安全的在控制台同一行输出消息,并换行
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    ///  await Console.Out.SafeWriteLineAsync("Hello World!");
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="msg"></param>
    public static async Task SafeWriteLineAsync(this TextWriter writer, string msg)
    {
        using (await _asyncLock.LockAsync())
        {
            if (_lastOutput != msg)
            {
                if (IsAnsiSupported())
                {
                    await writer.WriteLineAsync($"\e[1A\e[2K\e[1G{msg}");
                }
                else
                {
                    await writer.ClearPreviousLineAsync();
                    await writer.WriteLineAsync(msg);
                }
                _lastOutput = msg;
            }
        }
    }

    /// <summary>
    /// 线程安全的在控制台同一行输出消息,并换行
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    ///  Console.Out.SafeWriteLine("Hello World!");
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="msg"></param>
    public static void SafeWriteLine(this TextWriter writer, string msg)
    {
        lock (_syncLock)
        {
            if (_lastOutput == msg) return;
            if (IsAnsiSupported())
            {
                writer.WriteLine($"\e[1A\e[2K\e[1G{msg}");
            }
            else
            {
                writer.ClearPreviousLine();
                writer.WriteLine(msg);
            }
            _lastOutput = msg;
        }
    }

    /// <summary>
    /// 线程安全的在控制台同一行输出消息
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    ///  await Console.Out.SafeWriteAsync("Hello World!");
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="msg"></param>
    public static async Task SafeWriteAsync(this TextWriter writer, string msg)
    {
        using (await _asyncLock.LockAsync())
        {
            if (_lastOutput != msg)
            {
                if (IsAnsiSupported())
                {
                    await writer.WriteAsync($"\e[2K\e[1G{msg}");
                }
                else
                {
                    await writer.ClearCurrentLineAsync();
                    await writer.WriteAsync(msg);
                }
                _lastOutput = msg;
            }
        }
    }

    /// <summary>
    /// 线程安全的在控制台同一行输出消息
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    ///  Console.Out.SafeWrite("Hello World!");
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="msg"></param>
    public static void SafeWrite(this TextWriter writer, string msg)
    {
        lock (_syncLock)
        {
            if (_lastOutput == msg) return;
            if (IsAnsiSupported())
            {
                writer.Write($"\e[2K\e[1G{msg}");
            }
            else
            {
                writer.ClearCurrentLine();
                writer.Write(msg);
            }
            _lastOutput = msg;
        }
    }

    /// <summary>
    /// 线程安全的清除当前行
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    ///  await Console.Out.SafeClearCurrentLineAsync();
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <returns></returns>
    public static async Task SafeClearCurrentLineAsync(this TextWriter writer)
    {
        using (await _asyncLock.LockAsync())
        {
            await writer.ClearCurrentLineAsync();
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
    public static void SafeClearCurrentLine(this TextWriter writer)
    {
        lock (_syncLock)
        {
            writer.ClearCurrentLine();
        }
    }

    /// <summary>
    /// 线程安全的清除上一行,并将光标移动到该行行首
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    ///  await Console.Out.SafeClearPreviousLineAsync();
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <returns></returns>
    public static async Task SafeClearPreviousLineAsync(this TextWriter writer)
    {
        using (await _asyncLock.LockAsync())
        {
            await writer.ClearPreviousLineAsync();
        }
    }

    /// <summary>
    /// 线程安全的清除上一行,并将光标移动到该行行首
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
    public static void SafeClearPreviousLine(this TextWriter writer)
    {
        lock (_syncLock)
        {
            writer.ClearPreviousLine();
        }
    }

    /// <summary>
    /// 在控制台输出可点击的路径,使用 [<see langword="Ctrl + 鼠标左键" />] 即可打开路径
    /// <remarks>
    ///     <para>
    ///     使用方式:
    ///     <code>
    ///   <![CDATA[
    /// var path = @"F:\tools\test\test\bin\Release\net9.0\win-x64\publish";
    ///   Console.Out.WriteClickablePath(path, true, 5, true);
    /// Output:
    ///   bin\Release\net9.0\win-x64\publish
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="path">需要处理的完整路径</param>
    /// <param name="relative">是否输出相对路径,默认: <see langword="false" /></param>
    /// <param name="deep">当为相对路径的时候配置目录深度,仅保留最后 N 层目录</param>
    /// <param name="newLine">是否换行,默认: <see langword="false" />,行为同: Console.Write()</param>
    public static void WriteClickablePath(this TextWriter writer, string path, bool relative = false, int deep = 5, bool newLine = false)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var outputPath = relative ? path.GetClickableRelativePath(deep) : path.GetClickablePath();
        if (newLine) writer.WriteLine(outputPath);
        else writer.Write(outputPath);
    }

    /// <summary>
    /// 在当前行中将光标移动 X 个位置
    /// </summary>
    /// <param name="writer">TextWriter</param>
    /// <param name="positions">要移动的光标位置数，可以为正数(右移)或负数(左移)</param>
    public static void MoveCursorInCurrentLine(this TextWriter writer, int positions)
    {
        if (positions == 0) return;
        if (IsAnsiSupported())
        {
            var direction = positions > 0 ? 'C' : 'D'; // 'C' 表示向右移动，'D' 表示向左移动
            writer.Write($"\e[{Math.Abs(positions)}{direction}");
        }
        else
        {
            if (IsCursorPosSupported())
            {
                try
                {
                    var newLeft = Math.Max(0, Console.CursorLeft + positions);
                    Console.SetCursorPosition(newLeft, Console.CursorTop);
                }
                catch (IOException ex)
                {
                    // 记录异常或根据需要进行处理
                    writer.WriteLine($"IOException: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// 在当前行中将光标移动 X 个位置
    /// </summary>
    /// <param name="writer">TextWriter</param>
    /// <param name="positions">要移动的光标位置数，可以为正数(右移)或负数(左移)</param>
    public static async Task MoveCursorInCurrentLineAsync(this TextWriter writer, int positions)
    {
        if (positions == 0) return;
        if (IsAnsiSupported())
        {
            var direction = positions > 0 ? 'C' : 'D'; // 'C' 表示向右移动，'D' 表示向左移动
            await writer.WriteAsync($"\e[{Math.Abs(positions)}{direction}");
        }
        else
        {
            if (IsCursorPosSupported())
            {
                try
                {
                    var newLeft = Math.Max(0, Console.CursorLeft + positions);
                    Console.SetCursorPosition(newLeft, Console.CursorTop);
                }
                catch (IOException ex)
                {
                    // 记录异常或根据需要进行处理
                    await writer.WriteLineAsync($"IOException: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine();
            }
        }
    }

    private static async Task ClearPreviousLineAsync(this TextWriter writer)
    {
        if (!IsCursorPosSupported())
        {
            await writer.WriteLineAsync();
            return;
        }
        if (IsAnsiSupported())
        {
            // 合并多次 Write 调用为一次
            await writer.WriteAsync("\e[1A\e[2K\e[1G"); // 光标上移一行，清除整行，光标移动至行首
        }
        else
        {
            try
            {
                if (Console.CursorTop <= 0) return;
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                ClearBuffer(_lastOutput.Length);
                Console.Write(_clearBuffer, 0, _lastOutput.Length);
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            catch (IOException ex)
            {
                // 记录异常或根据需要进行处理
                await writer.WriteLineAsync($"IOException: {ex.Message}");
            }
        }
    }

    private static void ClearPreviousLine(this TextWriter writer)
    {
        if (!IsCursorPosSupported())
        {
            writer.WriteLine();
            return;
        }
        if (IsAnsiSupported())
        {
            // 合并多次 Write 调用为一次
            writer.Write("\e[1A\e[2K\e[1G"); // 光标上移一行，清除整行，光标移动至行首
        }
        else
        {
            try
            {
                if (Console.CursorTop <= 0) return;
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                ClearBuffer(_lastOutput.Length);
                Console.Write(_clearBuffer, 0, _lastOutput.Length);
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            catch (IOException ex)
            {
                // 记录异常或根据需要进行处理
                writer.WriteLine($"IOException: {ex.Message}");
            }
        }
    }

    private static async Task ClearCurrentLineAsync(this TextWriter writer)
    {
        if (!IsCursorPosSupported())
        {
            await writer.WriteLineAsync();
            return;
        }
        if (IsAnsiSupported())
        {
            await writer.WriteAsync("\e[2K\e[1G"); // 清除整行，光标移动至行首
        }
        else
        {
            try
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                ClearBuffer(_lastOutput.Length);
                Console.Write(_clearBuffer, 0, _lastOutput.Length);
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            catch (IOException ex)
            {
                // 记录异常或根据需要进行处理
                await writer.WriteLineAsync($"IOException: {ex.Message}");
            }
        }
    }

    private static void ClearCurrentLine(this TextWriter writer)
    {
        if (!IsCursorPosSupported())
        {
            writer.WriteLine();
            return;
        }
        if (IsAnsiSupported())
        {
            writer.Write("\e[2K\e[1G"); // 清除整行，光标移动至行首
        }
        else
        {
            try
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                ClearBuffer(_lastOutput.Length);
                Console.Write(_clearBuffer, 0, _lastOutput.Length);
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            catch (IOException ex)
            {
                // 记录异常或根据需要进行处理
                writer.WriteLine($"IOException: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 判断当前环境终端是否支持 ANSI 转义序列
    /// </summary>
    /// <returns>是否支持</returns>
    public static bool IsAnsiSupported()
    {
        if (_ansiSupported.HasValue)
        {
            return _ansiSupported.Value;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _ansiSupported = WinApis.IsAnsiSupported();
        }
        else
        {
            _ansiSupported = !Console.IsOutputRedirected;
        }
        return _ansiSupported.Value;
    }

    /// <summary>
    /// 判断当前环境终端是否支持控制台光标移动
    /// </summary>
    /// <returns>是否支持</returns>
    public static bool IsCursorPosSupported()
    {
        if (_cursorPosSupported.HasValue)
        {
            return _cursorPosSupported.Value;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _cursorPosSupported = WinApis.IsConsoleCursorPositionSupported();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var term = Environment.GetEnvironmentVariable("TERM");
            _cursorPosSupported = !string.IsNullOrWhiteSpace(term) && term != "dumb";
        }
        else
        {
            try
            {
                // 其他系统通过尝试设置光标位置,并捕获异常来判断是否支持光标移动
                var (Left, Top) = Console.GetCursorPosition();
                Console.SetCursorPosition(Left, Top);
                _cursorPosSupported = true;
            }
            catch (IOException)
            {
                // 捕获到 IOException，说明不支持
                _cursorPosSupported = false;
            }
            catch (PlatformNotSupportedException)
            {
                // 捕获到 PlatformNotSupportedException，说明不支持
                _cursorPosSupported = false;
            }
        }
        return _cursorPosSupported.Value;
    }

    /// <summary>
    /// 判断当前环境终端是否支持获取控制台窗口大小
    /// </summary>
    /// <returns>是否支持</returns>
    public static bool IsWindowSizeSupported()
    {
        if (_windowSizeSupported.HasValue)
        {
            return _windowSizeSupported.Value;
        }
        _windowSizeSupported = !(Console.IsOutputRedirected || Console.IsErrorRedirected);
        return _windowSizeSupported.Value;
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
    ///      await Console.Out.ShowProgressBarAsync(progress, "Processing...", Console.WindowWidth);
    ///      await Task.Delay(100);
    ///   }
    /// Output:
    ///   [==========---------------------] 5.1% Processing... 
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// <returns></returns>
    public static async Task ShowProgressBarAsync(this TextWriter writer, double progressPercentage, string message = "", int totalWidth = -1, char completedChar = '=', char incompleteChar = '-')
    {
        var output = GenerateProgressBarOutput(progressPercentage, message, totalWidth, completedChar, incompleteChar);
        if (IsAnsiSupported())
        {
            await writer.SafeWriteAsync(output);
        }
        else
        {
            await writer.SafeWriteLineAsync(output);
        }
        // 当进度为 100% 时，输出换行
        if (Math.Abs(progressPercentage - 100) <= 0.000001) await writer.WriteLineAsync();
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
    ///      Console.Out.ShowProgressBar(progress, "Processing...", Console.WindowWidth);
    ///      await Task.Delay(100);
    ///   }
    /// Output:
    ///   [==========---------------------] 5.1% Processing... 
    /// ]]>
    /// </code>
    ///     </para>
    /// </remarks>
    /// <returns></returns>
    public static void ShowProgressBar(this TextWriter writer, double progressPercentage, string message = "", int totalWidth = -1, char completedChar = '=', char incompleteChar = '-')
    {
        var output = GenerateProgressBarOutput(progressPercentage, message, totalWidth, completedChar, incompleteChar);
        if (IsAnsiSupported())
        {
            writer.SafeWrite(output);
        }
        else
        {
            writer.SafeWriteLine(output);
        }
        // 当进度为 100% 时，输出换行
        if (Math.Abs(progressPercentage - 100) <= 0.000001) writer.WriteLine();
    }

    private static string GenerateProgressBarOutput(double progressPercentage, string message, int totalWidth, char completedChar, char incompleteChar)
    {
        if (progressPercentage < 0) progressPercentage = 0;
        if (progressPercentage > 100) progressPercentage = 100;
        var progressText = $"{progressPercentage / 100.0:P1}".PadLeft(7, (char)32);
        // 使用 UTF-8 编码计算消息的字节长度
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var progressTextBytes = Encoding.UTF8.GetBytes(progressText);
        var extraWidth = progressTextBytes.Length + messageBytes.Length + 5; // 计算额外字符的宽度，包括边界和百分比信息
        if (totalWidth is -1)
        {
            if (IsWindowSizeSupported())
            {
                totalWidth = Math.Max(0, Console.WindowWidth - extraWidth);
                // 当 totalWidth 为 -1 并且最大宽度大于 100 时，将 totalWidth 设置为 100
                if (totalWidth > 100)
                {
                    totalWidth >>= 1; // 使用位运算除以 2
                }
                if (totalWidth < 100)
                {
                    totalWidth = Console.WindowWidth;
                }
            }
            else
            {
                totalWidth = Math.Max(0, 100 - extraWidth);
            }
        }
        else
        {
            totalWidth = Math.Max(0, totalWidth - extraWidth);
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
        return Encoding.UTF8.GetString(outputBytes);
    }
}
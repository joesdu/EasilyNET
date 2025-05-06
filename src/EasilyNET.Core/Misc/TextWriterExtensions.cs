using System.Runtime.InteropServices;
using System.Text;
using EasilyNET.Core.Commons;
using EasilyNET.Core.Threading;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">TextWriter Extensions</para>
///     <para xml:lang="zh"><see cref="Console.Out" /> 扩展方法</para>
/// </summary>
public static class TextWriterExtensions
{
    private static readonly AsyncLock _asyncLock = new();
    private static readonly Lock _syncLock = new();
    private static string _lastOutput = string.Empty;
    private static readonly char[] _loadingFrames;
    private static bool? _ansiSupported;
    private static bool? _cursorPosSupported;
    private static bool? _windowSizeSupported;
    private static char[] _clearBuffer = new char[256];

    static TextWriterExtensions()
    {
        // 检查平台是否支持 UTF-8 编码
        if (!Console.IsOutputRedirected && IsUtf8Supported())
        {
            // 启用 UTF-8 支持
            Console.OutputEncoding = Encoding.UTF8;
            // ⣿
            _loadingFrames = ['⣾', '⣷', '⣯', '⣟', '⡿', '⢿', '⣻', '⣽'];
        }
        // 当不支持 UTF-8 编码时,使用默认编码,并使用简单的字符
        else
        {
            //Console.OutputEncoding = Encoding.Default;
            _loadingFrames = ['-', '\\', '|', '/'];
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Thread-safe output message on the same line in the console, and wrap</para>
    ///     <para xml:lang="zh">线程安全的在控制台同一行输出消息,并换行</para>
    /// </summary>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    ///  await Console.Out.SafeWriteLineAsync("Hello World!");
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    /// <param name="msg">
    ///     <para xml:lang="en">Message</para>
    ///     <para xml:lang="zh">消息</para>
    /// </param>
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
    ///     <para xml:lang="en">Thread-safe output message on the same line in the console, and wrap</para>
    ///     <para xml:lang="zh">线程安全的在控制台同一行输出消息,并换行</para>
    /// </summary>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    ///  Console.Out.SafeWriteLine("Hello World!");
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    /// <param name="msg">
    ///     <para xml:lang="en">Message</para>
    ///     <para xml:lang="zh">消息</para>
    /// </param>
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
    ///     <para xml:lang="en">Thread-safe output message on the same line in the console</para>
    ///     <para xml:lang="zh">线程安全的在控制台同一行输出消息</para>
    /// </summary>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    ///  await Console.Out.SafeWriteAsync("Hello World!");
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    /// <param name="msg">
    ///     <para xml:lang="en">Message</para>
    ///     <para xml:lang="zh">消息</para>
    /// </param>
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
    ///     <para xml:lang="en">Thread-safe output message on the same line in the console</para>
    ///     <para xml:lang="zh">线程安全的在控制台同一行输出消息</para>
    /// </summary>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    ///  Console.Out.SafeWrite("Hello World!");
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    /// <param name="msg">
    ///     <para xml:lang="en">Message</para>
    ///     <para xml:lang="zh">消息</para>
    /// </param>
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
    ///     <para xml:lang="en">Thread-safe clear current line</para>
    ///     <para xml:lang="zh">线程安全的清除当前行</para>
    /// </summary>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    ///  await Console.Out.SafeClearCurrentLineAsync();
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    public static async Task SafeClearCurrentLineAsync(this TextWriter writer)
    {
        using (await _asyncLock.LockAsync())
        {
            await writer.ClearCurrentLineAsync();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Thread-safe clear current line</para>
    ///     <para xml:lang="zh">线程安全的清除当前行</para>
    /// </summary>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    ///  Console.Out.SafeClearCurrentLine();
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    public static void SafeClearCurrentLine(this TextWriter writer)
    {
        lock (_syncLock)
        {
            writer.ClearCurrentLine();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Thread-safe clear previous line and move cursor to the beginning of the line</para>
    ///     <para xml:lang="zh">线程安全的清除上一行,并将光标移动到该行行首</para>
    /// </summary>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    ///  await Console.Out.SafeClearPreviousLineAsync();
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    public static async Task SafeClearPreviousLineAsync(this TextWriter writer)
    {
        using (await _asyncLock.LockAsync())
        {
            await writer.ClearPreviousLineAsync();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Thread-safe clear previous line and move cursor to the beginning of the line</para>
    ///     <para xml:lang="zh">线程安全的清除上一行,并将光标移动到该行行首</para>
    /// </summary>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    ///  Console.Out.SafeClearPreviousLine();
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    public static void SafeClearPreviousLine(this TextWriter writer)
    {
        lock (_syncLock)
        {
            writer.ClearPreviousLine();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Output clickable path in the console, use [<see langword="Ctrl + Left Click" />] to open the path</para>
    ///     <para xml:lang="zh">在控制台输出可点击的路径,使用 [<see langword="Ctrl + 鼠标左键" />] 即可打开路径</para>
    /// </summary>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    /// var path = @"F:\tools\test\test\bin\Release\net9.0\win-x64\publish";
    ///   Console.Out.WriteClickablePath(path, true, 5, true);
    /// Output:
    ///   bin\Release\net9.0\win-x64\publish
    /// ]]>
    /// </code>
    /// </remarks>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    /// <param name="path">
    ///     <para xml:lang="en">Full path to process</para>
    ///     <para xml:lang="zh">需要处理的完整路径</para>
    /// </param>
    /// <param name="relative">
    ///     <para xml:lang="en">Whether to output relative path, default: <see langword="false" /></para>
    ///     <para xml:lang="zh">是否输出相对路径,默认: <see langword="false" /></para>
    /// </param>
    /// <param name="deep">
    ///     <para xml:lang="en">When it is a relative path, configure the directory depth, only keep the last N levels of directories</para>
    ///     <para xml:lang="zh">当为相对路径的时候配置目录深度,仅保留最后 N 层目录</para>
    /// </param>
    /// <param name="newLine">
    ///     <para xml:lang="en">Whether to wrap, default: <see langword="false" />, behavior is the same as Console.Write()</para>
    ///     <para xml:lang="zh">是否换行,默认: <see langword="false" />,行为同: Console.Write()</para>
    /// </param>
    public static void WriteClickablePath(this TextWriter writer, string path, bool relative = false, int deep = 5, bool newLine = false)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var outputPath = relative ? path.GetClickableRelativePath(deep) : path.GetClickablePath();
        if (newLine) writer.WriteLine(outputPath);
        else writer.Write(outputPath);
    }

    /// <summary>
    ///     <para xml:lang="en">Move the cursor X positions in the current line</para>
    ///     <para xml:lang="zh">在当前行中将光标移动 X 个位置</para>
    /// </summary>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    /// <param name="positions">
    ///     <para xml:lang="en">The number of cursor positions to move, can be positive (right) or negative (left)</para>
    ///     <para xml:lang="zh">要移动的光标位置数，可以为正数(右移)或负数(左移)</para>
    /// </param>
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
    ///     <para xml:lang="en">Move the cursor X positions in the current line</para>
    ///     <para xml:lang="zh">在当前行中将光标移动 X 个位置</para>
    /// </summary>
    /// <param name="writer">
    /// <see cref="TextWriter" />>
    /// </param>
    /// <param name="positions">
    ///     <para xml:lang="en">The number of cursor positions to move, can be positive (right) or negative (left)</para>
    ///     <para xml:lang="zh">要移动的光标位置数，可以为正数(右移)或负数(左移)</para>
    /// </param>
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

    /// <summary>
    ///     <para xml:lang="en">Whether UTF-8 encoding is supported</para>
    ///     <para xml:lang="zh">是否支持 UTF-8 编码</para>
    /// </summary>
    /// <returns></returns>
    public static bool IsUtf8Supported()
    {
        // 检查当前平台是否支持 UTF-8 编码
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var osVersion = Environment.OSVersion.Version;
            // Windows 10 (10.0) 和 Windows Server 2016 (10.0) 及以上版本支持 UTF-8
            return osVersion.Major >= 10;
        }
        // ReSharper disable once InvertIf
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var term = Environment.GetEnvironmentVariable("TERM");
            return !string.IsNullOrWhiteSpace(term) && term != "dumb";
        }
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Determine whether the current terminal environment supports ANSI escape sequences</para>
    ///     <para xml:lang="zh">判断当前环境终端是否支持 ANSI 转义序列</para>
    /// </summary>
    public static bool IsAnsiSupported()
    {
        if (_ansiSupported.HasValue)
        {
            return _ansiSupported.Value;
        }
        _ansiSupported = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WinApis.IsAnsiSupported() : !Console.IsOutputRedirected;
        return _ansiSupported.Value;
    }

    /// <summary>
    ///     <para xml:lang="en">Determine whether the current terminal environment supports console cursor movement</para>
    ///     <para xml:lang="zh">判断当前环境终端是否支持控制台光标移动</para>
    /// </summary>
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
    ///     <para xml:lang="en">Determine whether the current terminal environment supports getting console window size</para>
    ///     <para xml:lang="zh">判断当前环境终端是否支持获取控制台窗口大小</para>
    /// </summary>
    public static bool IsWindowSizeSupported()
    {
        if (_windowSizeSupported.HasValue) return _windowSizeSupported.Value;
        _windowSizeSupported = !(Console.IsOutputRedirected || Console.IsErrorRedirected);
        return _windowSizeSupported.Value;
    }

    /// <summary>
    ///     <para xml:lang="en">Output progress bar in the console, used in scenarios where progress needs to be displayed</para>
    ///     <para xml:lang="zh">在控制台输出进度条,用于某些时候需要显示进度的场景</para>
    /// </summary>
    /// <param name="writer">
    /// <see cref="TextWriter" />Writer
    /// </param>
    /// <param name="percentage">
    ///     <para xml:lang="en">Progress</para>
    ///     <para xml:lang="zh">进度</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">Message</para>
    ///     <para xml:lang="zh">消息</para>
    /// </param>
    /// <param name="width">
    ///     <para xml:lang="en">Overall width of the progress bar, excluding the message part</para>
    ///     <para xml:lang="zh">进度条整体宽度,不包含消息部分</para>
    /// </param>
    /// <param name="completedChar">
    ///     <para xml:lang="en">Character to fill the completed part</para>
    ///     <para xml:lang="zh">完成部分填充字符</para>
    /// </param>
    /// <param name="incompleteChar">
    ///     <para xml:lang="en">Character to fill the incomplete part</para>
    ///     <para xml:lang="zh">未完成部分填充字符</para>
    /// </param>
    /// <param name="isFixedBarWidth">
    ///     <para xml:lang="en"><see langword="true" />: Fixed progress bar width, <see langword="false" />: Fixed overall width</para>
    ///     <para xml:lang="zh"><see langword="true" />: 固定进度条宽度,<see langword="false" />: 固定整体宽度</para>
    /// </param>
    /// <remarks>
    ///     <para>Usage:</para>
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
    /// </remarks>
    public static async Task ShowProgressBarAsync(this TextWriter writer, double percentage, string message = "", int width = -1, char completedChar = '=', char incompleteChar = '-', bool isFixedBarWidth = true)
    {
        var output = GenerateProgressBarOutput(percentage, message, width, completedChar, incompleteChar, isFixedBarWidth);
        if (IsAnsiSupported())
        {
            await writer.SafeWriteAsync(output);
        }
        else
        {
            await writer.SafeWriteLineAsync(output);
        }
        // 当进度为 100% 时，输出换行
        if (Math.Abs(percentage - 100) <= 0.000001) await writer.WriteLineAsync();
    }

    /// <summary>
    ///     <para xml:lang="en">Output progress bar in the console, used in scenarios where progress needs to be displayed</para>
    ///     <para xml:lang="zh">在控制台输出进度条,用于某些时候需要显示进度的场景</para>
    /// </summary>
    /// <param name="writer">
    /// <see cref="TextWriter" />Writer
    /// </param>
    /// <param name="percentage">
    ///     <para xml:lang="en">Progress</para>
    ///     <para xml:lang="zh">进度</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">Message</para>
    ///     <para xml:lang="zh">消息</para>
    /// </param>
    /// <param name="width">
    ///     <para xml:lang="en">Width</para>
    ///     <para xml:lang="zh">宽度</para>
    /// </param>
    /// <param name="completedChar">
    ///     <para xml:lang="en">Character to fill the completed part</para>
    ///     <para xml:lang="zh">完成部分填充字符</para>
    /// </param>
    /// <param name="incompleteChar">
    ///     <para xml:lang="en">Character to fill the incomplete part</para>
    ///     <para xml:lang="zh">未完成部分填充字符</para>
    /// </param>
    /// <param name="isFixedBarWidth">
    ///     <para xml:lang="en"><see langword="true" />: Fixed progress bar width, <see langword="false" />: Fixed overall width</para>
    ///     <para xml:lang="zh"><see langword="true" />: 固定进度条宽度,<see langword="false" />: 固定整体宽度</para>
    /// </param>
    /// <remarks>
    ///     <para>Usage:</para>
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
    /// </remarks>
    public static void ShowProgressBar(this TextWriter writer, double percentage, string message = "", int width = -1, char completedChar = '=', char incompleteChar = '-', bool isFixedBarWidth = true)
    {
        var output = GenerateProgressBarOutput(percentage, message, width, completedChar, incompleteChar, isFixedBarWidth);
        if (IsAnsiSupported())
        {
            writer.SafeWrite(output);
        }
        else
        {
            writer.SafeWriteLine(output);
        }
        // 当进度为 100% 时，输出换行
        if (Math.Abs(percentage - 100) <= 0.000001) writer.WriteLine();
    }

    /// <summary>
    ///     <para xml:lang="en">Control the visibility of the console cursor, no effect if the platform is not supported</para>
    ///     <para xml:lang="zh">控制控制台光标可见性,若平台不受支持则无效果</para>
    /// </summary>
    /// <param name="writer">
    ///     <see cref="TextWriter" />
    /// </param>
    /// <param name="visible">
    ///     <para xml:lang="en"><see langword="true" />: Show cursor, <see langword="false" />: Hide cursor</para>
    ///     <para xml:lang="zh"><see langword="true" />: 显示光标, <see langword="false" />: 隐藏光标</para>
    /// </param>
    public static async Task SetCursorVisibilityAsync(this TextWriter writer, bool visible)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                Console.CursorVisible = visible;
            }
            catch (Exception)
            {
                // Ignore
            }
        }
        else if (IsAnsiSupported())
        {
            await writer.WriteAsync(visible ? "\e[?25h" : "\e[?25l");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Control the visibility of the console cursor, no effect if the platform is not supported</para>
    ///     <para xml:lang="zh">控制控制台光标可见性,若平台不受支持则无效果</para>
    /// </summary>
    /// <param name="writer">
    ///     <see cref="TextWriter" />
    /// </param>
    /// <param name="visible">
    ///     <para xml:lang="en"><see langword="true" />: Show cursor, <see langword="false" />: Hide cursor</para>
    ///     <para xml:lang="zh"><see langword="true" />: 显示光标, <see langword="false" />: 隐藏光标</para>
    /// </param>
    public static void SetCursorVisibility(this TextWriter writer, bool visible)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                Console.CursorVisible = visible;
            }
            catch (Exception)
            {
                // Ignore
            }
        }
        else if (IsAnsiSupported())
        {
            writer.Write(visible ? "\e[?25h" : "\e[?25l");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Output loading animation in the console</para>
    ///     <para xml:lang="zh">在控制台输出加载动画</para>
    /// </summary>
    /// <param name="writer">
    ///     <see cref="TextWriter" />
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <param name="delay">
    ///     <para xml:lang="en">Refresh interval</para>
    ///     <para xml:lang="zh">刷新间隔</para>
    /// </param>
    /// <param name="color">
    ///     <para xml:lang="en">Character color</para>
    ///     <para xml:lang="zh">字符颜色</para>
    /// </param>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    /// var cts = new CancellationTokenSource();
    ///   // 启动加载动画
    ///   var loading = Console.Out.ShowLoadingAsync(cts.Token);
    ///   await Task.Run(async () =>
    ///   {
    ///       await Task.Delay(3000, token);
    ///       await cts.CancelAsync();
    ///   }, token);
    ///   await loading;
    /// ]]>
    /// </code>
    /// </remarks>
    public static async Task ShowLoadingAsync(this TextWriter writer, CancellationToken cancellationToken, int delay = 100, ConsoleColor color = ConsoleColor.Green)
    {
        var frameIndex = 0;
        // 隐藏光标
        await writer.SetCursorVisibilityAsync(false);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.ForegroundColor = color;
                await writer.WriteAsync(_loadingFrames[frameIndex]);
                Console.ResetColor();
                await writer.FlushAsync(cancellationToken);
                frameIndex = (frameIndex + 1) % _loadingFrames.Length;
                await Task.Delay(delay, cancellationToken);
                await writer.WriteAsync('\b');
            }
        }
        catch (TaskCanceledException)
        {
            // 清除已经输出的字符
            if (IsAnsiSupported())
            {
                await writer.WriteAsync("\b \b"); // 使用 ANSI 转义序列清除字符
            }
            else
            {
                await writer.WriteAsync(' '); // 使用空格覆盖字符
                await writer.FlushAsync(cancellationToken);
            }
            // 恢复光标可见性
            await writer.SetCursorVisibilityAsync(true);
            Console.ResetColor();
        }
        finally
        {
            // 恢复光标可见性
            await writer.SetCursorVisibilityAsync(true);
            Console.ResetColor();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Output loading animation in the console</para>
    ///     <para xml:lang="zh">在控制台输出加载动画</para>
    /// </summary>
    /// <param name="writer">
    ///     <see cref="TextWriter" />
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <param name="delay">
    ///     <para xml:lang="en">Refresh interval</para>
    ///     <para xml:lang="zh">刷新间隔</para>
    /// </param>
    /// <param name="color">
    ///     <para xml:lang="en">Character color</para>
    ///     <para xml:lang="zh">字符颜色</para>
    /// </param>
    /// <remarks>
    ///     <para>Usage:</para>
    ///     <code>
    ///   <![CDATA[
    ///   var cts = new CancellationTokenSource();
    ///   // 启动加载动画
    ///   var task = Task.Factory.StartNew(() => Console.Out.ShowLoading(cts.Token), token);
    ///   // 模拟3秒后取消加载动画
    ///   Task.Delay(3000, token).ContinueWith(_ => cts.Cancel(), token);
    ///   // 等待加载动画任务完成
    ///   task.Wait(token);
    ///   ]]>
    /// </code>
    /// </remarks>
    public static void ShowLoading(this TextWriter writer, CancellationToken cancellationToken, int delay = 100, ConsoleColor color = ConsoleColor.Green)
    {
        var frameIndex = 0;
        writer.SetCursorVisibility(false);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.ForegroundColor = color;
                writer.Write(_loadingFrames[frameIndex]);
                Console.ResetColor();
                writer.Flush();
                frameIndex = (frameIndex + 1) % _loadingFrames.Length;
                Task.Delay(delay, cancellationToken).Wait(cancellationToken); // 使用 Task.Delay 替代 Thread.Sleep
                writer.Write('\b');
            }
        }
        catch (TaskCanceledException)
        {
            // 清除已经输出的字符
            if (IsAnsiSupported())
            {
                writer.Write("\b \b"); // 使用 ANSI 转义序列清除字符
            }
            else
            {
                writer.Write(' '); // 使用空格覆盖字符
                writer.Flush();
            }
            writer.SetCursorVisibility(true);
            Console.ResetColor();
        }
        finally
        {
            writer.SetCursorVisibility(true);
            Console.ResetColor();
        }
    }

    private static void ClearBuffer(int length)
    {
        if (length > _clearBuffer.Length)
        {
            Array.Resize(ref _clearBuffer, length);
        }
        Array.Fill(_clearBuffer, ' ', 0, length);
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

    private static string GenerateProgressBarOutput(double percentage, string message, int width, char completedChar, char incompleteChar, bool isFixedBarWidth = true)
    {
        if (percentage < 0) percentage = 0;
        if (percentage > 100) percentage = 100;
        var progressText = $"{percentage / 100.0:P1}".PadLeft(7, (char)32);
        // 使用 UTF-8 编码计算消息的字节长度
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var progressTextBytes = Encoding.UTF8.GetBytes(progressText);
        var extraWidth = progressTextBytes.Length + messageBytes.Length + 5; // 计算额外字符的宽度，包括边界和百分比信息
        // 当width表示非固定Bar长度时，根据窗口宽度计算Bar长度
        if (!isFixedBarWidth)
        {
            if (IsCursorPosSupported())
            {
                if (width is -1)
                {
                    width = Math.Max(0, Console.WindowWidth - extraWidth);
                }
                if (width >= Console.WindowWidth)
                {
                    width = Console.WindowWidth - extraWidth;
                }
            }
            else
            {
                // 当不支持检测的时候,则默认宽度设置为20
                width = 20 - extraWidth;
            }
        }
        var completedWidth = (int)(percentage * width) / 100;
        if (percentage.AreAlmostEqual(100d)) completedWidth = width; // 确保在 100% 时填满进度条
        var outputLength = width + extraWidth;
        var outputBytes = outputLength <= 256 ? stackalloc byte[outputLength] : new byte[outputLength];
        outputBytes[0] = 91; // ASCII for '['
        for (var i = 1; i <= completedWidth; i++)
        {
            outputBytes[i] = (byte)completedChar;
        }
        for (var i = completedWidth + 1; i <= width; i++)
        {
            outputBytes[i] = (byte)incompleteChar;
        }
        // ReSharper disable once GrammarMistakeInComment
        outputBytes[width + 1] = 93; // ASCII for ']'
        outputBytes[width + 2] = 32; // ASCII for ' '
        progressTextBytes.CopyTo(outputBytes[(width + 3)..]);
        outputBytes[width + 3 + progressTextBytes.Length] = 32; // ASCII for ' '
        messageBytes.CopyTo(outputBytes[(width + 4 + progressTextBytes.Length)..]);
        return Encoding.UTF8.GetString(outputBytes);
    }
}
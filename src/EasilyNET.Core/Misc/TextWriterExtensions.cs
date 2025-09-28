using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
public static partial class TextWriterExtensions
{
    private const int DefaultProgressBarWidth = 30; // 固定模式下默认进度条宽度
    private static readonly AsyncLock _asyncLock = new();
    private static readonly Lock _syncLock = new();

    private static string _lastOutput = string.Empty;

    // 记录显示宽度(去 ANSI, 大致处理全角双宽字符)
    private static int _lastOutputLength;
    private static readonly char[] _loadingFrames;
    private static bool? _ansiSupported;
    private static bool? _cursorPosSupported;
    private static bool? _windowSizeSupported;
    private static char[] _clearBuffer = new char[256];

    static TextWriterExtensions()
    {
        try
        {
            // 检查平台是否支持 UTF-8 编码
            if (!Console.IsOutputRedirected && IsUtf8Supported() && !Console.OutputEncoding.Equals(Encoding.UTF8))
            {
                Console.OutputEncoding = Encoding.UTF8; // 启用 UTF-8 支持
            }
        }
        catch (Exception)
        {
            // 忽略设置编码时的异常(受限宿主等场景)
        }
        if (Console.OutputEncoding.Equals(Encoding.UTF8))
        {
            // ⣿ 等 braille 动画字符
            _loadingFrames = ['⣾', '⣷', '⣯', '⣟', '⡿', '⢿', '⣻', '⣽'];
        }
        else
        {
            _loadingFrames = ['-', '\\', '|', '/'];
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Whether UTF-8 encoding is supported.</para>
    ///     <para xml:lang="zh">是否支持 UTF-8 编码</para>
    /// </summary>
    public static bool IsUtf8Supported()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var osVersion = Environment.OSVersion.Version;
            // Windows 10 (10.0) 和 Windows Server 2016 (10.0) 及以上版本支持 UTF-8
            return osVersion.Major >= 10;
        }
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return false;
        }
        var term = Environment.GetEnvironmentVariable("TERM");
        return !string.IsNullOrWhiteSpace(term) && term != "dumb";
    }

    /// <summary>
    ///     <para xml:lang="en">Determine whether ANSI escape sequences are supported.</para>
    ///     <para xml:lang="zh">判断当前环境是否支持 ANSI 转义序列</para>
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
    ///     <para xml:lang="en">Determine whether console cursor movement is supported.</para>
    ///     <para xml:lang="zh">判断当前环境是否支持光标移动</para>
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
                var (Left, Top) = Console.GetCursorPosition();
                Console.SetCursorPosition(Left, Top);
                _cursorPosSupported = true;
            }
            catch (IOException)
            {
                _cursorPosSupported = false;
            }
            catch (PlatformNotSupportedException)
            {
                _cursorPosSupported = false;
            }
        }
        return _cursorPosSupported.Value;
    }

    /// <summary>
    ///     <para xml:lang="en">Determine whether retrieving console window size is supported.</para>
    ///     <para xml:lang="zh">判断是否支持获取控制台窗口大小</para>
    /// </summary>
    public static bool IsWindowSizeSupported()
    {
        if (_windowSizeSupported.HasValue)
        {
            return _windowSizeSupported.Value;
        }
        _windowSizeSupported = !(Console.IsOutputRedirected || Console.IsErrorRedirected);
        return _windowSizeSupported.Value;
    }

    private static void ClearBuffer(int length)
    {
        if (length > _clearBuffer.Length)
        {
            Array.Resize(ref _clearBuffer, length);
        }
        Array.Fill(_clearBuffer, ' ', 0, length);
    }

    private static string GenerateProgressBarOutput(double percentage, string message, int width, char completedChar, char incompleteChar, bool isFixedBarWidth = true)
    {
        percentage = percentage switch
        {
            < 0   => 0,
            > 100 => 100,
            _     => percentage
        };
        if (isFixedBarWidth)
        {
            if (width < 0)
            {
                width = DefaultProgressBarWidth; // 修复默认 -1 时未设置宽度
            }
        }
        var progressText = $"{percentage / 100.0:P1}".PadLeft(7, ' ');
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var progressTextBytes = Encoding.UTF8.GetBytes(progressText);
        var extraWidth = progressTextBytes.Length + messageBytes.Length + 5;
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
                width = Math.Max(10, 20 - extraWidth); // 确保不为负
            }
        }
        if (width < 0)
        {
            width = 0;
        }
        var completedWidth = (int)(width * (percentage / 100.0));
        if (percentage.AreAlmostEqual(100d))
        {
            completedWidth = width;
        }
        var outputLength = width + extraWidth;
        if (outputLength < extraWidth)
        {
            outputLength = extraWidth; // 保护
        }
        var outputBytes = outputLength <= 256 ? stackalloc byte[outputLength] : new byte[outputLength];
        outputBytes[0] = 91; // ASCII for '['
        for (var i = 1; i <= completedWidth && i <= width; i++)
            outputBytes[i] = (byte)completedChar;
        for (var i = completedWidth + 1; i <= width; i++)
            outputBytes[i] = (byte)incompleteChar;
        var barCloseIndex = width + 1;
        outputBytes[barCloseIndex] = 93;     // ASCII for ']'
        outputBytes[barCloseIndex + 1] = 32; // ASCII for ' '
        progressTextBytes.CopyTo(outputBytes[(barCloseIndex + 2)..]);
        outputBytes[barCloseIndex + 2 + progressTextBytes.Length] = 32; // ASCII for ' '
        messageBytes.CopyTo(outputBytes[(barCloseIndex + 3 + progressTextBytes.Length)..]);
        return Encoding.UTF8.GetString(outputBytes);
    }

    /// <param name="writer"></param>
    extension(TextWriter writer)
    {
        /// <summary>
        ///     <para xml:lang="en">Move the cursor X positions in the current line.</para>
        ///     <para xml:lang="zh">在当前行中将光标移动 X 个位置</para>
        /// </summary>
        /// <param name="positions">正数右移,负数左移 / positive: right, negative: left</param>
        public void MoveCursorInCurrentLine(int positions)
        {
            if (positions == 0)
            {
                return;
            }
            if (IsAnsiSupported())
            {
                var direction = positions > 0 ? 'C' : 'D'; // 'C' 表示向右移动，'D' 表示向左移动
                writer.Write($"\e[{Math.Abs(positions)}{direction}");
            }
            else if (IsCursorPosSupported())
            {
                try
                {
                    var newLeft = Math.Max(0, Console.CursorLeft + positions);
                    Console.SetCursorPosition(newLeft, Console.CursorTop);
                }
                catch (IOException ex)
                {
                    writer.WriteLine($"IOException: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Thread-safe clear current line.</para>
        ///     <para xml:lang="zh">线程安全的清除当前行</para>
        /// </summary>
        /// <remarks>
        ///     <code><![CDATA[
        /// await Console.Out.SafeClearCurrentLineAsync();
        /// ]]></code>
        /// </remarks>
        public async Task SafeClearCurrentLineAsync()
        {
            using (await _asyncLock.LockAsync())
            {
                await writer.ClearCurrentLineAsync();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Thread-safe clear current line.</para>
        ///     <para xml:lang="zh">线程安全的清除当前行</para>
        /// </summary>
        /// <remarks>
        ///     <code><![CDATA[
        /// Console.Out.SafeClearCurrentLine();
        /// ]]></code>
        /// </remarks>
        public void SafeClearCurrentLine()
        {
            lock (_syncLock)
            {
                writer.ClearCurrentLine();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Thread-safe clear previous line and move cursor to line start.</para>
        ///     <para xml:lang="zh">线程安全的清除上一行,并将光标移动到该行行首</para>
        /// </summary>
        public async Task SafeClearPreviousLineAsync()
        {
            using (await _asyncLock.LockAsync())
            {
                await writer.ClearPreviousLineAsync();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Thread-safe clear previous line and move cursor to line start.</para>
        ///     <para xml:lang="zh">线程安全的清除上一行,并将光标移动到该行行首</para>
        /// </summary>
        public void SafeClearPreviousLine()
        {
            lock (_syncLock)
            {
                writer.ClearPreviousLine();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Output clickable path (Ctrl+Click in supporting terminals).</para>
        ///     <para xml:lang="zh">输出可点击路径(支持终端中 Ctrl+Left Click 打开)</para>
        /// </summary>
        /// <remarks>
        ///     <code><![CDATA[
        /// var path = @"F:\tools\test\publish";
        /// Console.Out.WriteClickablePath(path, true, 5, true);
        /// ]]></code>
        /// </remarks>
        public void WriteClickablePath(string path, bool relative = false, int deep = 5, bool newLine = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }
            var outputPath = relative ? path.GetClickableRelativePath(deep) : path.GetClickablePath();
            if (newLine)
            {
                writer.WriteLine(outputPath);
            }
            else
            {
                writer.Write(outputPath);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Move the cursor X positions in the current line.</para>
        ///     <para xml:lang="zh">在当前行中将光标移动 X 个位置</para>
        /// </summary>
        /// <param name="positions">正数右移,负数左移 / positive: right, negative: left</param>
        public async Task MoveCursorInCurrentLineAsync(int positions)
        {
            if (positions == 0)
            {
                return;
            }
            if (IsAnsiSupported())
            {
                var direction = positions > 0 ? 'C' : 'D'; // 'C' 表示向右移动，'D' 表示向左移动
                await writer.WriteAsync($"\e[{Math.Abs(positions)}{direction}");
            }
            else if (IsCursorPosSupported())
            {
                try
                {
                    var newLeft = Math.Max(0, Console.CursorLeft + positions);
                    Console.SetCursorPosition(newLeft, Console.CursorTop);
                }
                catch (IOException ex)
                {
                    await writer.WriteLineAsync($"IOException: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Show progress bar (async).</para>
        ///     <para xml:lang="zh">异步显示进度条</para>
        /// </summary>
        /// <remarks>
        ///     <code><![CDATA[
        /// for (var p=0d; p<=100; p+=0.5) {
        ///     await Console.Out.ShowProgressBarAsync(p, "Processing...", Console.WindowWidth);
        ///     await Task.Delay(50);
        /// }
        /// ]]></code>
        /// </remarks>
        public async Task ShowProgressBarAsync(double percentage, string message = "", int width = -1, char completedChar = '=', char incompleteChar = '-', bool isFixedBarWidth = true)
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
            if (Math.Abs(percentage - 100) <= 0.000001)
            {
                await writer.WriteLineAsync();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Show progress bar.</para>
        ///     <para xml:lang="zh">显示进度条</para>
        /// </summary>
        /// <remarks>
        ///     <code><![CDATA[
        /// for (var p=0d; p<=100; p+=0.5) {
        ///     Console.Out.ShowProgressBar(p, "Processing...", Console.WindowWidth);
        ///     Thread.Sleep(50);
        /// }
        /// ]]></code>
        /// </remarks>
        public void ShowProgressBar(double percentage, string message = "", int width = -1, char completedChar = '=', char incompleteChar = '-', bool isFixedBarWidth = true)
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
            if (Math.Abs(percentage - 100) <= 0.000001)
            {
                writer.WriteLine();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Set console cursor visibility (async).</para>
        ///     <para xml:lang="zh">异步设置控制台光标可见性</para>
        /// </summary>
        public async Task SetCursorVisibilityAsync(bool visible)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Console.CursorVisible = visible;
                }
                catch
                {
                    // ignored
                }
            }
            else if (IsAnsiSupported())
            {
                await writer.WriteAsync(visible ? "\e[?25h" : "\e[?25l");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Set console cursor visibility.</para>
        ///     <para xml:lang="zh">设置控制台光标可见性</para>
        /// </summary>
        public void SetCursorVisibility(bool visible)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Console.CursorVisible = visible;
                }
                catch
                {
                    // ignored
                }
            }
            else if (IsAnsiSupported())
            {
                writer.Write(visible ? "\e[?25h" : "\e[?25l");
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Show loading spinner (async).</para>
        ///     <para xml:lang="zh">异步显示加载动画</para>
        /// </summary>
        /// <remarks>
        ///     <code><![CDATA[
        /// using var cts = new CancellationTokenSource();
        /// var task = Console.Out.ShowLoadingAsync(cts.Token);
        /// await Task.Delay(3000);
        /// cts.Cancel();
        /// await task;
        /// ]]></code>
        /// </remarks>
        public async Task ShowLoadingAsync(CancellationToken cancellationToken, int delay = 100, ConsoleColor color = ConsoleColor.Green)
        {
            var frameIndex = 0;
            await writer.SetCursorVisibilityAsync(false);
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (Console.IsOutputRedirected)
                    {
                        await Task.Delay(delay, cancellationToken); // 重定向环境不画动画
                        continue;
                    }
                    Console.ForegroundColor = color;
                    await writer.WriteAsync($"\r{_loadingFrames[frameIndex]}");
                    Console.ResetColor();
                    await writer.FlushAsync(cancellationToken);
                    frameIndex = (frameIndex + 1) % _loadingFrames.Length;
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Cancellation is expected when stopping the loading spinner; exception is intentionally ignored.
            }
            finally
            {
                // 清理: 清除该行上的 spinner 字符
                if (IsAnsiSupported())
                {
                    await writer.WriteAsync("\r\e[2K");
                }
                else
                {
                    await writer.WriteAsync("\r ");
                }
                await writer.SetCursorVisibilityAsync(true);
                Console.ResetColor();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Show loading spinner.</para>
        ///     <para xml:lang="zh">显示加载动画</para>
        /// </summary>
        /// <remarks>
        ///     <code><![CDATA[
        /// using var cts = new CancellationTokenSource();
        /// var t = Task.Run(() => Console.Out.ShowLoading(cts.Token));
        /// Thread.Sleep(3000);
        /// cts.Cancel();
        /// t.Wait();
        /// ]]></code>
        /// </remarks>
        public void ShowLoading(CancellationToken cancellationToken, int delay = 100, ConsoleColor color = ConsoleColor.Green)
        {
            var frameIndex = 0;
            writer.SetCursorVisibility(false);
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (Console.IsOutputRedirected)
                    {
                        Task.Delay(delay, cancellationToken).Wait(cancellationToken);
                        continue;
                    }
                    Console.ForegroundColor = color;
                    writer.Write($"\r{_loadingFrames[frameIndex]}");
                    Console.ResetColor();
                    writer.Flush();
                    frameIndex = (frameIndex + 1) % _loadingFrames.Length;
                    Task.Delay(delay, cancellationToken).Wait(cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Cancellation is expected; ignore the exception to allow graceful exit.
            }
            finally
            {
                writer.Write(IsAnsiSupported() ? "\r\e[2K" : "\r ");
                writer.SetCursorVisibility(true);
                Console.ResetColor();
            }
        }

        private async Task ClearPreviousLineAsync()
        {
            if (!IsCursorPosSupported())
            {
                await writer.WriteLineAsync();
                return;
            }
            if (IsAnsiSupported())
            {
                await writer.WriteAsync("\e[1A\e[2K\e[1G"); // 光标上移一行，清除整行，光标移动至行首
            }
            else
            {
                try
                {
                    if (Console.CursorTop <= 0)
                    {
                        return;
                    }
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    ClearBuffer(_lastOutputLength);
                    Console.Write(_clearBuffer, 0, _lastOutputLength);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
                catch (IOException ex)
                {
                    await writer.WriteLineAsync($"IOException: {ex.Message}");
                }
            }
        }

        private void ClearPreviousLine()
        {
            if (!IsCursorPosSupported())
            {
                writer.WriteLine();
                return;
            }
            if (IsAnsiSupported())
            {
                writer.Write("\e[1A\e[2K\e[1G"); // 光标上移一行，清除整行，光标移动至行首
            }
            else
            {
                try
                {
                    if (Console.CursorTop <= 0)
                    {
                        return;
                    }
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    ClearBuffer(_lastOutputLength);
                    Console.Write(_clearBuffer, 0, _lastOutputLength);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
                catch (IOException ex)
                {
                    writer.WriteLine($"IOException: {ex.Message}");
                }
            }
        }

        private async Task ClearCurrentLineAsync()
        {
            if (!IsCursorPosSupported())
            {
                await writer.WriteLineAsync();
                return;
            }
            if (IsAnsiSupported())
            {
                await writer.WriteAsync("\e[2K\e[1G"); // 清除当前行并将光标移动至行首
            }
            else
            {
                try
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    ClearBuffer(_lastOutputLength);
                    Console.Write(_clearBuffer, 0, _lastOutputLength);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
                catch (IOException ex)
                {
                    await writer.WriteLineAsync($"IOException: {ex.Message}");
                }
            }
        }

        private void ClearCurrentLine()
        {
            if (!IsCursorPosSupported())
            {
                writer.WriteLine();
                return;
            }
            if (IsAnsiSupported())
            {
                writer.Write("\e[2K\e[1G"); // 清除当前行并将光标移动至行首
            }
            else
            {
                try
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    ClearBuffer(_lastOutputLength);
                    Console.Write(_clearBuffer, 0, _lastOutputLength);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
                catch (IOException ex)
                {
                    writer.WriteLine($"IOException: {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Same as SafeWriteLineAsync, force write even if text equals previous.</para>
        ///     <para xml:lang="zh">同 SafeWriteLineAsync, 即使与上次输出相同也强制输出</para>
        /// </summary>
        /// <param name="msg">消息 / Message</param>
        /// <param name="force">true: 强制输出 / force output</param>
        public async Task SafeWriteLineAsync(string msg, bool force)
        {
            using (await _asyncLock.LockAsync())
            {
                if (!force && _lastOutput == msg)
                {
                    return;
                }
                if (IsAnsiSupported())
                {
                    await writer.WriteLineAsync($"\e[1A\e[2K\e[1G{msg}");
                }
                else
                {
                    await writer.ClearPreviousLineAsync();
                    await writer.WriteLineAsync(msg);
                }
                UpdateLastOutput(msg);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Thread-safe output message on the same line in the console, and wrap (distinct output).</para>
        ///     <para xml:lang="zh">线程安全的在控制台同一行输出消息,并换行(相同文本将被忽略)</para>
        /// </summary>
        /// <remarks>
        /// Usage:
        /// <code><![CDATA[
        /// Console.Out.SafeWriteLine("Hello World!");
        /// ]]></code>
        /// </remarks>
        /// <param name="msg">消息 / Message</param>
        public void SafeWriteLine(string msg) => SafeWriteLine(writer, msg, false);

        /// <summary>
        ///     <para xml:lang="en">Same as SafeWriteLine, force write even if text equals previous.</para>
        ///     <para xml:lang="zh">同 SafeWriteLine, 即使与上次输出相同也强制输出</para>
        /// </summary>
        /// <param name="msg">消息 / Message</param>
        /// <param name="force">true: 强制输出 / force output</param>
        public void SafeWriteLine(string msg, bool force)
        {
            lock (_syncLock)
            {
                if (!force && _lastOutput == msg)
                {
                    return;
                }
                if (IsAnsiSupported())
                {
                    writer.WriteLine($"\e[1A\e[2K\e[1G{msg}");
                }
                else
                {
                    writer.ClearPreviousLine();
                    writer.WriteLine(msg);
                }
                UpdateLastOutput(msg);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Thread-safe output message on the same line (distinct output).</para>
        ///     <para xml:lang="zh">线程安全的在控制台同一行输出消息(相同文本将被忽略)</para>
        /// </summary>
        /// <remarks>
        /// Usage:
        /// <code><![CDATA[
        /// await Console.Out.SafeWriteAsync("Processing...");
        /// ]]></code>
        /// </remarks>
        /// <param name="msg">消息 / Message</param>
        public async Task SafeWriteAsync(string msg) => await SafeWriteAsync(writer, msg, false);

        /// <summary>
        ///     <para xml:lang="en">Same as SafeWriteAsync, force write even if text equals previous.</para>
        ///     <para xml:lang="zh">同 SafeWriteAsync, 即使与上次输出相同也强制输出</para>
        /// </summary>
        /// <param name="msg">消息 / Message</param>
        /// <param name="force">true: 强制输出 / force output</param>
        public async Task SafeWriteAsync(string msg, bool force)
        {
            using (await _asyncLock.LockAsync())
            {
                if (!force && _lastOutput == msg)
                {
                    return;
                }
                if (IsAnsiSupported())
                {
                    await writer.WriteAsync($"\e[2K\e[1G{msg}");
                }
                else
                {
                    await writer.ClearCurrentLineAsync();
                    await writer.WriteAsync(msg);
                }
                UpdateLastOutput(msg);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Thread-safe output message on the same line (distinct output).</para>
        ///     <para xml:lang="zh">线程安全的在控制台同一行输出消息(相同文本将被忽略)</para>
        /// </summary>
        /// <remarks>
        /// Usage:
        /// <code><![CDATA[
        /// Console.Out.SafeWrite("Processing...");
        /// ]]></code>
        /// </remarks>
        /// <param name="msg">消息 / Message</param>
        public void SafeWrite(string msg) => SafeWrite(writer, msg, false);
    }

    #region SafeWriteLine / SafeWrite (force overload)

    /// <summary>
    ///     <para xml:lang="en">Thread-safe output message on the same line in the console, and wrap (distinct output).</para>
    ///     <para xml:lang="zh">线程安全的在控制台同一行输出消息,并换行(相同文本将被忽略)</para>
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code><![CDATA[
    /// await Console.Out.SafeWriteLineAsync("Hello World!");
    /// ]]></code>
    /// </remarks>
    /// <param name="writer">
    ///     <see cref="TextWriter" />
    /// </param>
    /// <param name="msg">消息 / Message</param>
    public static async Task SafeWriteLineAsync(this TextWriter writer, string msg) => await SafeWriteLineAsync(writer, msg, false);

    /// <summary>
    ///     <para xml:lang="en">Same as SafeWrite, force write even if text equals previous.</para>
    ///     <para xml:lang="zh">同 SafeWrite, 即使与上次输出相同也强制输出</para>
    /// </summary>
    /// <param name="writer">
    ///     <see cref="TextWriter" />
    /// </param>
    /// <param name="msg">消息 / Message</param>
    /// <param name="force">true: 强制输出 / force output</param>
    public static void SafeWrite(this TextWriter writer, string msg, bool force)
    {
        lock (_syncLock)
        {
            if (!force && _lastOutput == msg)
            {
                return;
            }
            if (IsAnsiSupported())
            {
                writer.Write($"\e[2K\e[1G{msg}");
            }
            else
            {
                writer.ClearCurrentLine();
                writer.Write(msg);
            }
            UpdateLastOutput(msg);
        }
    }

    #endregion

    #region Helpers

    private static void UpdateLastOutput(string msg)
    {
        _lastOutput = msg;
        _lastOutputLength = CalculateDisplayWidth(msg);
    }

    // 去除 ANSI 序列并估算显示宽度(简单 East Asian 宽字符视为 2)
    private static readonly Regex _ansiRegex = AnsiEscapeSequenceRegex();

    private static int CalculateDisplayWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }
        var noAnsi = _ansiRegex.Replace(text, "");
        return noAnsi.EnumerateRunes().Sum(rune => IsWideRune(rune) ? 2 : 1);
    }

    private static bool IsWideRune(Rune rune) =>
        // 简单判定: 中日韩统一表意等常见宽字符范围
        rune.Value is (>= 0x1100 and <= 0x115F) // Hangul Jamo
            or (>= 0x2E80 and <= 0xA4CF)
            or (>= 0xAC00 and <= 0xD7A3) // Hangul Syllables
            or (>= 0xF900 and <= 0xFAFF) // CJK Compatibility Ideographs
            or (>= 0xFE10 and <= 0xFE19)
            or (>= 0xFE30 and <= 0xFE6F)
            or (>= 0xFF00 and <= 0xFF60)
            or (>= 0xFFE0 and <= 0xFFE6);

    [GeneratedRegex("\e\\[[0-9;]*[A-Za-z]", RegexOptions.Compiled)]
    private static partial Regex AnsiEscapeSequenceRegex();

    #endregion
}
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
            ClearCurrentLine();
            await writer.WriteAsync(msg);
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
            ClearPreviousLine();
        }
    }

    private static void ClearPreviousLine()
    {
        if (Console.CursorTop <= 0) return;
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop);
    }

    private static void ClearCurrentLine()
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop);
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
        var progressText = $"{progressPercentage / 100.0:P1}";
        var extraWidth = progressText.Length + message.Length + 5; // 计算额外字符的宽度，包括边界和百分比信息
        try
        {
            // 确保 totalWidth 不为负数
            totalWidth = totalWidth == -1 ? Math.Max(0, Console.WindowWidth - extraWidth) : Math.Max(0, totalWidth - extraWidth);
        }
        catch (Exception)
        {
            // 如果 Console.WindowWidth 抛出异常说明当前环境不支持,则将 totalWidth 设置为 80
            totalWidth = Math.Max(0, 80 - extraWidth);
        }
        var progressBarWidth = (int)progressPercentage * totalWidth / 100;
        if (Math.Abs(progressPercentage - 100) < 0.000001) progressBarWidth = totalWidth; // 确保在100%时填满进度条
        var progressBar = new string(completedChar, progressBarWidth).PadRight(totalWidth, incompleteChar);
        var output = $"[{progressBar}] {progressText} {message}";
        await writer.SafeWriteOutput(output);
    }
}
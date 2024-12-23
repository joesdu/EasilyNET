using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Local

namespace EasilyNET.Core.Commons;

[SupportedOSPlatform(nameof(OSPlatform.Windows))]
internal static partial class WinApis
{
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    /// <summary>
    ///     <para xml:lang="en">Gets the standard output handle</para>
    ///     <para xml:lang="zh">获取标准输出句柄</para>
    /// </summary>
    /// <param name="nStdHandle">
    ///     <para xml:lang="en">Standard device handle</para>
    ///     <para xml:lang="zh">标准设备句柄</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Standard output handle</para>
    ///     <para xml:lang="zh">标准输出句柄</para>
    /// </returns>
    [LibraryImport(WinLibName.Kernel32, EntryPoint = "GetStdHandle")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.SysInt)]
    private static partial nint GetStdHandle(int nStdHandle);

    /// <summary>
    ///     <para xml:lang="en">Gets the console mode</para>
    ///     <para xml:lang="zh">获取控制台模式</para>
    /// </summary>
    /// <param name="hConsoleHandle">
    ///     <para xml:lang="en">Console handle</para>
    ///     <para xml:lang="zh">控制台句柄</para>
    /// </param>
    /// <param name="lpMode">
    ///     <para xml:lang="en">Console mode</para>
    ///     <para xml:lang="zh">控制台模式</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Whether the operation was successful</para>
    ///     <para xml:lang="zh">是否成功</para>
    /// </returns>
    [LibraryImport(WinLibName.Kernel32, EntryPoint = "GetConsoleMode")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    /// <summary>
    ///     <para xml:lang="en">Sets the console mode</para>
    ///     <para xml:lang="zh">设置控制台模式</para>
    /// </summary>
    /// <param name="hConsoleHandle">
    ///     <para xml:lang="en">Console handle</para>
    ///     <para xml:lang="zh">控制台句柄</para>
    /// </param>
    /// <param name="dwMode">
    ///     <para xml:lang="en">Console mode</para>
    ///     <para xml:lang="zh">控制台模式</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Whether the operation was successful</para>
    ///     <para xml:lang="zh">是否成功</para>
    /// </returns>
    [LibraryImport(WinLibName.Kernel32, EntryPoint = "SetConsoleMode")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    /// <summary>
    ///     <para xml:lang="en">Gets the console screen buffer info</para>
    ///     <para xml:lang="zh">获取控制台屏幕缓冲区信息</para>
    /// </summary>
    /// <param name="hConsoleOutput">
    ///     <para xml:lang="en">Console output handle</para>
    ///     <para xml:lang="zh">控制台输出句柄</para>
    /// </param>
    /// <param name="lpConsoleScreenBufferInfo">
    ///     <para xml:lang="en">Console screen buffer info</para>
    ///     <para xml:lang="zh">控制台屏幕缓冲区信息</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Whether the operation was successful</para>
    ///     <para xml:lang="zh">是否成功</para>
    /// </returns>
    [LibraryImport(WinLibName.Kernel32, EntryPoint = "GetConsoleScreenBufferInfo")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    /// <summary>
    ///     <para xml:lang="en">Determines whether the terminal supports ANSI escape sequences</para>
    ///     <para xml:lang="zh">判断终端是否支持ANSI转义序列</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">Whether ANSI escape sequences are supported</para>
    ///     <para xml:lang="zh">是否支持</para>
    /// </returns>
    internal static bool IsAnsiSupported()
    {
        var handle = GetStdHandle(STD_OUTPUT_HANDLE);
        if (!GetConsoleMode(handle, out var mode))
        {
            return false;
        }
        mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        return SetConsoleMode(handle, mode);
    }

    /// <summary>
    ///     <para xml:lang="en">Determines whether the terminal supports setting the cursor position</para>
    ///     <para xml:lang="zh">判断终端是否支持设置光标位置</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">Whether setting the cursor position is supported</para>
    ///     <para xml:lang="zh">是否支持</para>
    /// </returns>
    internal static bool IsConsoleCursorPositionSupported()
    {
        var handle = GetStdHandle(STD_OUTPUT_HANDLE);
        return GetConsoleScreenBufferInfo(handle, out _);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public short wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }
}
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
    /// 获取标准输出句柄
    /// </summary>
    /// <param name="nStdHandle">标准设备句柄</param>
    /// <returns>标准输出句柄</returns>
    [LibraryImport(WinLibName.Kernel32, EntryPoint = "GetStdHandle")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.SysInt)]
    private static partial nint GetStdHandle(int nStdHandle);

    /// <summary>
    /// 获取控制台模式
    /// </summary>
    /// <param name="hConsoleHandle">控制台句柄</param>
    /// <param name="lpMode">控制台模式</param>
    /// <returns>是否成功</returns>
    [LibraryImport(WinLibName.Kernel32, EntryPoint = "GetConsoleMode")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    /// <summary>
    /// 设置控制台模式
    /// </summary>
    /// <param name="hConsoleHandle">控制台句柄</param>
    /// <param name="dwMode">控制台模式</param>
    /// <returns>是否成功</returns>
    [LibraryImport(WinLibName.Kernel32, EntryPoint = "SetConsoleMode")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    /// <summary>
    /// 获取控制台屏幕缓冲区信息
    /// </summary>
    /// <param name="hConsoleOutput">控制台输出句柄</param>
    /// <param name="lpConsoleScreenBufferInfo">控制台屏幕缓冲区信息</param>
    /// <returns>是否成功</returns>
    [LibraryImport(WinLibName.Kernel32, EntryPoint = "GetConsoleScreenBufferInfo")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    /// <summary>
    /// 判断终端是否支 ANSI 转义序列
    /// </summary>
    /// <returns>是否支持</returns>
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
    /// 判断终端是否支持设置光标位置
    /// </summary>
    /// <returns>是否支持</returns>
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
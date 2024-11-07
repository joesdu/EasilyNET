using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using EasilyNET.Core.Commons;

namespace WpfAutoDISample.Common;

internal static partial class WinApis
{
    private const int SW_RESTORE = 9;

    //private const string GlobalMutexName = "DeepLogic.SourceDebug";
    internal static readonly string GlobalMutexName = Assembly.GetExecutingAssembly().GetName().Name ?? "WpfAutoDISample";
    internal static Mutex _mutex = default!;

    /// <summary>
    /// 将指定窗口设置为前台窗口。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <returns>如果窗口被成功设置为前台窗口，则返回true；否则返回false。</returns>
    [LibraryImport(WinLibName.User32, EntryPoint = "SetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// 设置窗口的显示状态。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="nCmdShow">指定窗口的显示状态的标志。可以是SW_RESTORE等值。</param>
    /// <returns>如果窗口显示状态被成功设置，则返回true；否则返回false。</returns>
    [LibraryImport(WinLibName.User32, EntryPoint = "ShowWindowAsync")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// 判断指定窗口是否最小化。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <returns>如果窗口最小化，则返回true；否则返回false。</returns>
    [LibraryImport(WinLibName.User32, EntryPoint = "IsIconic")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(IntPtr hWnd);

    /// <summary>
    /// 确保应用程序只运行一个实例
    /// </summary>
    /// <returns></returns>
    internal static void EnsureSingleInstance(out bool createdNew)
    {
        _mutex = new(true, GlobalMutexName, out createdNew);
    }

    /// <summary>
    /// 查找已运行的实例并将其前置
    /// </summary>
    internal static void BringExistingInstanceToFront()
    {
        // 查找已经运行的实例的主窗口
        var currentProcess = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
        {
            if (process.Id == currentProcess.Id) continue;
            var hWnd = process.MainWindowHandle;
            if (IsIconic(hWnd))
            {
                _ = ShowWindowAsync(hWnd, SW_RESTORE);
            }
            _ = SetForegroundWindow(hWnd);
            break;
        }
    }
}
using EasilyNET.Core.Misc;
using Microsoft.Extensions.Hosting;
using WinFormAutoDISample.Common;
using WinFormAutoDISample.Properties;

namespace WinFormAutoDISample;

#pragma warning disable IDE0032 // 使用自动属性

internal sealed class App : ApplicationContext
{
    private static App? _current;

    private App(ref IHost host)
    {
        Host = host;
        Application.ApplicationExit += OnApplicationExit;
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static App Current => _current ?? throw new InvalidOperationException("应用程序尚未初始化");

    private IHost Host { get; }

    /// <summary>
    /// Services
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceProvider Services => CurrentAppHost.Services;

    private static IHost CurrentAppHost => Current.Host ?? throw new InvalidOperationException("无法获取AppHost，当前Application实例不是App类型。");

    public static void Initialize(ref IHost host)
    {
        _current = new(ref host);
    }

    private void OnApplicationExit(object? sender, EventArgs e)
    {
        WinApis._mutex.ReleaseMutex();
        Host.StopAsync().GetAwaiter().GetResult();
    }

    private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
    {
        HandleException(e.Exception);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        HandleException(e.ExceptionObject as Exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.SetObserved();
    }

    private static void HandleException(Exception? ex)
    {
        if (ex is null) return;
        var errorMessage = AppResource.Program_HandleHostInitializationAsync_应用程序启动时发生错误X.Format(ex.Message);
        MessageBox.Show(errorMessage, AppResource.Program_HandleHostInitializationAsync_错误, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
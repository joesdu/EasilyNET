using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WpfAutoDISample.Common;
using WpfAutoDISample.Views;

// ReSharper disable AsyncVoidMethod

namespace WpfAutoDISample;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private readonly ILogger<App> _logger;

    /// <inheritdoc />
    public App(ref IHost host)
    {
        InitializeComponent();
        Host = host;
        _logger = Services.GetRequiredService<ILogger<App>>();
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Visibility = Visibility.Visible;
    }

    private IHost Host { get; }

    /// <summary>
    /// Services
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceProvider Services => CurrentAppHost.Services;

    /// <summary>
    /// 获取当前AppHost
    /// </summary>
    private static IHost CurrentAppHost => (Current as App)?.Host ?? throw new InvalidOperationException("无法获取AppHost，当前Application实例不是App类型。");

    /// <inheritdoc />
    protected override async void OnExit(ExitEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException -= CurrentDomainUnhandledException;
        DispatcherUnhandledException -= AppDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException -= TaskSchedulerOnUnobservedTaskException;
        await Host.StopAsync().ConfigureAwait(false);
        Host.Dispose();
        // 这里不要忘记释放
        WinApis._mutex.ReleaseMutex();
        Shutdown();
        base.OnExit(e);
    }

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        DispatcherUnhandledException += AppDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        await Host.StartAsync().ConfigureAwait(false);
        base.OnStartup(e);
    }

    private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        HandleException(e.ExceptionObject as Exception);
    }

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.SetObserved();
    }

    private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.Handled = true;
    }

    private void HandleException(Exception? exception)
    {
        if (exception is null) return;
        _logger.LogError(exception, "Unhandled exception occurred.");
        //MessageBox.Show("发生未处理的异常，程序将继续运行。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
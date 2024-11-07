using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using WpfAutoDISample.Common;
using WpfAutoDISample.Views;

namespace WpfAutoDISample;

/// <summary>
/// Program
/// </summary>
public static class Program
{
    /// <summary>
    /// Main入口函数
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [STAThread]
    public static void Main(string[] args)
    {
        WinApis.EnsureSingleInstance(out var createdNew);
        if (!createdNew)
        {
            WinApis.BringExistingInstanceToFront();
            // 退出当前实例
            return;
        }
        ShowLoadingScreen(out var loading);
        var dispatcher = Dispatcher.CurrentDispatcher;
        InitializeHostAsync(args).ContinueWith(async t =>
                await dispatcher.InvokeAsync(async () =>
                    await HandleHostInitializationAsync(t, loading).ConfigureAwait(false)),
            TaskScheduler.Default);

        // 启动WPF消息循环
        Dispatcher.Run();
    }

    /// <summary>
    /// 显示加载屏幕
    /// </summary>
    /// <param name="loading"></param>
    private static void ShowLoadingScreen(out Loading loading)
    {
        loading = new();
        loading.Show();
    }

    /// <summary>
    /// 异步初始化主机
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private static Task<IHost> InitializeHostAsync(string[] args)
    {
        return Task.Factory.StartNew(() =>
        {
            // 创建一个通用主机
            var host = CreateHostBuilder(args).Build();
            host.InitializeApplication();
            return host;
        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    /// <summary>
    /// 处理主机初始化的结果
    /// </summary>
    /// <param name="task"></param>
    /// <param name="loading"></param>
    /// <returns></returns>
    private static async Task HandleHostInitializationAsync(Task<IHost> task, Loading loading)
    {
        if (task.Exception is not null)
        {
            // 处理异常
            MessageBox.Show($"应用程序启动时发生错误: {task.Exception.InnerException?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            loading.Close();
            return;
        }
        var host = await task.ConfigureAwait(false);
        // 配置主窗口
        var app = new App(ref host);
        // 关闭加载窗口
        loading.Close();
        app.Run();
    }

    private static IHostBuilder CreateHostBuilder(params string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(c =>
            {
                c.SetBasePath(AppContext.BaseDirectory);
                c.AddJsonFile("appsettings.json", false, false);
            })
            .ConfigureLogging((hbc, lb) =>
            {
                var logger = new LoggerConfiguration()
                             .ReadFrom.Configuration(hbc.Configuration)
                             .Enrich.FromLogContext()
                             .WriteTo.Async(wt =>
                             {
                                 if (hbc.HostingEnvironment.IsDevelopment())
                                 {
                                     wt.Debug();
                                 }
                                 if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && SysHelper.IsCurrentUserAdmin())
                                 {
                                     // 当为Windows系统时,添加事件日志,需要管理员权限才能写入Windows事件查看器
                                     // 避免日志信息过多,仅将错误日志写入系统事件查看器
                                     wt.EventLog(WinApis.GlobalMutexName, manageEventSource: true, restrictedToMinimumLevel: LogEventLevel.Error);
                                 }
                                 wt.Map(le => (le.Timestamp.DateTime, le.Level), (key, log) =>
                                     log.Async(o => o.File($"logs{Path.DirectorySeparatorChar}{key.Level}{Path.DirectorySeparatorChar}.log",
                                         shared: true,
                                         rollingInterval: RollingInterval.Day)));
                             }).CreateLogger();
                lb.ClearProviders();
                lb.AddSerilog(logger, true);
            })
            .ConfigureServices(sc =>
            {
                sc.AddApplicationModules<AppServiceModules>();
                sc.AddSingleton<WeakReferenceMessenger>();
                sc.AddSingleton<IMessenger, WeakReferenceMessenger>(sp => sp.GetRequiredService<WeakReferenceMessenger>());
                sc.AddSingleton(_ => Application.Current.Dispatcher);
            });
}
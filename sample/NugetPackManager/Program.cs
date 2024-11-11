using EasilyNET.Core.Misc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using WinFormAutoDISample.Common;
using WinFormAutoDISample.Listeners;
using WinFormAutoDISample.Messenger;
using WinFormAutoDISample.Properties;
using WinFormAutoDISample.Views;

// ReSharper disable AsyncVoidMethod

namespace WinFormAutoDISample;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        WinApis.EnsureSingleInstance(out var createdNew);
        if (!createdNew)
        {
            WinApis.BringExistingInstanceToFront();
            return;
        }
        ApplicationConfiguration.Initialize();
        ShowLoadingScreen(out var loading);
        InitializeHostAsync().ContinueWith(async t => await HandleHostInitializationAsync(t, loading), TaskScheduler.FromCurrentSynchronizationContext());
        Application.Run();
    }

    /// <summary>
    /// 显示加载屏幕
    /// </summary>
    /// <param name="loading"></param>
    private static void ShowLoadingScreen(out LoadingForm loading)
    {
        loading = new();
        loading.Show();
    }

    /// <summary>
    /// 异步初始化主机
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private static Task<IHost> InitializeHostAsync(params string[] args)
    {
        return Task.Factory.StartNew(() =>
        {
            // 创建一个通用主机
            var host = CreateHostBuilder(args).Build();
            host.InitializeApplication();
            App.Initialize(ref host);
            return host;
        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    /// <summary>
    /// 处理主机初始化的结果
    /// </summary>
    /// <param name="task"></param>
    /// <param name="loading"></param>
    /// <returns></returns>
    private static async Task HandleHostInitializationAsync(Task<IHost> task, LoadingForm loading)
    {
        if (task.Exception is not null)
        {
            // 处理异常
            var errorMessage = AppResource.Program_HandleHostInitializationAsync_应用程序启动时发生错误X.Format(task.Exception.InnerException?.Message);
            MessageBox.Show(errorMessage, AppResource.Program_HandleHostInitializationAsync_错误, MessageBoxButtons.OK);
            loading.Close();
            return;
        }
        var host = await task.ConfigureAwait(false);
        await host.StartAsync();
        // 关闭加载窗口
        loading.Close();
        // 启动主窗体
        var mainForm = host.Services.GetRequiredService<MainForm>();
        mainForm.FormClosed += MainForm_FormClosed;
        mainForm.Show();
    }

    private static void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        Application.Exit();
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
                sc.AddSingleton<IMessenger>(new Messenger.Messenger());
                sc.AddSingleton<PerformanceListener>();
            });
}
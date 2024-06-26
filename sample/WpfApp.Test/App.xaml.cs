using CommunityToolkit.Mvvm.Messaging;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;

// ReSharper disable RedundantExtendsListEntry

namespace WpfApp.Test;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Main入口函数
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [STAThread]
    public static void Main(string[] args)
    {
        // 不包含的程序集
        AssemblyHelper.AddExcludeLibs(nameof(MicaWPF));
        // 创建一个通用主机
        using var host = CreateHostBuilder(args).Build();
        host.InitializeApplication();
        host.StartAsync().ConfigureAwait(true).GetAwaiter().GetResult();

        // 配置主窗口
        var app = new App();
        app.InitializeComponent();
        app.MainWindow = host.Services.GetRequiredService<MainWindow>();
        app.MainWindow.Visibility = Visibility.Visible;
        app.Run();
        host.StopAsync().ConfigureAwait(true).GetAwaiter().GetResult();
    }

    private static IHostBuilder CreateHostBuilder(params string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(c =>
            {
                var basePath =
                    Path.GetDirectoryName(AppContext.BaseDirectory) ?? throw new DirectoryNotFoundException("Unable to find the base directory of the application.");
                _ = c.SetBasePath(basePath);
            })
            .ConfigureServices(sc =>
            {
                sc.AddApplicationModules<AppServiceModules>();
                sc.AddSingleton<WeakReferenceMessenger>();
                sc.AddSingleton<IMessenger, WeakReferenceMessenger>(provider => provider.GetRequiredService<WeakReferenceMessenger>());
                sc.AddSingleton(_ => Current.Dispatcher);
            });
}
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;
using Wpf.Ui;

// ReSharper disable RedundantExtendsListEntry

namespace WpfApp.Test;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly IHost _host = Host.CreateDefaultBuilder()
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
                                                  sc.AddTransient<ISnackbarService>(_ => new SnackbarService());
                                              })
                                              .Build();

    /// <summary>
    /// Main入口函数
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new App();
        app.InitializeComponent();
        app.MainWindow = _host.Services.GetRequiredService<MainWindow>();
        app.MainWindow.Visibility = Visibility.Visible;
        app.Startup += App_Startup;
        app.Exit += App_Exit;
        app.Run();
    }

    private static async void App_Exit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync().ConfigureAwait(true);
    }

    private static async void App_Startup(object sender, StartupEventArgs e)
    {
        _host.InitializeApplication();
        await _host.StartAsync().ConfigureAwait(true);
    }
}
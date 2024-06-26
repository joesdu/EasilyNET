using EasilyNET.AutoDependencyInjection.Core.Attributes;
using MicaWPF.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WpfApp.Test.ViewModels;

namespace WpfApp.Test;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public partial class MainWindow : MicaWindow
{
    private readonly MainWindowModel _model;

    /// <inheritdoc />
    public MainWindow(MainWindowModel mwm)
    {
        InitializeComponent();
        _model = mwm;
        DataContext = mwm;
    }

    private async void OnBtnClick(object sender, RoutedEventArgs e)
    {
        await Task.Factory.StartNew(async () =>
        {
            await Dispatcher.InvokeAsync(() =>
            {
                _model.Message = "Clicked";
                return Task.CompletedTask;
            });
        });
    }
}
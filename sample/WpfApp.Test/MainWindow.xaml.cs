using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using WpfApp.Test.ViewModels;

namespace WpfApp.Test;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public partial class MainWindow
{
    /// <inheritdoc />
    public MainWindow(MainWindowModel mwm)
    {
        InitializeComponent();
        DataContext = mwm;
    }
}
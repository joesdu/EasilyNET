using CommunityToolkit.Mvvm.ComponentModel;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable PartialTypeWithSinglePart

// ReSharper disable ClassNeverInstantiated.Global

namespace WpfAutoDISample.ViewModels;

/// <inheritdoc cref="ObservableObject" />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public partial class MainWindowModel : ObservableObject
{
    /// <summary>
    /// Message
    /// </summary>
    [ObservableProperty]
    private string message = "Hello WPF!";
}
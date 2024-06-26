using CommunityToolkit.Mvvm.ComponentModel;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ClassNeverInstantiated.Global

namespace WpfApp.Test.ViewModels;

/// <inheritdoc />
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public sealed class MainWindowModel : ObservableObject
{
    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = "Hello WPF!";
}
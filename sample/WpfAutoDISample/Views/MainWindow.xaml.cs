using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WpfAutoDISample.Common;
using WpfAutoDISample.Events;
using WpfAutoDISample.Models;
using WpfAutoDISample.ViewModels;

// ReSharper disable UnusedMember.Global

namespace WpfAutoDISample.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton, AddSelf = true)]
public partial class MainWindow
{
    private const string CollectionName = nameof(MainWindow);
    private readonly ILiteCollection<MainWindowState> _coll;
    private readonly ILiteDatabase _db;
    private readonly ILogger<MainWindow> _logger;
    private readonly IMessenger _messenger;

    /// <inheritdoc />
    public MainWindow(MainWindowModel mwm, ILogger<MainWindow> logger, IMessenger messenger, [FromKeyedServices(Constant.UiConfigServiceKey)] ILiteDatabase db)
    {
        InitializeComponent();
        DataContext = mwm;
        _logger = logger;
        _messenger = messenger;
        _db = db;
        _coll = _db.GetCollection<MainWindowState>(CollectionName);
        SizeChanged += MainWindow_SizeChanged;
        SourceInitialized += OnSourceInitialized;
        Closing += MainWindow_Closing;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 获取当前运行时的版本信息
        var version = Environment.Version;
        if (DataContext is MainWindowModel mv)
        {
            mv.Message = $"Hello WPF! 当前使用 .NET 版本: {version}";
        }
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // 获取当前窗体的尺寸
        var width = ActualWidth;
        var height = ActualHeight;
        _logger.LogInformation("MainWindow size changed: Width={Width}, Height={Height}", width, height);
        // 发布事件并传递尺寸信息
        _messenger.Send(new MainWindowSizeChangeEventArgs(width, height));
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            // 尝试获取窗口状态
            var state = _coll.FindById(new(new ObjectId(Uid)));
            if (state is null) return;
            // 应用窗口状态
            Width = state.Width;
            Height = state.Height;
            Top = state.Top;
            Left = state.Left;
        }
        catch (Exception)
        {
            _db.DropCollection(CollectionName);
        }
        _logger.LogInformation("Restored MainWindow state: Width={Width}, Height={Height}, Top={Top}, Left={Left}", Width, Height, Top, Left);
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // 保存窗口状态
        _coll.Upsert(new MainWindowState
        {
            Id = new(Uid),
            Width = Width,
            Height = Height,
            Top = Top,
            Left = Left
        });
        _logger.LogInformation("Saved MainWindow state: Width={Width}, Height={Height}, Top={Top}, Left={Left}", Width, Height, Top, Left);
    }
}
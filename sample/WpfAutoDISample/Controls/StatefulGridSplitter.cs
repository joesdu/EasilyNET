using CommunityToolkit.Mvvm.Messaging;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using WpfAutoDISample.Common;
using WpfAutoDISample.Enums;
using WpfAutoDISample.Events;
using WpfAutoDISample.Models;

namespace WpfAutoDISample.Controls;

/// <summary>
/// 使用LiteDB存储GridSplitter的状态,请设置Uid属性以唯一标识GridSplitter,Uid应该是一个ObjectId字符串
/// </summary>
public sealed class StatefulGridSplitter : GridSplitter, IDisposable
{
    private const string CollectionName = nameof(StatefulGridSplitter);
    private readonly ILiteCollection<GridSplitterState> _coll;
    private readonly IMessenger _messenger;
    private readonly ILiteDatabase db;
    private bool _disposed;

    public StatefulGridSplitter()
    {
        // 设置默认背景色
        Background = new SolidColorBrush(Color.FromRgb(99, 99, 99));
        db = App.Services.GetRequiredKeyedService<ILiteDatabase>(Constant.UiConfigServiceKey);
        _coll = db.GetCollection<GridSplitterState>(CollectionName);
        Loaded += StatefulGridSplitter_Loaded;
        DragCompleted += StatefulGridSplitter_DragCompleted;
        _messenger = App.Services.GetRequiredService<IMessenger>();
        _messenger.Register<StatefulGridSplitter, MainWindowSizeChangeEventArgs>(this, Handler);
    }

    /// <summary>
    /// 用来表示GridSplitter的最小范围,为了不让Grid拖动到不可见区域,默认为0.1
    /// </summary>
    public required double MinRatio { get; set; } = 0.1;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Handler(object recipient, MainWindowSizeChangeEventArgs message)
    {
        if (Parent is not Grid parentGrid) return;
        // 获取GridSplitter的当前列和行位置
        var columnIndex = Grid.GetColumn(this);
        var rowIndex = Grid.GetRow(this);
        var orientation = DetermineOrientation();
        if (orientation == GridSplitterOrientation.Vertical)
        {
            // 当为垂直方向时，当前列索引即为控制的列索引
            parentGrid.ColumnDefinitions[columnIndex - 1].MinWidth = message.Width * MinRatio;
            parentGrid.ColumnDefinitions[columnIndex + 1].MinWidth = message.Width * MinRatio;
            parentGrid.ColumnDefinitions[columnIndex - 1].MaxWidth = message.Width * (1 - MinRatio);
            parentGrid.ColumnDefinitions[columnIndex + 1].MaxWidth = message.Width * (1 - MinRatio);
        }
        else
        {
            // 当为水平方向时，当前行索引即为控制的列索引
            parentGrid.RowDefinitions[rowIndex - 1].MinHeight = message.Height * MinRatio;
            parentGrid.RowDefinitions[rowIndex + 1].MinHeight = message.Height * MinRatio;
            parentGrid.RowDefinitions[rowIndex - 1].MaxHeight = message.Height * (1 - MinRatio);
            parentGrid.RowDefinitions[rowIndex + 1].MaxHeight = message.Height * (1 - MinRatio);
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            Loaded -= StatefulGridSplitter_Loaded;
            DragCompleted -= StatefulGridSplitter_DragCompleted;
            db.Dispose();
            _messenger.Unregister<MainWindowSizeChangeEventArgs>(this);
        }

        // 释放非托管资源（如果有）
        _disposed = true;
    }

    ~StatefulGridSplitter()
    {
        Dispose(false);
    }

    private void StatefulGridSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (Parent is not Grid parentGrid || string.IsNullOrEmpty(Uid)) return;
        var orientation = DetermineOrientation();
        double realValue;
        switch (orientation)
        {
            case GridSplitterOrientation.Vertical:
            {
                var gridColumnIndex = Grid.GetColumn(this);
                realValue = parentGrid.ColumnDefinitions[gridColumnIndex - 1].Width.Value;
                break;
            }
            case GridSplitterOrientation.Horizontal:
            {
                var gridRowIndex = Grid.GetRow(this);
                realValue = parentGrid.RowDefinitions[gridRowIndex - 1].Height.Value;
                break;
            }
            case GridSplitterOrientation.Unknown:
                realValue = parentGrid.ActualWidth * MinRatio;
                break;
            default:
                throw new("未知GridSplitterOrientation状态");
        }
        _coll.Upsert(new GridSplitterState
        {
            Id = new(Uid),
            Value = realValue
        });
    }

    private GridSplitterOrientation DetermineOrientation()
    {
        // 如果Height不是自动（即有明确的值），且Width是自动（Double.NaN），则认为是水平分割器
        if (!double.IsNaN(Height) && double.IsNaN(Width))
        {
            return GridSplitterOrientation.Horizontal;
        }
        // 如果Width不是自动（即有明确的值），且Height是自动（Double.NaN），则认为是垂直分割器
        if (!double.IsNaN(Width) && double.IsNaN(Height))
        {
            return GridSplitterOrientation.Vertical;
        }
        // 如果无法确定，返回"Unknown"
        return GridSplitterOrientation.Unknown;
    }

    private void StatefulGridSplitter_Loaded(object sender, RoutedEventArgs e)
    {
        if (Parent is not Grid parentGrid || string.IsNullOrEmpty(Uid)) return;
        // 获取GridSplitter的当前列和行位置
        var columnIndex = Grid.GetColumn(this);
        var rowIndex = Grid.GetRow(this);
        var orientation = DetermineOrientation();
        var realValue = orientation is GridSplitterOrientation.Vertical
                            ? parentGrid.ActualWidth * MinRatio
                            : parentGrid.ActualHeight * (1 - MinRatio);
        try
        {
            var config = _coll.FindById(new(new ObjectId(Uid)));
            realValue = config?.Value ?? realValue;
        }
        catch (Exception)
        {
            // 当获取数据出现异常,表明数据结构可能发生了变化,删除原有的集合,当下次加载时会重新创建正确的数据结构
            db.DropCollection(CollectionName);
        }
        if (orientation == GridSplitterOrientation.Vertical)
        {
            // 当为垂直方向时，当前列索引即为控制的列索引
            parentGrid.ColumnDefinitions[columnIndex - 1].Width = new(realValue, GridUnitType.Pixel);
            parentGrid.ColumnDefinitions[columnIndex - 1].MinWidth = parentGrid.ActualWidth * MinRatio;
            parentGrid.ColumnDefinitions[columnIndex + 1].MinWidth = parentGrid.ActualWidth * MinRatio;
            parentGrid.ColumnDefinitions[columnIndex - 1].MaxWidth = parentGrid.ActualWidth * (1 - MinRatio);
            //parentGrid.ColumnDefinitions[columnIndex + 1].MaxWidth = parentGrid.ActualWidth * (1 - MinRatio);
        }
        else
        {
            // 当为水平方向时，当前行索引即为控制的列索引
            parentGrid.RowDefinitions[rowIndex - 1].Height = new(realValue, GridUnitType.Pixel);
            parentGrid.RowDefinitions[rowIndex - 1].MinHeight = parentGrid.ActualHeight * MinRatio;
            parentGrid.RowDefinitions[rowIndex + 1].MinHeight = parentGrid.ActualHeight * MinRatio;
            parentGrid.RowDefinitions[rowIndex - 1].MaxHeight = parentGrid.ActualHeight * (1 - MinRatio);
            //parentGrid.RowDefinitions[rowIndex + 1].MaxHeight = parentGrid.ActualHeight * (1 - MinRatio);
        }
    }
}
using LiteDB;

namespace WpfAutoDISample.Models;

internal sealed class MainWindowState
{
    [BsonId]
    public required ObjectId Id { get; set; } // 使用固定ID，因为只存储一个窗口状态

    public double Width { get; set; }

    public double Height { get; set; }

    public double Top { get; set; }

    public double Left { get; set; }
}
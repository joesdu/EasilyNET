// ReSharper disable ClassNeverInstantiated.Global

namespace WpfAutoDISample.Events;

internal sealed class MainWindowSizeChangeEventArgs(double width, double height) : EventArgs
{
    public double Width { get; } = width;

    public double Height { get; } = height;
}
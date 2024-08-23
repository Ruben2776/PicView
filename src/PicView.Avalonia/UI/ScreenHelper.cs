using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Platform;

namespace PicView.Avalonia.UI;

public readonly struct ScreenSize(int width, int height, int workingAreaWidth, int workingAreaHeight, double scaling)
{
    public int Width { get; init; } = width;
    public int Height { get; init; } = height;
    public int WorkingAreaWidth { get; init; } = workingAreaWidth;
    public int WorkingAreaHeight { get; init; } = workingAreaHeight;
    public double Scaling { get; init; } = scaling;
}

public static class ScreenHelper
{
    public static ScreenSize ScreenSize { get; set; }
    public static ScreenSize GetScreenSize(Window window)
    {
        var screen = window.Screens.ScreenFromWindow(window);
        
        return new ScreenSize
        {
            Width = screen.Bounds.Width,
            Height = screen.Bounds.Height,
            WorkingAreaWidth = screen.WorkingArea.Width,
            WorkingAreaHeight = screen.WorkingArea.Height,
            Scaling = screen.Scaling
        };
    }
}

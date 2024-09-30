using Avalonia.Controls;

namespace PicView.Avalonia.UI;

public readonly struct ScreenSize(int width, int height, int workingAreaWidth, int workingAreaHeight, int x, int y, double scaling)
{
    public int Width { get; init; } = width;
    public int Height { get; init; } = height;
    public double WorkingAreaWidth { get; init; } = workingAreaWidth;
    public double WorkingAreaHeight { get; init; } = workingAreaHeight;
    public double Scaling { get; init; } = scaling;
    
    public int X { get; init; }
    public int Y { get; init; }
}

public static class ScreenHelper
{
    public static ScreenSize ScreenSize { get; set; }
    public static ScreenSize GetScreenSize(Window window)
    {
        var screen = window.Screens.ScreenFromWindow(window);
        
        var monitorWidth = screen.WorkingArea.Width / screen.Scaling;
        var monitorHeight = screen.WorkingArea.Height / screen.Scaling;

        
        return new ScreenSize
        {
            Width = screen.Bounds.Width,
            Height = screen.Bounds.Height,
            WorkingAreaWidth = monitorWidth,
            WorkingAreaHeight = monitorHeight,
            X = screen.Bounds.X,
            Y = screen.Bounds.Y,
            Scaling = screen.Scaling
        };
    }
}

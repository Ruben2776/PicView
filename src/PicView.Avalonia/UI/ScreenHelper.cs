using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Platform;

namespace PicView.Avalonia.UI;

public struct ScreenSize(int width, int height, int workingAreaWidth, int workingAreaHeight, double scaling)
{
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;
    public int WorkingAreaWidth { get; set; } = workingAreaWidth;
    public int WorkingAreaHeight { get; set; } = workingAreaHeight;
    public double Scaling { get; set; } = scaling;
}

public static class ScreenHelper
{
    public static ScreenSize ScreenSize { get; set; }
    public static ScreenSize GetScreenSize(Control control)
    {
        var screen = GetScreen(control);
        
        return new ScreenSize
        {
            Width = screen.Bounds.Width,
            Height = screen.Bounds.Height,
            WorkingAreaWidth = screen.WorkingArea.Width,
            WorkingAreaHeight = screen.WorkingArea.Height,
            Scaling = screen.Scaling
        };
    }
    public static Screen? GetScreen(Control control)
    {
        var window = control.GetSelfAndLogicalAncestors().OfType<Window>().First();
        
        return window.Screens.ScreenFromPoint(new PixelPoint());
    }
}

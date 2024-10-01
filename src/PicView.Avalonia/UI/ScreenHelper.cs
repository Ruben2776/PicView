using Avalonia.Controls;

namespace PicView.Avalonia.UI;

public class ScreenSize(double workingAreaWidth, double workingAreaHeight, double scaling)
{
    public double WorkingAreaWidth { get; init; } = workingAreaWidth;
    public double WorkingAreaHeight { get; init; } = workingAreaHeight;
    public double Scaling { get; init; } = scaling;
}

public static class ScreenHelper
{
    public static ScreenSize? ScreenSize { get; set; }
    public static ScreenSize? GetScreenSize(Window window)
    {
        var screen = window.Screens.ScreenFromVisual(window);
        
        var monitorWidth = screen.Bounds.Width / screen.Scaling;
        var monitorHeight = screen.Bounds.Height / screen.Scaling;

        
        return new ScreenSize(monitorWidth, monitorHeight, screen.Scaling)
        {
            WorkingAreaWidth = monitorWidth,
            WorkingAreaHeight = monitorHeight,
            Scaling = screen.Scaling,
        };
    }
}

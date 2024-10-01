using Avalonia.Controls;

namespace PicView.Avalonia.UI;
public readonly struct ScreenSize(double workingAreaWidth, double workingAreaHeight, double scaling)
{
    public double WorkingAreaWidth { get; init; } = workingAreaWidth;
    public double WorkingAreaHeight { get; init; } = workingAreaHeight;
    public double Scaling { get; init; } = scaling;
}

public static class ScreenHelper
{
    private static readonly Lock _lock = new();
    public static ScreenSize ScreenSize { get; private set; }
    public static void UpdateScreenSize(Window window)
    {
        // Need to lock it to prevent multiple calls
        lock (_lock)
        {
            var screen = window.Screens.ScreenFromVisual(window);
        
            var monitorWidth = screen.Bounds.Width / screen.Scaling;
            var monitorHeight = screen.Bounds.Height / screen.Scaling;
        
            ScreenSize = new ScreenSize(monitorWidth, monitorHeight, screen.Scaling)
            {
                WorkingAreaWidth = monitorWidth,
                WorkingAreaHeight = monitorHeight,
                Scaling = screen.Scaling,
            };
        }
    }
}

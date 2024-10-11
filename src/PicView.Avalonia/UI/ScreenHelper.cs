using Avalonia.Controls;

namespace PicView.Avalonia.UI;

public readonly record struct ScreenSize
{
    public double WorkingAreaWidth { get; init; }
    public double WorkingAreaHeight { get; init; }
    public double Scaling { get; init; }
}

public static class ScreenHelper
{
    private static readonly Lock Lock = new();
    public static ScreenSize ScreenSize { get; private set; }

    public static void UpdateScreenSize(Window window)
    {
        // TODO: Add support for dragging between multiple monitors
        // Dragging to monitor with different scaling (DPI) causes weird incorrect size behavior,
        // but starting the application works fine for either monitor, until you drag it to the other.
        // It works most of the time in debug mode, but not so much for AOT release
        
        // Need to lock it to prevent multiple calls
        lock (Lock)
        {
            var screen = window.Screens.ScreenFromVisual(window);
        
            var monitorWidth = screen.WorkingArea.Width / screen.Scaling;
            var monitorHeight = screen.WorkingArea.Height / screen.Scaling;
        
            ScreenSize = new ScreenSize           
            {
                WorkingAreaWidth = monitorWidth,
                WorkingAreaHeight = monitorHeight,
                Scaling = screen.Scaling,
            };
        }
    }
}
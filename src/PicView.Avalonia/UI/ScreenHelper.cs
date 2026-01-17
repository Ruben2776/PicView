using Avalonia.Controls;
using PicView.Core.Sizing;

namespace PicView.Avalonia.UI;

/// <summary>
/// Provides utilities for obtaining and managing screen information.
/// </summary>
public static class ScreenHelper
{
    private static readonly Lock Lock = new();
    
    /// <summary>
    /// Gets the current screen dimensions and scaling information.
    /// </summary>
    public static ScreenSize ScreenSize { get; private set; }

    /// <summary>
    /// Updates the <see cref="ScreenSize"/> property based on the window's current screen.
    /// </summary>
    /// <param name="window">The window to obtain screen information from.</param>
    /// <remarks>
    /// This method is thread-safe and uses a lock to prevent concurrent updates.
    /// 
    /// TODO: Add support for dragging between multiple monitors.
    /// Dragging to monitor with different scaling (DPI) causes weird incorrect size behavior,
    /// but starting the application works fine for either monitor, until you drag it to the other.
    /// </remarks>
    public static void UpdateScreenSize(Window window)
    {
        // Need to lock it to prevent multiple calls
        lock (Lock)
        {
            var screen = window.Screens.ScreenFromVisual(window);

            if (screen is null)
            {
                // Create dummy screen
                ScreenSize = new ScreenSize
                {
                    WorkingAreaWidth = 1920,
                    WorkingAreaHeight = 1050,
                    Width = 1920,
                    Height = 1080,
                    Scaling = 1,
                    X = 0,
                    Y = 0
                };
                return;
            }
        
            var monitorWidth = screen.WorkingArea.Width / screen.Scaling;
            var monitorHeight = screen.WorkingArea.Height / screen.Scaling;
            
            var width = screen.Bounds.Width / screen.Scaling;
            var height = screen.Bounds.Height / screen.Scaling;
        
            ScreenSize = new ScreenSize           
            {
                WorkingAreaWidth = monitorWidth,
                WorkingAreaHeight = monitorHeight,
                Width = width,
                Height = height,
                Scaling = screen.Scaling,
                X = screen.Bounds.X,
                Y = screen.Bounds.Y
            };
        }
    }

    public static int GetWindowMaxHeight()
    {
        return ScreenSize.WorkingAreaHeight switch
        {
            > 500 and < 600 => 500,
            > 600 and < 700 => 550,
            >= 650 and < 750 => 720,
            >= 750 => 750,
            _ => SizeDefaults.WindowMinSize
        };
    }
}
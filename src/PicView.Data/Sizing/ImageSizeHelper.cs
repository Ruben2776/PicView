using Avalonia;
using Avalonia.Controls;

namespace PicView.Data.Sizing;

public static class ImageSizeHelper
{
    public static Size GetScaledImageSize(double width, double height, Control control)
    {
        if (width <= 0 || height <= 0) { return new Size(0,0); }

        double maxWidth, maxHeight;
        var screen = Screen.ScreenHelper.GetScreen(control);

        var padding = screen.Bounds.Height - (screen.WorkingArea.Height - 55) + screen.Bounds.Width - screen.WorkingArea.Width;
        
        maxWidth = Math.Min(screen.WorkingArea.Width - padding, width);
        maxHeight = Math.Min(screen.WorkingArea.Height - padding, height);
        
        var aspectRatio = Math.Min(maxWidth / width, maxHeight / height);

        return new Size(width * aspectRatio, height * aspectRatio);

    }
}
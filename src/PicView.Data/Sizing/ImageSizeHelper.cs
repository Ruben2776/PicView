using Avalonia;
using Avalonia.Controls;

namespace PicView.Data.Sizing;

public static class ImageSize
{
    public static Size GetScaledImageSize(double width, double height, Control control)
    {
        if (width <= 0 || height <= 0) { return new Size(0,0); }

        double maxWidth, maxHeight;
        var screen = Screen.ScreenHelper.GetScreen(control);

        var padding = 70;
        
        maxWidth = Math.Min(screen.WorkingArea.Width - padding, width);
        maxHeight = Math.Min(screen.WorkingArea.Height - padding, height);
        
        var aspectRatio = Math.Min(maxWidth / width, maxHeight / height);

        return new Size(width * aspectRatio, height * aspectRatio);

    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace PicView.Data.Sizing;

public static class ImageSizeHelper
{
    public static double[] GetScaledImageSize(double width, double height, int rotation, Window window)
    {
        if (width <= 0 || height <= 0) { throw new InvalidOperationException(); }

        double maxWidth, maxHeight;
        var screen = Screen.ScreenHelper.GetScreen(window);

        var padding = 
            screen.Bounds.Height - (screen.WorkingArea.Height - 55) + screen.Bounds.Width - screen.WorkingArea.Width;
        
        maxWidth = Math.Min(screen.WorkingArea.Width - padding, width);
        maxHeight = Math.Min(screen.WorkingArea.Height - padding, height);
        
        var aspectRatio = Math.Min(maxWidth / width, maxHeight / height);
        
        var interfaceSize = 190;
        double titleMaxWidth = 0;
        
        if (Dispatcher.UIThread.CheckAccess())
        {
            var x = rotation is 0 or 180 ? Math.Max(maxWidth, window.MinWidth) : Math.Max(maxHeight, window.MinHeight);
            titleMaxWidth = x - interfaceSize < interfaceSize ? interfaceSize : x - interfaceSize;
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var x = rotation is 0 or 180 ? Math.Max(maxWidth, window.MinWidth) : Math.Max(maxHeight, window.MinHeight);
                titleMaxWidth = x - interfaceSize < interfaceSize ? interfaceSize : x - interfaceSize;
            }, Avalonia.Threading.DispatcherPriority.Normal).Wait();
        }
        
        return new[] { width * aspectRatio, height * aspectRatio, titleMaxWidth };
    }
}
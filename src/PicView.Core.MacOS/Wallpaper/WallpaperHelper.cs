using System.Diagnostics;
using PicView.Core.MacOS.AppleScripts;

namespace PicView.Core.MacOS.Wallpaper;

public static class WallpaperHelper
{
    public enum WallpaperStyle
    {
        FillScreen,
        FitToScreen,
        StretchToFillScreen,
        Center
    }

    public static async Task SetWallpaper(string imagePath)
    {
        // Convert backslashes to forward slashes for macOS paths
        imagePath = imagePath.Replace('\\', '/');

        var script = $"tell application \"System Events\" to tell every desktop to set picture to \"{imagePath}\"";

        await AppleScriptManager.ExecuteAppleScriptAsync(script).ConfigureAwait(false);
    }
}
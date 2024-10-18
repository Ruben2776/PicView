using System.Runtime.InteropServices;

namespace PicView.Avalonia.Wallpaper;

public enum WallpaperStyle
{
    Tile,
    Center,
    Stretch,
    Fit,
    Fill
}

public static class WallpaperManager
{
    public static int GetWallpaperStyle(WallpaperStyle style)
    {
        switch (style)
        {
            case WallpaperStyle.Tile:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return -1;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 0;
                }
                break;
            case WallpaperStyle.Center:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return -1;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 1;
                }
                break;
            case WallpaperStyle.Stretch:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return -1;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 2;
                }
                break;
            case WallpaperStyle.Fit:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return -1;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 3;
                }
                break;
            case WallpaperStyle.Fill:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return -1;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 4;
                }
                break;
            default:
                return 3;
        }
        return 0;
    }
}

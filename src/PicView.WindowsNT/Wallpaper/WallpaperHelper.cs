using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PicView.WindowsNT.Wallpaper;

public static class WallpaperHelper
{
    public enum WallpaperStyle
    {
        Tile,
        Center,
        Stretch,
        Fit,
        Fill
    }

    // Used to check for wallpaper support
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam,
        string pvParam, uint fWinIni);

    /// <summary>
    /// Set the desktop wallpaper.
    /// </summary>
    /// <param name="path">Path of the wallpaper</param>
    /// <param name="style">Wallpaper style</param>
    public static void SetDesktopWallpaper(string path, WallpaperStyle style)
    {
        // Set the wallpaper style and tile.
        // Two registry values are set in the Control Panel\Desktop key.
        // TileWallpaper
        //  0: The wallpaper picture should not be tiled
        //  1: The wallpaper picture should be tiled
        // WallpaperStyle
        //  0:  The image is centered if TileWallpaper=0 or tiled if TileWallpaper=1
        //  2:  The image is stretched to fill the screen
        //  6:  The image is resized to fit the screen while maintaining the aspect
        //      ratio. (Windows 7 and later)
        //  10: The image is resized and cropped to fill the screen while
        //      maintaining the aspect ratio. (Windows 7 and later)
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        switch (style)
        {
            case WallpaperStyle.Tile:
                key.SetValue(@"WallpaperStyle", "0");
                key.SetValue(@"TileWallpaper", "1");
                break;

            case WallpaperStyle.Center:
                key.SetValue(@"WallpaperStyle", "0");
                key.SetValue(@"TileWallpaper", "0");
                break;

            case WallpaperStyle.Stretch:
                key.SetValue(@"WallpaperStyle", "2");
                key.SetValue(@"TileWallpaper", "0");
                break;

            case WallpaperStyle.Fit: // (Windows 7 and later)
                key.SetValue(@"WallpaperStyle", "6");
                key.SetValue(@"TileWallpaper", "0");
                break;

            default:
            case WallpaperStyle.Fill: // (Windows 7 and later)
                key.SetValue(@"WallpaperStyle", "10");
                key.SetValue(@"TileWallpaper", "0");
                break;
        }

        key.Close();

        // Set the desktop wallpaper by calling the Win32 API SystemParametersInfo
        // with the SPI_SETDESKWALLPAPER desktop parameter. The changes should
        // persist, and also be immediately visible.
        if (!SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE))
        {
            throw new Win32Exception();
        }
    }

    private const uint SPI_SETDESKWALLPAPER = 20;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDWININICHANGE = 0x02;
}
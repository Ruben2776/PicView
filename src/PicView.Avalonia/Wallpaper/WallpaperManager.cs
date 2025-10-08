using System.Runtime.InteropServices;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Wallpaper;

public static class WallpaperManager
{
    
    public static async Task SetAsWallpaper(string path, WallpaperStyle style, MainViewModel vm)
    {
        if (vm.PlatformService is null)
        {
            return;
        }
        
        if (!vm.GlobalSettings.ShowSetAsWallpaper.Value)
            return;
        
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;
        try
        {
            var file = await ImageFormatConverter.ConvertToCommonSupportedFormatAsync(path, vm).ConfigureAwait(false);

            vm.PlatformService?.SetAsWallpaper(file, GetWallpaperStyle(style));
        }
        catch (Exception e)
        {
            TooltipHelper.ShowTooltipMessage(e.Message, true);
#if DEBUG
            Console.WriteLine(e);   
#endif
        }
        finally
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }
    
    public static int GetWallpaperStyle(WallpaperStyle style)
    {
        switch (style)
        {
            case WallpaperStyle.Tile:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return 5; 
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 0;
                }
                break;
            case WallpaperStyle.Center:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return 4; 
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 1;
                }
                break;
            case WallpaperStyle.Stretch:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return 3; 
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 2;
                }
                break;
            case WallpaperStyle.Fit:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return 2; 
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 3;
                }
                break;
            case WallpaperStyle.Fill:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return 1;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return 4;
                }
                break;
            default:
                return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 1 : 3;
        }
        return 0;
    }
}

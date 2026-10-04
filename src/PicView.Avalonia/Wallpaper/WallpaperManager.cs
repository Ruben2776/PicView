using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Wallpaper;

public static class WallpaperManager
{
    public static async Task SetAsWallpaper(string path, WallpaperStyle style, MainWindowViewModel vm)
    {
        if (!vm.GlobalSettings.ShowSetAsWallpaper.Value)
        {
            return;
        }
        
        vm.IsLoadingIndicatorShown.Value = true;
        try
        {
            var file = await ImageFormatConverter.ConvertToCommonSupportedFormatAsync(path, vm)
                .ConfigureAwait(false);

            var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
            core?.PlatformService?.SetAsWallpaper(file, GetWallpaperStyle(style));
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
            vm.IsLoadingIndicatorShown.Value = false;
        }
    }
    
    public static int GetWallpaperStyle(WallpaperStyle style)
    {
        switch (style)
        {
            case WallpaperStyle.Tile:
                if (OperatingSystem.IsMacOS())
                {
                    return 5; 
                }
                if (OperatingSystem.IsWindows())
                {
                    return 0;
                }
                break;
            case WallpaperStyle.Center:
                if (OperatingSystem.IsMacOS())
                {
                    return 4; 
                }
                if (OperatingSystem.IsWindows())
                {
                    return 1;
                }
                break;
            case WallpaperStyle.Stretch:
                if (OperatingSystem.IsMacOS())
                {
                    return 3; 
                }
                if (OperatingSystem.IsWindows())
                {
                    return 2;
                }
                break;
            case WallpaperStyle.Fit:
                if (OperatingSystem.IsMacOS())
                {
                    return 2; 
                }
                if (OperatingSystem.IsWindows())
                {
                    return 3;
                }
                break;
            case WallpaperStyle.Fill:
                if (OperatingSystem.IsMacOS())
                {
                    return 1;
                }
                if (OperatingSystem.IsWindows())
                {
                    return 4;
                }
                break;
            default:
                return OperatingSystem.IsMacOS() ? 1 : 3;
        }
        return 0;
    }
}

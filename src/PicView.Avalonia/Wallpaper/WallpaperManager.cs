using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Wallpaper;

public static class WallpaperManager
{
    // TODO: Sometimes when saving wallpaper with effects applied, the wallpaper will be entirely black (tested on Windows 10).
    
    /// <summary>
    /// Sets the active tab's image as the wallpaper, as a save would write it.
    /// </summary>
    public static async Task SetAsWallpaper(WallpaperStyle style, MainWindowViewModel vm)
    {
        using var busy = vm.WindowTabs.ActiveTab.CurrentValue.ClaimBusy();
        try
        {
            var file = await FileSaver.ExportCommonFormat(vm).ConfigureAwait(false);
            if (string.IsNullOrEmpty(file))
            {
                return;
            }

            var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
            if (core?.PlatformService is { } platform)
            {
                await platform.SetAsWallpaper(file, GetWallpaperStyle(style)).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            TooltipHelper.ShowTooltipMessage(vm, e.Message, true);
#if DEBUG
            Console.WriteLine(e);
#endif
        }
    }

    /// <summary>
    /// Sets the file at <paramref name="path"/> as the wallpaper, such as a gallery item's: as it is when it is in a
    /// common format, else as a temporary PNG, since the OS cannot show the others.
    /// </summary>
    public static async Task SetAsWallpaper(string path, WallpaperStyle style, MainWindowViewModel vm)
    {
        using var busy = vm.WindowTabs.ActiveTab.CurrentValue.ClaimBusy();
        try
        {
            var file = await FileSaver.ExportCommonFormat(path).ConfigureAwait(false);
            if (file is null)
            {
                TooltipHelper.ShowTooltipMessage(vm, TranslationManager.Translation.UnexpectedError ?? "Error", true);
                return;
            }

            var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
            if (core?.PlatformService is { } platform)
            {
                await platform.SetAsWallpaper(file, GetWallpaperStyle(style)).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            TooltipHelper.ShowTooltipMessage(vm, e.Message, true);
#if DEBUG
            Console.WriteLine(e);
#endif
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

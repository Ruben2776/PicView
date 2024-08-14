using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.ProcessHandling;

namespace PicView.Avalonia.UI;

public static class FunctionsHelper
{
    public static MainViewModel? Vm;

    public static Task<Func<Task>> GetFunctionByName(string functionName)
    {
        // Remember to have exact matching names, or it will be null
        return Task.FromResult<Func<Task>>(functionName switch
        {
            // Navigation values
            "Next" => Next,
            "Prev" => Prev,
            "NextFolder" => NextFolder,
            "PrevFolder" => PrevFolder,
            "Up" => Up,
            "Down" => Down,
            "Last" => Last,
            "First" => First,

            // Scroll
            "ScrollUp" => ScrollUp,
            "ScrollDown" => ScrollDown,
            "ScrollToTop" => ScrollToTop,
            "ScrollToBottom" => ScrollToBottom,

            // Zoom
            "ZoomIn" => ZoomIn,
            "ZoomOut" => ZoomOut,
            "ResetZoom" => ResetZoom,
            "ChangeCtrlZoom" => ChangeCtrlZoom,

            // Toggles
            "ToggleScroll" => ToggleScroll,
            "ToggleLooping" => ToggleLooping,
            "ToggleGallery" => ToggleGallery,

            // Scale Window
            "AutoFitWindow" => AutoFitWindow,
            "AutoFitWindowAndStretch" => AutoFitWindowAndStretch,
            "NormalWindow" => NormalWindow,
            "NormalWindowAndStretch" => NormalWindowAndStretch,

            // Window functions
            "Fullscreen" => Fullscreen,
            "SetTopMost" => SetTopMost,
            "Close" => Close,
            "ToggleInterface" => ToggleInterface,
            "NewWindow" => NewWindow,
            "Center" => Center,

            // Windows
            "AboutWindow" => AboutWindow,
            "EffectsWindow" => EffectsWindow,
            "ImageInfoWindow" => ImageInfoWindow,
            "ResizeWindow" => ResizeWindow,
            "SettingsWindow" => SettingsWindow,
            "KeybindingsWindow" => KeybindingsWindow,

            // Open functions
            "Open" => Open,
            "OpenWith" => OpenWith,
            "OpenInExplorer" => OpenInExplorer,
            "Save" => Save,
            "Print" => Print,
            "Reload" => Reload,

            // Copy functions
            "CopyFile" => CopyFile,
            "CopyFilePath" => CopyFilePath,
            "CopyImage" => CopyImage,
            "CopyBase64" => CopyBase64,
            "DuplicateFile" => DuplicateFile,
            "CutFile" => CutFile,
            "Paste" => Paste,

            // File functions
            "DeleteFile" => DeleteFile,
            "Rename" => Rename,
            "ShowFileProperties" => ShowFileProperties,

            // Image functions
            "ResizeImage" => ResizeImage,
            "Crop" => Crop,
            "Flip" => Flip,
            "OptimizeImage" => OptimizeImage,
            "Stretch" => Stretch,

            // Set stars
            "Set0Star" => Set0Star,
            "Set1Star" => Set1Star,
            "Set2Star" => Set2Star,
            "Set3Star" => Set3Star,
            "Set4Star" => Set4Star,
            "Set5Star" => Set5Star,

            // Misc
            "ChangeBackground" => ChangeBackground,
            "GalleryClick" => GalleryClick,
            "Slideshow" => Slideshow,
            "ColorPicker" => ColorPicker,

            _ => null
        });
    }

    #region Functions

    #region Menus

    public static Task CloseMenus()
    {
        if (Vm is null)
        {
            return Task.CompletedTask;
        }
        UIHelper.CloseMenus(Vm);
        return Task.CompletedTask;
    }

    public static Task ToggleFileMenu()
    {
        if (Vm is null)
        {
            return Task.CompletedTask;
        }
        UIHelper.ToggleFileMenu(Vm);
        return Task.CompletedTask;
    }

    public static Task ToggleImageMenu()
    {
        if (Vm is null)
        {
            return Task.CompletedTask;
        }
        UIHelper.ToggleImageMenu(Vm);
        return Task.CompletedTask;
    }

    public static Task ToggleSettingsMenu()
    {
        if (Vm is null)
        {
            return Task.CompletedTask;
        }
        UIHelper.ToggleSettingsMenu(Vm);
        return Task.CompletedTask;
    }

    public static Task ToggleToolsMenu()
    {
        if (Vm is null)
        {
            return Task.CompletedTask;
        }
        UIHelper.ToggleToolsMenu(Vm);
        return Task.CompletedTask;
    }

    #endregion Menus

    #region Navigation, zoom and rotation

    public static async Task Next()
    {
        await NavigationHelper.Iterate(next: true, Vm);
    }
    
    public static async Task NextFolder()
    {
        await NavigationHelper.GoToNextFolder(true, Vm);
    }
    
    public static async Task Last()
    {
        await NavigationHelper.NavigateFirstOrLast(last: true, Vm);
    }

    public static async Task Prev()
    {
        await NavigationHelper.Iterate(next: false, Vm);
    }
    
    public static async Task PrevFolder()
    {
        await NavigationHelper.GoToNextFolder(false, Vm);
    }

    public static async Task First()
    {
        await NavigationHelper.NavigateFirstOrLast(last: false, Vm);
    }

    public static async Task Up()
    {
        if (Vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(Direction.Up, Vm);
            return;
        }

        if (Vm.IsScrollingEnabled)
        {
            if (Vm.ImageViewer.ImageScrollViewer.Offset.Y == 0)
            {
               Vm.ImageViewer.Rotate(clockWise: true);
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Vm.ImageViewer.ImageScrollViewer.LineUp();
                });
            }
        }
        else
        {
            Vm.ImageViewer.Rotate(clockWise: true);
        }
    }

    public static async Task RotateRight()
    {
        if (Vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Vm.ImageViewer.Rotate(clockWise: false);
        });
        
        // Check if it should move the cursor
        if (!SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            return;
        }
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var titleBar = desktop.MainWindow.GetControl<Control>("Titlebar");
            var button = titleBar.GetControl<Button>("RotateRightButton");
            if (button.IsPointerOver)
            {
                var p = button.PointToScreen(new Point(10, 15));
                Vm.PlatformService?.SetCursorPos(p.X, p.Y);
            }
        });
    }

    public static Task RotateLeft()
    {
        if (Vm is null)
        {
            return Task.CompletedTask;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            return Task.CompletedTask;
        }
        Vm.ImageViewer.Rotate(clockWise: true);
        return Task.CompletedTask;
    }

    public static async Task Down()
    {
        if (Vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(Direction.Down, Vm);
            return;
        }

        if (Vm.IsScrollingEnabled)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Vm.ImageViewer.ImageScrollViewer.Offset.Y == 0)
                {
                    Vm.ImageViewer.Rotate(clockWise: false);
                }
                else
                {
                    Vm.ImageViewer.ImageScrollViewer.LineUp();
                }
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Vm.ImageViewer.Rotate(clockWise: false);
            });
        }
    }
    
    public static async Task ScrollDown()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Vm.ImageViewer.ImageScrollViewer.LineDown();
        });
    }
    
    public static async Task ScrollUp()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Vm.ImageViewer.ImageScrollViewer.LineUp();
        });
    }

    public static async Task ScrollToTop()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Vm.ImageViewer.ImageScrollViewer.ScrollToHome();
        });
    }

    public static async Task ScrollToBottom()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Vm.ImageViewer.ImageScrollViewer.ScrollToEnd();
        });
    }

    public static async Task ZoomIn()
    {
        if (Vm is null)
        {
            return;
        }
        //await Dispatcher.UIThread.InvokeAsync(Vm.ImageViewer.ZoomIn);
    }

    public static async Task ZoomOut()
    {
        if (Vm is null)
        {
            return;
        }
        //await Dispatcher.UIThread.InvokeAsync(Vm.ImageViewer.ZoomOut);
    }

    public static async Task ResetZoom()
    {
        if (Vm is null)
        {
            return;
        }
        //await Dispatcher.UIThread.InvokeAsync(() => Vm.ImageViewer.ResetZoom(true));
    }
    
    #endregion

    #region Toggle UI functions

    public static async Task ToggleScroll()
    {
        if (Vm is null)
        {
            return;
        }
        
        UIHelper.ToggleScroll(Vm);
        await SettingsHelper.SaveSettingsAsync();
    }

    public static async Task ChangeCtrlZoom()
    {
        if (Vm is null)
        {
            return;
        }
        await UIHelper.ChangeCtrlZoom(Vm);
    }

    public static async Task ToggleLooping()
    {
        if (Vm is null)
        {
            return;
        }
        await UIHelper.ToggleLooping(Vm);
    }
    
    public static async Task ToggleInterface()
    {
        if (Vm is null)
        {
            return;
        }
        await HideInterfaceLogic.ToggleUI(Vm);
    }
    
    public static async Task ToggleSubdirectories()
    {
        if (Vm is null)
        {
            return;
        }

        await UIHelper.ToggleSubdirectories(vm: Vm);
    }
    
    public static async Task ToggleBottomToolbar()
    {
        if (Vm is null)
        {
            return;
        }
        await HideInterfaceLogic.ToggleBottomToolbar(Vm);
    }
    
    #endregion

    #region Gallery functions

    public static async Task ToggleGallery()
    {
        await GalleryFunctions.ToggleGallery(Vm).ConfigureAwait(false);
    }

    public static async Task OpenCloseBottomGallery()
    {
        await GalleryFunctions.OpenCloseBottomGallery(Vm).ConfigureAwait(false);
    }
    
    public static async Task CloseGallery()
    {
        await GalleryFunctions.CloseGallery(Vm);
    }
    
    public static async Task GalleryClick()
    {
        await GalleryNavigation.GalleryClick(Vm);
    }

    #endregion
    
    #region Windows and window functions

    public static async Task ShowStartUpMenu()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ErrorHandling.ShowStartUpMenu(Vm);
        });
    }
    
    public static async Task Close()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            desktop.MainWindow?.Close();
        });
    }
    
    public static async Task Center()
    {
        // TODO: scroll to center when the gallery is open
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowHelper.CenterWindowOnScreen();
        });
    }

    public static async Task NewWindow()
    {        
        await Task.Run(ProcessHelper.StartNewProcess);
    }

    public static Task AboutWindow()
    {
        Vm?.PlatformService?.ShowAboutWindow();
        return Task.CompletedTask;
    }

    public static Task KeybindingsWindow()
    {
        Vm?.PlatformService?.ShowKeybindingsWindow();
        return Task.CompletedTask;
    }

    public static Task EffectsWindow()
    {
        Vm?.PlatformService?.ShowEffectsWindow();
        return Task.CompletedTask;
    }

    public static Task ImageInfoWindow()
    {
        Vm.PlatformService.ShowExifWindow();
        return Task.CompletedTask;
    }

    public static Task ResizeWindow()
    {
        Vm?.PlatformService?.ShowResizeWindow();
        return Task.CompletedTask;
    }

    public static Task SettingsWindow()
    {
        Vm?.PlatformService.ShowSettingsWindow();
        return Task.CompletedTask;
    }
    
    #endregion Windows

    #region Image Scaling and Window Behavior
    
    public static async Task Stretch()
    {
        await WindowHelper.Stretch(Vm);
    }
    public static async Task AutoFitWindow()
    {
        await WindowHelper.ToggleAutoFit(Vm);
    }

    public static async Task AutoFitWindowAndStretch()
    {
        await WindowHelper.AutoFitAndStretch(Vm);
    }

    public static async Task NormalWindow()
    {
        await WindowHelper.NormalWindow(Vm);
    }

    public static async Task NormalWindowAndStretch()
    {
        await WindowHelper.NormalWindowStretch(Vm);
    }

    public static async Task Fullscreen()
    {
        if (Vm is null)
        {
            return;
        }

        await WindowHelper.ToggleFullscreen(Vm);
    }

    public static async Task SetTopMost()
    {
        if (Vm is null)
        {
            return;
        }

        await WindowHelper.ToggleTopMost(Vm);
    }

    #endregion

    #region File funnctions

    public static async Task OpenLastFile()
    {
        if (Vm is null)
        {
            return;
        }

        await FileHistoryNavigation.OpenLastFileAsync(Vm).ConfigureAwait(false);
    }

    public static async Task OpenPreviousFileHistoryEntry()
    {
        if (Vm is null)
        {
            return;
        }

        await FileHistoryNavigation.PrevAsync(Vm).ConfigureAwait(false);
    }
    public static async Task OpenNextFileHistoryEntry()
    {
        if (Vm is null)
        {
            return;
        }

        await FileHistoryNavigation.NextAsync(Vm).ConfigureAwait(false);
    }
    
    public static async Task Print()
    {
        await Task.Run(() =>
        {
            Vm?.PlatformService?.Print(Vm.FileInfo.FullName);
        });
    }

    public static async Task Open()
    {
        if (Vm is null)
        {
            return;
        }

        var file = await FilePickerHelper.OpenFile();
        if (file is null)
        {
            return;
        }
        
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? file.Path.AbsolutePath : file.Path.LocalPath;
        _ = Task.Run(() => NavigationHelper.LoadPicFromStringAsync(path, Vm));
    }

    public static Task OpenWith()
    {
        Vm?.PlatformService?.OpenWith(Vm.FileInfo?.FullName);
        return Task.CompletedTask;
    }

    public static Task OpenInExplorer()
    {
        Vm?.PlatformService?.LocateOnDisk(Vm.FileInfo.FullName);
        return Task.CompletedTask;
    }

    public static async Task Save()
    {
        if (Vm is null)
        {
            return;
        }
        await FilePickerHelper.SaveFileAsync(Vm.FileInfo?.FullName, Vm);
    }
    
    public static async Task DeleteFile()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() =>
        {
            FileDeletionHelper.DeleteFileWithErrorMsg(Vm.FileInfo.FullName, true);
        });
    }

    public static async Task Rename()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            UIHelper.GetEditableTitlebar.SelectFileName();
        });
    }
    
    public static async Task ShowFileProperties()
    {
        await Task.Run(() => Vm?.PlatformService?.ShowFileProperties(Vm.FileInfo.FullName));
    }
    
    #endregion

    #region Copy and Paste functions

    public static async Task CopyFile()
    {
        await ClipboardHelper.CopyFileToClipboard(Vm?.FileInfo.FullName, Vm);
    }

    public static async Task CopyFilePath()
    {
        await ClipboardHelper.CopyTextToClipboard(Vm?.FileInfo.FullName);
    }

    public static async Task CopyImage()
    {
        if (Vm is null)
        {
            return;
        }
        await ClipboardHelper.CopyImageToClipboard(Vm.FileInfo.FullName);
    }

    public static async Task CopyBase64()
    {
        if (Vm is null)
        {
            return;
        }
        await ClipboardHelper.CopyBase64ToClipboard(Vm.FileInfo.FullName, vm: Vm);
    }

    public static async Task DuplicateFile()
    {
        if (Vm is null)
        {
            return;
        }
        if (!NavigationHelper.CanNavigate(Vm))
        {
            return;
        }
        var oldPath = Vm.FileInfo.FullName;
        var newPath = FileHelper.DuplicateAndReturnFileName(oldPath);
        if (File.Exists(newPath))
        {
            await NavigationHelper.LoadPicFromFile(newPath, Vm);
        }
    }

    public static async Task CutFile()
    {
        if (Vm is null)
        {
            return;
        }
        await ClipboardHelper.CutFile(Vm.FileInfo.FullName);
    }

    public static async Task Paste()
    {
        if (Vm is null)
        {
            return;
        }
        await ClipboardHelper.Paste(Vm);
    }
    
    #endregion

    #region Image Functions
    
    public static Task ChangeBackground()
    {
        return Task.CompletedTask;
    }
    
    public static async Task Reload()
    {
        if (Vm is null)
        {
            return;
        }
        if (!NavigationHelper.CanNavigate(Vm))
        {
            return;
        }

        Vm.ImageIterator?.Clear();
        Vm.CurrentView = new ImageViewer();
        await NavigationHelper.LoadPicFromStringAsync(Vm.FileInfo.FullName, Vm);
    }

    public static Task ResizeImage()
    {
        return Task.CompletedTask;
    }

    public static Task Crop()
    {
        return Task.CompletedTask;
    }

    public static async Task Flip()
    {
        if (Vm is null)
        {
            return;
        }
        await UIHelper.Flip(Vm);
    }

    public static async Task OptimizeImage()
    {
        if (Vm is null)
        {
            return;
        }
        if (!NavigationHelper.CanNavigate(Vm))
        {
            return;
        }
        if (Vm.FileInfo is null)
        {
            return;
        }
        await Task.Run(() =>
        {
            try
            {
                ImageOptimizer imageOptimizer = new()
                {
                    OptimalCompression = true
                };
                imageOptimizer.LosslessCompress(Vm.FileInfo.FullName);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        });
        SetTitleHelper.RefreshTitle(Vm);
    }

    public static Task Slideshow()
    {
        return Task.CompletedTask;
    }

    public static Task ColorPicker()
    {
        return Task.CompletedTask;
    }
    
    #endregion

    #region Sorting

    public static async Task SortFilesByName()
    {
        if (Vm?.PlatformService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(Vm.PlatformService, Vm, FileListHelper.SortFilesBy.Name);
    }

    public static async Task SortFilesByCreationTime()
    {
        if (Vm?.PlatformService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(Vm?.PlatformService, Vm, FileListHelper.SortFilesBy.CreationTime);
    }

    public static async Task SortFilesByLastAccessTime()
    {
        if (Vm?.PlatformService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(Vm?.PlatformService, Vm, FileListHelper.SortFilesBy.LastAccessTime);
    }

    public static async Task SortFilesByLastWriteTime()
    {
        if (Vm?.PlatformService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(Vm?.PlatformService, Vm, FileListHelper.SortFilesBy.LastWriteTime);
    }

    public static async Task SortFilesBySize()
    {
        if (Vm?.PlatformService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(Vm?.PlatformService, Vm, FileListHelper.SortFilesBy.FileSize);
    }

    public static async Task SortFilesByExtension()
    {
        if (Vm?.PlatformService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(Vm?.PlatformService, Vm, FileListHelper.SortFilesBy.Extension);
    }

    public static async Task SortFilesRandomly()
    {
        if (Vm?.PlatformService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(Vm?.PlatformService, Vm, FileListHelper.SortFilesBy.Random);
    }

    public static async Task SortFilesAscending()
    {
        if (Vm?.PlatformService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(Vm?.PlatformService, Vm, ascending: true);
    }

    public static async Task SortFilesDescending()
    {
        if (Vm?.PlatformService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(Vm?.PlatformService, Vm, ascending: false);
    }

    #endregion Sorting

    #region Rating

    public static async Task Set0Star()
    {
        if (Vm is null)
        {
            return;
        }

        await Task.Run(() => { EXIFHelper.SetEXIFRating(Vm.FileInfo.FullName, 0); });
        Vm.EXIFRating = 0;
    }

    public static async Task Set1Star()
    {
        if (Vm is null)
        {
            return;
        }

        await Task.Run(() => { EXIFHelper.SetEXIFRating(Vm.FileInfo.FullName, 1); });
        Vm.EXIFRating = 1;
    }

    public static async Task Set2Star()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => { EXIFHelper.SetEXIFRating(Vm.FileInfo.FullName, 2); });
        Vm.EXIFRating = 2;
    }

    public static async Task Set3Star()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => { EXIFHelper.SetEXIFRating(Vm.FileInfo.FullName, 3); });
        Vm.EXIFRating = 3;
    }

    public static async Task Set4Star()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => { EXIFHelper.SetEXIFRating(Vm.FileInfo.FullName, 4); });
        Vm.EXIFRating = 4;
    }

    public static async Task Set5Star()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => { EXIFHelper.SetEXIFRating(Vm.FileInfo.FullName, 5); });
        Vm.EXIFRating = 5;
    }

    #endregion

    #region Open GPS link

    public static async Task OpenGoogleMaps()
    {
        if (Vm is null)
        {
            return;
        }
        if (string.IsNullOrEmpty(Vm.GoogleLink))
        {
            return;
        }

        await Task.Run(() => ProcessHelper.OpenLink(Vm.GoogleLink));
    }
    
    public static async Task OpenBingMaps()
    {
        if (Vm is null)
        {
            return;
        }
        if (string.IsNullOrEmpty(Vm.BingLink))
        {
            return;
        }

        await Task.Run(() => ProcessHelper.OpenLink(Vm.BingLink));
    }

    #endregion

    #region Wallpaper and lockscreen image
    
    public static async Task SetAsWallpaper()
    {
        if (Vm is null)
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await SetAsWallpaperFilled();
        }
        // TODO: Add support for macOS
    }

    public static async Task SetAsWallpaperTiled()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => Vm.PlatformService.SetAsWallpaper(Vm.FileInfo.FullName, 0));
    }
    
    public static async Task SetAsWallpaperCentered()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => Vm.PlatformService.SetAsWallpaper(Vm.FileInfo.FullName, 1));
    }
    
    public static async Task SetAsWallpaperStretched()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => Vm.PlatformService.SetAsWallpaper(Vm.FileInfo.FullName, 2));
    }
    
    public static async Task SetAsWallpaperFitted()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => Vm.PlatformService.SetAsWallpaper(Vm.FileInfo.FullName, 3));
    }
    
    public static async Task SetAsWallpaperFilled()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => Vm.PlatformService.SetAsWallpaper(Vm.FileInfo.FullName, 4));
    }
    
    public static async Task SetAsLockscreenCentered()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => Vm.PlatformService.SetAsLockScreen(Vm.FileInfo.FullName));
    }
    
    public static async Task SetAsLockScreen()
    {
        if (Vm is null)
        {
            return;
        }
        await Task.Run(() => Vm.PlatformService.SetAsLockScreen(Vm.FileInfo.FullName));
    }

    #endregion

    #region Other settings

    public static async Task ResetSettings()
    {
        SettingsHelper.DeleteSettingFiles();
        SettingsHelper.SetDefaults();
        await SettingsHelper.SaveSettingsAsync();
        string args;
        if (!NavigationHelper.CanNavigate(Vm))
        {
            var argsList = Environment.GetCommandLineArgs();
            args = argsList.Length > 1 ? argsList[1] : string.Empty;
        }
        else
        {
            args = Vm.FileInfo.FullName;
        }
        ProcessHelper.RestartApp(args);
        await Close();
    }

    #endregion
    
    #endregion


}
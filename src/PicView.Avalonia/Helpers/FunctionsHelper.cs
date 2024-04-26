using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.ProcessHandling;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PicView.Avalonia.Helpers;

public static class FunctionsHelper
{
    public static MainViewModel? Vm;

    #region Functions list

    #region Menus

    public static Task CloseMenus()
    {
        Vm.IsFileMenuVisible = false;
        Vm.IsImageMenuVisible = false;
        Vm.IsSettingsMenuVisible = false;
        Vm.IsToolsMenuVisible = false;
        return Task.CompletedTask;
    }

    public static Task ToggleFileMenu()
    {
        Vm.IsFileMenuVisible = !Vm.IsFileMenuVisible;
        Vm.IsImageMenuVisible = false;
        Vm.IsSettingsMenuVisible = false;
        Vm.IsToolsMenuVisible = false;
        return Task.CompletedTask;
    }

    public static Task ToggleImageMenu()
    {
        Vm.IsFileMenuVisible = false;
        Vm.IsImageMenuVisible = !Vm.IsImageMenuVisible;
        Vm.IsSettingsMenuVisible = false;
        Vm.IsToolsMenuVisible = false;
        return Task.CompletedTask;
    }

    public static Task ToggleSettingsMenu()
    {
        Vm.IsFileMenuVisible = false;
        Vm.IsImageMenuVisible = false;
        Vm.IsSettingsMenuVisible = !Vm.IsSettingsMenuVisible;
        Vm.IsToolsMenuVisible = false;
        return Task.CompletedTask;
    }

    public static Task ToggleToolsMenu()
    {
        Vm.IsFileMenuVisible = false;
        Vm.IsImageMenuVisible = false;
        Vm.IsSettingsMenuVisible = false;
        Vm.IsToolsMenuVisible = !Vm.IsToolsMenuVisible;
        return Task.CompletedTask;
    }

    #endregion Menus

    public static Task Print()
    {
        return Task.CompletedTask;
    }

    public static async Task Next()
    {
        await NavigationHelper.Iterate(next: true, Vm);
    }

    public static async Task NextButton()
    {
        await NavigationHelper.IterateButton(next: true, Vm);
    }

    public static async Task Last()
    {
        await NavigationHelper.NavigateFirstOrLast(last: true, Vm);
    }

    public static async Task Prev()
    {
        await NavigationHelper.Iterate(next: false, Vm);
    }

    public static async Task PrevButton()
    {
        await NavigationHelper.IterateButton(next: false, Vm);
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
            // TODO - Implement gallery navigation
            return;
        }

        if (Vm.IsScrollingEnabled)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Vm.ImageViewer.ImageScrollViewer.Offset.Y == 0)
                {
                    Vm.ImageViewer.Rotate(clockWise: true, animate: true);
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
                Vm.ImageViewer.Rotate(clockWise: true, animate: true);
            });
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
            Vm.ImageViewer.Rotate(clockWise: true, animate: true);
        });
    }

    public static async Task RotateLeft()
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
            Vm.ImageViewer.Rotate(clockWise: false, animate: true);
        });
    }

    public static async Task Down()
    {
        if (Vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            // TODO - Implement gallery navigation
            return;
        }

        if (Vm.IsScrollingEnabled)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Vm.ImageViewer.ImageScrollViewer.Offset.Y == 0)
                {
                    Vm.ImageViewer.Rotate(clockWise: false, animate: true);
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
                Vm.ImageViewer.Rotate(clockWise: false, animate: true);
            });
        }
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
        await Dispatcher.UIThread.InvokeAsync(Vm.ImageViewer.ZoomIn);
    }

    public static async Task ZoomOut()
    {
        if (Vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(Vm.ImageViewer.ZoomOut);
    }

    public static async Task ResetZoom()
    {
        if (Vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() => Vm.ImageViewer.ResetZoom(true));
    }

    public static async Task ToggleScroll()
    {
        if (Vm is null)
        {
            return;
        }
        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            Vm.ToggleScrollBarVisibility = ScrollBarVisibility.Disabled;
            Vm.GetScrolling = TranslationHelper.GetTranslation("ScrollingDisabled");
            Vm.IsScrollingEnabled = false;
            SettingsHelper.Settings.Zoom.ScrollEnabled = false;
        }
        else
        {
            Vm.ToggleScrollBarVisibility = ScrollBarVisibility.Visible;
            Vm.GetScrolling = TranslationHelper.GetTranslation("ScrollingEnabled");
            Vm.IsScrollingEnabled = true;
            SettingsHelper.Settings.Zoom.ScrollEnabled = true;
        }
        WindowHelper.SetSize(Vm);
        await SettingsHelper.SaveSettingsAsync();
    }

    public static async Task ChangeCtrlZoom()
    {
        if (Vm is null)
        {
            return;
        }
        SettingsHelper.Settings.Zoom.CtrlZoom = !SettingsHelper.Settings.Zoom.CtrlZoom;
        Vm.GetCtrlZoom = SettingsHelper.Settings.Zoom.CtrlZoom
            ? TranslationHelper.GetTranslation("CtrlToZoom")
            : TranslationHelper.GetTranslation("ScrollToZoom");
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    public static Task ToggleLooping()
    {
        return Task.CompletedTask;
    }

    public static async Task ToggleGallery()
    {
        await GalleryFunctions.ToggleGallery(Vm).ConfigureAwait(false);
    }

    public static async Task ToggleBottomGallery()
    {
        await GalleryFunctions.ToggleBottomGallery(Vm).ConfigureAwait(false);
    }

    public static async Task OpenCloseBottomGallery()
    {
        await GalleryFunctions.OpenCloseBottomGallery(Vm).ConfigureAwait(false);
    }

    public static Task AutoFitWindow()
    {
        return Task.CompletedTask;
    }

    public static Task AutoFitWindowAndStretch()
    {
        return Task.CompletedTask;
    }

    public static Task NormalWindow()
    {
        return Task.CompletedTask;
    }

    public static Task NormalWindowAndStretch()
    {
        return Task.CompletedTask;
    }

    public static Task Fullscreen()
    {
        return Task.CompletedTask;
    }

    public static async Task SetTopMost()
    {
        if (Vm is null)
        {
            return;
        }

        await WindowHelper.ToggleTopMost(Vm);
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

    public static Task ToggleInterface()
    {
        return Task.CompletedTask;
    }

    public static Task NewWindow()
    {
        ProcessHelper.StartNewProcess();
        return Task.CompletedTask;
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
        return Task.CompletedTask;
    }

    public static Task ImageInfoWindow()
    {
        Vm.ShowExifWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    public static Task ResizeWindow()
    {
        return Task.CompletedTask;
    }

    public static Task SettingsWindow()
    {
        Vm?.ShowSettingsWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    public static async Task Open()
    {
        if (Vm is null)
        {
            return;
        }

        Vm.FileService ??= new FileService();
        var file = await FileService.OpenFile();
        if (file is null)
        {
            return;
        }

        Vm.CurrentView = new ImageViewer();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var path = file.Path.AbsolutePath;
            await Vm.LoadPicFromFile(new FileInfo(path));
        }
        else
        {
            await Vm.LoadPicFromFile(new FileInfo(file.Path.LocalPath));
        }
    }

    public static Task OpenWith()
    {
        Vm?.PlatformService?.OpenWith(Vm.FileInfo?.FullName);
        return Task.CompletedTask;
    }

    public static Task OpenInExplorer()
    {
        if (Vm is null)
        {
            return Task.CompletedTask;
        }
        Vm?.PlatformService?.LocateOnDisk();
        return Task.CompletedTask;
    }

    public static Task Save()
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

        Vm.ImageIterator.PreLoader.Clear();
        Vm.CurrentView = new ImageViewer();
        await Vm.LoadPicFromString(Vm.FileInfo.FullName);
    }

    public static async Task ToggleSubdirectories()
    {
        if (Vm is null)
        {
            return;
        }

        if (SettingsHelper.Settings.Sorting.IncludeSubDirectories)
        {
            Vm.IsIncludingSubdirectories = false;
            SettingsHelper.Settings.Sorting.IncludeSubDirectories = false;
        }
        else
        {
            Vm.IsIncludingSubdirectories = true;
            SettingsHelper.Settings.Sorting.IncludeSubDirectories = true;
        }

        await Vm.ImageIterator.ReloadFileList();
        Vm.SetTitle();
        await SettingsHelper.SaveSettingsAsync();
    }

    public static Task CopyFile()
    {
        return Task.CompletedTask;
    }

    public static Task CopyFilePath()
    {
        return Task.CompletedTask;
    }

    public static Task CopyImage()
    {
        return Task.CompletedTask;
    }

    public static Task CopyBase64()
    {
        return Task.CompletedTask;
    }

    public static Task DuplicateFile()
    {
        return Task.CompletedTask;
    }

    public static Task CutFile()
    {
        return Task.CompletedTask;
    }

    public static Task Paste()
    {
        return Task.CompletedTask;
    }

    public static Task DeleteFile()
    {
        return Task.CompletedTask;
    }

    public static Task Rename()
    {
        return Task.CompletedTask;
    }

    public static Task ShowFileProperties()
    {
        return Task.CompletedTask;
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
        if (!NavigationHelper.CanNavigate(Vm))
        {
            return;
        }
        if (Vm.ScaleX == 1)
        {
            Vm.ScaleX = -1;
            Vm.GetFlipped = Vm.UnFlip;
        }
        else
        {
            Vm.ScaleX = 1;
            Vm.GetFlipped = Vm.Flip;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Vm.ImageViewer.Flip(animate: true);
        });
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
        Vm.RefreshTitle();
    }

    public static Task Stretch()
    {
        return Task.CompletedTask;
    }

    public static Task Set0Star()
    {
        return Task.CompletedTask;
    }

    public static Task Set1Star()
    {
        return Task.CompletedTask;
    }

    public static Task Set2Star()
    {
        return Task.CompletedTask;
    }

    public static Task Set3Star()
    {
        return Task.CompletedTask;
    }

    public static Task Set4Star()
    {
        return Task.CompletedTask;
    }

    public static Task Set5Star()
    {
        return Task.CompletedTask;
    }

    public static Task ChangeBackground()
    {
        return Task.CompletedTask;
    }

    public static Task GalleryClick()
    {
        return Task.CompletedTask;
    }

    public static async Task Center()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowHelper.CenterWindowOnScreen();
        });
    }

    public static Task Slideshow()
    {
        return Task.CompletedTask;
    }

    public static Task ColorPicker()
    {
        return Task.CompletedTask;
    }

    public static async Task ToggleBottomToolbar()
    {
        if (Vm is null)
        {
            return;
        }
        await WindowHelper.ToggleBottomToolbar(Vm);
    }

    public static async Task ToggleUI()
    {
        if (Vm is null)
        {
            return;
        }
        await WindowHelper.ToggleUI(Vm);
    }

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

    #endregion Functions list
}
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using ImageMagick;
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
    public static IPlatformSpecificService? PlatformSpecificService;

    #region Functions list

    public static Task Print()
    {
        throw new NotImplementedException();
    }

    public static async Task Next()
    {
        if (Vm is null)
        {
            return;
        }

        if (Vm.IsGalleryOpen)
        {
            // TODO - Implement gallery navigation
            return;
        }

        if (!NavigationHelper.CanNavigate(Vm))
        {
            return;
        }
        await NavigationHelper.Navigate(true, Vm);
    }

    public static async Task Last()
    {
        if (Vm is null)
        {
            return;
        }

        if (Vm.IsGalleryOpen)
        {
            // TODO - Implement gallery navigation
            return;
        }

        if (!NavigationHelper.CanNavigate(Vm))
        {
            return;
        }
        await NavigationHelper.NavigateFirstOrLast(true, Vm);
    }

    public static async Task Prev()
    {
        if (Vm is null)
        {
            return;
        }

        if (Vm.IsGalleryOpen)
        {
            // TODO - Implement gallery navigation
            return;
        }

        if (!NavigationHelper.CanNavigate(Vm))
        {
            return;
        }
        await NavigationHelper.Navigate(false, Vm);
    }

    public static async Task First()
    {
        if (Vm is null)
        {
            return;
        }

        if (Vm.IsGalleryOpen)
        {
            // TODO - Implement gallery navigation
            return;
        }

        if (!NavigationHelper.CanNavigate(Vm))
        {
            return;
        }
        await NavigationHelper.NavigateFirstOrLast(false, Vm);
    }

    public static async Task Up()
    {
        if (Vm is null)
        {
            return;
        }

        if (Vm.IsGalleryOpen)
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

        if (Vm.IsGalleryOpen)
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

        if (Vm.IsGalleryOpen)
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

        if (Vm.IsGalleryOpen)
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
        throw new NotImplementedException();
    }

    public static Task ToggleGallery()
    {
        Vm?.ToggleGalleryCommand.Execute(null);
        return Task.CompletedTask;
    }

    public static Task AutoFitWindow()
    {
        throw new NotImplementedException();
    }

    public static Task AutoFitWindowAndStretch()
    {
        throw new NotImplementedException();
    }

    public static Task NormalWindow()
    {
        throw new NotImplementedException();
    }

    public static Task NormalWindowAndStretch()
    {
        throw new NotImplementedException();
    }

    public static Task Fullscreen()
    {
#if DEBUG
        // Show Avalonia DevTools in DEBUG mode
        return Task.CompletedTask;
#endif
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public static Task NewWindow()
    {
        ProcessHelper.StartNewProcess();
        return Task.CompletedTask;
    }

    public static Task AboutWindow()
    {
        PlatformSpecificService?.ShowAboutWindow();
        return Task.CompletedTask;
    }

    public static Task KeybindingsWindow()
    {
        Vm?.ShowKeybindingsWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    public static Task EffectsWindow()
    {
        throw new NotImplementedException();
    }

    public static Task ImageInfoWindow()
    {
        Vm.ShowExifWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    public static Task ResizeWindow()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public static Task OpenInExplorer()
    {
        Vm?.ShowInFolderCommand.Execute(null);
        return Task.CompletedTask;
    }

    public static Task Save()
    {
        throw new NotImplementedException();
    }

    public static async Task Reload()
    {
        if (Vm is null)
        {
            return;
        }

        if (Vm.IsGalleryOpen)
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
        throw new NotImplementedException();
    }

    public static Task CopyFilePath()
    {
        throw new NotImplementedException();
    }

    public static Task CopyImage()
    {
        throw new NotImplementedException();
    }

    public static Task CopyBase64()
    {
        throw new NotImplementedException();
    }

    public static Task DuplicateFile()
    {
        throw new NotImplementedException();
    }

    public static Task CutFile()
    {
        throw new NotImplementedException();
    }

    public static Task Paste()
    {
        throw new NotImplementedException();
    }

    public static Task DeleteFile()
    {
        throw new NotImplementedException();
    }

    public static Task Rename()
    {
        throw new NotImplementedException();
    }

    public static Task ShowFileProperties()
    {
        throw new NotImplementedException();
    }

    public static Task ResizeImage()
    {
        throw new NotImplementedException();
    }

    public static Task Crop()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public static Task Set0Star()
    {
        throw new NotImplementedException();
    }

    public static Task Set1Star()
    {
        throw new NotImplementedException();
    }

    public static Task Set2Star()
    {
        throw new NotImplementedException();
    }

    public static Task Set3Star()
    {
        throw new NotImplementedException();
    }

    public static Task Set4Star()
    {
        throw new NotImplementedException();
    }

    public static Task Set5Star()
    {
        throw new NotImplementedException();
    }

    public static Task ChangeBackground()
    {
        throw new NotImplementedException();
    }

    public static Task GalleryClick()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public static Task ColorPicker()
    {
        throw new NotImplementedException();
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
        if (Vm is null)
        {
            return;
        }
        if (PlatformSpecificService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(PlatformSpecificService, Vm, FileListHelper.SortFilesBy.Name);
    }

    public static async Task SortFilesByCreationTime()
    {
        if (Vm is null)
        {
            return;
        }
        if (PlatformSpecificService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(PlatformSpecificService, Vm, FileListHelper.SortFilesBy.CreationTime);
    }

    public static async Task SortFilesByLastAccessTime()
    {
        if (Vm is null)
        {
            return;
        }
        if (PlatformSpecificService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(PlatformSpecificService, Vm, FileListHelper.SortFilesBy.LastAccessTime);
    }

    public static async Task SortFilesByLastWriteTime()
    {
        if (Vm is null)
        {
            return;
        }
        if (PlatformSpecificService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(PlatformSpecificService, Vm, FileListHelper.SortFilesBy.LastWriteTime);
    }

    public static async Task SortFilesBySize()
    {
        if (Vm is null)
        {
            return;
        }
        if (PlatformSpecificService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(PlatformSpecificService, Vm, FileListHelper.SortFilesBy.FileSize);
    }

    public static async Task SortFilesByExtension()
    {
        if (Vm is null)
        {
            return;
        }
        if (PlatformSpecificService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(PlatformSpecificService, Vm, FileListHelper.SortFilesBy.Extension);
    }

    public static async Task SortFilesRandomly()
    {
        if (Vm is null)
        {
            return;
        }
        if (PlatformSpecificService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(PlatformSpecificService, Vm, FileListHelper.SortFilesBy.Random);
    }

    public static async Task SortFilesAscending()
    {
        if (Vm is null)
        {
            return;
        }
        if (PlatformSpecificService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(PlatformSpecificService, Vm, ascending: true);
    }

    public static async Task SortFilesDescending()
    {
        if (Vm is null)
        {
            return;
        }
        if (PlatformSpecificService is null)
        {
            return;
        }
        await SortingHelper.UpdateFileList(PlatformSpecificService, Vm, ascending: false);
    }

    #endregion Sorting

    #endregion Functions list
}
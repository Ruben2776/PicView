using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;

namespace PicView.Avalonia.UI;

public static class UIFunctions
{
    public static MainViewModel? Vm;

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

    public static Task ResetZoom()
    {
        throw new NotImplementedException();
    }

    public static Task ToggleScroll()
    {
        throw new NotImplementedException();
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

    public static Task SetTopMost()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public static Task AboutWindow()
    {
        Vm?.ShowAboutWindowCommand.Execute(null);
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

    public static Task Open()
    {
        throw new NotImplementedException();
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
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Vm.ImageViewer.Flip(animate: true);
        });
    }

    public static Task OptimizeImage()
    {
        throw new NotImplementedException();
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

    #endregion Functions list
}
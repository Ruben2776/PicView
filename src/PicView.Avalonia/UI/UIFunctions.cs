using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.UI;

public class UIFunctions
{
    private static MainViewModel? _vm;

    public UIFunctions(MainViewModel vm)
    {
        _vm = vm;
    }

    #region Functions list

    public Task Print()
    {
        throw new NotImplementedException();
    }

    public static async Task Next()
    {
        await NavigationHelper.Navigate(true, _vm);
    }

    public static async Task Prev()
    {
        await NavigationHelper.Navigate(false, _vm);
    }

    public async Task Up()
    {
        if (_vm is null)
        {
            return;
        }

        if (_vm.IsScrollingEnabled)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ImageViewer.ImageScrollViewer.Offset.Y == 0)
                {
                    _vm.ImageViewer.Rotate(clockWise: true, animate: true);
                }
                else
                {
                    _vm.ImageViewer.ImageScrollViewer.LineUp();
                }
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _vm.ImageViewer.Rotate(clockWise: true, animate: true);
            });
        }
    }

    public async Task Down()
    {
        if (_vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.Rotate(clockWise: false, animate: true);
        });
    }

    public async Task ScrollToTop()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.ImageScrollViewer.ScrollToHome();
        });
    }

    public async Task ScrollToBottom()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.ImageScrollViewer.ScrollToEnd();
        });
    }

    public async Task ZoomIn()
    {
        if (_vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(_vm.ImageViewer.ZoomIn);
    }

    public async Task ZoomOut()
    {
        if (_vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(_vm.ImageViewer.ZoomOut);
    }

    public Task ResetZoom()
    {
        throw new NotImplementedException();
    }

    public Task ToggleScroll()
    {
        throw new NotImplementedException();
    }

    public Task ToggleLooping()
    {
        throw new NotImplementedException();
    }

    public Task ToggleGallery()
    {
        throw new NotImplementedException();
    }

    public Task AutoFitWindow()
    {
        throw new NotImplementedException();
    }

    public Task AutoFitWindowAndStretch()
    {
        throw new NotImplementedException();
    }

    public Task NormalWindow()
    {
        throw new NotImplementedException();
    }

    public Task NormalWindowAndStretch()
    {
        throw new NotImplementedException();
    }

    public Task Fullscreen()
    {
#if DEBUG
        // Show Avalonia DevTools in DEBUG mode
        return Task.CompletedTask;
#endif
        throw new NotImplementedException();
    }

    public Task SetTopMost()
    {
        throw new NotImplementedException();
    }

    public async Task Close()
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

    public Task ToggleInterface()
    {
        throw new NotImplementedException();
    }

    public Task NewWindow()
    {
        throw new NotImplementedException();
    }

    public Task AboutWindow()
    {
        _vm?.ShowAboutWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    public Task KeybindingsWindow()
    {
        _vm?.ShowKeybindingsWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    public Task EffectsWindow()
    {
        throw new NotImplementedException();
    }

    public Task ImageInfoWindow()
    {
        _vm.ShowExifWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    public Task ResizeWindow()
    {
        throw new NotImplementedException();
    }

    public Task SettingsWindow()
    {
        _vm?.ShowSettingsWindowCommand.Execute(null);
        return Task.CompletedTask;
    }

    public Task Open()
    {
        throw new NotImplementedException();
    }

    public Task OpenWith()
    {
        throw new NotImplementedException();
    }

    public Task OpenInExplorer()
    {
        _vm?.ShowInFolderCommand.Execute(null);
        return Task.CompletedTask;
    }

    public Task Save()
    {
        throw new NotImplementedException();
    }

    public Task Reload()
    {
        throw new NotImplementedException();
    }

    public Task CopyFile()
    {
        throw new NotImplementedException();
    }

    public Task CopyFilePath()
    {
        throw new NotImplementedException();
    }

    public Task CopyImage()
    {
        throw new NotImplementedException();
    }

    public Task CopyBase64()
    {
        throw new NotImplementedException();
    }

    public Task DuplicateFile()
    {
        throw new NotImplementedException();
    }

    public Task CutFile()
    {
        throw new NotImplementedException();
    }

    public Task Paste()
    {
        throw new NotImplementedException();
    }

    public Task DeleteFile()
    {
        throw new NotImplementedException();
    }

    public Task Rename()
    {
        throw new NotImplementedException();
    }

    public Task ShowFileProperties()
    {
        throw new NotImplementedException();
    }

    public Task ResizeImage()
    {
        throw new NotImplementedException();
    }

    public Task Crop()
    {
        throw new NotImplementedException();
    }

    public async Task Flip()
    {
        if (_vm is null)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _vm.ImageViewer.Flip(animate: true);
        });
    }

    public Task OptimizeImage()
    {
        throw new NotImplementedException();
    }

    public Task Stretch()
    {
        throw new NotImplementedException();
    }

    public Task Set0Star()
    {
        throw new NotImplementedException();
    }

    public Task Set1Star()
    {
        throw new NotImplementedException();
    }

    public Task Set2Star()
    {
        throw new NotImplementedException();
    }

    public Task Set3Star()
    {
        throw new NotImplementedException();
    }

    public Task Set4Star()
    {
        throw new NotImplementedException();
    }

    public Task Set5Star()
    {
        throw new NotImplementedException();
    }

    public Task ChangeBackground()
    {
        throw new NotImplementedException();
    }

    public Task GalleryClick()
    {
        throw new NotImplementedException();
    }

    public async Task Center()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowHelper.CenterWindowOnScreen();
        });
    }

    public Task Slideshow()
    {
        throw new NotImplementedException();
    }

    public Task ColorPicker()
    {
        throw new NotImplementedException();
    }

    #endregion Functions list
}
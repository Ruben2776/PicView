using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

public static class MouseShortcuts
{
    public static async ValueTask HandlePointerWheelChanged(
        PointerWheelEventArgs e,
        MainWindowViewModel mainViewModel,
        MainWindow mainWindow,
        AutoScrollViewer imageScrollViewer,
        Func<PointerWheelEventArgs, ValueTask>? zoomIn,
        Func<PointerWheelEventArgs, ValueTask>? zoomOut)
    {
        // Don't handle mouse wheel if the view is not the image viewer
        // or a dialog is opened
        var shouldReturn = await Dispatcher.UIThread.InvokeAsync(() =>
            mainViewModel.WindowTabs.ActiveTab.Value.CurrentView.Value is not ImageViewer || mainWindow.IsDialogOpen);
        if (shouldReturn)
        {
            return;
        }
        
        e.Handled = true;

        var ctrl = e.KeyModifiers == KeyModifiers.Control;
        var shift = e.KeyModifiers == KeyModifiers.Shift;
        var reverse = e.Delta.Y < 0;

        if (Settings.Zoom.ScrollEnabled)
        {
            if (!shift)
            {
                if (ctrl && !Settings.Zoom.CtrlZoom)
                {
                    if (IsTouchPadOrTouch(e))
                    {
                        return;
                    }

                    await LoadNextPicAsync(reverse, mainViewModel);
                    return;
                }

                if (IsVerticalScrollBarVisible(imageScrollViewer))
                {
                    ScrollVertically(reverse, imageScrollViewer);
                }
                else
                {
                    await LoadNextPicAsync(reverse, mainViewModel);
                }

                return;
            }
        }

        if (Settings.Zoom.CtrlZoom)
        {
            if (ctrl)
            {
                if (IsTouchPadOrTouch(e))
                {
                    return;
                }

                if (reverse)
                {
                    if (zoomOut is not null)
                    {
                        await zoomOut(e);
                    }
                }
                else
                {
                    if (zoomIn is not null)
                    {
                        await zoomIn(e);
                    }
                }
            }
            else
            {
                await ScrollOrNavigateAsync(e, reverse, mainViewModel, imageScrollViewer);
            }
        }
        else
        {
            if (ctrl)
            {
                await ScrollOrNavigateAsync(e, reverse, mainViewModel, imageScrollViewer);
            }
            else
            {
                if (reverse)
                {
                    if (zoomOut is not null)
                    {
                        await zoomOut(e);
                    }
                }
                else
                {
                    if (zoomIn is not null)
                    {
                        await zoomIn(e);
                    }
                }
            }
        }
    }

    private static bool IsTouchPadOrTouch(PointerEventArgs e)
        => Settings.Zoom.IsUsingTouchPad || e.Pointer.Type == PointerType.Touch;

    private static bool IsVerticalScrollBarVisible(AutoScrollViewer imageScrollViewer)
        => imageScrollViewer.VerticalScrollBarVisibility is ScrollBarVisibility.Visible or ScrollBarVisibility.Auto;

    private static void ScrollVertically(bool reverse, AutoScrollViewer imageScrollViewer)
    {
        if (reverse)
        {
            imageScrollViewer.LineDown();
        }
        else
        {
            imageScrollViewer.LineUp();
        }
    }

    private static async ValueTask ScrollOrNavigateAsync(
        PointerWheelEventArgs e,
        bool reverse,
        MainWindowViewModel mainViewModel,
        AutoScrollViewer imageScrollViewer)
    {
        if (!Settings.Zoom.ScrollEnabled || e.KeyModifiers == KeyModifiers.Shift)
        {
            if (IsTouchPadOrTouch(e))
            {
                return;
            }

            await LoadNextPicAsync(reverse, mainViewModel);
        }
        else
        {
            if (IsVerticalScrollBarVisible(imageScrollViewer))
            {
                ScrollVertically(reverse, imageScrollViewer);
            }
            else
            {
                await LoadNextPicAsync(reverse, mainViewModel);
            }
        }
    }

    private static async ValueTask LoadNextPicAsync(bool reverse, MainWindowViewModel mainViewModel)
    {
        if (Settings.Zoom.IsUsingTouchPad)
        {
            return;
        }

        var next = reverse ? Settings.Zoom.HorizontalReverseScroll : !Settings.Zoom.HorizontalReverseScroll;
        if (next)
        {
            await mainViewModel.WindowTabs.NextFile().ConfigureAwait(false);
        }
        else
        {
            await mainViewModel.WindowTabs.PrevFile().ConfigureAwait(false);

        }
    }
    
    public static async Task MainWindow_PointerPressed(PointerPressedEventArgs e, Window window)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        var prop = e.GetCurrentPoint(topLevel).Properties;

        if (window.DataContext is not MainWindowViewModel windowViewModel)
        {
            return;
        }

        // Handle mouse side buttons
        if (prop.IsXButton1Pressed)
        {
            switch (Settings.Navigation.MouseSideButtonNavigationMode)
            {
                default:
                case NavigationMode.None:
                    return;
                case NavigationMode.NavigatingFileHistory:
                    await windowViewModel.Mapper.OpenPreviousFileHistoryEntry().ConfigureAwait(false);
                    return;
                case NavigationMode.NavigatingBetweenDirectories:
                    await windowViewModel.WindowTabs.PrevFolder().ConfigureAwait(false);
                    return;
                case NavigationMode.NavigatingBetweenFiles:
                    await windowViewModel.WindowTabs.PrevFile().ConfigureAwait(false);
                    return;
                case NavigationMode.NavigatingBetweenArchives:
                    await windowViewModel.WindowTabs.PrevArchive().ConfigureAwait(false);
                    return;
            }
        }
        if (prop.IsXButton2Pressed)
        {
            switch (Settings.Navigation.MouseSideButtonNavigationMode)
            {
                default:
                case NavigationMode.None:
                    return;
                case NavigationMode.NavigatingFileHistory:
                    await windowViewModel.Mapper.OpenNextFileHistoryEntry().ConfigureAwait(false);
                    return;
                case NavigationMode.NavigatingBetweenDirectories:
                    await windowViewModel.WindowTabs.NextFolder().ConfigureAwait(false);
                    return;
                case NavigationMode.NavigatingBetweenFiles:
                    await windowViewModel.WindowTabs.NextFile().ConfigureAwait(false);
                    return;
                case NavigationMode.NavigatingBetweenArchives:
                    await windowViewModel.WindowTabs.NextArchive().ConfigureAwait(false);
                    return;
            }
        }
        // Handle double click (only for the left mouse button, so that rapid
        // double-clicks of the side buttons don't fall through and trigger the
        // left-button double-click behavior such as toggling fullscreen)
        if (e.ClickCount is 2 && prop.IsLeftButtonPressed)
        {
            switch (Settings.UIProperties.DoubleClickBehavior)
            {
                case 1:
                    await windowViewModel.Mapper.ResetZoom();
                    break;
                case 2:
                    await windowViewModel.Mapper.ToggleFullscreen();
                    break;
            }
        }
    }
}

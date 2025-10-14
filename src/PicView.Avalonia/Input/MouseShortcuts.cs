using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Input;

public static class MouseShortcuts
{
    private static AutoScrollViewer? _imageScrollViewer;
    private static Func<PointerWheelEventArgs, Task>? _zoomIn;
    private static Func<PointerWheelEventArgs, Task>? _zoomOut;

    public static void InitializeMouseShortcuts(
        AutoScrollViewer imageScrollViewer,
        Func<PointerWheelEventArgs, Task> zoomIn,
        Func<PointerWheelEventArgs, Task> zoomOut)
    {
        _imageScrollViewer = imageScrollViewer;
        _zoomIn = zoomIn;
        _zoomOut = zoomOut;
    }

    public static async Task HandlePointerWheelChanged(PointerWheelEventArgs e)
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
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

                    await LoadNextPicAsync(reverse, vm);
                    return;
                }

                if (IsVerticalScrollBarVisible())
                {
                    ScrollVertically(reverse);
                }
                else
                {
                    await LoadNextPicAsync(reverse, vm);
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
                    if (_zoomOut is not null)
                    {
                        await _zoomOut(e);
                    }
                }
                else
                {
                    if (_zoomIn is not null)
                    {
                        await _zoomIn(e);
                    }
                }
            }
            else
            {
                await ScrollOrNavigateAsync(e, reverse, vm);
            }
        }
        else
        {
            if (ctrl)
            {
                await ScrollOrNavigateAsync(e, reverse, vm);
            }
            else
            {
                if (reverse)
                {
                    if (_zoomOut is not null)
                    {
                        await _zoomOut(e);
                    }
                }
                else
                {
                    if (_zoomIn is not null)
                    {
                        await _zoomIn(e);
                    }
                }
            }
        }
    }

    private static bool IsTouchPadOrTouch(PointerEventArgs e)
        => Settings.Zoom.IsUsingTouchPad || e.Pointer.Type == PointerType.Touch;

    private static bool IsVerticalScrollBarVisible()
        => _imageScrollViewer.VerticalScrollBarVisibility is ScrollBarVisibility.Visible or ScrollBarVisibility.Auto;

    private static void ScrollVertically(bool reverse)
    {
        if (reverse)
        {
            _imageScrollViewer.LineDown();
        }
        else
        {
            _imageScrollViewer.LineUp();
        }
    }

    private static async Task ScrollOrNavigateAsync(PointerWheelEventArgs e, bool reverse, MainViewModel mainViewModel)
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
            if (IsVerticalScrollBarVisible())
            {
                ScrollVertically(reverse);
            }
            else
            {
                await LoadNextPicAsync(reverse, mainViewModel);
            }
        }
    }

    private static async Task LoadNextPicAsync(bool reverse, MainViewModel mainViewModel)
    {
        if (Settings.Zoom.IsUsingTouchPad)
        {
            return;
        }

        var next = reverse ? Settings.Zoom.HorizontalReverseScroll : !Settings.Zoom.HorizontalReverseScroll;
        await NavigationManager.Navigate(next, mainViewModel, CancellationToken.None).ConfigureAwait(false);
    }
    
    public static async Task MainWindow_PointerPressed(PointerPressedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        var prop = e.GetCurrentPoint(topLevel).Properties;

        if (prop.IsXButton1Pressed)
        {
            if (Settings.Navigation.IsNavigatingFileHistory)
            {
                await FunctionsMapper.OpenPreviousFileHistoryEntry().ConfigureAwait(false);
            }
            else if (Settings.Navigation.IsNavigatingBetweenDirectories)
            {
                await FunctionsMapper.PrevFolder().ConfigureAwait(false);
            }
        }
        else if (prop.IsXButton2Pressed)
        {
            if (Settings.Navigation.IsNavigatingFileHistory)
            {
                await FunctionsMapper.OpenNextFileHistoryEntry().ConfigureAwait(false);
            }
            else if (Settings.Navigation.IsNavigatingBetweenDirectories)
            {
                await FunctionsMapper.NextFolder().ConfigureAwait(false);
            }
        }
    }
}

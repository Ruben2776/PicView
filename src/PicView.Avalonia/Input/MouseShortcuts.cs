using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.IPlatform;
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
        var reverse = e.Delta.Y < 0;

        var keybind = new Keybind(reverse ? MouseButton.WheelUp : MouseButton.WheelDown, e.KeyModifiers);
        if (Settings.Zoom.ScrollEnabled)
        {
            if (IsVerticalScrollBarVisible(imageScrollViewer))
            {
                ScrollVertically(reverse, imageScrollViewer);
                return;
            }
        }
        
        var actionName = KeybindingManager.GetActionName(keybind);
        if (actionName is null)
        {
            // Pressed key(s) have no associated function
            return;
        }
        
        var function = mainViewModel.Mapper.GetFunctionByName(actionName);
        if (function is null)
        {
            return;
        }

        if (string.Equals(function.Method.Name, nameof(IFunctionsMapper.ZoomIn), StringComparison.Ordinal))
        {
            await zoomIn(e).ConfigureAwait(false);
            return;
        }
        if (string.Equals(function.Method.Name, nameof(IFunctionsMapper.ZoomOut), StringComparison.Ordinal))
        {
            await zoomOut(e).ConfigureAwait(false);
            return;
        }

        if (!IsTouchPadOrTouch(e))
        {
            await function.Invoke().ConfigureAwait(false);
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
    
    public static async Task MainWindow_PointerPressed(PointerPressedEventArgs e, MainWindow window)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        var prop = e.GetCurrentPoint(topLevel).Properties;

        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        // Handle double click (only for the left mouse button, so that rapid
        // double-clicks of the side buttons don't fall through and trigger the
        // left-button double-click behavior such as toggling fullscreen)
        if (e.ClickCount is 2 && prop.IsLeftButtonPressed)
        {
            // Prevent unintended behavior when double-clicking
            if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
            {
                if (imageViewer.GalleryView.IsPointerOver)
                {
                    return;
                }

                if (imageViewer.ClickArrowLeft.IsPointerOver || imageViewer.ClickArrowRight.IsPointerOver)
                {
                    return;
                }
            }

            if (window.SharedBottomBar.IsPointerOver || window.SharedTitleBar.IsPointerOver)
            {
                return;
            }
            
            switch (Settings.UIProperties.DoubleClickBehavior)
            {
                case 1:
                    await vm.Mapper.ResetZoom().ConfigureAwait(false);
                    return;
                case 2:
                    await vm.Mapper.ToggleFullscreen().ConfigureAwait(false);
                    return;
            }
        }

        Keybind? currentKeys;
        if (prop.IsMiddleButtonPressed)
        {
            currentKeys = new Keybind(MouseButton.Middle, e.KeyModifiers);
        }
        else if (prop.IsXButton1Pressed)
        {
            currentKeys = new Keybind(MouseButton.XButton1, e.KeyModifiers);
        }
        else if (prop.IsXButton2Pressed)
        {
            currentKeys = new Keybind(MouseButton.XButton2, e.KeyModifiers);
        }
        else
        {
            return;
        }
        // Get the action string name quickly via dictionary lookup
        var actionName = KeybindingManager.GetActionName(currentKeys.Value);
        if (actionName is null)
        {
            // Pressed key(s) have no associated function
            return;
        }

        // Map the string to the instance-specific function using the view model's mapper
        var function = vm.Mapper.GetFunctionByName(actionName);
        if (function is not null)
        {
            await function.Invoke().ConfigureAwait(false);
        }
    }
}

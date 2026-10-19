using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

/// <summary>
/// What clicking does while the image viewer is shown: a left double-click resets the zoom or toggles fullscreen,
/// a left click navigates when navigating by clicking is on, and the middle and side buttons run their Keybinds.
/// </summary>
public static class ImageViewerClickBehavior
{
    public static async Task PointerPressedAsync(PointerPressedEventArgs e, MainWindow window)
    {
        var state = InputModes.StateOf(window);
        if (state.Resolve() is InputMode.Dialog)
        {
            return;
        }
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

        if (prop.IsLeftButtonPressed)
        {
            // Handle double click (only for the left mouse button, so that rapid
            // double-clicks of the side buttons don't fall through and trigger the
            // left-button double-click behavior such as toggling fullscreen)
            if (e.ClickCount is 2)
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
            else if (Settings.Navigation.IsNavigatingByClicking)
            {
                if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer && imageViewer.MainPanel.IsPointerOver)
                {
                    if (imageViewer.GalleryView.IsPointerOver)
                    {
                        return;
                    }

                    if (imageViewer.ClickArrowLeft.IsPointerOver || imageViewer.ClickArrowRight.IsPointerOver)
                    {
                        return;
                    }
                    var point = e.GetPosition(imageViewer);
                    var isLeftSide = point.X < imageViewer.Bounds.Width / 2;
                    var isRightSide = point.X >= imageViewer.Bounds.Width / 2;
                    if (isLeftSide)
                    {
                        imageViewer.Cursor = new Cursor(StandardCursorType.LeftSide);
                        await vm.Mapper.Prev().ConfigureAwait(false);
                    }
                    else if (isRightSide)
                    {
                        await vm.Mapper.Next().ConfigureAwait(false);
                    }
                }
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

        await window.KeybindRouter.MouseButtonAsync(currentKeys.Value).ConfigureAwait(false);
    }
}

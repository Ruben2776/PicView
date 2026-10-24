using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Input;

/// <summary>
/// What clicking does while the image viewer is shown: a left double-click resets the zoom or toggles fullscreen,
/// a left click navigates when navigating by clicking is on, and the middle and side buttons run their Keybinds.
/// On the start menu a left double-click on empty space toggles fullscreen when that is the double-click setting.
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
        if (Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
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
                var view = vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue;
                if (view is ImageViewer imageViewer)
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
                else if (view is StartUpMenu startUpMenu && startUpMenu.Buttons.IsPointerOver)
                {
                    return;
                }

                if (window.SharedBottomBar.IsPointerOver || window.SharedTitleBar.IsPointerOver)
                {
                    return;
                }

                switch (DoubleClickActionFor(Settings.UIProperties.DoubleClickBehavior, canResetZoom: view is ImageViewer))
                {
                    case DoubleClickAction.ResetZoom:
                        await vm.ActionRunner.Run(Actions.ResetZoom).ConfigureAwait(false);
                        return;
                    case DoubleClickAction.ToggleFullscreen:
                        await vm.ActionRunner.Run(Actions.ToggleFullscreen).ConfigureAwait(false);
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
                        await vm.ActionRunner.Run(Actions.Prev).ConfigureAwait(false);
                    }
                    else if (isRightSide)
                    {
                        await vm.ActionRunner.Run(Actions.Next).ConfigureAwait(false);
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

    /// <summary>
    /// What a left double-click does with <paramref name="doubleClickBehavior"/> (1 resets the zoom, 2 toggles
    /// fullscreen). Resetting the zoom only applies where there is an image to zoom, so it does nothing on the start menu.
    /// </summary>
    internal static DoubleClickAction DoubleClickActionFor(int doubleClickBehavior, bool canResetZoom) =>
        doubleClickBehavior switch
        {
            1 when canResetZoom => DoubleClickAction.ResetZoom,
            2 => DoubleClickAction.ToggleFullscreen,
            _ => DoubleClickAction.None
        };
}

internal enum DoubleClickAction
{
    None,
    ResetZoom,
    ToggleFullscreen
}

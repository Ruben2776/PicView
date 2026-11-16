using Avalonia;
using Avalonia.Controls;
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
        var prop = e.GetCurrentPoint(window).Properties;

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
                var view = vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value;
                switch (view)
                {
                    case NavigableView nav when nav.GalleryView.IsPointerOver:
                        return;
                    case NavigableView nav when nav.ClickArrowLeft.IsPointerOver || nav.ClickArrowRight.IsPointerOver:
                    case StartUpMenu startUpMenu when startUpMenu.Buttons.IsPointerOver:
                        return;
                    // A quick second alignment point, or a double-click on a split handle, is the viewer's
                    case NavigableView { Viewer: { } viewer } when viewer.TakesPress(e):
                        return;
                }

                if (window.SharedBottomBar.IsPointerOver || window.SharedTitleBar.IsPointerOver)
                {
                    return;
                }

                switch (DoubleClickActionFor(Settings.UIProperties.DoubleClickBehavior, canResetZoom: view is NavigableView))
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
                // Any viewer in the slot navigates by clicking, unless it is zoomed, where a press pans, or it takes
                // the press, as a split handle or an alignment point does. This runs from the window's tunnel, before
                // the viewer's own controls see the press, so handling it there would not stop this
                if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value is NavigableView
                    {
                        Viewer: { IsZoomed: false } viewer
                    } nav &&
                    nav.ContentPanel.IsPointerOver && !viewer.TakesPress(e))
                {
                    if (nav.GalleryView.IsPointerOver)
                    {
                        return;
                    }

                    if (nav.ClickArrowLeft.IsPointerOver || nav.ClickArrowRight.IsPointerOver)
                    {
                        return;
                    }

                    var point = e.GetPosition(nav);
                    var isLeftSide = point.X < nav.Bounds.Width / 2;
                    var isRightSide = point.X >= nav.Bounds.Width / 2;
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

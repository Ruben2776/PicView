﻿using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.Editing;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic.Sizing;
using System.Windows;
using System.Windows.Input;
using PicView.Core.Navigation;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.ConfigureWindows;
using static PicView.WPF.UILogic.TransformImage.Scroll;
using static PicView.WPF.UILogic.TransformImage.ZoomLogic;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.Shortcuts;

internal static class MainMouseKeys
{
    internal static void MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (GetMainWindow.TitleText.InnerTextBox.IsKeyboardFocusWithin)
        {
            // Fix focus
            EditTitleBar.Refocus();
            return;
        }

        if (ColorPicking.IsRunning)
        {
            ColorPicking.StopRunning(true);
            return;
        }

        // Move window when Shift is being held down
        if (SettingsHelper.Settings.UIProperties.ShowInterface == false ||
            (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            WindowSizing.Move(sender, e);
            return;
        }

        // Fix focus
        EditTitleBar.Refocus();

        // Logic for auto scrolling
        if (IsAutoScrolling)
        {
            // Report position and enable autoscrolltimer
            AutoScrollOrigin = e.GetPosition(GetMainWindow);
            AutoScrollTimer.Enabled = true;
            return;
        }

        // Reset zoom on double click
        if (e.ClickCount == 2)
        {
            ResetZoom();
            return;
        }

        // Drag logic
        if (SettingsHelper.Settings.Zoom.ScrollEnabled == false &&
            GetMainWindow.MainImage
                .IsMouseDirectlyOver) // Only send it when mouse over to not disturb other mouse events
        {
            PreparePanImage(sender, e);
        }
    }

    internal static async Task MouseButtonDownAsync(object sender, MouseButtonEventArgs e)
    {
        switch (e.ChangedButton)
        {
            case MouseButton.Right:
                // Stop running color picking when right clicking
                if (ColorPicking.IsRunning)
                {
                    ColorPicking.StopRunning(false);
                }
                else if (IsAutoScrolling)
                {
                    StopAutoScroll();
                }

                break;

            case MouseButton.Left:
                if (IsAutoScrolling)
                {
                    StopAutoScroll();
                }

                break;

            case MouseButton.Middle:
                if (!IsAutoScrolling)
                {
                    StartAutoScroll(e);
                }
                else
                {
                    StopAutoScroll();
                }

                break;

            case MouseButton.XButton1:
                await FileHistoryNavigation.PrevAsync().ConfigureAwait(false);
                break;

            case MouseButton.XButton2:
                await FileHistoryNavigation.NextAsync().ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal static void MainImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Mouse.Captured != null)
        {
            GetMainWindow.MainImage.ReleaseMouseCapture();
        }

        // Stop auto-scrolling or dragging image
        if (IsAutoScrolling)
        {
            StopAutoScroll();
        }
    }

    /// <summary>
    /// Used to drag image
    /// or getting position for AutoSrollTimer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal static void MainImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (IsAutoScrolling)
        {
            AutoScrollPos = e.GetPosition(GetMainWindow.Scroller);
            AutoScrollTimer.Start();
        }

        if (ColorPicking.IsRunning)
        {
            if (GetColorPicker.Opacity is 1)
            {
                ColorPicking.StartRunning();
            }
        }
        else if (Mouse.LeftButton == MouseButtonState.Pressed)
            PanImage(sender, e);
    }

    /// <summary>
    /// Zooms, scrolls or changes image with mousewheel
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal static async Task MainImage_MouseWheelAsync(object sender, MouseWheelEventArgs e)
    {
        // Don't execute keys when entering in GoToPicBox || QuickResize
        if (ShouldIgnoreMouseWheel())
        {
            return;
        }

        // Disable normal scroll, so we can use our own values
        e.Handled = true;

        // Make sure not to fire off events when auto-scrolling
        if (IsAutoScrolling)
        {
            return;
        }

        // Determine horizontal scrolling direction
        var direction = SettingsHelper.Settings.Zoom.HorizontalReverseScroll ? e.Delta > 0 : e.Delta < 0;

        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown && !GalleryFunctions.IsGalleryOpen)
        {
            await HandleFullscreenGalleryAsync(direction, e).ConfigureAwait(false);
        }
        else if (GalleryFunctions.IsGalleryOpen)
        {
            await GetMainWindow.Dispatcher.InvokeAsync(() =>
                GalleryNavigation.ScrollGallery(direction, false,
                    (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift, false));
        }
        else if (ShouldHandleScroll())
        {
            await GetMainWindow.Dispatcher.InvokeAsync(() => HandleScroll(direction));
        }
        else
        {
            await HandleNavigateOrZoomAsync(direction, e).ConfigureAwait(false);
        }
    }

    private static bool ShouldIgnoreMouseWheel()
    {
        if (GetImageSettingsMenu?.GoToPic != null && GetImageSettingsMenu.GoToPic.GoToPicBox.IsKeyboardFocusWithin)
        {
            return true;
        }

        return
            GetQuickResize != null && (GetQuickResize.WidthBox.IsKeyboardFocused ||
                                       GetQuickResize.HeightBox.IsKeyboardFocused);
    }

    private static bool ShouldHandleScroll()
    {
        return SettingsHelper.Settings.Zoom.ScrollEnabled
               && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible
               && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift
               && Math.Abs(GetMainWindow.Scroller.ExtentHeight - GetMainWindow.Scroller.ViewportHeight) > 1;
    }

    private static async Task HandleFullscreenGalleryAsync(bool direction, MouseWheelEventArgs e)
    {
        if (GetPicGallery is not null && GetPicGallery.IsMouseOver)
        {
            await GetMainWindow.Dispatcher.InvokeAsync(() =>
                GalleryNavigation.ScrollGallery(direction, false,
                    (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift, false));
        }
        else
        {
            await HandleNavigateOrZoomAsync(direction, e).ConfigureAwait(false);
        }
    }

    private static void HandleScroll(bool direction)
    {
        const int zoomSpeed = 40;

        if (direction)
        {
            GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - zoomSpeed);
        }
        else
        {
            GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + zoomSpeed);
        }
    }

    private static async Task HandleNavigateOrZoomAsync(bool direction, MouseWheelEventArgs e)
    {
        var next = direction ? NavigateTo.Previous : NavigateTo.Next;

        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            await ScrollOrNext().ConfigureAwait(false);
        }
        else
        {
            await ZoomOrNext(SettingsHelper.Settings.Zoom.CtrlZoom).ConfigureAwait(false);
        }
        return;

        async Task ScrollOrNext()
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ||
                GetMainWindow.Scroller.ScrollableHeight is 0)
            {
                await GoToNextImage(next).ConfigureAwait(false);
            }
            else
            {
                await GetMainWindow.Dispatcher.InvokeAsync(() => HandleScroll(e.Delta > 0));
            }
        }

        async Task ZoomOrNext(bool ctrlZoom)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ctrlZoom)
                {
                    await GetMainWindow.Dispatcher.InvokeAsync(() => Zoom(e.Delta > 0));
                }
                else
                {
                    await GoToNextImage(next).ConfigureAwait(false);
                }
            }
            else if (ctrlZoom)
            {
                await GoToNextImage(next).ConfigureAwait(false);
            }
            else
            {
                await GetMainWindow.Dispatcher.InvokeAsync(() => Zoom(e.Delta > 0));
            }
        }
    }
}
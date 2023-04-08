using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.Editing;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.DragAndDrop;
using PicView.UILogic.Sizing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.TransformImage.Scroll;
using static PicView.UILogic.TransformImage.ZoomLogic;
using static PicView.UILogic.UC;

namespace PicView.Shortcuts
{
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

            if (Color_Picking.IsRunning)
            {
                Color_Picking.StopRunning(true);
                return;
            }

            // Move window when Shift is being held down
            if (Properties.Settings.Default.ShowInterface == false || (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
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
            if (Settings.Default.ScrollEnabled == false && GetMainWindow.MainImage.IsMouseDirectlyOver) // Only send it when mouse over to not disturb other mouse events
            {
                PreparePanImage(sender, e);
            }
        }

        internal static async Task MouseButtonDownAsync(object sender, MouseButtonEventArgs e)
        {
            if (GetFileHistory is null)
            {
                GetFileHistory = new FileHistory();
            }

            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                    // Stop running color picking when right clicking
                    if (Color_Picking.IsRunning)
                    {
                        Color_Picking.StopRunning(false);
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
                    await GetFileHistory.PrevAsync().ConfigureAwait(false);
                    break;

                case MouseButton.XButton2:
                    await GetFileHistory.NextAsync().ConfigureAwait(false);
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
            // Stop autoscrolling or dragging image
            if (IsAutoScrolling)
            {
                StopAutoScroll();
            }
        }

        /// <summary>
        /// Used to drag image
        /// or getting position for autoscrolltimer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsAutoScrolling)
            {
                // Start automainWindow.Scroller and report position
                AutoScrollPos = e.GetPosition(GetMainWindow.Scroller);
                AutoScrollTimer.Start();
            }

            if (Color_Picking.IsRunning)
            {
                if (GetColorPicker.Opacity == 1)
                {
                    Color_Picking.StartRunning();
                }

                return;
            }

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

            // Make sure not to fire off events when autoscrolling
            if (IsAutoScrolling)
            {
                return;
            }

            // Determine horizontal scrolling direction
            bool direction = Settings.Default.HorizontalReverseScroll ? e.Delta > 0 : e.Delta < 0;

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await HandleFullscreenGalleryAsync(direction, e).ConfigureAwait(false);
            }
            else if (GalleryFunctions.IsHorizontalOpen)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => 
                    GalleryNavigation.ScrollTo(direction, false, (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift));
            }

            else if (ShouldHandleScroll())
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => HandleScroll(direction));
            }

            else
            {
                await HandleNavigateOrZoomAsync(direction, e).ConfigureAwait(false);
            }
        }

        private static bool ShouldIgnoreMouseWheel()
        {
            if (GetImageSettingsMenu.GoToPic != null && GetImageSettingsMenu.GoToPic.GoToPicBox.IsKeyboardFocusWithin)
            {
                return true;
            }

            if (GetQuickResize != null && (GetQuickResize.WidthBox.IsKeyboardFocused || GetQuickResize.HeightBox.IsKeyboardFocused))
            {
                return true;
            }

            return false;
        }

        private static bool ShouldHandleScroll()
        {
            return Settings.Default.ScrollEnabled
                   && GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible
                   && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift
                   && Math.Abs(GetMainWindow.Scroller.ExtentHeight - GetMainWindow.Scroller.ViewportHeight) > 1;
        }

        private static async Task HandleFullscreenGalleryAsync(bool direction, MouseWheelEventArgs e)
        {
            if (GetPicGallery is not null && GetPicGallery.IsMouseOver)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => GalleryNavigation.ScrollTo(direction, false, true));
            }
            else
            {
                await HandleNavigateOrZoomAsync(direction, e).ConfigureAwait(false);
            }
        }

        private static void HandleScroll(bool direction)
        {
            var zoomSpeed = 40;

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
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (Settings.Default.CtrlZoom)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => Zoom(e.Delta > 0));
                }
                else
                {
                    await Navigation.GoToNextImage(next).ConfigureAwait(false);
                }
            }
            else
            {
                if (Settings.Default.CtrlZoom)
                {
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            await Navigation.GoToNextImage(next).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Navigation.GoToNextImage(next).ConfigureAwait(false);
                    }
                }
                else
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => Zoom(e.Delta > 0));
                }
            }
        }
    }
}
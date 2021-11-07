using PicView.Editing;
using PicView.PicGallery;
using PicView.UILogic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.TransformImage.Scroll;
using static PicView.UILogic.TransformImage.ZoomLogic;
using static PicView.UILogic.UC;

namespace PicView.Shortcuts
{
    internal static class MainMouseKeys
    {
        internal static async Task MouseLeftButtonDownAsync(object sender, MouseButtonEventArgs e)
        {
            if (GetMainWindow.TitleText.InnerTextBox.IsKeyboardFocusWithin)
            {
                // Fix focus
                EditTitleBar.Refocus();
                return;
            }

            if (Color_Picking.IsRunning)
            {
                await Color_Picking.StopRunningAsync(true).ConfigureAwait(false);
                return;
            }

            // Move window when Shift is being held down
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                UILogic.Sizing.WindowSizing.Move(sender, e);
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
            if (Properties.Settings.Default.ScrollEnabled == false && GetMainWindow.MainImage.IsMouseDirectlyOver) // Only send it when mouse over to not disturb other mouse events
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
                    if (Color_Picking.IsRunning)
                    {
                        await Color_Picking.StopRunningAsync(false).ConfigureAwait(false);
                    }
                    else if (IsAutoScrolling)
                    {
                        StopAutoScroll();
                        return;
                    }
                    break;

                case MouseButton.Left:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        UILogic.DragAndDrop.DragToExplorer.DragFile(sender, e);
                    }
                    if (IsAutoScrolling)
                    {
                        StopAutoScroll();
                        return;
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
                    await ChangeImage.History.PrevAsync().ConfigureAwait(false);
                    break;

                case MouseButton.XButton2:
                    await ChangeImage.History.NextAsync().ConfigureAwait(false);
                    break;

                default: break;
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
            // Disable normal scroll, so we can use our own values
            e.Handled = true;

            // Make sure not to fire off events when autoscrolling
            if (IsAutoScrolling)
            {
                return;
            }

            // Determine horizontal scrolling direction
            bool dir = Properties.Settings.Default.HorizontalReverseScroll ? e.Delta < 0 : e.Delta > 0;

            // 1. Handle horizontal gallery
            if (GalleryFunctions.IsHorizontalOpen)
            {
                if (Properties.Settings.Default.FullscreenGalleryHorizontal && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    await NavigateToPicAsync(dir).ConfigureAwait(false);
                }
                else
                {
                    GalleryNavigation.ScrollTo(sender, e);
                }

                return;
            }

            // 2. Handle scroll enabled and shift not held down
            if (Properties.Settings.Default.ScrollEnabled && ConfigureWindows.GetMainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Visible
                && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                var zoomSpeed = 40;

                if (e.Delta > 0)
                {
                    GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset - zoomSpeed);
                }
                else
                {
                    GetMainWindow.Scroller.ScrollToVerticalOffset(GetMainWindow.Scroller.VerticalOffset + zoomSpeed);
                }

                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (Properties.Settings.Default.CtrlZoom)
                {
                    Zoom(e.Delta > 0);
                }
                else
                {
                    await NavigateToPicAsync(dir).ConfigureAwait(false);
                }
            }
            else
            {
                if (Properties.Settings.Default.CtrlZoom)
                {
                    await NavigateToPicAsync(dir).ConfigureAwait(false);
                }
                else
                {
                    Zoom(e.Delta > 0);
                }
            }
        }
    }
}

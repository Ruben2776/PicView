using PicView.UI.PicGallery;
using PicView.UI.Sizing;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.Library.Fields;
using static PicView.Library.Utilities;
using static PicView.UI.Sizing.WindowLogic;

namespace PicView.UI.TransformImage
{
    internal static class ZoomLogic
    {
        /// <summary>
        /// Returns zoom percentage. if 100%, return empty string
        /// </summary>
        internal static string ZoomPercentage
        {
            get
            {
                var zoom = Math.Round(AspectRatio * 100);

                if (st == null)
                {
                    return string.Empty;
                }

                if (st.ScaleX == 1)
                {
                    return string.Empty;
                }

                return zoom + "%";
            }
        }

        /// <summary>
        /// Returns aspect ratio as a formatted string
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        internal static string StringAspect(int width, int height)
        {
            var gcd = GCD(width, height);
            var x = width / gcd;
            var y = height / gcd;

            if (x == width && y == height || x > 16 || y > 9)
            {
                return ") ";
            }

            return ", " + x + ":" + y + ") ";
        }

        // Zoom
        /// <summary>
        /// Pan and Zoom, reset zoom and double click to reset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Zoom_img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Move window when Shift is being held down
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                Move(sender, e);
                return;
            }

            // Fix focus
            EditTitleBar.Refocus();

            // Logic for auto scrolling
            if (AutoScrolling)
            {
                // Report position and enable autoscrolltimer
                autoScrollOrigin = e.GetPosition(mainWindow);
                autoScrollTimer.Enabled = true;
                return;
            }
            // Reset zoom on double click
            if (e.ClickCount == 2)
            {
                ResetZoom();
                return;
            }
            // Drag logic
            if (!Scroll.IsScrollEnabled)
            {
                // Report position for image drag
                mainWindow.img.CaptureMouse();
                start = e.GetPosition(mainWindow);
                origin = new Point(tt.X, tt.Y);
            }
        }

        internal static void Bg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (mainWindow.Bar.Bar.IsKeyboardFocusWithin)
            {
                // Fix focus
                EditTitleBar.Refocus();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Zoom_img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Stop autoscrolling or dragging image
            if (AutoScrolling)
            {
                Scroll.StopAutoScroll();
            }
            else
            {
                mainWindow.img.ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// Used to drag image
        /// or getting position for autoscrolltimer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Zoom_img_MouseMove(object sender, MouseEventArgs e)
        {
            if (AutoScrolling)
            {
                // Start automainWindow.Scroller and report position
                autoScrollPos = e.GetPosition(mainWindow.Scroller);
                autoScrollTimer.Start();
            }

            // Don't drag when full scale
            // and don't drag it if mouse not held down on image
            if (!mainWindow.img.IsMouseCaptured || st.ScaleX == 1)
            {
                return;
            }

            // Drag image by modifying X,Y coordinates
            var v = start - e.GetPosition(mainWindow);
            tt.X = origin.X - v.X;
            tt.Y = origin.Y - v.Y;
            e.Handled = true;
        }

        /// <summary>
        /// Zooms, scrolls or changes image with mousewheel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Zoom_img_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Disable normal scroll, so we can use our own values
            e.Handled = true;

            if (Properties.Settings.Default.ScrollEnabled && !AutoScrolling)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    Zoom(e.Delta, true); // Scale zoom by holding Ctrl when scroll is enabled
                }
                else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    Pic(e.Delta > 0);
                }
                else
                {
                    // Scroll vertical when scroll enabled
                    var zoomSpeed = 45;
                    if (e.Delta > 0)
                    {
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset - zoomSpeed);
                    }
                    else if (e.Delta < 0)
                    {
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + zoomSpeed);
                    }
                }
            }
            // Change image with shift being held down
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                Pic(e.Delta > 0);
            }
            // Scale when Ctrl being held down
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !AutoScrolling)
            {
                Zoom(e.Delta, true);
            }
            // Zoom
            else if (!AutoScrolling)
            {
                Zoom(e.Delta, false);
            }
        }

        /// <summary>
        /// Manipulates the required elements to allow zooming
        /// by modifying ScaleTransform and TranslateTransform
        /// </summary>
        internal static void InitializeZoom()
        {
            // Set center
            //mainWindow.img.RenderTransformOrigin = new Point(0.5, 0.5); // Already set in xaml

            // Add children, which can be manipulated ;)
            mainWindow.img.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection {
                            new ScaleTransform(),
                            new TranslateTransform()
                        }
            };

            mainWindow.imgBorder.IsManipulationEnabled = true;
            mainWindow.Scroller.ClipToBounds = mainWindow.img.ClipToBounds = true;

            // Add children to fields
            st = (ScaleTransform)((TransformGroup)mainWindow.img.RenderTransform).Children.First(tr => tr is ScaleTransform);
            tt = (TranslateTransform)((TransformGroup)mainWindow.img.RenderTransform).Children.First(tr => tr is TranslateTransform);
        }

        /// <summary>
        /// Resets element values to their loaded values
        /// </summary>
        internal static void ResetZoom()
        {
            if (mainWindow.img.Source == null) { return; }

            // Scale to default
            var scaletransform = new ScaleTransform();
            scaletransform.ScaleX = scaletransform.ScaleY = 1.0;
            mainWindow.img.LayoutTransform = scaletransform;

            st.ScaleX = st.ScaleY = 1;
            tt.X = tt.Y = 0;
            mainWindow.img.RenderTransformOrigin = new Point(0.5, 0.5);

            Tooltip.CloseToolTipMessage();
            IsZoomed = false;

            // Reset size
            ScaleImage.TryFitImage();

            // Display non-zoomed values
            if (Pics.Count == 0)
            {
                /// Display values from web
                SetTitle.SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height);
            }
            else
            {
                SetTitle.SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, FolderIndex);
            }

            IsZoomed = false;
        }

        /// <summary>
        /// Scales or zooms, depending on given values
        /// </summary>
        /// <param name="i"></param>
        /// <param name="zoomMode"></param>
        internal static void Zoom(int i, bool zoomMode)
        {
            /// Don't zoom when gallery is open
            if (UserControls.UC.picGallery != null)
            {
                if (GalleryFunctions.IsOpen)
                {
                    return;
                }
            }

            /// Scales the window with img.LayoutTransform
            if (zoomMode)
            {
                /// Start from 1 or zoom value
                if (IsZoomed)
                {
                    AspectRatio += i > 0 ? .01 : -.01;
                }
                else
                {
                    AspectRatio = 1;
                }

                var scaletransform = new ScaleTransform();

                scaletransform.ScaleX = scaletransform.ScaleY = AspectRatio;
                mainWindow.img.LayoutTransform = scaletransform;
            }

            /// Pan and zoom
            else
            {
                /// Get position where user points cursor
                var position = Mouse.GetPosition(mainWindow.img);

                /// Use our position as starting point for zoom
                mainWindow.img.RenderTransformOrigin = new Point(position.X / xWidth, position.Y / xHeight);

                /// Determine zoom speed
                var zoomValue = st.ScaleX > 1.3 ? .03 : .01;
                if (st.ScaleX > 1.5)
                {
                    zoomValue += .005;
                }

                if (st.ScaleX > 1.7)
                {
                    zoomValue += .007;
                }

                if (st.ScaleX >= 1.0 && st.ScaleX + zoomValue >= 1.0 || st.ScaleX - zoomValue >= 1.0)
                {
                    zoomValue = i > 0 ? zoomValue : -zoomValue;
                    // Start zoom
                    st.ScaleY = st.ScaleX += zoomValue;
                    AspectRatio += zoomValue;
                }

                if (st.ScaleX < 1.0)
                {
                    /// Don't zoom less than 1.0, does not work so good...
                    st.ScaleX = st.ScaleY = 1.0;
                }
                //zoomValue = i > 0 ? zoomValue : -zoomValue;
                //st.ScaleY = st.ScaleX += zoomValue;
            }

            IsZoomed = true;

            /// Displays zoompercentage in the center window
            if (!string.IsNullOrEmpty(ZoomPercentage))
            {
                Tooltip.ShowTooltipMessage(ZoomPercentage, true);
            }
            else
            {
                Tooltip.CloseToolTipMessage();
            }

            /// Display updated values
            if (Pics.Count == 0)
            {
                /// Display values from web
                SetTitle.SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height);
            }
            else
            {
                SetTitle.SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, FolderIndex);
            }
        }
    }
}
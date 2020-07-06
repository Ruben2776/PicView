using PicView.Library;
using PicView.SystemIntegration;
using PicView.UI.PicGallery;
using PicView.UI.UserControls;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static PicView.ChangeImage.Navigation;
using static PicView.Library.Fields;
using static PicView.Library.Utilities;
using static PicView.UI.Sizing.WindowLogic;
using static PicView.UI.TransformImage.Scroll;

namespace PicView.UI.TransformImage
{
    internal static class ZoomLogic
    {
        private static ScaleTransform scaleTransform;
        private static TranslateTransform translateTransform;
        private static Point origin;
        private static Point start;

        /// Used to determine final point when zooming,
        /// since DoubleAnimation changes value of
        /// TranslateTransform continuesly.
        internal static double ZoomValue { get; set; }

        /// <summary>
        /// Returns zoom percentage. if 100%, return empty string
        /// </summary>
        internal static string ZoomPercentage
        {
            get
            {
                if (scaleTransform == null || ZoomValue <= 1)
                {
                    return string.Empty;
                }

                var zoom = Math.Round(ZoomValue * 100);

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

        /// <summary>
        /// Manipulates the required elements to allow zooming
        /// by modifying ScaleTransform and TranslateTransform
        /// </summary>
        internal static void InitializeZoom()
        {
            // Initialize transforms
            TheMainWindow.MainImage.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection {
                            new ScaleTransform(),
                            new TranslateTransform()
                        }
            };

            TheMainWindow.Scroller.ClipToBounds = TheMainWindow.MainImage.ClipToBounds = true;

            // Set transforms to UI elements
            scaleTransform = (ScaleTransform)((TransformGroup)TheMainWindow.MainImage.RenderTransform).Children.First(tr => tr is ScaleTransform);
            translateTransform = (TranslateTransform)((TransformGroup)TheMainWindow.MainImage.RenderTransform).Children.First(tr => tr is TranslateTransform);
        }

        // Zoom
        /// <summary>
        /// Pan and Zoom, reset zoom and double click to reset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Move window when Shift is being held down
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                Move(sender, e);
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                Editing.Color_Picking.StopRunning(true);
            }

            // Fix focus
            EditTitleBar.Refocus();

            // Logic for auto scrolling
            if (IsAutoScrolling)
            {
                // Report position and enable autoscrolltimer
                AutoScrollOrigin = e.GetPosition(TheMainWindow);
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
            if (!IsScrollEnabled)
            {
                // Report position for image drag
                TheMainWindow.MainImage.CaptureMouse();
                start = e.GetPosition(TheMainWindow.MainImageBorder);
                origin = new Point(translateTransform.X, translateTransform.Y);
            }
        }

        internal static void Bg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TheMainWindow.TitleText.InnerTextBox.IsKeyboardFocusWithin)
            {
                // Fix focus
                EditTitleBar.Refocus();
                return;
            }

            // Reset zoom on double click
            if (e.ClickCount == 2)
            {
                ResetZoom();
                return;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Stop autoscrolling or dragging image
            if (IsAutoScrolling)
            {
                StopAutoScroll();
            }
            else
            {
                TheMainWindow.MainImage.ReleaseMouseCapture();
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
                AutoScrollPos = e.GetPosition(TheMainWindow.Scroller);
                AutoScrollTimer.Start();
            }

            // Don't drag when full scale
            // and don't drag it if mouse not held down on image
            if (!TheMainWindow.MainImage.IsMouseCaptured || scaleTransform.ScaleX == 1)
            {
                return;
            }

            // Drag image by modifying X,Y coordinates
            var dragMousePosition = start - e.GetPosition(TheMainWindow);

            var newXproperty = origin.X - dragMousePosition.X;
            var newYproperty = origin.Y - dragMousePosition.Y;

            var isXOutOfBorder = TheMainWindow.MainImageBorder.ActualWidth < (TheMainWindow.MainImage.ActualWidth * scaleTransform.ScaleX);
            var isYOutOfBorder = TheMainWindow.MainImageBorder.ActualHeight < (TheMainWindow.MainImage.ActualHeight * scaleTransform.ScaleY);
            if ((isXOutOfBorder && newXproperty > 0) || (!isXOutOfBorder && newXproperty < 0))
            {
                newXproperty = 0;
            }
            if ((isYOutOfBorder && newYproperty > 0) || (!isYOutOfBorder && newYproperty < 0))
            {
                newYproperty = 0;
            }
            var maxX = TheMainWindow.MainImageBorder.ActualWidth - (TheMainWindow.MainImage.ActualWidth * scaleTransform.ScaleX);
            if ((isXOutOfBorder && newXproperty < maxX) || (!isXOutOfBorder && newXproperty > maxX))
            {
                newXproperty = maxX;
            }
            var maxY = TheMainWindow.MainImageBorder.ActualHeight - (TheMainWindow.MainImage.ActualHeight * scaleTransform.ScaleY);
            if ((isXOutOfBorder && newYproperty < maxY) || (!isXOutOfBorder && newYproperty > maxY))
            {
                newYproperty = maxY;
            }

            translateTransform.X = newXproperty;
            translateTransform.Y = newYproperty;

            e.Handled = true;
        }

        /// <summary>
        /// Zooms, scrolls or changes image with mousewheel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Disable normal scroll, so we can use our own values
            e.Handled = true;

            if (Properties.Settings.Default.ScrollEnabled && !IsAutoScrolling)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    Pic(e.Delta > 0);
                }
                else
                {
                    // Scroll vertical when scroll enabled
                    var zoomSpeed = 45;
                    if (e.Delta > 0)
                    {
                        TheMainWindow.Scroller.ScrollToVerticalOffset(TheMainWindow.Scroller.VerticalOffset - zoomSpeed);
                    }
                    else if (e.Delta < 0)
                    {
                        TheMainWindow.Scroller.ScrollToVerticalOffset(TheMainWindow.Scroller.VerticalOffset + zoomSpeed);
                    }
                }
            }
            // Change image with shift being held down
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                Pic(e.Delta > 0);
            }
            // Zoom
            else if (!IsAutoScrolling && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Zoom(e.Delta > 0);
            }
        }

        /// <summary>
        /// Resets element values to their loaded values
        /// </summary>
        internal static void ResetZoom(bool animate = true)
        {
            if (TheMainWindow.MainImage.Source == null) { return; }

            if (animate)
            {
                BeginZoomAnimation(1);
            }
            else
            {
                scaleTransform.ScaleX = scaleTransform.ScaleY = 1.0;
                translateTransform.X = translateTransform.Y = 0.0;
            }

            Tooltip.CloseToolTipMessage();
            ZoomValue = 1;

            // Display non-zoomed values
            if (Pics.Count == 0)
            {
                /// Display values from web
                SetTitle.SetTitleString((int)TheMainWindow.MainImage.Source.Width, (int)TheMainWindow.MainImage.Source.Height);
            }
            else
            {
                SetTitle.SetTitleString((int)TheMainWindow.MainImage.Source.Width, (int)TheMainWindow.MainImage.Source.Height, FolderIndex);
            }

            UC.GetQuickSettingsMenu.ZoomSlider.Value = 1.0;
        }

        /// <summary>
        /// Scales or zooms, depending on given values
        /// </summary>
        /// <param name="i">increment</param>
        internal static void Zoom(bool increment)
        {
            /// Don't zoom when gallery is open
            if (UC.GetPicGallery != null)
            {
                if (GalleryFunctions.IsOpen)
                {
                    return;
                }
            }

            ZoomValue = scaleTransform.ScaleX;

            /// Determine zoom speed
            var zoomSpeed = .095;

            if (increment)
            {
                // Increase speed determined by how much is zoomed in
                if (ZoomValue > 1.2)
                {
                    zoomSpeed += .135;
                }
                if (ZoomValue > 1.5)
                {
                    zoomSpeed += .16;
                }
                if (ZoomValue > 1.8)
                {
                    zoomSpeed += .19;
                }
            }
            else
            {
                // Zoom out faster
                if (ZoomValue > 1.2)
                {
                    zoomSpeed += .4;
                }
                if (ZoomValue > 1.5)
                {
                    zoomSpeed += .55;
                }
                // Make it go negative
                zoomSpeed = -zoomSpeed;
            }

            // Set speed
            ZoomValue += zoomSpeed;

            if (ZoomValue < 1.0)
            {
                /// Don't zoom less than 1.0,
                ZoomValue = 1.0;
            }

            Zoom(ZoomValue);
        }

        internal static void Zoom(double value)
        {
            if (value > UC.GetQuickSettingsMenu.ZoomSlider.Maximum)
            {
                return;
            }

            ZoomValue = value;

            BeginZoomAnimation(ZoomValue);

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
                SetTitle.SetTitleString((int)TheMainWindow.MainImage.Source.Width, (int)TheMainWindow.MainImage.Source.Height);
            }
            else
            {
                SetTitle.SetTitleString((int)TheMainWindow.MainImage.Source.Width, (int)TheMainWindow.MainImage.Source.Height, FolderIndex);
            }

            if (UC.GetQuickSettingsMenu.ZoomSlider.Value != value)
            {
                UC.GetQuickSettingsMenu.ZoomSlider.Value = value;
            }

        }

        private static void BeginZoomAnimation(double zoomValue)
        {
            Point relative = Mouse.GetPosition(TheMainWindow.MainImage);

            // Calculate new position
            double absoluteX = relative.X * scaleTransform.ScaleX + translateTransform.X;
            double absoluteY = relative.Y * scaleTransform.ScaleY + translateTransform.Y;

            // Reset to zero if value is one, which is reset
            double newTranslateValueX = zoomValue > 1 ? absoluteX - relative.X * zoomValue : 0;
            double newTranslateValueY = zoomValue > 1 ? absoluteY - relative.Y * zoomValue : 0;


            var duration = new Duration(TimeSpan.FromSeconds(.3));

            var scaleAnim = new DoubleAnimation(zoomValue, duration)
            {
                // Set stop to make sure animation doesn't hold ownership of scaletransform
                FillBehavior = FillBehavior.Stop
            };

            scaleAnim.Completed += delegate
            {
                // Hack it to keep the intended value
                scaleTransform.ScaleX = scaleTransform.ScaleY = zoomValue;

                // Make sure value stays correct
                ZoomValue = 1.0;
            };

            var translateAnimX = new DoubleAnimation(translateTransform.X, newTranslateValueX, duration)
            {
                // Set stop to make sure animation doesn't hold ownership of translateTransform
                FillBehavior = FillBehavior.Stop
            };

            translateAnimX.Completed += delegate
            {
                // Hack it to keep the intended value
                translateTransform.X = newTranslateValueX;
            };

            var translateAnimY = new DoubleAnimation(translateTransform.Y, newTranslateValueY, duration)
            {
                // Set stop to make sure animation doesn't hold ownership of translateTransform
                FillBehavior = FillBehavior.Stop
            };

            translateAnimY.Completed += delegate
            {
                // Hack it to keep the intended value
                translateTransform.Y = newTranslateValueY;
            };

            // Start animations

            translateTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimX);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimY);

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

        }
    }
}
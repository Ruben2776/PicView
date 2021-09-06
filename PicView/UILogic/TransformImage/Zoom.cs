using PicView.PicGallery;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static PicView.ChangeImage.Navigation;

namespace PicView.UILogic.TransformImage
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
        /// Greatest Common Divisor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        internal static int GCD(int x, int y)
        {
            return y == 0 ? x : GCD(y, x % y);
        }

        /// <summary>
        /// Manipulates the required elements to allow zooming
        /// by modifying ScaleTransform and TranslateTransform
        /// </summary>
        internal static void InitializeZoom()
        {
            // Initialize transforms
            ConfigureWindows.GetMainWindow.MainImage.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection {
                            new ScaleTransform(),
                            new TranslateTransform()
                        }
            };

            ConfigureWindows.GetMainWindow.Scroller.ClipToBounds = ConfigureWindows.GetMainWindow.MainImage.ClipToBounds = true;

            // Set transforms to UI elements
            scaleTransform = (ScaleTransform)((TransformGroup)ConfigureWindows.GetMainWindow.MainImage.RenderTransform).Children.First(tr => tr is ScaleTransform);
            translateTransform = (TranslateTransform)((TransformGroup)ConfigureWindows.GetMainWindow.MainImage.RenderTransform).Children.First(tr => tr is TranslateTransform);
        }

        internal static void PreparePanImage(object sender, MouseButtonEventArgs e)
        {
            // Report position for image drag
            ConfigureWindows.GetMainWindow.MainImage.CaptureMouse();
            start = e.GetPosition(ConfigureWindows.GetMainWindow.MainImageBorder);
            origin = new Point(translateTransform.X, translateTransform.Y);
        }

        internal static void PanImage(object sender, MouseEventArgs e)
        {
            // Don't drag when full scale
            // and don't drag it if mouse not held down on image
            if (!ConfigureWindows.GetMainWindow.MainImage.IsMouseCaptured || scaleTransform.ScaleX == 1)
            {
                return;
            }

            // Drag image by modifying X,Y coordinates
            var dragMousePosition = start - e.GetPosition(ConfigureWindows.GetMainWindow);

            var newXproperty = origin.X - dragMousePosition.X;
            var newYproperty = origin.Y - dragMousePosition.Y;

            // Keep panning it in bounds if in normal window
            if (Properties.Settings.Default.Fullscreen == false)
            {
                var isXOutOfBorder = ConfigureWindows.GetMainWindow.MainImageBorder.ActualWidth < (ConfigureWindows.GetMainWindow.MainImage.ActualWidth * scaleTransform.ScaleX);
                var isYOutOfBorder = ConfigureWindows.GetMainWindow.MainImageBorder.ActualHeight < (ConfigureWindows.GetMainWindow.MainImage.ActualHeight * scaleTransform.ScaleY);
                if ((isXOutOfBorder && newXproperty > 0) || (!isXOutOfBorder && newXproperty < 0))
                {
                    newXproperty = 0;
                }
                if ((isYOutOfBorder && newYproperty > 0) || (!isYOutOfBorder && newYproperty < 0))
                {
                    newYproperty = 0;
                }
                var maxX = ConfigureWindows.GetMainWindow.MainImageBorder.ActualWidth - (ConfigureWindows.GetMainWindow.MainImage.ActualWidth * scaleTransform.ScaleX);
                if ((isXOutOfBorder && newXproperty < maxX) || (!isXOutOfBorder && newXproperty > maxX))
                {
                    newXproperty = maxX;
                }
                var maxY = ConfigureWindows.GetMainWindow.MainImageBorder.ActualHeight - (ConfigureWindows.GetMainWindow.MainImage.ActualHeight * scaleTransform.ScaleY);
                if ((isXOutOfBorder && newYproperty < maxY) || (!isXOutOfBorder && newYproperty > maxY))
                {
                    newYproperty = maxY;
                }
            }

            translateTransform.X = newXproperty;
            translateTransform.Y = newYproperty;

            e.Handled = true;
        }

        /// <summary>
        /// Resets element values to their loaded values
        /// </summary>
        internal static void ResetZoom(bool animate = true)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null) { return; }

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
                SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width, (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height);
            }
            else
            {
                SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width, (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height, FolderIndex);
            }
        }

        /// <summary>
        /// Determine zoom direction and speed
        /// </summary>
        /// <param name="i">increment</param>
        internal static async Task ZoomAsync(bool increment)
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
            var zoomSpeed = Properties.Settings.Default.ZoomSpeed;

            if (increment)
            {
                // Increase speed determined by how much is zoomed in
                if (ZoomValue >= 1.2)
                {
                    zoomSpeed += .2;
                }
                if (ZoomValue >= 1.4)
                {
                    zoomSpeed += .25;
                }
                if (ZoomValue >= 1.8)
                {
                    zoomSpeed += .3;
                }
                if (ZoomValue >= 2)
                {
                    zoomSpeed += .4;
                }
                if (ZoomValue >= 2.2)
                {
                    zoomSpeed += .6;
                }
            }
            else
            {
                if (ZoomValue >= 1.2)
                {
                    zoomSpeed += .3;
                }
                if (ZoomValue >= 1.4)
                {
                    zoomSpeed += .4;
                }
                if (ZoomValue >= 1.8)
                {
                    zoomSpeed += .5;
                }
                if (ZoomValue >= 2)
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

            await ZoomAsync(ZoomValue).ConfigureAwait(false);
        }

        /// <summary>
        /// Zooms to given value
        /// </summary>
        /// <param name="value"></param>
        internal static async Task ZoomAsync(double value)
        {
            ZoomValue = value;

            BeginZoomAnimation(ZoomValue);

            /// Displays zoompercentage in the center window
            if (!string.IsNullOrEmpty(ZoomPercentage))
            {
                await Tooltip.ShowTooltipMessage(ZoomPercentage, true).ConfigureAwait(false);
            }
            else
            {
                Tooltip.CloseToolTipMessage();
            }
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
            {
                /// Display updated values
                if (Pics.Count == 0)
                {
                    /// Display values from web
                    SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width, (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height);
                }
                else
                {
                    SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width, (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height, FolderIndex);
                }
            }));
        }

        private static void BeginZoomAnimation(double zoomValue)
        {
            // TODO Make zoom work when image rotated
            Point relative = Mouse.GetPosition(ConfigureWindows.GetMainWindow.MainImage);

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
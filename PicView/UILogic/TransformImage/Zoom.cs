using PicView.ChangeTitlebar;
using PicView.PicGallery;
using PicView.Properties;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;

namespace PicView.UILogic.TransformImage
{
    internal static class ZoomLogic
    {
        private static ScaleTransform? scaleTransform;
        internal static TranslateTransform? translateTransform;
        private static Point origin;
        private static Point start;

        /// <summary>
        /// Used to determine final point when zooming,
        /// since DoubleAnimation changes value of TranslateTransform continuesly.
        /// </summary>
        internal static double ZoomValue { get; set; }

        /// <summary>
        /// Returns zoom percentage. if 100%, return empty string
        /// </summary>
        internal static string ZoomPercentage
        {
            get
            {
                if (scaleTransform == null || ZoomValue == 0 || ZoomValue == 1)
                {
                    return string.Empty;
                }

                var zoom = Math.Round(ZoomValue * 100);

                return zoom + "%";
            }
        }

        internal static bool IsZoomed
        {
            get
            {
                if (scaleTransform is null)
                {
                    return false;
                }
                return scaleTransform.ScaleX != 1.0;
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

            if (x > 48 || y > 18)
            {
                return ") ";
            }

            return $", {x} : {y}) ";
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
            ConfigureWindows.GetMainWindow.MainImageBorder.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection {
                            new ScaleTransform(),
                            new TranslateTransform()
                        }
            };

            ConfigureWindows.GetMainWindow.ParentContainer.ClipToBounds = ConfigureWindows.GetMainWindow.MainImageBorder.ClipToBounds = true;

            // Set transforms to UI elements
            scaleTransform = (ScaleTransform)((TransformGroup)ConfigureWindows.GetMainWindow.MainImageBorder.RenderTransform).Children.First(tr => tr is ScaleTransform);
            translateTransform = (TranslateTransform)((TransformGroup)ConfigureWindows.GetMainWindow.MainImageBorder.RenderTransform).Children.First(tr => tr is TranslateTransform);
        }

        internal static void PreparePanImage(object sender, MouseButtonEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.IsActive == false)
            {
                return;
            }
            // Report position for image drag
            ConfigureWindows.GetMainWindow.MainImage.CaptureMouse();
            start = e.GetPosition(ConfigureWindows.GetMainWindow.ParentContainer);
            origin = new Point(translateTransform.X, translateTransform.Y);
        }

        internal static void PanImage(object sender, MouseEventArgs e)
        {
            // Check if mouse capture is allowed and window is active
            if (!ConfigureWindows.GetMainWindow.MainImage.IsMouseCaptured || !ConfigureWindows.GetMainWindow.IsActive)
            {
                return;
            }

            if (scaleTransform.ScaleX == 1)
            {
                if (!Settings.Default.Fullscreen && !GalleryFunctions.IsHorizontalFullscreenOpen)
                    return;
            }

            // Calculate the change in mouse position
            var dragDelta = start - e.GetPosition(ConfigureWindows.GetMainWindow);

            // Update the image position
            var dragMousePosition = start - e.GetPosition(ConfigureWindows.GetMainWindow);

            var newXproperty = origin.X - dragMousePosition.X;
            var newYproperty = origin.Y - dragMousePosition.Y;

            // Keep panning it in bounds 
            if (Properties.Settings.Default.AutoFitWindow) // TODO develop solution where you can keep window in bounds when using normal window behavior and fullscreen
            {
                var isXOutOfBorder = ConfigureWindows.GetMainWindow.Scroller.ActualWidth < (ConfigureWindows.GetMainWindow.MainImageBorder.ActualWidth * scaleTransform.ScaleX);
                var isYOutOfBorder = ConfigureWindows.GetMainWindow.Scroller.ActualHeight < (ConfigureWindows.GetMainWindow.MainImageBorder.ActualHeight * scaleTransform.ScaleY);
                var maxX = ConfigureWindows.GetMainWindow.Scroller.ActualWidth - (ConfigureWindows.GetMainWindow.MainImageBorder.ActualWidth * scaleTransform.ScaleX);
                var maxY = ConfigureWindows.GetMainWindow.Scroller.ActualHeight - (ConfigureWindows.GetMainWindow.MainImageBorder.ActualHeight * scaleTransform.ScaleY);

                if (isXOutOfBorder && newXproperty < maxX || isXOutOfBorder == false && newXproperty > maxX)
                {
                    newXproperty = maxX;
                }

                if (isXOutOfBorder && newYproperty < maxY || isXOutOfBorder == false && newYproperty > maxY)
                {
                    newYproperty = maxY;
                }

                if (isXOutOfBorder && newXproperty > 0 || isXOutOfBorder == false && newXproperty < 0)
                {
                    newXproperty = 0;
                }
                if (isYOutOfBorder && newYproperty > 0 || isYOutOfBorder == false && newYproperty < 0)
                {
                    newYproperty = 0;
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
            if (ConfigureWindows.GetMainWindow.MainImage.Source == null
                || scaleTransform == null
                || translateTransform == null) { return; }

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
                SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width, (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height, FolderIndex, null);
            }
        }

        /// <summary>
        /// Determine zoom direction and speed
        /// </summary>
        /// <param name="i">increment</param>
        internal static void Zoom(bool isZoomIn)
        {
            // Disable zoom if cropping tool is active
            if (UC.GetCropppingTool != null && UC.GetCropppingTool.IsVisible)
            {
                return;
            }

            var currentZoom = scaleTransform.ScaleX;
            var zoomSpeed = Settings.Default.ZoomSpeed;

            // Increase speed based on the current zoom level
            if (currentZoom > 14 && isZoomIn)
            {
                return;
            }
            else if (currentZoom > 4)
            {
                zoomSpeed += 1.5;
            }
            else if (currentZoom > 3.2)
            {
                zoomSpeed += 1;
            }
            else if (currentZoom > 1.6)
            {
                zoomSpeed += 0.5;
            }

            if (!isZoomIn)
            {
                zoomSpeed = -zoomSpeed;
            }

            currentZoom += zoomSpeed;
            currentZoom = Math.Max(0.09, currentZoom);
            Zoom(currentZoom);
        }

        /// <summary>
        /// Zooms to given value
        /// </summary>
        /// <param name="value"></param>
        internal static void Zoom(double value)
        {
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
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
            {
                // Display updated values
                if (Pics.Count == 0)
                {
                    ///  values from web
                    SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width, (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height);
                }
                else
                {
                    SetTitle.SetTitleString((int)ConfigureWindows.GetMainWindow.MainImage.Source.Width, (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height, FolderIndex, null);
                }
            });
        }

        private static void BeginZoomAnimation(double zoomValue)
        {
            // TODO Make zoom work when image rotated
            Point relative = Mouse.GetPosition(ConfigureWindows.GetMainWindow.MainImageBorder);

            // Calculate new position
            double absoluteX = relative.X * scaleTransform.ScaleX + translateTransform.X;
            double absoluteY = relative.Y * scaleTransform.ScaleY + translateTransform.Y;

            // Reset to zero if value is one, which is reset
            double newTranslateValueX = zoomValue != 1 ? absoluteX - relative.X * zoomValue : 0;
            double newTranslateValueY = zoomValue != 1 ? absoluteY - relative.Y * zoomValue : 0;

            var duration = new Duration(TimeSpan.FromSeconds(.25));

            var scaleAnim = new DoubleAnimation(zoomValue, duration)
            {
                // Set stop to make sure animation doesn't hold ownership of scaletransform
                FillBehavior = FillBehavior.Stop
            };

            scaleAnim.Completed += delegate
            {
                // Hack it to keep the intended value
                scaleTransform.ScaleX = scaleTransform.ScaleY = zoomValue;
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
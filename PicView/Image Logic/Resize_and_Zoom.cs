using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.Fields;
using static PicView.Helper;
using static PicView.Navigation;
using static PicView.Scroll;
using static PicView.SetTitle;
using static PicView.Tooltip;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class Resize_and_Zoom
    {
        /// <summary>
        /// Returns zoom percentage. if 100%, return empty string
        /// </summary>
        internal static string ZoomPercentage
        {
            get
            {
                var zoom = Math.Round(AspectRatio * 100);
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
            var x = (width / gcd);
            var y = (height / gcd);

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
            // Move window when Ctrl is being held down
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Move(sender, e);
                return;
            }
            // Logic for auto scrolling
            if (autoScrolling)
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
            if (!IsScrollEnabled)
            {
                // Report position for image drag
                mainWindow.img.CaptureMouse();
                start = e.GetPosition(mainWindow);
                origin = new Point(tt.X, tt.Y);
            }
        }

        internal static void Bg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Move(sender, e);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Zoom_img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Stop autoscrolling or dragging image
            if (autoScrolling)
            {
                StopAutoScroll();
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
            if (autoScrolling)
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

            if (Properties.Settings.Default.ScrollEnabled && !autoScrolling)
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
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !autoScrolling)
            {
                Zoom(e.Delta, true);
            }
            // Zoom
            else if (!autoScrolling)
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
            if (mainWindow.img.Source == null)
            {
                return;
            }

            // Scale to default
            var scaletransform = new ScaleTransform();
            scaletransform.ScaleX = scaletransform.ScaleY = 1.0;
            mainWindow.img.LayoutTransform = scaletransform;

            st.ScaleX = st.ScaleY = 1;
            tt.X = tt.Y = 0;
            mainWindow.img.RenderTransformOrigin = new Point(0.5, 0.5);

            CloseToolTipStyle();
            isZoomed = false;

            // Reset size
            TryZoomFit();

            // Display non-zoomed values
            if (canNavigate)
            {
                SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, FolderIndex);
            }
            else
            {
                // Display values from web
                SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, Pics[FolderIndex]);
            }

            isZoomed = false;
        }

        /// <summary>
        /// Scales or zooms, depending on given values
        /// </summary>
        /// <param name="i"></param>
        /// <param name="zoomMode"></param>
        internal static void Zoom(int i, bool zoomMode)
        {
            /// Don't zoom when gallery is open
            if (picGallery != null)
            {
                if (GalleryMisc.IsOpen)
                {
                    return;
                }
            }

            /// Scales the window with img.LayoutTransform
            if (zoomMode)
            {
                /// Start from 1 or zoom value
                if (isZoomed)
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

            isZoomed = true;

            /// Displays zoompercentage in the center window
            if (!string.IsNullOrEmpty(ZoomPercentage))
            {
                ToolTipStyle(ZoomPercentage, true);
            }
            else
            {
                CloseToolTipStyle();
            }

            /// Display updated values
            if (canNavigate)
            {
                SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, FolderIndex);
            }
            else
            {
                /// Display values from web
                SetTitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, Pics[FolderIndex]);
            }
        }

        /// <summary>
        /// Tries to call Zoomfit with additional error checking
        /// </summary>
        internal static void TryZoomFit()
        {
            if (freshStartup)
            {
                return;
            }

            if (Pics != null)
            {
                if (Pics.Count > FolderIndex)
                {
                    var tmp = PreLoading.Preloader.Load(Pics[FolderIndex]);
                    if (tmp != null)
                    {
                        ZoomFit(tmp.PixelWidth, tmp.PixelHeight);
                    }
                    else
                    {
                        var size = ImageDecoder.ImageSize(Pics[FolderIndex]);
                        if (size.HasValue)
                        {
                            ZoomFit(size.Value.Width, size.Value.Height);
                        }
                        else if (mainWindow.img.Source != null)
                        {
                            ZoomFit(mainWindow.img.Source.Width, mainWindow.img.Source.Height);
                        }
                        else if (xWidth != 0 && xHeight != 0)
                        {
                            ZoomFit(xWidth, xHeight);
                        }
                    }
                }
            }
            else if (mainWindow.img.Source != null)
            {
                ZoomFit(mainWindow.img.Source.Width, mainWindow.img.Source.Height);
            }
            else if (xWidth != 0 && xHeight != 0)
            {
                ZoomFit(xWidth, xHeight);
            }
        }

        /// <summary>
        /// Tries to call Zoomfit with specified path
        /// </summary>
        internal static void TryZoomFit(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                ZoomFit(465, 515); // Numbers for min width and height for mainWindow
                return;
            }

            var size = ImageDecoder.ImageSize(source);
            if (size.HasValue)
            {
                ZoomFit(size.Value.Width, size.Value.Height);
            }
            else
            {
                ZoomFit(465, 515);
            }
        }

        /// <summary>
        /// Fits image size based on users screen resolution
        /// or window size
        /// </summary>
        /// <param name="width">The pixel width of the image</param>
        /// <param name="height">The pixel height of the image</param>
        internal static void ZoomFit(double width, double height)
        {
            double maxWidth, maxHeight;
            var interfaceHeight = 90; /// TopBar + mainWindow.LowerBar height + extra padding

            if (FitToWindow)
            {
                /// Get max width and height, based on user's screen
                if (Properties.Settings.Default.ShowInterface)
                {
                    maxWidth = Math.Min(MonitorInfo.Width - ComfySpace, width);
                    maxHeight = Math.Min((MonitorInfo.Height - interfaceHeight), height);
                }
                /// - 2 for window border
                else
                {
                    maxWidth = Math.Min((MonitorInfo.Width - ComfySpace) - 2, width - 2);
                    maxHeight = Math.Min(MonitorInfo.Height - 2, height - 2);
                }
            }
            else
            {
                /// Get max width and height, based on window size
                maxWidth = Math.Min(mainWindow.Width, width);

                if (Properties.Settings.Default.ShowInterface)
                {
                    maxHeight = Math.Min(mainWindow.Height - interfaceHeight, height);
                }
                else
                {
                    maxHeight = Math.Min(mainWindow.Height, height);
                }
            }

            AspectRatio = Math.Min((maxWidth / width), (maxHeight / height));

            if (IsScrollEnabled)
            {
                /// Calculate height based on width
                mainWindow.img.Width = maxWidth;
                mainWindow.img.Height = maxWidth * height / width;

                /// Set mainWindow.Scroller height to aspect ratio calculation
                mainWindow.Scroller.Height = (height * AspectRatio);

                /// Update values
                xWidth = mainWindow.img.Width;
                xHeight = mainWindow.Scroller.Height;
            }
            else
            {
                /// Reset mainWindow.Scroller's height to auto
                mainWindow.Scroller.Height = double.NaN;

                /// Fit image by aspect ratio calculation
                /// and update values
                mainWindow.img.Width = xWidth = (width * AspectRatio);
                mainWindow.img.Height = xHeight = (height * AspectRatio);
            }

            if (FitToWindow)
            {
                /// Update mainWindow.TitleBar width to dynamically fit new size
                var interfaceSize = 220; // logo and buttons width + extra padding
                mainWindow.Bar.MaxWidth = xWidth - interfaceSize < interfaceSize ? interfaceSize : xWidth - interfaceSize;

                /// TODO Loses position gradually if not forced to center
                if (!Properties.Settings.Default.Fullscreen)
                {
                    CenterWindowOnScreen();
                }
            }

            if (isZoomed)
            {
                ResetZoom();
            }



            /*

                            _.._   _..---.
                         .-"    ;-"       \
                        /      /           |
                       |      |       _=   |
                       ;   _.-'\__.-')     |
                        `-'      |   |    ;
                                 |  /;   /      _,
                               .-.;.-=-./-""-.-` _`
                              /   |     \     \-` `,
                             |    |      |     |
                             |____|______|     |
                              \0 / \0   /      /
                           .--.-""-.`--'     .'
                          (#   )          ,  \
                          ('--'          /\`  \
                           \       ,,  .'      \
                            `-._    _.'\        \
                   jgs          `""`    \        \

                   So much math!
            */
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.Fields;
using static PicView.Interface;
using static PicView.Navigation;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class Resize_and_Zoom
    {
        #region Zoom, Scroll, Rotate and Flip

        // Auto scroll

        /// <summary>
        /// Starts the auto scroll feature and shows the sign on the ui
        /// </summary>
        /// <param name="e"></param>
        internal static void StartAutoScroll(MouseButtonEventArgs e)
        {
            // Don't scroll if not scrollable
            if (mainWindow.Scroller.ComputedVerticalScrollBarVisibility == Visibility.Collapsed)
                return;

            autoScrolling = true;
            autoScrollOrigin = e.GetPosition(mainWindow.Scroller);

            ShowAutoScrollSign();
        }

        /// <summary>
        /// Stop auto scroll feature and remove sign from the ui
        /// </summary>
        internal static void StopAutoScroll()
        {
            autoScrollTimer.Stop();
            //window.ReleaseMouseCapture();
            autoScrollTimer.Enabled = false;
            autoScrolling = false;
            autoScrollOrigin = null;
            HideAutoScrollSign();
        }

        /// <summary>
        /// Uses timer to scroll vertical up/down every seventh milisecond
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="E"></param>
        internal static async void AutoScrollTimerEvent(object sender, System.Timers.ElapsedEventArgs E)
        {
            // Error checking
            if (autoScrollPos == null || autoScrollOrigin == null)
                return;

            // Start in dispatcher because timer is threaded
            await mainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (autoScrollOrigin.HasValue)
                {
                    // Calculate offset by Y coordinate
                    var offset = (autoScrollPos.Y - autoScrollOrigin.Value.Y) / 15;

                    //ToolTipStyle("pos = " + autoScrollPos.Y.ToString() + " origin = " + autoScrollOrigin.Value.Y.ToString()
                    //    + Environment.NewLine + "offset = " + offset, false);

                    if (autoScrolling)
                        // Tell the scrollviewer to scroll to calculated offset
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + offset);
                }
            }));
        }

        // Zoom
        /// <summary>
        /// Pan and Zoom, reset zoom and double click to reset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Zoom_img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Move(sender, e);
                return;
            }
            if (autoScrolling)
            {
                // Report position and enable autoscrolltimer
                autoScrollOrigin = e.GetPosition(mainWindow);
                autoScrollTimer.Enabled = true;
                return;
            }
            if (e.ClickCount == 2)
            {
                ResetZoom();
                return;
            }
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
                Move(sender, e);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Zoom_img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Stop autoscrolling or dragging image
            if (autoScrolling)
                StopAutoScroll();
            else
                mainWindow.img.ReleaseMouseCapture();
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
                return;

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
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset - zoomSpeed);
                    else if (e.Delta < 0)
                        mainWindow.Scroller.ScrollToVerticalOffset(mainWindow.Scroller.VerticalOffset + zoomSpeed);
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                Pic(e.Delta > 0);
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !autoScrolling)
            {
                Zoom(e.Delta, true);
            }
            else
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
            mainWindow.img.RenderTransformOrigin = new Point(0.5, 0.5);

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
                return;

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
            ZoomFit(xWidth, xHeight);

            // Display non-zoomed values
            string[] titleString;
            if (canNavigate)
            {
                titleString = TitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, FolderIndex);
                mainWindow.Title = titleString[0];
                mainWindow.Bar.Text = titleString[1];
                mainWindow.Bar.ToolTip = titleString[2];
            }
            else
            {
                // Display values from web
                titleString = TitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, PicPath);
                mainWindow.Title = titleString[0];
                mainWindow.Bar.Text = titleString[1];
                mainWindow.Bar.ToolTip = titleString[1];
            }
        }

        /// <summary>
        /// Scales or zooms, depending on given values
        /// </summary>
        /// <param name="i"></param>
        /// <param name="zoomMode"></param>
        internal static void Zoom(int i, bool zoomMode)
        {
            // Don't zoom when gallery is open
            if (picGallery != null)
            {
                if (PicGalleryLogic.IsOpen)
                {
                    return;
                }
            }

            // Scales the window with img.LayoutTransform
            if (zoomMode)
            {
                // Start from 1 or zoom value
                if (isZoomed)
                    AspectRatio += i > 0 ? .01 : -.01;
                else
                    AspectRatio = 1;

                var scaletransform = new ScaleTransform();

                scaletransform.ScaleX = scaletransform.ScaleY = AspectRatio;
                mainWindow.img.LayoutTransform = scaletransform;
            }

            // Pan and zoom
            else
            {
                // Get position where user points cursor
                var position = Mouse.GetPosition(mainWindow.img);

                // Use our position as starting point for zoom
                mainWindow.img.RenderTransformOrigin = new Point(position.X / xWidth, position.Y / xHeight);

                // Determine zoom speed
                var zoomValue = st.ScaleX > 1.3 ? .03 : .01;
                if (st.ScaleX > 1.5)
                    zoomValue += .005;
                if (st.ScaleX > 1.7)
                    zoomValue += .007;

                if (st.ScaleX >= 1.0 && st.ScaleX + zoomValue >= 1.0 || st.ScaleX - zoomValue >= 1.0)
                {
                    zoomValue = i > 0 ? zoomValue : -zoomValue;
                    // Start zoom
                    st.ScaleY = st.ScaleX += zoomValue;
                    AspectRatio += zoomValue;
                }

                if (st.ScaleX < 1.0)
                {
                    // Don't zoom less than 1.0, does not work so good...
                    st.ScaleX = st.ScaleY = 1.0;
                }
                //zoomValue = i > 0 ? zoomValue : -zoomValue;
                //st.ScaleY = st.ScaleX += zoomValue;

            }

            isZoomed = true;

            // Displays zoompercentage in the center window
            if (!string.IsNullOrEmpty(ZoomPercentage))
                ToolTipStyle(ZoomPercentage, true);
            else
                CloseToolTipStyle();

            // Display updated values
            string[] titleString;
            if (canNavigate)
            {
                titleString = TitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, FolderIndex);
                mainWindow.Title = titleString[0];
                mainWindow.Bar.Text = titleString[1];
                mainWindow.Bar.ToolTip = titleString[2];
            }
            else
            {
                // Display values from web
                titleString = TitleString((int)mainWindow.img.Source.Width, (int)mainWindow.img.Source.Height, PicPath);
                mainWindow.Title = titleString[0];
                mainWindow.Bar.Text = titleString[1];
                mainWindow.Bar.ToolTip = titleString[1];
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
            var interfaceHeight = 93; // TopBar + mainWindow.LowerBar height
            //var interfaceHeight = (mainWindow.TitleBar.ActualHeight + mainWindow.LowerBar.ActualHeight) + 2; // + 2 for window border

            if (FitToWindow)
            {
                // Get max width and height, based on user's screen
                if (Properties.Settings.Default.ShowInterface)
                {
                    maxWidth = Math.Min(MonitorInfo.Width - ComfySpace, width);
                    maxHeight = Math.Min((MonitorInfo.Height - interfaceHeight), height);
                }
                else
                {
                    maxWidth = Math.Min(MonitorInfo.Width - 2, width - 2);
                    maxHeight = Math.Min(MonitorInfo.Height - 2, height - 2);
                }
            }       
            else
            {
                // Get max width and height, based on window size
                maxWidth = Math.Min(mainWindow.Width, width);

                if (Properties.Settings.Default.ShowInterface)
                    maxHeight = Math.Min(mainWindow.Height - interfaceHeight, height);
                else
                    maxHeight = Math.Min(mainWindow.Height, height);
            }

            AspectRatio = Math.Min((maxWidth / width), (maxHeight / height));

            if (IsScrollEnabled)
            {
                // Calculate height based on width
                mainWindow.img.Width = maxWidth;
                mainWindow.img.Height = maxWidth * height / width;

                // Set mainWindow.Scroller height to aspect ratio calculation
                mainWindow.Scroller.Height = (height * AspectRatio);

                // Update values
                xWidth = mainWindow.img.Width;
                xHeight = mainWindow.Scroller.Height;
            }
            else
            {
                // Reset mainWindow.Scroller's height to auto
                mainWindow.Scroller.Height = double.NaN;

                // Fit image by aspect ratio calculation
                // and update values
                mainWindow.img.Height = xHeight = (height * AspectRatio);
                mainWindow.img.Width = xWidth = (width * AspectRatio);
            }

            if (FitToWindow)
            {
                // Update mainWindow.TitleBar width to fit new size
                var interfaceSize = 220; // logo and buttons width + extra padding
                mainWindow.Bar.MaxWidth = xWidth - interfaceSize < interfaceSize ? interfaceSize : xWidth - interfaceSize;

                // Loses position gradually if not forced to center
                if (!Properties.Settings.Default.Fullscreen)
                    CenterWindowOnScreen();
            }


            isZoomed = false;

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

        #endregion Zoom, Scroll, Rotate and Flip
    }
}

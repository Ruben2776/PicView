using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static PicView.Fields;
using static PicView.FileFunctions;
using static PicView.Resize_and_Zoom;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class Interface
    {

        #region UserControl Specifics

        /// <summary>
        /// Shows a black tooltip on screen in a given time
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="center">If centered or on bottom</param>
        /// <param name="time">How long until it fades away</param>
        internal static void ToolTipStyle(object message, bool center, TimeSpan time)
        {
            sexyToolTip.Visibility = Visibility.Visible;

            if (center)
            {
                sexyToolTip.Margin = new Thickness(0, 0, 0, 0);
                sexyToolTip.VerticalAlignment = VerticalAlignment.Center;
            }
            else
            {
                sexyToolTip.Margin = new Thickness(0, 0, 0, 15);
                sexyToolTip.VerticalAlignment = VerticalAlignment.Bottom;
            }

            sexyToolTip.SexyToolTipText.Text = message.ToString();
            var anim = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            anim.Completed += (s, _) => AnimationHelper.Fade(sexyToolTip, TimeSpan.FromSeconds(1.5), time, 1, 0);

            sexyToolTip.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        /// <summary>
        /// Shows a black tooltip on screen for a small time
        /// </summary>
        /// <param name="message">The message to display</param>
        internal static void ToolTipStyle(object message, bool center = false)
        {
            ToolTipStyle(message, center, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Hides the Messagebox ToolTipStyle
        /// </summary>
        internal static void CloseToolTipStyle()
        {
            sexyToolTip.Visibility = Visibility.Hidden;
        }

        //// AjaxLoading
        ///// <summary>
        ///// Loads AjaxLoading and adds it to the window
        ///// </summary>
        //internal static void LoadAjaxLoading()
        //{
        //    ajaxLoading = new AjaxLoading
        //    {
        //        Focusable = false,
        //        Opacity = 0
        //    };

        //    mainWindow.bg.Children.Add(ajaxLoading);
        //}

        /// <summary>
        /// Start loading animation
        /// </summary>
        internal static void AjaxLoadingStart()
        {
            if (ajaxLoading.Opacity != 1)
            {
                AnimationHelper.Fade(ajaxLoading, 1, TimeSpan.FromSeconds(.2));
            }
        }

        /// <summary>
        /// End loading animation
        /// </summary>
        internal static void AjaxLoadingEnd()
        {
            if (ajaxLoading.Opacity != 0)
            {
                AnimationHelper.Fade(ajaxLoading, 0, TimeSpan.FromSeconds(.2));
            }
        }

        // AutoScrollSign

       

        internal static void HideAutoScrollSign()
        {
            autoScrollSign.Visibility = Visibility.Collapsed;
            autoScrollSign.Opacity = 0;
        }

        internal static void ShowAutoScrollSign()
        {
            Canvas.SetTop(autoScrollSign, autoScrollOrigin.Value.Y);
            Canvas.SetLeft(autoScrollSign, autoScrollOrigin.Value.X);
            autoScrollSign.Visibility = Visibility.Visible;
            autoScrollSign.Opacity = 1;
        }

        

        #endregion UserControl Specifics

        #region Manipulate Interface        

        /// <summary>
        /// Toggle between hidden interface and default
        /// </summary>
        internal static void HideInterface(bool slideshow = false, bool navigationButtons = true)
        {
            // Hide interface
            if (Properties.Settings.Default.ShowInterface)
            {
                mainWindow.TitleBar.Visibility =
                mainWindow.LowerBar.Visibility =
                mainWindow.LeftBorderRectangle.Visibility =
                mainWindow.RightBorderRectangle.Visibility
                = Visibility.Collapsed;

                if (navigationButtons)
                    clickArrowLeft.Visibility =
                    clickArrowRight.Visibility =
                    x2.Visibility =
                    minus.Visibility = Visibility.Visible;
                else
                    clickArrowLeft.Visibility =
                    clickArrowRight.Visibility =
                    x2.Visibility =
                    minus.Visibility = Visibility.Collapsed;

                if (!slideshow || !Properties.Settings.Default.Fullscreen)
                    Properties.Settings.Default.ShowInterface = false;

                if (activityTimer != null)
                    activityTimer.Start();
            }
            // Show interface
            else
            {
                Properties.Settings.Default.ShowInterface = true;

                mainWindow.TitleBar.Visibility =
                mainWindow.LowerBar.Visibility =
                mainWindow.LeftBorderRectangle.Visibility =
                mainWindow.RightBorderRectangle.Visibility = Visibility.Visible;

                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                minus.Visibility = Visibility.Collapsed;


                if (!FitToWindow)
                    ZoomFit(xWidth, xHeight);

                if (activityTimer != null)
                    activityTimer.Stop();
            }

            ToggleMenus.Close_UserControls();
        }

        /// <summary>
        /// Hides/shows interface elements with a fade animation
        /// </summary>
        /// <param name="show"></param>
        internal static async void FadeControlsAsync(bool show)
        {
            var fadeTo = show ? 1 : 0;

            /// Might cause unnecessary cpu usage? Need to check
            await mainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (!Properties.Settings.Default.ShowInterface | Slidetimer.Enabled == true)
                {
                    if (clickArrowRight != null && clickArrowLeft != null && x2 != null)
                    {
                        AnimationHelper.Fade(clickArrowLeft, fadeTo, TimeSpan.FromSeconds(.5));
                        AnimationHelper.Fade(clickArrowRight, fadeTo, TimeSpan.FromSeconds(.5));
                        AnimationHelper.Fade(x2, fadeTo, TimeSpan.FromSeconds(.5));
                        AnimationHelper.Fade(minus, fadeTo, TimeSpan.FromSeconds(.5));
                    }
                }

                ScrollbarFade(show);
            }));
        }


        /// <summary>
        /// Logic for mouse movements on MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            //If Mouse is hidden, show it and interface elements.
            if (e.MouseDevice.OverrideCursor == Cursors.None)
            {
                Mouse.OverrideCursor = null;
                HideCursorTimer.Stop();
            }

            // Stop timer if mouse moves on mainwindow and show elements
            activityTimer.Stop();
            FadeControlsAsync(true);

            // If Slideshow is running the interface will hide after 2,5 sec.
            if (Slidetimer.Enabled == true)
            {
                MouseIdleTimer.Start();
            }
            else
            {
                MouseIdleTimer.Stop();
            }
        }

        /// <summary>
        /// Logic for mouse leave mainwindow event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MainWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            // Start timer when mouse leaves mainwindow
            activityTimer.Start();
        }

        /// <summary>
        /// Find scrollbar and start fade animation
        /// </summary>
        /// <param name="show"></param>
        internal static void ScrollbarFade(bool show)
        {
            var s = mainWindow.Scroller.Template.FindName("PART_VerticalScrollBar", mainWindow.Scroller) as System.Windows.Controls.Primitives.ScrollBar;

            if (show)
            {
                AnimationHelper.Fade(s, 1, TimeSpan.FromSeconds(.7));
            }
            else
            {
                AnimationHelper.Fade(s, 0, TimeSpan.FromSeconds(1));
            }
        }

        /// <summary>
        /// Returns string with file name, folder position,
        /// zoom, aspect ratio, resolution and file size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static string[] TitleString(int width, int height, int index)
        {
            var s1 = new StringBuilder();
            s1.Append(AppName).Append(" - ").Append(Path.GetFileName(Pics[index])).Append(" ").Append(index + 1).Append("/").Append(Pics.Count).Append(" files")
                    .Append(" (").Append(width).Append(" x ").Append(height).Append(StringAspect(width, height)).Append(GetSizeReadable(new FileInfo(Pics[index]).Length));

            if (!string.IsNullOrEmpty(ZoomPercentage))
                s1.Append(" - ").Append(ZoomPercentage);

            var array = new string[3];
            array[0] = s1.ToString();
            s1.Remove(0, AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            s1.Replace(Path.GetFileName(Pics[index]), Pics[index]);
            array[2] = s1.ToString();
            return array;
        }

        /// <summary>
        /// Returns string with file name,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static string[] TitleString(int width, int height, string path)
        {
            var s1 = new StringBuilder();
            s1.Append(AppName).Append(" - ").Append(path).Append(" (").Append(width).Append(" x ").Append(height).Append(StringAspect(width, height));

            if (!string.IsNullOrEmpty(ZoomPercentage))
                s1.Append(" - ").Append(ZoomPercentage);

            var array = new string[2];
            array[0] = s1.ToString();
            s1.Remove(0, AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            return array;
        }

        /// <summary>
        /// Toggles scroll and displays it with TooltipStle
        /// </summary>
        internal static bool IsScrollEnabled
        {
            get { return Properties.Settings.Default.ScrollEnabled; }
            set
            {
                Properties.Settings.Default.ScrollEnabled = value;
                mainWindow.Scroller.VerticalScrollBarVisibility = value ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
                if (!freshStartup && !string.IsNullOrEmpty(PicPath))
                {
                    ZoomFit(mainWindow.img.Source.Width, mainWindow.img.Source.Height);
                    ToolTipStyle(value ? "Scrolling enabled" : "Scrolling disabled");
                }
            }
        }

        internal static void ChangeBackground(object sender, RoutedEventArgs e)
        {
            if (mainWindow.imgBorder == null)
                return;

            if (!(mainWindow.imgBorder.Background is SolidColorBrush cc))
                return;

            if (cc.Color == Colors.White)
            {
                mainWindow.imgBorder.Background = new SolidColorBrush(Colors.Transparent);
                Properties.Settings.Default.BgColorWhite = false;
            }

            else
            {
                mainWindow.imgBorder.Background = new SolidColorBrush(Colors.White);
                Properties.Settings.Default.BgColorWhite = true;
            }

        }



        #endregion Manipulate Interface
    }
}

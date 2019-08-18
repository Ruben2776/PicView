

using PicView.UserControls;
using PicView.Windows;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static PicView.FileFunctions;
using static PicView.Variables;
using static PicView.Navigation;
using static PicView.Resize_and_Zoom;


namespace PicView
{
    internal static class Interface
    {
        #region Interface logic

        #region UserControl Specifics

        // Add timers
        internal static void AddTimers()
        {
            autoScrollTimer = new System.Timers.Timer()
            {
                Interval = 7,
                AutoReset = true,
                Enabled = false
            };
            autoScrollTimer.Elapsed += AutoScrollTimerEvent;

            activityTimer = new System.Timers.Timer()
            {
                Interval = 1500,
                AutoReset = true,
                Enabled = false
            };
            activityTimer.Elapsed += ActivityTimer_Elapsed;

            fastPicTimer = new System.Timers.Timer()
            {
                Interval = 1,
                Enabled = false
            };
            fastPicTimer.Elapsed += FastPic;

            Slidetimer = new System.Timers.Timer()
            {
                Interval = Properties.Settings.Default.Slidetimeren,
                Enabled = false
            };
            Slidetimer.Elapsed += SlideTimer_Elapsed;

            HideCursorTimer = new System.Timers.Timer()
            {
                Interval = 2500,
                Enabled = false
            };
            HideCursorTimer.Elapsed += HideCursorTimer_Elapsed;

            MouseIdleTimer = new System.Timers.Timer()
            {
                Interval = 2500,
                Enabled = false
            };
            MouseIdleTimer.Elapsed += MouseIdleTimer_Elapsed;
        }

        // Load controls

        /// <summary>
        /// Loads ClickArrow and adds it to the window
        /// </summary>
        internal static void LoadClickArrow(bool right)
        {
            if (right)
            {
                clickArrowRight = new ClickArrow(true)
                {
                    Focusable = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                mainWindow.bg.Children.Add(clickArrowRight);
            }
            else
            {
                clickArrowLeft = new ClickArrow(false)
                {
                    Focusable = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                mainWindow.bg.Children.Add(clickArrowLeft);
            }
        }

        /// <summary>
        /// Loads x2 and adds it to the window
        /// </summary>
        internal static void Loadx2()
        {
            x2 = new X2()
            {
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            mainWindow.bg.Children.Add(x2);
        }

        /// <summary>
        /// Loads Minus and adds it to the window
        /// </summary>
        internal static void LoadMinus()
        {
            minus = new Minus()
            {
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 50, 0)
            };

            mainWindow.bg.Children.Add(minus);
        }

        /// <summary>
        /// Loads FileMenu and adds it to the window
        /// </summary>
        internal static void LoadFileMenu()
        {
            fileMenu = new FileMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 152, 0)
            };

            mainWindow.bg.Children.Add(fileMenu);
        }

        /// <summary>
        /// Loads ImageSettingsMenu and adds it to the window
        /// </summary>
        internal static void LoadImageSettingsMenu()
        {
            imageSettingsMenu = new ImageSettings
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 112, 0)
            };

            mainWindow.bg.Children.Add(imageSettingsMenu);
        }

        /// <summary>
        /// Loads QuickSettingsMenu and adds it to the window
        /// </summary>
        internal static void LoadQuickSettingsMenu()
        {
            quickSettingsMenu = new QuickSettingsMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(30, 0, 0, 0)
            };

            mainWindow.bg.Children.Add(quickSettingsMenu);
        }

        /// <summary>
        /// Loads FunctionsMenu and adds it to the window
        /// </summary>
        internal static void LoadFunctionsMenu()
        {
            functionsMenu = new UserControls.Menus.FunctionsMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(40, 0, 0, 0)
            };

            mainWindow.bg.Children.Add(functionsMenu);
        }



        // Tooltip

        /// <summary>
        /// Loads TooltipStyle and adds it to the window
        /// </summary>
        internal static void LoadTooltipStyle()
        {
            sexyToolTip = new SexyToolTip
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden
            };

            mainWindow.bg.Children.Add(sexyToolTip);
        }

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

        /// <summary>
        /// Loads AutoScrollSign and adds it to the window
        /// </summary>
        internal static void LoadAutoScrollSign()
        {
            autoScrollSign = new AutoScrollSign
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                Width = 20,
                Height = 35
            };

            mainWindow.topLayer.Children.Add(autoScrollSign);
        }

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

        // Toggle open close menus

        /// <summary>
        /// Toggles whether ImageSettingsMenu is open or not with a fade animation
        /// </summary>
        internal static bool ImageSettingsMenuOpen
        {
            get { return imageSettingsMenuOpen; }
            set
            {
                imageSettingsMenuOpen = value;
                imageSettingsMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { imageSettingsMenu.Visibility = Visibility.Hidden; };
                }
                else
                    da.To = 1;
                if (imageSettingsMenu != null)
                    imageSettingsMenu.BeginAnimation(UIElement.OpacityProperty, da);
            }
        }

        /// <summary>
        /// Toggles whether FileMenu is open or not with a fade animation
        /// </summary>
        internal static bool FileMenuOpen
        {
            get { return fileMenuOpen; }
            set
            {
                fileMenuOpen = value;
                fileMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { fileMenu.Visibility = Visibility.Hidden; };
                }
                else
                    da.To = 1;
                if (fileMenu != null)
                    fileMenu.BeginAnimation(UIElement.OpacityProperty, da);
            }
        }

        /// <summary>
        /// Toggles whether QuickSettingsMenu is open or not with a fade animation
        /// </summary>
        internal static bool QuickSettingsMenuOpen
        {
            get { return quickSettingsMenuOpen; }
            set
            {
                quickSettingsMenuOpen = value;
                quickSettingsMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    Application.Current.Resources["ChosenColor"] = AnimationHelper.GetPrefferedColorOver();
                    da.To = 0;
                    da.Completed += delegate { quickSettingsMenu.Visibility = Visibility.Hidden; };
                }
                else
                    da.To = 1;
                if (quickSettingsMenu != null)
                    quickSettingsMenu.BeginAnimation(UIElement.OpacityProperty, da);
            }
        }

        /// <summary>
        /// Toggles whether FunctionsMenu is open or not with a fade animation
        /// </summary>
        internal static bool FunctionsMenuOpen
        {
            get { return functionsMenuOpen; }
            set
            {
                functionsMenuOpen = value;
                functionsMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { functionsMenu.Visibility = Visibility.Hidden; };
                }
                else
                    da.To = 1;
                if (functionsMenu != null)
                    functionsMenu.BeginAnimation(UIElement.OpacityProperty, da);
            }
        }

        /// <summary>
        /// Check if any UserControls are open
        /// </summary>
        /// <returns></returns>
        internal static bool UserControls_Open()
        {
            if (ImageSettingsMenuOpen)
                return true;

            if (FileMenuOpen)
                return true;

            if (QuickSettingsMenuOpen)
                return true;

            if (FunctionsMenuOpen)
                return true;

            return false;
        }

        /// <summary>
        /// Closes usercontrol menus
        /// </summary>
        internal static void Close_UserControls()
        {
            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;

            if (FunctionsMenuOpen)
                FunctionsMenuOpen = false;
        }

        /// <summary>
        /// Closes usercontrol menus
        /// </summary>
        internal static void Close_UserControls(object sender, RoutedEventArgs e)
        {
            Close_UserControls();
        }

        /// <summary>
        /// Toggles whether open menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Toggle_open_menu(object sender, RoutedEventArgs e)
        {
            FileMenuOpen = !FileMenuOpen;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;

            if (FunctionsMenuOpen)
                FunctionsMenuOpen = false;
        }

        /// <summary>
        /// Toggles whether image menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Toggle_image_menu(object sender, RoutedEventArgs e)
        {
            ImageSettingsMenuOpen = !ImageSettingsMenuOpen;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;

            if (FunctionsMenuOpen)
                FunctionsMenuOpen = false;
        }

        /// <summary>
        /// Toggles whether quick settings menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Toggle_quick_settings_menu(object sender, RoutedEventArgs e)
        {
            QuickSettingsMenuOpen = !QuickSettingsMenuOpen;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (FunctionsMenuOpen)
                FunctionsMenuOpen = false;
        }

        /// <summary>
        /// Toggles whether functions menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Toggle_Functions_menu(object sender, RoutedEventArgs e)
        {
            FunctionsMenuOpen = !FunctionsMenuOpen;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;
        }

        #endregion UserControl Specifics

        #region Manipulate Interface

        

        /// <summary>
        /// Toggle between hidden interface and default
        /// </summary>
        internal static void HideInterface(bool slideshow = false, bool navigationButtons = true)
        {
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
                    minus.Visibility =
                    Visibility.Visible;

                if (!slideshow)
                    Properties.Settings.Default.ShowInterface = false;

                if (activityTimer != null)
                    activityTimer.Start();
            }
            else
            {
                mainWindow.TitleBar.Visibility =
                mainWindow.LowerBar.Visibility =
                mainWindow.LeftBorderRectangle.Visibility =
                mainWindow.RightBorderRectangle.Visibility
                = Visibility.Visible;

                if (navigationButtons)
                    clickArrowLeft.Visibility =
                    clickArrowRight.Visibility =
                    x2.Visibility =
                    minus.Visibility =
                    Visibility.Collapsed;

                Properties.Settings.Default.ShowInterface = true;
                if (activityTimer != null)
                    activityTimer.Stop();
            }
        }

        /// <summary>
        /// Hides/shows interface elements with a fade animation
        /// </summary>
        /// <param name="show"></param>
        internal static async void FadeControlsAsync(bool show)
        {
            var fadeTo = show ? 1 : 0;

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
        /// Timer starts FadeControlsAsync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void ActivityTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            FadeControlsAsync(false);
        }

        /// <summary>
        /// Timer to show/hide cursor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void HideCursorTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new System.Threading.ThreadStart(() =>
            {
                AnimationHelper.Fade(clickArrowLeft, 0, TimeSpan.FromSeconds(.5));
                AnimationHelper.Fade(clickArrowRight, 0, TimeSpan.FromSeconds(.5));
                AnimationHelper.Fade(x2, 0, TimeSpan.FromSeconds(.5));
                Mouse.OverrideCursor = Cursors.None;
            }));
            MouseIdleTimer.Stop();
        }

        /// <summary>
        /// Timer to check if Mouse is idle in Slideshow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void MouseIdleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            HideCursorTimer.Start();
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



        #endregion Manipulate Interface

        #region Windows

        /// <summary>
        /// Show About window in a dialog
        /// </summary>
        internal static void AboutWindow()
        {
            Window window = new About
            {
                Width = mainWindow.Width,
                Height = mainWindow.Height,
                Opacity = 0,
                Owner = Application.Current.MainWindow,
            };

            var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            window.BeginAnimation(UIElement.OpacityProperty, animation);

            window.ShowDialog();
        }

        /// <summary>
        /// Show Help window in a dialog
        /// </summary>
        internal static void HelpWindow()
        {
            Window window = new Help
            {
                Width = mainWindow.Width,
                Height = mainWindow.Height,
                Opacity = 0,
                Owner = Application.Current.MainWindow,
            };

            var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            window.BeginAnimation(UIElement.OpacityProperty, animation);
            window.Show();
        }

        /// <summary>
        /// Show All Settings window in a dialog
        /// </summary>
        internal static void AllSettingsWindow()
        {
            Window window = new AllSettings
            {
                Width = mainWindow.Width,
                Height = mainWindow.Height,
                Opacity = 0,
                Owner = Application.Current.MainWindow,
            };

            var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            window.BeginAnimation(UIElement.OpacityProperty, animation);

            window.ShowDialog();
        }

        ///// <summary>
        ///// Show Tools window
        ///// </summary>
        //internal static void ToolsWindow()
        //{
        //    new Tools().Show();
        //}

        #endregion Windows

        #region MouseOver Button Events

        /*

            Adds MouseOver events for the given elements with the AnimationHelper.
            Changes color depending on the users settings.

        */

        // Logo Mouse Over
        //internal static void LogoMouseOver(object sender, MouseEventArgs e)
        //{
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, pBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, iBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, cBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, vBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, iiBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, eBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, wBrush, false);
        //}

        //internal static void LogoMouseLeave(object sender, MouseEventArgs e)
        //{
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, pBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, iBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, cBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, vBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, iiBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, eBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, wBrush, false);
        //}

        //internal static void LogoMouseButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(pBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(iBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(cBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(vBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(iiBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(eBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(wBrush, false);
        //}

        // Close Button

        internal static void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, mainWindow.CloseButtonBrush, false);
        }

        internal static void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.CloseButtonBrush, false);
        }

        internal static void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, mainWindow.CloseButtonBrush, false);
        }

        // MaxButton
        internal static void MaxButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, mainWindow.MaxButtonBrush, false);
        }

        internal static void MaxButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.MaxButtonBrush, false);
        }

        internal static void MaxButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, mainWindow.MaxButtonBrush, false);
        }

        // MinButton
        internal static void MinButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, mainWindow.MinButtonBrush, false);
        }

        internal static void MinButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.MinButtonBrush, false);
        }

        internal static void MinButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, mainWindow.MinButtonBrush, false);
        }

        // LeftButton
        internal static void LeftButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.LeftArrowFill,
                false
            );
        }

        internal static void LeftButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.LeftArrowFill, false);
        }

        internal static void LeftButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.LeftArrowFill,
                false
            );
        }

        // RightButton
        internal static void RightButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.RightArrowFill,
                false
            );
        }

        internal static void RightButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.RightArrowFill, false);
        }

        internal static void RightButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.RightArrowFill,
                false
            );
        }

        // OpenMenuButton
        internal static void OpenMenuButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.FolderFill,
                false
            );
        }

        internal static void OpenMenuButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.FolderFill, false);
        }

        internal static void OpenMenuButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.FolderFill,
                false
            );
        }

        // ImageButton
        internal static void ImageButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath1Fill,
                false
            );
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath2Fill,
                false
            );
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath3Fill,
                false
            );
            //AnimationHelper.MouseEnterColorEvent(
            //    mainColor.A,
            //    mainColor.R,
            //    mainColor.G,
            //    mainColor.B,
            //    ImagePath4Fill,
            //    false
            //);
        }

        internal static void ImageButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.ImagePath1Fill, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.ImagePath2Fill, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.ImagePath3Fill, false);
            //AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath4Fill, false);
        }

        internal static void ImageButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath1Fill,
                false
            );
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath2Fill,
                false
            );
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.ImagePath3Fill,
                false
            );
            //AnimationHelper.MouseLeaveColorEvent(
            //    mainColor.A,
            //    mainColor.R,
            //    mainColor.G,
            //    mainColor.B,
            //    ImagePath4Fill,
            //    false
            //);
        }

        // SettingsButton
        internal static void SettingsButtonButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.SettingsButtonFill,
                false
            );
        }

        internal static void SettingsButtonButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.SettingsButtonFill, false);
        }

        internal static void SettingsButtonButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.SettingsButtonFill,
                false
            );
        }

        // FunctionMenu
        internal static void FunctionMenuButtonButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.QuestionButtonFill1,
                false
            );
        }

        internal static void FunctionMenuButtonButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(mainWindow.QuestionButtonFill1, false);
        }

        internal static void FunctionMenuButtonButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                mainWindow.QuestionButtonFill1,
                false
            );
        }

        #endregion MouseOver Button Events

        #endregion Interface logic

        /// <summary>
        /// Timer starts Slideshow Fade animation.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="e"></param>
        internal static async void SlideTimer_Elapsed(object server, System.Timers.ElapsedEventArgs e)
        {
            await Application.Current.MainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                AnimationHelper.Fade(mainWindow.img, 0, TimeSpan.FromSeconds(.5));
                Pic(true, false);
                AnimationHelper.Fade(mainWindow.img, 1, TimeSpan.FromSeconds(.5));
            }));
        }
    }
}

using PicView.PicGallery;
using PicView.SystemIntegration;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static PicView.UILogic.ConfigureWindows;
using static PicView.UILogic.HideInterfaceLogic;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.UC;

namespace PicView.UILogic.Sizing
{
    internal static class WindowSizing
    {
        /// <summary>
        /// Used to get and set monitor size
        /// </summary>
        internal static MonitorSize MonitorInfo { get; set; }

        /// <summary>
        /// Set whether to fit window to image or image to window
        /// </summary>
        internal static bool SetWindowBehavior()
        {
            if (Properties.Settings.Default.AutoFitWindow)
            {
                ConfigureWindows.GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                ConfigureWindows.GetMainWindow.ResizeMode = ResizeMode.CanMinimize;
                ConfigureWindows.GetMainWindow.GripButton.Visibility = Visibility.Collapsed;

                if (GetQuickSettingsMenu != null)
                {
                    GetQuickSettingsMenu.SetFit.IsChecked = true;
                }

                ConfigureWindows.GetMainWindow.WindowState = WindowState.Normal;
            }
            else
            {
                ConfigureWindows.GetMainWindow.SizeToContent = SizeToContent.Manual;
                ConfigureWindows.GetMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;
                ConfigureWindows.GetMainWindow.GripButton.Visibility = Visibility.Visible;

                if (GetQuickSettingsMenu != null)
                {
                    GetQuickSettingsMenu.SetFit.IsChecked = false;
                }
            }

            return Properties.Settings.Default.AutoFitWindow;
        }

        #region Window Functions

        /// <summary>
        /// Move window and maximize on double click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Move(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (ConfigureWindows.GetMainWindow.WindowState == WindowState.Maximized) // Reset back to previous state from maximized
            {
                var mousePos = GetMainWindow.PointToScreen(Mouse.GetPosition(GetMainWindow.TitleBar));
                GetMainWindow.Top = mousePos.Y;
                GetMainWindow.Left = mousePos.X;

                ConfigureWindows.GetMainWindow.WindowState = WindowState.Normal;
                ConfigureWindows.GetMainWindow.GripButton.Visibility = Visibility.Visible;

                // Reset margin
                GetMainWindow.TitleBar.Margin = new Thickness(0);
                GetMainWindow.LowerBar.Margin = new Thickness(0);
            }

            if (GetMainWindow.TitleText.InnerTextBox.IsFocused)
            {
                if (e.ClickCount == 2)
                {
                    Fullscreen_Restore(true);
                }
                return;
            }

            if (e.ClickCount == 2)
            {
                Fullscreen_Restore(true);
            }
            else
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    GetMainWindow.DragMove();
                }

                // Update info for possible new screen, needs more engineering
                // Seems to work
                MonitorInfo = MonitorSize.GetMonitorSize();
                SetWindowSize();
            }
        }

        /// <summary>
        /// Move alternative window
        /// </summary>
        internal static void MoveAlt(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                GetMainWindow.DragMove();
                SetWindowSize();
            }
        }

        /// <summary>
        /// Function made to restore and drag window from maximized windowstate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Restore_From_Move(object sender, MouseEventArgs e)
        {
            if (GetMainWindow.WindowState == WindowState.Maximized && e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    GetMainWindow.DragMove();
                    SetWindowSize();
                }
                catch (InvalidOperationException)
                {
                    //Supress "Can only call DragMove when primary mouse button is down"
                }
            }
        }

        /// <summary>
        /// Fullscreen/restore window
        /// </summary>
        internal static void Fullscreen_Restore(bool forceFullscreen = false)
        {
            if (forceFullscreen || !Properties.Settings.Default.Fullscreen)
            {
                // Show fullscreen logic
                RenderFullscreen();

                // Handle if browsing gallery
                if (GalleryFunctions.IsOpen)
                {
                    GalleryLoad.LoadLayout(false);
                    GalleryNavigation.ScrollTo();
                }

                ShowNavigation(Properties.Settings.Default.ShowAltInterfaceButtons);
                ShowShortcuts(Properties.Settings.Default.ShowAltInterfaceButtons);

                Properties.Settings.Default.Fullscreen = true;
            }
            else
            {
                GetMainWindow.Topmost = Properties.Settings.Default.TopMost;
                Properties.Settings.Default.Fullscreen = false;

                if (Properties.Settings.Default.ShowInterface)
                {
                    ShowNavigation(false);
                    ShowTopandBottom(true);
                    ShowShortcuts(false);
                }
                else
                {
                    ShowNavigation(true);
                    ShowTopandBottom(false);
                    ShowShortcuts(true);
                }

                GetMainWindow.WindowState = WindowState.Normal;
                // Reset margin from fullscreen
                GetMainWindow.ParentContainer.Margin = new Thickness(0);
                GetMainWindow.TitleBar.Margin = new Thickness(0);
                GetMainWindow.LowerBar.Margin = new Thickness(0);

                SetWindowBehavior();

                if (Properties.Settings.Default.AutoFitWindow)
                {
                    GetMainWindow.Width =
                    GetMainWindow.Height = double.NaN;
                }
                else
                {
                    SetLastWindowSize();
                    ConfigureWindows.GetMainWindow.ParentContainer.Width = ConfigureWindows.GetMainWindow.Width;
                    ConfigureWindows.GetMainWindow.ParentContainer.Height = ConfigureWindows.GetMainWindow.Height;
                }

                if (Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                {
                    Slideshow.SlideTimer.Enabled = false;
                }
                ConfigureSettings.ConfigColors.UpdateColor(); // Regain border

                if (ConfigureWindows.GetFakeWindow is not null)
                {
                    ConfigureWindows.GetFakeWindow.Hide();
                }

                _ = TryFitImageAsync();
            }
        }

        internal static void RenderFullscreen()
        {
            ShowTopandBottom(false);
            GetMainWindow.Topmost = true;

            GetMainWindow.ResizeMode = ResizeMode.CanMinimize;
            GetMainWindow.SizeToContent = SizeToContent.Manual;
            GetMainWindow.WindowState = WindowState.Maximized;
            GetMainWindow.Width = MonitorInfo.Width;
            GetMainWindow.Height = MonitorInfo.Height;

            // Fix buttons appearing out of window
            GetMainWindow.ParentContainer.Margin = new Thickness(8);

            GetMainWindow.Top = MonitorInfo.WorkArea.Top;
            GetMainWindow.Left = MonitorInfo.WorkArea.Left;

            GetMainWindow.WindowState = WindowState.Maximized;
            ConfigureSettings.ConfigColors.UpdateColor(true);

            _ = TryFitImageAsync();
        }

        internal static void SetMaximized()
        {
            if (GetMainWindow.WindowState != WindowState.Maximized)
            {
                GetMainWindow.WindowState = WindowState.Maximized;
            }

            // Fix buttons appearing out of window
            GetMainWindow.TitleBar.Margin = new Thickness(8, 8, 8, 0);
            GetMainWindow.LowerBar.Margin = new Thickness(8, 0, 8, 8);

            ConfigureWindows.GetMainWindow.GripButton.Visibility = Visibility.Collapsed;
            GetMainWindow.TitleText.MaxWidth = MonitorInfo.WorkArea.Width - 192 * MonitorInfo.DpiScaling;
        }

        /// <summary>
        /// Centers on the current monitor
        /// </summary>
        internal static void CenterWindowOnScreen()
        {
            GetMainWindow.Top = ((MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - GetMainWindow.ActualHeight) / 2 + (MonitorInfo.WorkArea.Top * MonitorInfo.DpiScaling);
            GetMainWindow.Left = ((MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - GetMainWindow.ActualWidth) / 2 + (MonitorInfo.WorkArea.Left * MonitorInfo.DpiScaling);

            SetWindowSize();
        }

        internal static void SetLastWindowSize()
        {
            ConfigureWindows.GetMainWindow.Top = Properties.Settings.Default.Top;
            ConfigureWindows.GetMainWindow.Left = Properties.Settings.Default.Left;
            ConfigureWindows.GetMainWindow.Width = Properties.Settings.Default.Width;
            ConfigureWindows.GetMainWindow.Height = Properties.Settings.Default.Height;
        }

        internal static void SetWindowSize()
        {
            Properties.Settings.Default.Top = GetMainWindow.Top;
            Properties.Settings.Default.Left = GetMainWindow.Left;
            Properties.Settings.Default.Height = GetMainWindow.Height;
            Properties.Settings.Default.Width = GetMainWindow.Width;

            Properties.Settings.Default.Save();
        }

        #endregion Window Functions

        #region Changed Events

        internal static void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (ConfigureWindows.GetMainWindow.WindowState)
            {
                case WindowState.Normal:
                    if (Properties.Settings.Default.Fullscreen)
                    {
                        Fullscreen_Restore();
                    }
                    return;
                case WindowState.Maximized:
                    SetMaximized();
                    return;
                default:
                    return;
            }
        }

        internal static void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // Update size when screen resulution changes
            MonitorInfo = MonitorSize.GetMonitorSize();
            _ = TryFitImageAsync();
        }

        #endregion Changed Events

        /// <summary>
        /// Save settings when closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Window_Closing(object sender, CancelEventArgs e)
        {
            // Close Extra windows when closing
            if (GetFakeWindow != null)
            {
                GetFakeWindow.Close();
            }

            if (GalleryFunctions.IsOpen)
            {
                if (Properties.Settings.Default.FullscreenGalleryHorizontal || Properties.Settings.Default.FullscreenGalleryVertical)
                {
                    Properties.Settings.Default.StartInFullscreenGallery = true;
                }
                else
                {
                    Properties.Settings.Default.StartInFullscreenGallery = false;
                }
            }
            else
            {
                Properties.Settings.Default.StartInFullscreenGallery = false;
            }

            GetMainWindow.Hide(); // Make it feel faster

            if (!Properties.Settings.Default.AutoFitWindow && !Properties.Settings.Default.Fullscreen)
            {
                Properties.Settings.Default.Top = GetMainWindow.Top;
                Properties.Settings.Default.Left = GetMainWindow.Left;
                Properties.Settings.Default.Height = GetMainWindow.Height;
                Properties.Settings.Default.Width = GetMainWindow.Width;
            }

            Properties.Settings.Default.Save();
            FileHandling.DeleteFiles.DeleteTempFiles();
            FileHandling.RecentFiles.WriteToFile();

#if DEBUG
            Trace.Unindent();
            Trace.WriteLine("Debugging closed at " + DateTime.Now);
            Trace.Flush();
#endif
            Environment.Exit(0);
        }


    }
}
using PicView.SystemIntegration;
using PicView.UILogic.Loading;
using PicView.UILogic.PicGallery;
using PicView.Views.Windows;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static PicView.UILogic.HideInterfaceLogic;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.UC;

namespace PicView.UILogic.Sizing
{
    internal static class WindowLogic
    {
        internal static FakeWindow fakeWindow;

        /// <summary>
        /// Used to get and set monitor size
        /// </summary>
        internal static MonitorSize MonitorInfo { get; set; }

        /// <summary>
        /// Set whether to fit window to image or image to window
        /// </summary>
        internal static bool AutoFitWindow
        {
            get
            {
                return Properties.Settings.Default.AutoFitWindow;
            }
            set
            {
                Properties.Settings.Default.AutoFitWindow = value;

                if (value)
                {
                    LoadWindows.GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    LoadWindows.GetMainWindow.ResizeMode = ResizeMode.CanMinimize;
                    LoadWindows.GetMainWindow.GripButton.Visibility = Visibility.Collapsed;

                    if (GetQuickSettingsMenu != null)
                    {
                        GetQuickSettingsMenu.SetFit.IsChecked = value;
                    }

                    LoadWindows.GetMainWindow.WindowState = WindowState.Normal;
                }
                else
                {
                    LoadWindows.GetMainWindow.SizeToContent = SizeToContent.Manual;
                    LoadWindows.GetMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;
                    LoadWindows.GetMainWindow.GripButton.Visibility = Visibility.Visible;

                    if (GetQuickSettingsMenu != null)
                    {
                        GetQuickSettingsMenu.SetFit.IsChecked = value;
                    }
                }

                // Resize it
                TryFitImage();
            }
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

            if (LoadWindows.GetMainWindow.TitleText.InnerTextBox.IsFocused)
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
                    LoadWindows.GetMainWindow.DragMove();
                }

                // Update info for possible new screen, needs more engineering
                // Seems to work
                MonitorInfo = MonitorSize.GetMonitorSize();
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
                LoadWindows.GetMainWindow.DragMove();
            }
        }

        /// <summary>
        /// Function made to restore and drag window from maximized windowstate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Restore_From_Move(object sender, MouseEventArgs e)
        {
            if (LoadWindows.GetMainWindow.WindowState == WindowState.Maximized && e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    LoadWindows.GetMainWindow.DragMove();
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

                // Save size to get back to it when restoring
                if (!Properties.Settings.Default.AutoFitWindow)
                {
                    Properties.Settings.Default.Top = LoadWindows.GetMainWindow.Top;
                    Properties.Settings.Default.Left = LoadWindows.GetMainWindow.Left;
                    Properties.Settings.Default.Height = LoadWindows.GetMainWindow.Height;
                    Properties.Settings.Default.Width = LoadWindows.GetMainWindow.Width;
                }

                Properties.Settings.Default.Fullscreen = true;

                ShowTopandBottom(false);

                LoadWindows.GetMainWindow.Topmost = true;

                LoadWindows.GetMainWindow.ResizeMode = ResizeMode.CanMinimize;
                LoadWindows.GetMainWindow.SizeToContent = SizeToContent.Manual;
                LoadWindows.GetMainWindow.Width = MonitorInfo.Width;
                LoadWindows.GetMainWindow.Height = MonitorInfo.Height;

                LoadWindows.GetMainWindow.Top = MonitorInfo.WorkArea.Top;
                LoadWindows.GetMainWindow.Left = MonitorInfo.WorkArea.Left;

                // Handle if browsing gallery
                if (GalleryFunctions.IsOpen)
                {
                    GalleryLoad.LoadLayout();
                    GalleryScroll.ScrollTo();
                }
                else
                {
                    ShowNavigation(true);
                    ShowShortcuts(true);

                    if (GetGalleryShortcut != null)
                    {
                        GetGalleryShortcut.Opacity =
                        GetClickArrowLeft.Opacity =
                        GetClickArrowRight.Opacity =
                        Getx2.Opacity =
                        GetRestorebutton.Opacity =
                        GetMinus.Opacity = 1;
                    }
                }

                ConfigureSettings.ConfigColors.UpdateColor(true);
                Properties.Settings.Default.Save();
            }
            else
            {
                LoadWindows.GetMainWindow.Topmost = false;

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

                if (AutoFitWindow)
                {
                    LoadWindows.GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    LoadWindows.GetMainWindow.ResizeMode = ResizeMode.NoResize;

                    if (GetQuickSettingsMenu != null)
                    {
                        GetQuickSettingsMenu.SetFit.IsChecked = true;
                    }

                    LoadWindows.GetMainWindow.WindowState = WindowState.Normal;

                    LoadWindows.GetMainWindow.Width = double.NaN;
                    LoadWindows.GetMainWindow.Height = double.NaN;

                    LoadWindows.GetMainWindow.Top -= LoadWindows.GetMainWindow.LowerBar.ActualHeight / 2; // It works...
                }
                else
                {
                    LoadWindows.GetMainWindow.SizeToContent = SizeToContent.Manual;
                    LoadWindows.GetMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                    if (GetQuickSettingsMenu != null)
                    {
                        GetQuickSettingsMenu.SetFit.IsChecked = false;
                    }

                    LoadWindows.GetMainWindow.Top = Properties.Settings.Default.Top;
                    LoadWindows.GetMainWindow.Left = Properties.Settings.Default.Left;
                    LoadWindows.GetMainWindow.Height = Properties.Settings.Default.Height;
                    LoadWindows.GetMainWindow.Width = Properties.Settings.Default.Width;

                    LoadWindows.GetMainWindow.ParentContainer.Width = double.NaN;
                    LoadWindows.GetMainWindow.ParentContainer.Height = double.NaN;
                }

                TryFitImage();
                ConfigureSettings.ConfigColors.UpdateColor(); // Regain border

                Properties.Settings.Default.Fullscreen = false;
            }
        }

        /// <summary>
        /// Centers on the current monitor
        /// </summary>
        internal static void CenterWindowOnScreen()
        {
            //move to the centre
            LoadWindows.GetMainWindow.Left = ((MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - LoadWindows.GetMainWindow.ActualWidth) / 2 + MonitorInfo.WorkArea.Left;
            LoadWindows.GetMainWindow.Top = ((MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - LoadWindows.GetMainWindow.ActualHeight) / 2 + MonitorInfo.WorkArea.Top;
        }

        #endregion Window Functions

        #region Changed Events

        internal static void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (LoadWindows.GetMainWindow.WindowState == WindowState.Maximized)
            {
                LoadWindows.GetMainWindow.WindowState = WindowState.Normal;
                Fullscreen_Restore();
            }
        }

        internal static void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // Update size when screen resulution changes
            MonitorInfo = MonitorSize.GetMonitorSize();
            TryFitImage();
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
            if (fakeWindow != null)
            {
                fakeWindow.Close();
            }

            LoadWindows.GetMainWindow.Hide(); // Make it feel faster

            if (!Properties.Settings.Default.AutoFitWindow && !Properties.Settings.Default.Fullscreen)
            {
                Properties.Settings.Default.Top = LoadWindows.GetMainWindow.Top;
                Properties.Settings.Default.Left = LoadWindows.GetMainWindow.Left;
                Properties.Settings.Default.Height = LoadWindows.GetMainWindow.Height;
                Properties.Settings.Default.Width = LoadWindows.GetMainWindow.Width;
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
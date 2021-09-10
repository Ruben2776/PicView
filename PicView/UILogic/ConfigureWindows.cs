using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.Views.Windows;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static PicView.UILogic.HideInterfaceLogic;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.UC;

namespace PicView.UILogic
{
    internal static class ConfigureWindows
    {
        internal static FakeWindow GetFakeWindow { get; set; }
        internal static SettingsWindow GetSettingsWindow { get; set; }
        internal static InfoWindow GetInfoWindow { get; set; }
        internal static EffectsWindow GetEffectsWindow { get; set; }
        internal static ImageInfoWindow GetImageInfoWindow { get; set; }

        /// <summary>
        /// The Main Window?
        /// </summary>
        internal static readonly MainWindow GetMainWindow = (MainWindow)Application.Current.MainWindow;

        /// <summary>
        /// Primary ContextMenu
        /// </summary>
        internal static System.Windows.Controls.ContextMenu MainContextMenu { get; set; }

        internal static bool IsMainWindowTopMost
        {
            get { return Properties.Settings.Default.TopMost; }
            set
            {
                Properties.Settings.Default.TopMost = value;
                GetMainWindow.Topmost = value;
            }
        }

        #region Windows

        /// <summary>
        /// Show Help window in a dialog
        /// </summary>
        internal static void InfoWindow()
        {
            if (GetInfoWindow == null)
            {
                GetInfoWindow = new InfoWindow();
                GetInfoWindow.Show();
            }
            else
            {
                if (GetInfoWindow.Visibility == Visibility.Visible)
                {
                    GetInfoWindow.Focus();
                }
                else
                {
                    GetInfoWindow.Show();
                }
            }
            if (Properties.Settings.Default.Fullscreen)
            {
                GetSettingsWindow.Topmost = true;
                GetSettingsWindow.BringIntoView();
            }
            else
            {
                GetSettingsWindow.Topmost = false;
            }
        }

        /// <summary>
        /// Show All Settings window
        /// </summary>
        internal static void AllSettingsWindow()
        {
            if (GetSettingsWindow == null)
            {
                GetSettingsWindow = new SettingsWindow();

                GetSettingsWindow.Show();
            }
            else
            {
                if (GetSettingsWindow.Visibility == Visibility.Visible)
                {
                    GetSettingsWindow.Focus();
                }
                else
                {
                    GetSettingsWindow.Show();
                }
            }
            if (Properties.Settings.Default.Fullscreen)
            {
                GetSettingsWindow.Topmost = true;
                GetSettingsWindow.BringIntoView();
            }
            else
            {
                GetSettingsWindow.Topmost = false;
            }
        }

        /// <summary>
        /// Show Effects window
        /// </summary>
        internal static void EffectsWindow()
        {
            if (GetEffectsWindow == null)
            {
                GetEffectsWindow = new EffectsWindow();

                GetEffectsWindow.Show();
            }
            else
            {
                if (GetEffectsWindow.Visibility == Visibility.Visible)
                {
                    GetEffectsWindow.Focus();
                }
                else
                {
                    GetEffectsWindow.Show();
                }
            }
            if (Properties.Settings.Default.Fullscreen)
            {
                GetEffectsWindow.Topmost = true;
                GetEffectsWindow.BringIntoView();
            }
            else
            {
                GetEffectsWindow.Topmost = false;
            }
        }

        /// <summary>
        /// Show ImageInfo window
        /// </summary>
        internal static void ImageInfoWindow()
        {
            if (GetImageInfoWindow == null)
            {
                GetImageInfoWindow = new ImageInfoWindow { Owner = GetMainWindow };

                GetImageInfoWindow.Show();
            }
            else
            {
                if (GetImageInfoWindow.Visibility == Visibility.Visible)
                {
                    GetImageInfoWindow.Focus();
                }
                else
                {
                    GetImageInfoWindow.Show();
                }
            }
            if (Properties.Settings.Default.Fullscreen)
            {
                GetEffectsWindow.Topmost = true;
                GetImageInfoWindow.BringIntoView();
            }
            else
            {
                GetImageInfoWindow.Topmost = false;
            }
        }

        #endregion Windows

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
                    GalleryLoad.LoadLayout();
                    GalleryNavigation.ScrollTo();
                }

                ShowNavigation(Properties.Settings.Default.ShowAltInterfaceButtons);
                ShowShortcuts(Properties.Settings.Default.ShowAltInterfaceButtons);

                Properties.Settings.Default.Fullscreen = true;
            }
            else
            {
                GetMainWindow.Topmost = Properties.Settings.Default.TopMost;

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
                    GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    GetMainWindow.ResizeMode = ResizeMode.NoResize;

                    if (GetQuickSettingsMenu != null)
                    {
                        GetQuickSettingsMenu.SetFit.IsChecked = true;
                    }

                    GetMainWindow.Width = double.NaN;
                    GetMainWindow.Height = double.NaN;

                    GetMainWindow.Top -= GetMainWindow.LowerBar.ActualHeight / 2; // It works...
                }
                else
                {
                    GetMainWindow.SizeToContent = SizeToContent.Manual;
                    GetMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                    if (GetQuickSettingsMenu != null)
                    {
                        GetQuickSettingsMenu.SetFit.IsChecked = false;
                    }

                    GetMainWindow.Top = Properties.Settings.Default.Top;
                    GetMainWindow.Left = Properties.Settings.Default.Left;
                    GetMainWindow.Height = Properties.Settings.Default.Height;
                    GetMainWindow.Width = Properties.Settings.Default.Width;

                    GetMainWindow.ParentContainer.Width = double.NaN;
                    GetMainWindow.ParentContainer.Height = double.NaN;
                }

                if (GetMainWindow.WindowState == WindowState.Maximized)
                {
                    GetMainWindow.WindowState = WindowState.Normal;
                    // Reset margin from fullscreen
                    GetMainWindow.ParentContainer.Margin = new Thickness(0);
                }

                if (Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                {
                    Slideshow.SlideTimer.Enabled = false;
                }

                Properties.Settings.Default.Fullscreen = false;
                TryFitImage();
                ConfigureSettings.ConfigColors.UpdateColor(); // Regain border
            }
        }

        internal static void RenderFullscreen()
        {
            // Save size to get back to it when restoring
            if (!Properties.Settings.Default.AutoFitWindow)
            {
                Properties.Settings.Default.Top = GetMainWindow.Top;
                Properties.Settings.Default.Left = GetMainWindow.Left;
                Properties.Settings.Default.Height = GetMainWindow.Height;
                Properties.Settings.Default.Width = GetMainWindow.Width;
            }

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

            ConfigureSettings.ConfigColors.UpdateColor(true);
        }

        /// <summary>
        /// Centers on the current monitor
        /// </summary>
        internal static void CenterWindowOnScreen()
        {
            //move to the centre
            GetMainWindow.Left = ((MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - GetMainWindow.ActualWidth) / 2 + MonitorInfo.WorkArea.Left;
            GetMainWindow.Top = ((MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - GetMainWindow.ActualHeight) / 2 + MonitorInfo.WorkArea.Top;
        }

        #endregion Window Functions

        #region Changed Events

        internal static void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (GetMainWindow.WindowState == WindowState.Maximized)
            {
                RenderFullscreen();
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
            if (GetFakeWindow != null)
            {
                GetFakeWindow.Close();
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
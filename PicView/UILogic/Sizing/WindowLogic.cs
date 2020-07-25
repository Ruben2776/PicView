using PicView.SystemIntegration;
using PicView.UILogic.PicGallery;
using PicView.Views.Windows;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static PicView.Library.Fields;
using static PicView.UILogic.HideInterfaceLogic;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.UC;

namespace PicView.UILogic.Sizing
{
    internal static class WindowLogic
    {
        internal static FakeWindow fakeWindow;

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
                    TheMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    TheMainWindow.ResizeMode = ResizeMode.CanMinimize;

                    if (GetQuickSettingsMenu != null)
                    {
                        GetQuickSettingsMenu.SetFit.IsChecked = value;
                    }

                    TheMainWindow.WindowState = WindowState.Normal;
                }
                else
                {
                    TheMainWindow.SizeToContent = SizeToContent.Manual;
                    TheMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

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

            if (TheMainWindow.TitleText.InnerTextBox.IsFocused)
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
                    TheMainWindow.DragMove();
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
                TheMainWindow.DragMove();
            }
        }

        /// <summary>
        /// Function made to restore and drag window from maximized windowstate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Restore_From_Move(object sender, MouseEventArgs e)
        {
            if (TheMainWindow.WindowState == WindowState.Maximized && e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    TheMainWindow.DragMove();
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
                    Properties.Settings.Default.Top = TheMainWindow.Top;
                    Properties.Settings.Default.Left = TheMainWindow.Left;
                    Properties.Settings.Default.Height = TheMainWindow.Height;
                    Properties.Settings.Default.Width = TheMainWindow.Width;
                }

                Properties.Settings.Default.Fullscreen = true;

                ShowTopandBottom(false);

                TheMainWindow.Topmost = true;

                TheMainWindow.ResizeMode = ResizeMode.CanMinimize;
                TheMainWindow.SizeToContent = SizeToContent.Manual;
                TheMainWindow.Width = MonitorInfo.Width;
                TheMainWindow.Height = MonitorInfo.Height;

                TheMainWindow.Top = MonitorInfo.WorkArea.Top;
                TheMainWindow.Left = MonitorInfo.WorkArea.Left;

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
                TheMainWindow.Topmost = false;

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
                    TheMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    TheMainWindow.ResizeMode = ResizeMode.NoResize;

                    if (GetQuickSettingsMenu != null)
                    {
                        GetQuickSettingsMenu.SetFit.IsChecked = true;
                    }

                    TheMainWindow.WindowState = WindowState.Normal;

                    TheMainWindow.Width = double.NaN;
                    TheMainWindow.Height = double.NaN;

                    TheMainWindow.Top -= TheMainWindow.LowerBar.ActualHeight / 2; // It works...
                }
                else
                {
                    TheMainWindow.SizeToContent = SizeToContent.Manual;
                    TheMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                    if (GetQuickSettingsMenu != null)
                    {
                        GetQuickSettingsMenu.SetFit.IsChecked = false;
                    }

                    TheMainWindow.Top = Properties.Settings.Default.Top;
                    TheMainWindow.Left = Properties.Settings.Default.Left;
                    TheMainWindow.Height = Properties.Settings.Default.Height;
                    TheMainWindow.Width = Properties.Settings.Default.Width;

                    TheMainWindow.ParentContainer.Width = double.NaN;
                    TheMainWindow.ParentContainer.Height = double.NaN;
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
            TheMainWindow.Left = (MonitorInfo.WorkArea.Width - TheMainWindow.ActualWidth) / 2 + MonitorInfo.WorkArea.Left;
            TheMainWindow.Top = (MonitorInfo.WorkArea.Height - TheMainWindow.ActualHeight) / 2 + MonitorInfo.WorkArea.Top;
        }

        #endregion Window Functions

        #region Changed Events

        internal static void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (TheMainWindow.WindowState == WindowState.Maximized)
            {
                TheMainWindow.WindowState = WindowState.Normal;
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

            TheMainWindow.Hide(); // Make it feel faster

            if (!Properties.Settings.Default.AutoFitWindow && !Properties.Settings.Default.Fullscreen)
            {
                Properties.Settings.Default.Top = TheMainWindow.Top;
                Properties.Settings.Default.Left = TheMainWindow.Left;
                Properties.Settings.Default.Height = TheMainWindow.Height;
                Properties.Settings.Default.Width = TheMainWindow.Width;
            }

            Properties.Settings.Default.Save();
            FileHandling.DeleteFiles.DeleteTempFiles();
            FileHandling.RecentFiles.WriteToFile();

#if DEBUG
            Trace.Unindent();
            Trace.WriteLine("Goodbye cruel world!");
            Trace.Flush();
#endif
            Environment.Exit(0);
        }
    }
}
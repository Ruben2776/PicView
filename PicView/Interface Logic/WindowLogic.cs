using PicView.Windows;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.Fields;
using static PicView.Helper;
using static PicView.HideInterfaceLogic;
using static PicView.Resize_and_Zoom;

namespace PicView
{
    internal static class WindowLogic
    {
        internal static FakeWindow fake;

        /// <summary>
        /// Set whether to fit window to image or image to window
        /// </summary>
        internal static bool FitToWindow
        {
            get
            {
                return Properties.Settings.Default.FitToWindow;
            }
            set
            {
                Properties.Settings.Default.FitToWindow = value;

                if (value)
                {
                    mainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    mainWindow.ResizeMode = ResizeMode.CanMinimize;

                    //if (quickSettingsMenu != null)
                    //{
                    //    quickSettingsMenu.SetFit.IsChecked = true;
                    //    quickSettingsMenu.SetCenter.IsChecked = false;
                    //}

                    mainWindow.WindowState = WindowState.Normal;

                }
                else
                {
                    mainWindow.SizeToContent = SizeToContent.Manual;
                    mainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                    //if (quickSettingsMenu != null)
                    //{
                    //    quickSettingsMenu.SetCenter.IsChecked = true;
                    //    quickSettingsMenu.SetFit.IsChecked = false;
                    //}
                }

                // Resize it
                TryZoomFit();
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

            if (e.ClickCount == 2)
            {
                // Prevent method from being called twice
                var bar = sender as TextBlock;
                if (bar != null)
                {
                    if (bar.Name == "Bar")
                    {
                        return;
                    }
                }
                Maximize_Restore();
            }
            else
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    mainWindow.DragMove();
                }

                // Update info for possible new screen, needs more engineering
                MonitorInfo = MonitorSize.GetMonitorSize();
            }
        }


        /// <summary>
        /// Function made to restore and drag window from maximized windowstate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Restore_From_Move(object sender, MouseEventArgs e)
        {
            if (mainWindow.WindowState == WindowState.Maximized && e.LeftButton == MouseButtonState.Pressed)
            {
                //Maximize_Restore();
                try
                {
                    mainWindow.DragMove();
                }
                catch (InvalidOperationException)
                {
                    //Supress "Can only call DragMove when primary mouse button is down"
                }
            }
        }

        /// <summary>
        /// Maximizes/restore window
        /// </summary>
        internal static void Maximize_Restore()
        {
            // Maximize
            if (mainWindow.WindowState == WindowState.Normal)
            {
                // Update new setting and sizing
                if (FitToWindow)
                {
                    FitToWindow = false;
                }

                // Tell Windows that it's maximized
                mainWindow.WindowState = WindowState.Maximized;
                SystemCommands.MaximizeWindow(mainWindow);
            }

            // Restore
            else if (mainWindow.WindowState == WindowState.Maximized)
            {
                // Update new setting and sizing
                FitToWindow = true;

                // Tell Windows that it's normal
                mainWindow.WindowState = WindowState.Normal;
                SystemCommands.RestoreWindow(mainWindow);
            }
        }

        /// <summary>
        /// Fullscreen/restore window
        /// </summary>
        internal static void Fullscreen_Restore(bool startup = false)
        {
            if (startup || !Properties.Settings.Default.Fullscreen)
            {
                if (!FitToWindow)
                {
                    Properties.Settings.Default.Save();
                }

                Properties.Settings.Default.Fullscreen = true;
                // Update new setting and sizing
                //FitToWindow = false;

                ShowTopandBottom(false);
                ShowNavigation(false);
                ShowShortcuts(false);

                mainWindow.ResizeMode = ResizeMode.CanMinimize;
                mainWindow.SizeToContent = SizeToContent.Manual;
                mainWindow.Width = mainWindow.bg.Width = SystemParameters.PrimaryScreenWidth;
                mainWindow.Height = SystemParameters.PrimaryScreenHeight;

                mainWindow.Top = 0;
                mainWindow.Left = 0;

                mainWindow.Topmost = true;

                RemoveBorderColor();
            }
            else
            {
                mainWindow.Topmost = false;

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

                if (FitToWindow)
                {
                    mainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    mainWindow.ResizeMode = ResizeMode.NoResize;

                    //if (quickSettingsMenu != null)
                    //{
                    //    quickSettingsMenu.SetFit.IsChecked = true;
                    //}

                    mainWindow.WindowState = WindowState.Normal;

                    mainWindow.Width = mainWindow.bg.Width = double.NaN;
                    mainWindow.Height = mainWindow.bg.Height = double.NaN;

                    mainWindow.Top -= mainWindow.LowerBar.ActualHeight / 2; // It works...
                }
                else
                {
                    mainWindow.SizeToContent = SizeToContent.Manual;
                    mainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                    //if (quickSettingsMenu != null)
                    //{
                    //    quickSettingsMenu.SetCenter.IsChecked = true;
                    //}

                    mainWindow.Top = Properties.Settings.Default.Top;
                    mainWindow.Left = Properties.Settings.Default.Left;
                    mainWindow.Height = Properties.Settings.Default.Height;
                    mainWindow.Width = Properties.Settings.Default.Width;

                    mainWindow.bg.Width = double.NaN;
                    mainWindow.bg.Height = double.NaN;

                    TryZoomFit();
                }

                UpdateColor(); // Regain border              

                Properties.Settings.Default.Fullscreen = false;
            }

        }

        /// <summary>
        /// Centers on the current monitor
        /// </summary>
        internal static void CenterWindowOnScreen()
        {
            if (!FitToWindow)
            {
                return;
            }

            //move to the centre
            mainWindow.Left = (((MonitorInfo.WorkArea.Width - (mainWindow.Width * MonitorInfo.DpiScaling)) / 2) + (MonitorInfo.WorkArea.Left * MonitorInfo.DpiScaling));
            mainWindow.Top = ((MonitorInfo.WorkArea.Height - (mainWindow.Height * MonitorInfo.DpiScaling)) / 2) + (MonitorInfo.WorkArea.Top * MonitorInfo.DpiScaling);

        }

        #endregion
    }
}

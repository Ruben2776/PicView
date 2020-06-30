using PicView.SystemIntegration;
using PicView.UI.Windows;
using System;
using System.Windows;
using System.Windows.Input;
using static PicView.Library.Fields;
using static PicView.UI.HideInterfaceLogic;
using static PicView.UI.Sizing.ScaleImage;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.Sizing
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

                    if (quickSettingsMenu != null)
                    {
                        quickSettingsMenu.SetFit.IsChecked = value;
                    }

                    TheMainWindow.WindowState = WindowState.Normal;
                }
                else
                {
                    TheMainWindow.SizeToContent = SizeToContent.Manual;
                    TheMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                    if (quickSettingsMenu != null)
                    {
                        quickSettingsMenu.SetFit.IsChecked = value;
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

            if (TheMainWindow.Bar.Bar.IsFocused)
            {
                if (e.ClickCount == 2)
                {
                    Maximize_Restore();
                    //EditTitleBar.Refocus();
                    //e.Handled = true;
                }
                //if (e.ClickCount == 2)
                //{
                //    mainWindow.Bar.Bar.SelectAll();
                //}
                return;
            }

            if (e.ClickCount == 2)
            {
                Maximize_Restore();
                e.Handled = true;
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
        /// Maximizes/restore window
        /// </summary>
        internal static void Maximize_Restore()
        {
            // Maximize
            if (TheMainWindow.WindowState == WindowState.Normal)
            {
                Maximize();
            }

            // Restore
            else if (TheMainWindow.WindowState == WindowState.Maximized)
            {
                Restore();
            }
        }

        internal static void Maximize()
        {
            // Update new setting and sizing
            AutoFitWindow = false;
            Properties.Settings.Default.Maximized = true;

            // Tell Windows that it's maximized
            TheMainWindow.WindowState = WindowState.Maximized;
            SystemCommands.MaximizeWindow(TheMainWindow);
            TheMainWindow.LowerBar.Height = 44; // Seems to fix UI going below Windows taskbar
        }

        internal static void Restore()
        {
            // Update new setting and sizing
            AutoFitWindow = true;
            Properties.Settings.Default.Maximized = false;

            // Tell Windows that it's normal
            TheMainWindow.WindowState = WindowState.Normal;
            SystemCommands.RestoreWindow(TheMainWindow);
            TheMainWindow.LowerBar.Height = 35; // Set it back

            if (!Properties.Settings.Default.AutoFitWindow)
            {
                if (Properties.Settings.Default.Width != 0)
                {
                    TheMainWindow.Top = Properties.Settings.Default.Top;
                    TheMainWindow.Left = Properties.Settings.Default.Left;
                    TheMainWindow.Height = Properties.Settings.Default.Height;
                    TheMainWindow.Width = Properties.Settings.Default.Width;
                }
            }
            else
            {
                CenterWindowOnScreen();
            }

            ConfigColors.UpdateColor();
        }

        /// <summary>
        /// Fullscreen/restore window
        /// </summary>
        internal static void Fullscreen_Restore(bool startup = false)
        {
            if (startup || !Properties.Settings.Default.Fullscreen)
            {
                if (!AutoFitWindow)
                {
                    Properties.Settings.Default.Save();
                }

                Properties.Settings.Default.Fullscreen = true;
                // Update new setting and sizing
                //FitToWindow = false;

                ShowTopandBottom(false);
                ShowNavigation(false);
                ShowShortcuts(false);

                TheMainWindow.ResizeMode = ResizeMode.CanMinimize;
                TheMainWindow.SizeToContent = SizeToContent.Manual;
                TheMainWindow.Width = TheMainWindow.ParentContainer.Width = SystemParameters.PrimaryScreenWidth;
                TheMainWindow.Height = SystemParameters.PrimaryScreenHeight;

                TheMainWindow.Top = 0;
                TheMainWindow.Left = 0;

                TheMainWindow.Topmost = true;

                ConfigColors.UpdateColor(true);
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

                    if (quickSettingsMenu != null)
                    {
                        quickSettingsMenu.SetFit.IsChecked = true;
                    }

                    TheMainWindow.WindowState = WindowState.Normal;

                    TheMainWindow.Width = TheMainWindow.ParentContainer.Width = double.NaN;
                    TheMainWindow.Height = TheMainWindow.ParentContainer.Height = double.NaN;

                    TheMainWindow.Top -= TheMainWindow.LowerBar.ActualHeight / 2; // It works...
                }
                else
                {
                    TheMainWindow.SizeToContent = SizeToContent.Manual;
                    TheMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                    if (quickSettingsMenu != null)
                    {
                        quickSettingsMenu.SetFit.IsChecked = false;
                    }

                    TheMainWindow.Top = Properties.Settings.Default.Top;
                    TheMainWindow.Left = Properties.Settings.Default.Left;
                    TheMainWindow.Height = Properties.Settings.Default.Height;
                    TheMainWindow.Width = Properties.Settings.Default.Width;

                    TheMainWindow.ParentContainer.Width = double.NaN;
                    TheMainWindow.ParentContainer.Height = double.NaN;
                }

                TryFitImage();
                ConfigColors.UpdateColor(); // Regain border

                Properties.Settings.Default.Fullscreen = false;
            }
        }

        /// <summary>
        /// Centers on the current monitor
        /// </summary>
        internal static void CenterWindowOnScreen()
        {
            //move to the centre
            TheMainWindow.Left = (((MonitorInfo.WorkArea.Width - (TheMainWindow.Width * MonitorInfo.DpiScaling)) / 2) + (MonitorInfo.WorkArea.Left * MonitorInfo.DpiScaling));
            TheMainWindow.Top = ((MonitorInfo.WorkArea.Height - (TheMainWindow.Height * MonitorInfo.DpiScaling)) / 2) + (MonitorInfo.WorkArea.Top * MonitorInfo.DpiScaling);
        }

        #endregion Window Functions
    }
}
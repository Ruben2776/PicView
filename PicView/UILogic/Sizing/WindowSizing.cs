using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.Views.UserControls.Buttons;
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
        internal static void SetWindowBehavior()
        {
            if (Settings.Default.AutoFitWindow)
            {
                GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                GetMainWindow.ResizeMode = ResizeMode.CanMinimize;

                if (GetGripButton is not null)
                {
                    GetGripButton.Visibility = Visibility.Collapsed;
                }

                if (GetQuickSettingsMenu != null)
                {
                    GetQuickSettingsMenu.SetFit.IsChecked = true;
                }

                GetMainWindow.WindowState = WindowState.Normal;

                GetMainWindow.Width =
                GetMainWindow.Height =
                GetMainWindow.ParentContainer.Width =
                GetMainWindow.ParentContainer.Height =
                GetMainWindow.MainImageBorder.Width =
                GetMainWindow.MainImageBorder.Height = double.NaN;
            }
            else
            {
                GetMainWindow.SizeToContent = SizeToContent.Manual;
                GetMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                if (GetGripButton is null)
                {
                    GetGripButton = new GripButton();
                    GetMainWindow.LowerBar.Children.Add(GetGripButton);
                }
                GetGripButton.Visibility = Visibility.Visible;

                if (GetQuickSettingsMenu != null)
                {
                    GetQuickSettingsMenu.SetFit.IsChecked = false;
                }

                GetMainWindow.Width =
                GetMainWindow.Height =
                GetMainWindow.MainImageBorder.Width =
                GetMainWindow.MainImageBorder.Height = double.NaN;

                GetMainWindow.ParentContainer.Width = GetMainWindow.Width;
                GetMainWindow.ParentContainer.Height = GetMainWindow.Height;
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

            if (GetMainWindow.WindowState == WindowState.Maximized) // Reset back to previous state from maximized
            {
                var mousePos = GetMainWindow.PointToScreen(Mouse.GetPosition(GetMainWindow));
                GetMainWindow.Top = mousePos.Y;
                GetMainWindow.Left = mousePos.X;

                Restore_From_Move();
            }

            if (e.ClickCount == 2)
            {
                if (Settings.Default.AutoFitWindow)
                {
                    Settings.Default.AutoFitWindow = false;
                    SetWindowBehavior();
                    Settings.Default.AutoFitWindow = true;
                }
                Fullscreen_Restore(true);
            }
            else
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    GetMainWindow.DragMove();                
                    // Update info for possible new screen, needs more engineering
                    // Seems to work
                    MonitorInfo = MonitorSize.GetMonitorSize();

                    SetWindowSize();
                }
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
                MonitorInfo = MonitorSize.GetMonitorSize();

                SetWindowSize();
            }
        }

        /// <summary>
        /// Function made to restore and drag window from maximized windowstate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Restore_From_Move()
        {
            GetMainWindow.WindowState = WindowState.Normal;
            Settings.Default.Maximized = false;
            GetGripButton.Visibility = Visibility.Visible;

            // Reset margin
            GetMainWindow.TitleBar.Margin = new Thickness(0);
            GetMainWindow.LowerBar.Margin = new Thickness(0);

            SetWindowSize();
        }

        /// <summary>
        /// Fullscreen/restore window
        /// </summary>
        internal static void Fullscreen_Restore(bool gotoFullscreen)
        {
            if (Navigation.ClickArrowLeftClicked || Navigation.ClickArrowRightClicked) 
            {
                return; // Fixes weird unintentional hit
            }

            SetWindowSize();

            if (GetMainWindow.WindowState == WindowState.Maximized || gotoFullscreen)
            {
                // Show fullscreen logic
                RenderFullscreen();

                // Handle if browsing gallery
                if (GalleryFunctions.IsHorizontalOpen)
                {
                    GalleryLoad.LoadLayout();
                    GalleryNavigation.ScrollTo();
                }

                ShowNavigation(Settings.Default.ShowAltInterfaceButtons);
                ShowShortcuts(Settings.Default.ShowAltInterfaceButtons);

                Settings.Default.Fullscreen = true;
            }
            else
            {
                GetMainWindow.Topmost = Settings.Default.TopMost;
                Settings.Default.Fullscreen = false;

                if (Settings.Default.ShowInterface)
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

                // Reset margin from fullscreen
                GetMainWindow.ParentContainer.Margin = new Thickness(0);
                GetMainWindow.TitleBar.Margin = new Thickness(0);
                GetMainWindow.LowerBar.Margin = new Thickness(0);

                SetWindowBehavior();

                if (Settings.Default.AutoFitWindow == false)
                {
                    SetLastWindowSize();
                }

                if (Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                {
                    Slideshow.SlideTimer.Enabled = false;
                }

                TryFitImage();
            }
        }

        internal static void RenderFullscreen()
        {
            Settings.Default.ScrollEnabled = false; // Don't scroll in fulscreen

            ShowTopandBottom(false);
            GetMainWindow.Topmost = true;

            GetMainWindow.ResizeMode = ResizeMode.CanMinimize;
            GetMainWindow.SizeToContent = SizeToContent.Manual;
            GetMainWindow.Width = MonitorInfo.WorkArea.Width;
            GetMainWindow.Height = MonitorInfo.WorkArea.Height;

            TryFitImage();

            GetMainWindow.Top = 0;
            GetMainWindow.Left = 0;
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

            if (GetGripButton is not null)
            {
                GetGripButton.Visibility = Visibility.Collapsed;
            }

            GetMainWindow.TitleText.MaxWidth = MonitorInfo.WorkArea.Width - 192 * MonitorInfo.DpiScaling;

            Settings.Default.Maximized = true;
        }

        /// <summary>
        /// Centers on the current monitor
        /// </summary>
        internal static void CenterWindowOnScreen(bool horizontal = true)
        {
            GetMainWindow.Top = ((MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - GetMainWindow.ActualHeight) / 2 + MonitorInfo.WorkArea.Top;
            if (horizontal)
                GetMainWindow.Left = ((MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - GetMainWindow.ActualWidth) / 2 + MonitorInfo.WorkArea.Left;
        }

        internal static void SetLastWindowSize()
        {
            GetMainWindow.Dispatcher.Invoke(() =>
            {
                GetMainWindow.Top = Settings.Default.Top;
                GetMainWindow.Left = Settings.Default.Left;
                GetMainWindow.Width = Double.IsNaN(Settings.Default.Width) ? GetMainWindow.Width :
                    Double.IsNaN(Settings.Default.Width) ? GetMainWindow.ActualWidth : Settings.Default.Width;
                GetMainWindow.Height = Double.IsNaN(Settings.Default.Height) ? GetMainWindow.Height :
                    Double.IsNaN(Settings.Default.Height) ? GetMainWindow.ActualHeight : Settings.Default.Height;
            });
        }

        internal static void SetWindowSize()
        {
            if (Settings.Default.AutoFitWindow && Settings.Default.Fullscreen)
                return;

            GetMainWindow.Dispatcher.Invoke(() =>
            {
                if (GetMainWindow.WindowState == WindowState.Maximized || Properties.Settings.Default.Fullscreen)
                    return;

                Settings.Default.Top = GetMainWindow.Top;
                Settings.Default.Left = GetMainWindow.Left;
                Settings.Default.Height = GetMainWindow.ActualHeight;
                Settings.Default.Width = GetMainWindow.ActualWidth;

                Settings.Default.Save();
            });
        }

        #endregion Window Functions

        #region Changed Events

        internal static void MainWindow_StateChanged()
        {
            switch (GetMainWindow.WindowState)
            {
                case WindowState.Normal:
                    return;

                case WindowState.Maximized:
                    SetMaximized();
                    return;

                default:
                    return;
            }
        }

        internal static void SystemEvents_DisplaySettingsChanged()
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
        internal static void Window_Closing()
        {
            GetMainWindow.Hide(); // Make it feel faster

            SetWindowSize();

            Navigation.Pics.Clear(); // Make it cancel task

            // Close Extra windows when closing
            GetInfoWindow?.Close();

            GetImageInfoWindow?.Close();

            GetEffectsWindow?.Close();

            GetSettingsWindow?.Close();

            Settings.Default.Save();
            DeleteFiles.DeleteTempFiles();
            Navigation.GetFileHistory.WriteToFile();
            Application.Current.Shutdown();
        }
    }
}
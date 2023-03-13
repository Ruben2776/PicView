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
                }
                SetMaximized();
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
        internal static void Restore_From_Move()
        {
            GetMainWindow.WindowState = WindowState.Normal;
            Settings.Default.Maximized = false;
            GetGripButton.Visibility = Visibility.Visible;

            // Reset margin
            GetMainWindow.TitleBar.Margin = new Thickness(0);
            GetMainWindow.LowerBar.Margin = new Thickness(0);
        }

        /// <summary>
        /// Fullscreen/restore window
        /// </summary>
        internal static void Fullscreen_Restore(bool forceFullscreen = false)
        {
            if (forceFullscreen || Settings.Default.Fullscreen == false)
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

                if (Settings.Default.AutoFitWindow == false)
                {
                    SetLastWindowSize();
                }

                SetWindowBehavior();

                if (Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                {
                    Slideshow.SlideTimer.Enabled = false;
                }

                _ = TryFitImageAsync();
            }
        }

        internal static void RenderFullscreen()
        {
            Settings.Default.ScrollEnabled = false; // Don't scroll in fulscreen

            SetWindowSize();

            ShowTopandBottom(false);
            GetMainWindow.Topmost = true;

            GetMainWindow.ResizeMode = ResizeMode.CanMinimize;
            GetMainWindow.SizeToContent = SizeToContent.Manual;
            GetMainWindow.Width = MonitorInfo.WorkArea.Width;
            GetMainWindow.Height = MonitorInfo.WorkArea.Height;

            GetMainWindow.MainImageBorder.Width = GetMainWindow.Width;
            GetMainWindow.MainImageBorder.Height = GetMainWindow.Height;

            _ = TryFitImageAsync();

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
                GetMainWindow.Width = Settings.Default.Width;
                GetMainWindow.Height = Settings.Default.Height;
            });
        }

        internal static void SetWindowSize()
        {
            GetMainWindow.Dispatcher.Invoke(() =>
            {
                Settings.Default.Top = GetMainWindow.Top;
                Settings.Default.Left = GetMainWindow.Left;
                Settings.Default.Height = GetMainWindow.Height;
                Settings.Default.Width = GetMainWindow.Width;

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
                    if (Settings.Default.Fullscreen)
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

        internal static void SystemEvents_DisplaySettingsChanged()
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
        internal static void Window_Closing()
        {
            if (!Settings.Default.AutoFitWindow && !Settings.Default.Fullscreen)
            {
                Settings.Default.Top = GetMainWindow.Top;
                Settings.Default.Left = GetMainWindow.Left;
                Settings.Default.Height = GetMainWindow.Height;
                Settings.Default.Width = GetMainWindow.Width;
            }

            Navigation.Pics.Clear(); // Make it cancel task

            GetMainWindow.Hide(); // Make it feel faster

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
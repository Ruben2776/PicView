using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.PicGallery;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.Views.UserControls.Buttons;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static PicView.WPF.UILogic.ConfigureWindows;
using static PicView.WPF.UILogic.HideInterfaceLogic;
using static PicView.WPF.UILogic.Sizing.ScaleImage;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.UILogic.Sizing;

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
        if (SettingsHelper.Settings.WindowProperties.AutoFit)
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
            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                SettingsHelper.Settings.WindowProperties.AutoFit = false;
                SetWindowBehavior();
                SettingsHelper.Settings.WindowProperties.AutoFit = true;
            }

            Fullscreen_Restore(true);
        }
        else
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;
            try
            {
                GetMainWindow.DragMove();
                // Update info for possible new screen, needs more engineering
                // Seems to work
                MonitorInfo = MonitorSize.GetMonitorSize(GetMainWindow);

                SetWindowSize(GetMainWindow);
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception);
                throw;
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

        if (e.LeftButton != MouseButtonState.Pressed)
            return;
        try
        {
            GetMainWindow.DragMove();
            MonitorInfo = MonitorSize.GetMonitorSize(GetMainWindow);

            SetWindowSize(GetMainWindow);
        }
        catch (Exception exception)
        {
            Trace.WriteLine(exception);
        }
    }

    /// <summary>
    /// Function made to restore and drag window from maximized window state
    /// </summary>
    internal static void Restore_From_Move()
    {
        GetMainWindow.WindowState = WindowState.Normal;
        if (GetGripButton is not null)
        {
            GetGripButton.Visibility = Visibility.Visible;
        }

        // Reset margin
        GetMainWindow.TitleBar.Margin = new Thickness(0);
        GetMainWindow.LowerBar.Margin = new Thickness(0);

        SetWindowSize(ConfigureWindows.GetMainWindow);
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

        if (GetMainWindow.MainImage.Source is null && !gotoFullscreen)
        {
            Restore_From_Move();
            return;
        }

        SetWindowSize(ConfigureWindows.GetMainWindow); // Fixes popping up on wrong monitor?
        MonitorInfo = MonitorSize.GetMonitorSize(GetMainWindow);

        if (GetMainWindow.WindowState == WindowState.Maximized || gotoFullscreen)
        {
            // Show fullscreen logic
            RenderFullscreen();

            // Handle if browsing gallery
            if (GalleryFunctions.IsGalleryOpen)
            {
                GalleryToggle.OpenLayout();
                GalleryNavigation.ScrollToGalleryCenter();
            }

            if (ErrorHandling.CheckOutOfRange() is false)
            {
                IsNavigationShown(SettingsHelper.Settings.UIProperties.ShowAltInterfaceButtons);
                if (!SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
                    IsShortcutsShown(SettingsHelper.Settings.UIProperties.ShowAltInterfaceButtons);
            }

            SettingsHelper.Settings.WindowProperties.Fullscreen = true;

            Close_UserControls();
        }
        else
        {
            GetMainWindow.Topmost = SettingsHelper.Settings.WindowProperties.TopMost;
            SettingsHelper.Settings.WindowProperties.Fullscreen = false;

            if (SettingsHelper.Settings.UIProperties.ShowInterface)
            {
                IsNavigationShown(false);
                IsTopAndBottomShown(true);
                IsShortcutsShown(false);
            }
            else
            {
                IsNavigationShown(true);
                IsTopAndBottomShown(false);
                IsShortcutsShown(true);
            }

            // Reset margin from fullscreen
            GetMainWindow.ParentContainer.Margin = new Thickness(0);
            GetMainWindow.TitleBar.Margin = new Thickness(0);
            GetMainWindow.LowerBar.Margin = new Thickness(0);

            SetWindowBehavior();

            if (SettingsHelper.Settings.WindowProperties.AutoFit == false)
            {
                SetLastWindowSize(GetMainWindow);
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
        IsTopAndBottomShown(false);

        GetMainWindow.ResizeMode = ResizeMode.CanMinimize;
        GetMainWindow.SizeToContent = SizeToContent.Manual;
        GetMainWindow.Width = MonitorInfo.WorkArea.Width;
        GetMainWindow.Height = MonitorInfo.WorkArea.Height;

        TryFitImage();

        CenterWindowOnScreen(GetMainWindow);
    }

    /// <summary>
    /// Centers on the current monitor
    /// </summary>
    internal static void CenterWindowOnScreen(Window window, bool horizontal = true)
    {
        window?.Dispatcher.Invoke(() =>
        {
            window.Top =
                ((MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling) - window.ActualHeight) / 2 +
                MonitorInfo.WorkArea.Top;
            if (horizontal)
                window.Left =
                    ((MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling) - window.ActualWidth) / 2 +
                    MonitorInfo.WorkArea.Left;
        });
    }

    internal static void SetLastWindowSize(Window window)
    {
        window?.Dispatcher.Invoke(() =>
        {
            var top = SettingsHelper.Settings.WindowProperties.Top;
            if (top < SystemParameters.VirtualScreenTop)
            {
                top = SystemParameters.VirtualScreenTop;
            }
            if (window.Top + window.Height > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
            {
                top = SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop - window.Height;
            }

            var left = SettingsHelper.Settings.WindowProperties.Left;
            if (left < 0)
            {
                left = MonitorInfo.WorkArea.Left;
            }

            if (left < SystemParameters.VirtualScreenLeft)
            {
                left = SystemParameters.VirtualScreenLeft;
            }

            if (left + window.Width > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth)
            {
                left = SystemParameters.VirtualScreenWidth + SystemParameters.VirtualScreenLeft - window.Width;
            }
            window.Top = top;
            window.Left = left;
            window.Width = double.IsNaN(SettingsHelper.Settings.WindowProperties.Width) ? window.Width :
                double.IsNaN(SettingsHelper.Settings.WindowProperties.Width) ? window.ActualWidth : SettingsHelper.Settings.WindowProperties.Width;
            window.Height = double.IsNaN(SettingsHelper.Settings.WindowProperties.Height) ? window.Height :
                double.IsNaN(SettingsHelper.Settings.WindowProperties.Height) ? window.ActualHeight : SettingsHelper.Settings.WindowProperties.Height;
        });
    }

    internal static void SetWindowSize(Window window)
    {
        if (SettingsHelper.Settings.WindowProperties.AutoFit && SettingsHelper.Settings.WindowProperties.Fullscreen)
            return;

        window?.Dispatcher.Invoke(async () =>
        {
            if (window.WindowState == WindowState.Maximized || SettingsHelper.Settings.WindowProperties.Fullscreen)
                return;

            SettingsHelper.Settings.WindowProperties.Top = window.Top;
            SettingsHelper.Settings.WindowProperties.Left = window.Left;
            SettingsHelper.Settings.WindowProperties.Height = window.ActualHeight;
            SettingsHelper.Settings.WindowProperties.Width = window.ActualWidth;

            await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
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
                GetMainWindow.WindowState = WindowState.Normal;
                Fullscreen_Restore(true);
                return;

            case WindowState.Minimized:
            default:
                return;
        }
    }

    internal static void SystemEvents_DisplaySettingsChanged()
    {
        // Update size when screen resolution changes
        MonitorInfo = MonitorSize.GetMonitorSize(GetMainWindow);
        TryFitImage();
    }

    #endregion Changed Events

    /// <summary>
    /// Save settings when closing
    /// </summary>
    internal static async void Window_Closing()
    {
        GetMainWindow.Hide(); // Make it feel faster

        SetWindowSize(GetMainWindow);

        // Close Extra windows when closing
        GetAboutWindow?.Close();

        GetImageInfoWindow?.Close();

        GetEffectsWindow?.Close();

        GetSettingsWindow?.Close();

        // Set last opened file, determine if URL/archive or file
        if (GetMainWindow.MainImage.Source != null)
        {
            if (Navigation.Pics.Count > 0 && Navigation.FolderIndex < Navigation.Pics.Count)
            {
                SettingsHelper.Settings.StartUp.LastFile = !string.IsNullOrWhiteSpace(ArchiveHelper.TempZipFile) ?
                    ArchiveHelper.TempZipFile : Navigation.Pics[Navigation.FolderIndex];
            }
            else
            {
                var url = GetMainWindow.TitleText.Text.GetURL();
                if (!string.IsNullOrEmpty(url))
                {
                    SettingsHelper.Settings.StartUp.LastFile = url;
                }
            }
        }
        Navigation.Pics?.Clear(); // Make it cancel task

        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
        FileDeletionHelper.DeleteTempFiles();
        FileHistoryNavigation.WriteToFile();
        await CustomKeybindings.UpdateKeyBindingsFile();
        Environment.Exit(0);
    }
}
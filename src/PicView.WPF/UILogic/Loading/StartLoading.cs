﻿using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic.Sizing;
using PicView.WPF.Views.UserControls.Misc;
using PicView.WPF.Views.Windows;
using System.Windows;
using System.Windows.Controls;
using static PicView.WPF.UILogic.Loading.LoadContextMenus;
using static PicView.WPF.UILogic.Loading.LoadControls;
using static PicView.WPF.UILogic.Sizing.WindowSizing;
using static PicView.WPF.UILogic.TransformImage.ZoomLogic;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.UILogic.Loading;

internal static class StartLoading
{
    internal static async Task LoadedEvent(Startup_Window startupWindow)
    {
        GetSpinWaiter = new SpinWaiter();
        startupWindow.TheGrid.Children.Add(GetSpinWaiter);
        Panel.SetZIndex(GetSpinWaiter, 99);

        // Load sizing properties
        MonitorInfo = MonitorSize.GetMonitorSize(startupWindow);
        await SettingsHelper.LoadSettingsAsync().ConfigureAwait(false);
        if (SettingsHelper.Settings.WindowProperties.AutoFit == false && SettingsHelper.Settings.WindowProperties.Fullscreen == false)
        {
            SetLastWindowSize(startupWindow);
        }
        else
        {
            CenterWindowOnScreen(startupWindow);
        }

        var args = Environment.GetCommandLineArgs();
        ImageSizeCalculationHelper.ImageSize? size = null;
        MainWindow? mainWindow = null;
        await startupWindow.Dispatcher.InvokeAsync(() =>
        {
            mainWindow = new MainWindow();
            ConfigureWindows.GetMainWindow = mainWindow;
            GetSpinWaiter = GetSpinWaiter;

            if (SettingsHelper.Settings.WindowProperties.AutoFit == false)
            {
                SetLastWindowSize(mainWindow);
            }

            // Set min size to DPI scaling
            mainWindow.MinWidth *= MonitorInfo.DpiScaling;
            mainWindow.MinHeight *= MonitorInfo.DpiScaling;

            SetWindowBehavior();
            if (SettingsHelper.Settings.WindowProperties.AutoFit == false)
                SetLastWindowSize(ConfigureWindows.GetMainWindow);

            mainWindow.Scroller.VerticalScrollBarVisibility = SettingsHelper.Settings.Zoom.ScrollEnabled
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled;

            if (!SettingsHelper.Settings.UIProperties.ShowInterface)
            {
                mainWindow.TitleBar.Visibility =
                    mainWindow.LowerBar.Visibility
                        = Visibility.Collapsed;
            }
            else if (!SettingsHelper.Settings.UIProperties.ShowBottomNavBar)
            {
                mainWindow.LowerBar.Visibility
                    = Visibility.Collapsed;
            }

            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                ConfigureWindows.GetMainWindow.MainImage.Width = startupWindow.ActualWidth;
                ConfigureWindows.GetMainWindow.MainImage.Height = startupWindow.ActualHeight;
            }
            else
            {
                ConfigureWindows.GetMainWindow.Width = startupWindow.ActualWidth;
                ConfigureWindows.GetMainWindow.Height = startupWindow.ActualHeight;
            }

            ConfigureWindows.GetMainWindow.Show();
            if (SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                if (args.Length < 2)
                {
                    SettingsHelper.Settings.WindowProperties.Fullscreen = false;
                }
                else
                {
                    Fullscreen_Restore(true);
                }
            }

            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                ScaleImage.TryFitImage();
            }

            startupWindow.TheGrid.Children.Remove(GetSpinWaiter);
            mainWindow.ParentContainer.Children.Add(GetSpinWaiter);
        });

        var language = SettingsHelper.Settings.UIProperties.UserLanguage;
        await Core.Localization.TranslationHelper.LoadLanguage(language).ConfigureAwait(false);
        Navigation.Pics = new List<string>();

        if (args.Length > 1)
        {
            await QuickLoad.QuickLoadAsync(args[1]).ConfigureAwait(false);
        }
        else
        {
            if (SettingsHelper.Settings.StartUp.OpenLastFile)
            {
                await FileHistoryNavigation.OpenLastFileAsync().ConfigureAwait(false);
            }
            else if (SettingsHelper.Settings.StartUp.OpenSpecificFile)
            {
                await LoadPic.LoadPicFromStringAsync(SettingsHelper.Settings.StartUp.OpenSpecificString).ConfigureAwait(false);
            }
            else
            {
                ErrorHandling.Unload(true);
            }
        }

        await mainWindow.Dispatcher.InvokeAsync(startupWindow.Close);

        await CustomKeybindings.LoadKeybindings().ConfigureAwait(false);

        ConfigColors.UpdateColor();
    }

    internal static void AddDictionaries()
    {
        // Add dictionaries
        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Themes/Styles/Menu.xaml", UriKind.Relative)
            }
        );

        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Themes/Styles/ToolTip.xaml", UriKind.Relative)
            }
        );

        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Themes/Styles/Slider.xaml", UriKind.Relative)
            }
        );

        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Views/Resources/MainContextMenu.xaml", UriKind.Relative)
            }
        );

        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Views/Resources/WindowContextMenu.xaml", UriKind.Relative)
            }
        );

        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Views/Resources/NavigationContextMenu.xaml", UriKind.Relative)
            }
        );

        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Views/Resources/Icons.xaml", UriKind.Relative)
            }
        );

        Application.Current.Resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Views/Resources/GalleryContextMenu.xaml", UriKind.Relative)
            }
        );
    }

    internal static void AddUiElementsAndUpdateValues()
    {
        // Update values
        ConfigureWindows.GetMainWindow.FolderButton.BackgroundEvents();
        ConfigureWindows.GetMainWindow.AllowDrop = true;
        ConfigColors.SetColors();

        AddDictionaries();
        LoadClickArrow(true);
        LoadClickArrow(false);
        Loadx2();
        LoadMinus();
        LoadRestoreButton();
        LoadGalleryShortcut();

        // Update WindowStyle
        if (!SettingsHelper.Settings.UIProperties.ShowInterface)
        {
            GetClickArrowLeft.Opacity =
                GetClickArrowRight.Opacity =
                    GetX2.Opacity =
                        GetMinus.Opacity =
                            GetRestoreButton.Opacity =
                                GetGalleryShortcut.Opacity =
                                    0;

            GetClickArrowLeft.Visibility =
                GetClickArrowRight.Visibility =
                    GetX2.Visibility =
                        GetMinus.Visibility =
                            GetGalleryShortcut.Visibility =
                                GetRestoreButton.Visibility =
                                    Visibility.Visible;
        }
        else if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            GetClickArrowLeft.Opacity =
                GetClickArrowRight.Opacity =
                    GetX2.Opacity =
                        GetMinus.Opacity =
                            GetRestoreButton.Opacity =
                                    1;

            GetClickArrowLeft.Visibility =
                GetClickArrowRight.Visibility =
                    GetX2.Visibility =
                        GetMinus.Visibility =
                                GetRestoreButton.Visibility =
                                    Visibility.Visible;

            if (!SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                GetGalleryShortcut.Opacity = 1;
                GetGalleryShortcut.Visibility = Visibility.Visible;
            }
        }

        // Add UserControls :)
        LoadFileMenu();
        LoadImageSettingsMenu();
        LoadQuickSettingsMenu();
        LoadToolsAndEffectsMenu();
        LoadTooltipStyle();

        // Initialize things!
        InitializeZoom();

        // Add things!
        Timers.AddTimers();
        AddContextMenus();
    }
}
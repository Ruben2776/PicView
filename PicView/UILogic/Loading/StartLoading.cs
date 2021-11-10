using PicView.PicGallery;
using PicView.SystemIntegration;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static PicView.ChangeImage.Error_Handling;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Loading.LoadContextMenus;
using static PicView.UILogic.Loading.LoadControls;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.TransformImage.ZoomLogic;
using static PicView.UILogic.UC;

namespace PicView.UILogic.Loading
{
    internal static class StartLoading
    {
        internal static void LoadedEvent()
        {
            FreshStartup = true;
            Pics = new List<string>();

            // Load sizing properties
            MonitorInfo = MonitorSize.GetMonitorSize();

            // Set min size to DPI scaling
            ConfigureWindows.GetMainWindow.MinWidth *= MonitorInfo.DpiScaling;
            ConfigureWindows.GetMainWindow.MinHeight *= MonitorInfo.DpiScaling;

            if (Properties.Settings.Default.AutoFitWindow == false)
            {
                SetWindowBehavior();
            }

            ConfigureWindows.GetMainWindow.Scroller.VerticalScrollBarVisibility = Properties.Settings.Default.ScrollEnabled ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;

            // Set min size to DPI scaling
            ConfigureWindows.GetMainWindow.MinWidth *= MonitorInfo.DpiScaling;
            ConfigureWindows.GetMainWindow.MinHeight *= MonitorInfo.DpiScaling;

            if (!Properties.Settings.Default.ShowInterface)
            {
                ConfigureWindows.GetMainWindow.TitleBar.Visibility =
                   ConfigureWindows.GetMainWindow.LowerBar.Visibility
                   = Visibility.Collapsed;
            }
        }

        internal static void ContentRenderedEvent()
        {
            // Load image if possible
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
            {
                // Determine proper startup size
                if (Properties.Settings.Default.AutoFitWindow == false && Properties.Settings.Default.Width != 0)
                {
                    SetLastWindowSize();
                }
                else if (Properties.Settings.Default.AutoFitWindow)
                {
                    SetWindowBehavior();
                }

                Unload(true); // Load clean setup when starting up without arguments
            }
            else
            {
                if (Properties.Settings.Default.StartInFullscreenGallery)
                {
                    _ = GalleryToggle.OpenFullscreenGalleryAsync(true).ConfigureAwait(false);
                }

                // Determine prefered UI for startup
                if (Properties.Settings.Default.Fullscreen)
                {
                    Sizing.WindowSizing.Fullscreen_Restore(true);
                }

                else if (Properties.Settings.Default.Width > 0 && Properties.Settings.Default.AutoFitWindow == false)
                {
                    SetLastWindowSize();
                }
                else
                {
                    UILogic.Sizing.WindowSizing.SetWindowBehavior();
                }

                _ = ChangeImage.LoadPic.QuickLoadAsync(args[1]).ConfigureAwait(false);
                // TODO maybe load extra images if multiple arguments
            }

            ConfigureSettings.ConfigColors.UpdateColor();

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
                    Source = new Uri(@"/PicView;component/Views/UserControls/Misc/MainContextMenu.xaml", UriKind.Relative)
                }
            );

            // Load UI and events
            AddUIElementsAndUpdateValues();
        }

        private static void AddUIElementsAndUpdateValues()
        {
            // Update values
            ConfigureSettings.ConfigColors.SetColors();
            ConfigureWindows.GetMainWindow.AllowDrop = true;

            LoadClickArrow(true);
            LoadClickArrow(false);
            Loadx2();
            LoadMinus();
            LoadRestoreButton();
            LoadGalleryShortcut();

            // Update WindowStyle
            if (!Properties.Settings.Default.ShowInterface)
            {
                GetClickArrowLeft.Opacity =
                GetClickArrowRight.Opacity =
                Getx2.Opacity =
                GetMinus.Opacity =
                GetRestorebutton.Opacity =
                GetGalleryShortcut.Opacity =
                0;

                GetClickArrowLeft.Visibility =
                GetClickArrowRight.Visibility =
                Getx2.Visibility =
                GetMinus.Visibility =
                GetGalleryShortcut.Visibility =
                GetRestorebutton.Visibility =
                Visibility.Visible;
            }
            else if (Properties.Settings.Default.Fullscreen)
            {
                GetClickArrowLeft.Opacity =
                GetClickArrowRight.Opacity =
                Getx2.Opacity =
                GetMinus.Opacity =
                GetRestorebutton.Opacity =
                GetGalleryShortcut.Opacity =
                1;

                GetClickArrowLeft.Visibility =
                GetClickArrowRight.Visibility =
                Getx2.Visibility =
                GetMinus.Visibility =
                GetGalleryShortcut.Visibility =
                GetRestorebutton.Visibility =
                Visibility.Visible;
            }

            // Add UserControls :)
            LoadFileMenu();
            LoadImageSettingsMenu();
            LoadQuickSettingsMenu();
            LoadToolsAndEffectsMenu();
            LoadAutoScrollSign();
            LoadTooltipStyle();

            // Initilize Things!
            InitializeZoom();
            ChangeImage.History.InstantiateQ();

            // Add things!
            Timers.AddTimers();
            AddContextMenus();
        }
    }
}
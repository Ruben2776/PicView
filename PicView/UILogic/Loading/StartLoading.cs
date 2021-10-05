using PicView.FileHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.Translations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
        internal static async Task LoadedEventsAsync()
        {
#if DEBUG
            Trace.Listeners.Add(new TextWriterTraceListener("Debug.log"));
            Trace.Unindent();
            Trace.WriteLine(SetTitle.AppName + " started at " + DateTime.Now);
#endif
            // Subscribe to Windows resized event || Need to be exactly on load
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(ConfigureWindows.GetMainWindow).Handle);
            source.AddHook(new HwndSourceHook(NativeMethods.WndProc));

            LoadLanguage.DetermineLanguage();

            FreshStartup = true;
            Pics = new List<string>();

            // Load sizing properties
            MonitorInfo = MonitorSize.GetMonitorSize();
            SetWindowBehavior();
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

            // Load image if possible
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
            {
                Unload(); // Load clean setup when starting up without arguments

                // Reset PicGallery and don't allow it to run,
                // if only 1 image
                Properties.Settings.Default.FullscreenGalleryHorizontal = false;

                // Don't start it in fullscreen with no image
                Properties.Settings.Default.Fullscreen = false;

                // Determine proper startup size
                if (Properties.Settings.Default.AutoFitWindow == false && Properties.Settings.Default.Width > 0)
                {
                    UILogic.Sizing.WindowSizing.SetLastWindowSize();
                }
                else
                {
                    ConfigureWindows.GetMainWindow.Width = ConfigureWindows.GetMainWindow.MinWidth;
                    ConfigureWindows.GetMainWindow.Height = ConfigureWindows.GetMainWindow.MinHeight;
                    UILogic.Sizing.WindowSizing.CenterWindowOnScreen();
                }
                return;
            }
            else
            {
                // Determine prefered UI for startup
                if (Properties.Settings.Default.Fullscreen)
                {
                    UILogic.Sizing.WindowSizing.Fullscreen_Restore(true);
                }
                else if (Properties.Settings.Default.StartInFullscreenGallery)
                {
                    await GalleryToggle.OpenFullscreenGalleryAsync(Properties.Settings.Default.FullscreenGalleryHorizontal, true).ConfigureAwait(false);
                    Timers.PicGalleryTimerHack();
                }
                else if (Properties.Settings.Default.AutoFitWindow == false && Properties.Settings.Default.Width > 0)
                {
                    UILogic.Sizing.WindowSizing.SetLastWindowSize();
                }

                await QuickLoad(args[1]).ConfigureAwait(false);
            }
        }

        internal static void ContentRenderedEvent()
        {
#if DEBUG
            Trace.WriteLine("ContentRendered started");
#endif
            #region Add dictionaries

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

            #endregion Add dictionaries

            ConfigureSettings.ConfigColors.UpdateColor();
            ConfigureWindows.GetMainWindow.Topmost = Properties.Settings.Default.TopMost;

            // Load UI and events
            AddUIElementsAndUpdateValues();

#if DEBUG
            Trace.WriteLine("Start Completed ");
#endif
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

            Eventshandling.SetMainWindowEvents();

            // Initilize Things!
            RecentFiles.Initialize();
            InitializeZoom();

            // Add things!
            Timers.AddTimers();
            AddContextMenus();

            // Updates settings from older version to newer version
            if (Properties.Settings.Default.CallUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.CallUpgrade = false;
            }
        }
    }
}
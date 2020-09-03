using PicView.FileHandling;
using PicView.SystemIntegration;
using PicView.UILogic.PicGallery;
using PicView.UILogic.Sizing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using static PicView.ChangeImage.Error_Handling;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Loading.LoadContextMenus;
using static PicView.UILogic.Loading.LoadControls;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.TransformImage.Scroll;
using static PicView.UILogic.TransformImage.ZoomLogic;
using static PicView.UILogic.UC;

namespace PicView.UILogic.Loading
{
    internal static class StartLoading
    {
        internal static void PreStart()
        {
#if DEBUG
            Trace.Listeners.Add(new TextWriterTraceListener("Debug.log"));
            Trace.Unindent();
            Trace.WriteLine(SetTitle.AppName + " started at " + DateTime.Now);
#endif
            // theese two line have to be exactly onload
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(ConfigureWindows.GetMainWindow).Handle);
            source.AddHook(new HwndSourceHook(NativeMethods.WndProc));

            FreshStartup = true;

            if (!Properties.Settings.Default.ShowInterface)
            {
                ConfigureWindows.GetMainWindow.TitleBar.Visibility =
                ConfigureWindows.GetMainWindow.LowerBar.Visibility 
                = Visibility.Collapsed;
            }

            if (Properties.Settings.Default.UserLanguage != "en")
            {
                try
                {
                    Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                    {
                        Source = new Uri(@"/PicView;component/Translations/" + Properties.Settings.Default.UserLanguage + ".xaml", UriKind.Relative)
                    };
                }
                catch (Exception)
                {
                    Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                    {
                        Source = new Uri(@"/PicView;component/Translations/en.xaml", UriKind.Relative)
                    };
                }
            }

            Pics = new List<string>();

            // Load image if possible
            var arg = Application.Current.Properties["ArbitraryArgName"];
            if (arg == null)
            {
                Unload();

                // Reset PicGallery and don't allow it to run,
                // if only 1 image
                Properties.Settings.Default.PicGallery = 1;

                // Don't start it in fullscreen with no image
                Properties.Settings.Default.Fullscreen = false;

                // Determine proper startup size
                if (Properties.Settings.Default.Width != 0)
                {
                    SetLastWindowSize();
                }
                else
                {
                    ConfigureWindows.GetMainWindow.Width = 750 * MonitorInfo.DpiScaling;
                    ConfigureWindows.GetMainWindow.MinHeight = 700 * MonitorInfo.DpiScaling;
                }
            }
            else if (File.Exists(arg.ToString()))
            {
                var file = arg.ToString();
                // Determine prefered UI for startup
                if (Properties.Settings.Default.Fullscreen)
                {
                    ConfigureWindows.Fullscreen_Restore(true);
                }
                else if (Properties.Settings.Default.PicGallery == 2)
                {
                    GalleryToggle.OpenFullscreenGallery(true);
                }
                else if (Properties.Settings.Default.Width != 0)
                {
                    SetLastWindowSize();
                }

                Pic(file);
            }
            else
            {
                // Set file associations
            }

            // Load sizing properties
            MonitorInfo = MonitorSize.GetMonitorSize();
            AutoFitWindow = Properties.Settings.Default.AutoFitWindow;
            IsScrollEnabled = Properties.Settings.Default.ScrollEnabled;

            // Set min size to DPI scaling
            ConfigureWindows.GetMainWindow.MinWidth *= MonitorInfo.DpiScaling;
            ConfigureWindows.GetMainWindow.MinHeight *= MonitorInfo.DpiScaling;
            ConfigureSettings.ConfigColors.UpdateColor();
        }

        internal static void Start()
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

            ConfigureWindows.GetMainWindow.Topmost = Properties.Settings.Default.TopMost;

            // Load UI and events
            AddUIElementsAndUpdateValues();

#if DEBUG
            Trace.WriteLine("Start Completed ");
#endif

            if (Properties.Settings.Default.AutoFitWindow)
            {
                ConfigureWindows.CenterWindowOnScreen();
            }
        }

        private static void SetLastWindowSize()
        {
            ConfigureWindows.GetMainWindow.Top = Properties.Settings.Default.Top;
            ConfigureWindows.GetMainWindow.Left = Properties.Settings.Default.Left;
            ConfigureWindows.GetMainWindow.Width = Properties.Settings.Default.Width;
            ConfigureWindows.GetMainWindow.Height = Properties.Settings.Default.Height;
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
            LoadTooltipStyle();
            LoadFileMenu();
            LoadImageSettingsMenu();
            LoadQuickSettingsMenu();
            LoadToolsAndEffectsMenu();
            LoadAutoScrollSign();

            Eventshandling.Go();

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
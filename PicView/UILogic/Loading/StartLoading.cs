using PicView.FileHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.Translations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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
        internal static async Task LoadedEvemtAsync()
        {
#if DEBUG
            Trace.Listeners.Add(new TextWriterTraceListener("Debug.log"));
            Trace.Unindent();
            Trace.WriteLine(SetTitle.AppName + " started at " + DateTime.Now);
#endif
            // theese two line have to be exactly onload
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(ConfigureWindows.GetMainWindow).Handle);
            source.AddHook(new HwndSourceHook(NativeMethods.WndProc));

            LoadLanguage.DetermineLanguage();

            FreshStartup = true;
            Pics = new List<string>();

            // Load sizing properties
            MonitorInfo = MonitorSize.GetMonitorSize();
            AutoFitWindow();
            await SetScrollBehaviour(Properties.Settings.Default.ScrollEnabled).ConfigureAwait(false);

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                // Set min size to DPI scaling
                ConfigureWindows.GetMainWindow.MinWidth *= MonitorInfo.DpiScaling;
                ConfigureWindows.GetMainWindow.MinHeight *= MonitorInfo.DpiScaling;
            });

            

            if (!Properties.Settings.Default.ShowInterface)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
                {
                    ConfigureWindows.GetMainWindow.TitleBar.Visibility =
                       ConfigureWindows.GetMainWindow.LowerBar.Visibility
                       = Visibility.Collapsed;
                });

            }

            // Load image if possible
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                {
                    Unload(); // Load clean setup when starting up without arguments

                    // Reset PicGallery and don't allow it to run,
                    // if only 1 image
                    Properties.Settings.Default.FullscreenGallery = false;

                    // Don't start it in fullscreen with no image
                    Properties.Settings.Default.Fullscreen = false;

                    // Determine proper startup size
                    if (Properties.Settings.Default.AutoFitWindow == false && Properties.Settings.Default.Width > 0)
                    {
                        SetLastWindowSize();
                    }
                    else
                    {
                        ConfigureWindows.GetMainWindow.Width = ConfigureWindows.GetMainWindow.MinWidth;
                        ConfigureWindows.GetMainWindow.Height = ConfigureWindows.GetMainWindow.MinHeight;
                        ConfigureWindows.CenterWindowOnScreen();
                    }
                });

            }
            else
            {
                // Determine prefered UI for startup
                if (Properties.Settings.Default.Fullscreen)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                    {
                        ConfigureWindows.Fullscreen_Restore(true);
                    });
                }
                else if (Properties.Settings.Default.FullscreenGallery)
                {
                    await GalleryToggle.OpenFullscreenGalleryAsync(true).ConfigureAwait(false);
                    Timers.PicGalleryTimerHack();

                }
                else if (Properties.Settings.Default.Width > 0 && Properties.Settings.Default.AutoFitWindow == false)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                    {
                        SetLastWindowSize();
                    });
                }

                await LoadPicFromString(args[1]).ConfigureAwait(false);
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

            if (Properties.Settings.Default.AutoFitWindow && !Properties.Settings.Default.Fullscreen)
            {
                ConfigureWindows.CenterWindowOnScreen();
                ConfigureWindows.GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
            }

#if DEBUG
            Trace.WriteLine("Start Completed ");
#endif
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
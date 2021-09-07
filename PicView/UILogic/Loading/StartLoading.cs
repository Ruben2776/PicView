using PicView.FileHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.Translations;
using PicView.UILogic.Sizing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

            if (!Properties.Settings.Default.ShowInterface)
            {
                ConfigureWindows.GetMainWindow.TitleBar.Visibility =
                ConfigureWindows.GetMainWindow.LowerBar.Visibility
                = Visibility.Collapsed;
            }

            FreshStartup = true;
            Pics = new List<string>();

            // Load sizing properties
            MonitorInfo = MonitorSize.GetMonitorSize();
            AutoFitWindow = Properties.Settings.Default.AutoFitWindow;
            IsScrollEnabled = Properties.Settings.Default.ScrollEnabled;

            // Set min size to DPI scaling
            ConfigureWindows.GetMainWindow.MinWidth *= MonitorInfo.DpiScaling;
            ConfigureWindows.GetMainWindow.MinHeight *= MonitorInfo.DpiScaling;

            // Load image if possible
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
            {
                Unload();

                // Reset PicGallery and don't allow it to run,
                // if only 1 image
                Properties.Settings.Default.FullscreenGallery = false;

                // Don't start it in fullscreen with no image
                Properties.Settings.Default.Fullscreen = false;

                // Determine proper startup size
                if (Properties.Settings.Default.Width != 0)
                {
                    SetLastWindowSize();
                }
                else
                {
                    ConfigureWindows.GetMainWindow.Width = ConfigureWindows.GetMainWindow.MinWidth;
                    ConfigureWindows.GetMainWindow.Height = ConfigureWindows.GetMainWindow.MinHeight;
                }
            }
            else if (args[1].StartsWith('.'))
            {
                // Set file associations

                var process = Process.GetCurrentProcess();

                if (args[1] == ".remove")
                {
                    var removestring = ".jpeg,.jpe,.jpg,.png,.bmp,.ico,.gif,.webp,.jfif,.tiff,.wbmp,.psd,.psb,.svg,.3fr,.arw,.cr2,.crw,.dcr,.dng,.erf,.kdc,.mef,.mdc,.mos,.mrw,.nef,.nrw,.orf,.pef,.raf,.raw,.rw2,.srf,.x3f,.cut,.exr,.emf,.dds,.dib,.hdr,.heic,.tga,.pcx,.pgm,.wmf,.wpg,.xbm,.xcf.xpm";
                    var rmArgs = removestring.Split(',');
                    foreach (var item in rmArgs)
                    {
                        NativeMethods.DeleteAssociation(item, process.Id.ToString(CultureInfo.InvariantCulture), process.MainModule.FileName);
                    }
                }
                else
                {
                    var exts = args[1].Split(',');

                    foreach (var item in args)
                    {
                        NativeMethods.SetAssociation(item, process.Id.ToString(CultureInfo.InvariantCulture));
                    }
                }

                Environment.Exit(0);
            }
            else
            {
                // Determine prefered UI for startup
                if (Properties.Settings.Default.Fullscreen)
                {
                    ConfigureWindows.Fullscreen_Restore(true);
                }
                else if (Properties.Settings.Default.FullscreenGallery)
                {
                    await GalleryToggle.OpenFullscreenGalleryAsync(true).ConfigureAwait(true); // await needs to be true, or it won't load
                }
                else if (AutoFitWindow)
                {
                    ScaleImage.TryFitImage(args[1]);
                }
                else if (Properties.Settings.Default.Width != 0)
                {
                    SetLastWindowSize();
                }

                await LoadPiFrom(args[1]).ConfigureAwait(false);
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
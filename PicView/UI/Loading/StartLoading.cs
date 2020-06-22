using PicView.FileHandling;
using PicView.SystemIntegration;
using PicView.UI.PicGallery;
using PicView.UI.Sizing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using static PicView.ChangeImage.Error_Handling;
using static PicView.ChangeImage.Navigation;
using static PicView.Library.Fields;
using static PicView.Library.Resources.Timers;
using static PicView.UI.Loading.LoadContextMenus;
using static PicView.UI.Loading.LoadControls;
using static PicView.UI.Sizing.WindowLogic;
using static PicView.UI.TransformImage.Scroll;
using static PicView.UI.TransformImage.ZoomLogic;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.Loading
{
    internal static class StartLoading
    {
        internal static void PreStart()
        {
#if DEBUG
            Trace.Listeners.Add(new TextWriterTraceListener("Debug.log"));
            Trace.Unindent();
            Trace.WriteLine(AppName + " started at " + DateTime.Now);
#endif
            // theese two line have to be exactly onload
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(mainWindow).Handle);
            source.AddHook(new HwndSourceHook(NativeMethods.WndProc));

            FreshStartup = true;

            if (!Properties.Settings.Default.ShowInterface)
            {
                mainWindow.TitleBar.Visibility =
                mainWindow.LowerBar.Visibility =
                mainWindow.LeftBorderRectangle.Visibility =
                mainWindow.RightBorderRectangle.Visibility
                = Visibility.Collapsed;
            }
        }

        internal static void Start()
        {
#if DEBUG
            Trace.WriteLine("ContentRendered started");
#endif
            MonitorInfo = MonitorSize.GetMonitorSize();
            AutoFitWindow = Properties.Settings.Default.AutoFitWindow;

            Pics = new List<string>();

            // Load image if possible
            var arg = Application.Current.Properties["ArbitraryArgName"];
            if (arg == null)
            {
                Unload();
                if (Properties.Settings.Default.Maximized)
                {
                    Maximize();
                }
                else
                {
                    ConfigColors.UpdateColor();
                    SetDefaultSize();
                }
            }
            else
            {
                if (Properties.Settings.Default.Fullscreen)
                {
                    Fullscreen_Restore(true);
                }
                else if (Properties.Settings.Default.Maximized)
                {
                    Maximize();
                }
                else
                {
                    ConfigColors.UpdateColor();

                    if (AutoFitWindow)
                    {
                        if (!ScaleImage.TryFitImage(arg.ToString()))
                        {
                            SetDefaultSize();
                        }
                    }
                    else
                    {
                        SetDefaultSize();
                    }
                }

                Pic(arg.ToString());
            }

            AddUIElementsAndUpdateValues();

#if DEBUG
            Trace.WriteLine("Start Completed ");
#endif
        }

        private static void SetDefaultSize()
        {
            // If normal window style
            if (!AutoFitWindow)
            {
                if (Properties.Settings.Default.Width != 0)
                {
                    mainWindow.Top = Properties.Settings.Default.Top;
                    mainWindow.Left = Properties.Settings.Default.Left;
                    mainWindow.Width = Properties.Settings.Default.Width;
                    mainWindow.Height = Properties.Settings.Default.Height;
                }
                else
                {
                    // Execute logic for first time startup
                    mainWindow.Width = 815;
                    mainWindow.Height = 970;
                    CenterWindowOnScreen();
                }
            }
            else
            {
                mainWindow.img.Width = 815;
                mainWindow.img.Height = 970;
            }
        }

        private static void AddUIElementsAndUpdateValues()
        {
            // Update values
            ConfigColors.SetColors();
            mainWindow.AllowDrop = true;
            IsScrollEnabled = Properties.Settings.Default.ScrollEnabled;

            LoadClickArrow(true);
            LoadClickArrow(false);
            Loadx2();
            LoadMinus();
            LoadGalleryShortcut();

            // Update WindowStyle
            if (!Properties.Settings.Default.ShowInterface)
            {
                clickArrowLeft.Opacity =
                clickArrowRight.Opacity =
                x2.Opacity =
                minus.Opacity =
                galleryShortcut.Opacity =
                0;

                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                minus.Visibility =
                galleryShortcut.Visibility =
                Visibility.Visible;
            }

            // Load PicGallery, if needed
            if (Properties.Settings.Default.PicGallery > 0)
            {
                picGallery = new UserControls.PicGallery
                {
                    Opacity = 0,
                    Visibility = Visibility.Collapsed
                };

                mainWindow.bg.Children.Add(picGallery);
                Panel.SetZIndex(picGallery, 999);

                if (Properties.Settings.Default.PicGallery == 2)
                {
                    GalleryToggle.OpenFullscreenGallery();
                }
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
            AddTimers();
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
using PicView.UserControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using static PicView.AjaxLoader;
using static PicView.ContextMenus;
using static PicView.Error_Handling;
using static PicView.Fields;
using static PicView.LoadControls;
using static PicView.Navigation;
using static PicView.Resize_and_Zoom;
using static PicView.Scroll;
using static PicView.Timers;
using static PicView.Utilities;
using static PicView.WindowLogic;
using static PicView.UC;

namespace PicView
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
            // this two line have to be exactly onload
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

            ajaxLoading = new AjaxLoading
            {
                Opacity = 0
            };
            mainWindow.bg.Children.Add(ajaxLoading);
            AjaxLoadingStart();
        }

        internal static void Start()
        {

#if DEBUG
            Trace.WriteLine("ContentRendered started");
#endif
            MonitorInfo = MonitorSize.GetMonitorSize();
            AutoFit = Properties.Settings.Default.AutoFit;

            // If normal window style
            if (!AutoFit)
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
                    mainWindow.Height = 1015;
                    CenterWindowOnScreen();
                }
            }

            Pics = new List<string>();

            // Load image if possible
            if (Application.Current.Properties["ArbitraryArgName"] == null)
            {
                Unload();
                if (Properties.Settings.Default.Maximized)
                {
                    Maximize();
                }
                else
                {
                    UpdateColor();
                }
            }
            else
            {
                Pic(Application.Current.Properties["ArbitraryArgName"].ToString());

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
                    UpdateColor();
                }
            }

            // Update values
            SetColors();
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
                picGallery = new PicGallery
                {
                    Opacity = 0,
                    Visibility = Visibility.Collapsed
                };

                mainWindow.bg.Children.Add(picGallery);
                Panel.SetZIndex(picGallery, 999);

                if (Properties.Settings.Default.PicGallery == 2 && FreshStartup)
                {
                    ToggleGallery.Toggle();
                }
            }

            // Add UserControls :)
            LoadTooltipStyle();
            LoadFileMenu();
            LoadImageSettingsMenu();
            LoadQuickSettingsMenu();
            LoadToolsAndEffectsMenu();
            LoadAutoScrollSign();

            // Initilize Things!
            RecentFiles.Initialize();
            InitializeZoom();

            // Add things!
            Eventshandling.Go();
            AddTimers();
            AddContextMenus();

            // Updates settings from older version to newer version
            if (Properties.Settings.Default.CallUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.CallUpgrade = false;
            }
            AjaxLoadingEnd();

#if DEBUG
            Trace.WriteLine("Start Completed ");
#endif
        }
    }
}

using PicView.FileHandling;
using PicView.SystemIntegration;
using PicView.UILogic.PicGallery;
using PicView.UILogic.Sizing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using static PicView.ChangeImage.Error_Handling;
using static PicView.ChangeImage.Navigation;
using static PicView.Library.Fields;
using static PicView.Library.Resources.Timers;
using static PicView.UILogic.Loading.LoadContextMenus;
using static PicView.UILogic.Loading.LoadControls;
using static PicView.UILogic.Sizing.WindowLogic;
using static PicView.UILogic.TransformImage.Scroll;
using static PicView.UILogic.TransformImage.ZoomLogic;
using static PicView.UILogic.UserControls.UC;

namespace PicView.UILogic.Loading
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
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(TheMainWindow).Handle);
            source.AddHook(new HwndSourceHook(NativeMethods.WndProc));

            FreshStartup = true;

            if (!Properties.Settings.Default.ShowInterface)
            {
                TheMainWindow.TitleBar.Visibility =
                TheMainWindow.LowerBar.Visibility =
                TheMainWindow.LeftBorderRectangle.Visibility =
                TheMainWindow.RightBorderRectangle.Visibility
                = Visibility.Collapsed;
            }

            // Set user language
            Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
            {
                Source = new Uri(@"/PicView;component/Translations/en-US.xaml", UriKind.Relative)
            };
        }

        internal static void Start()
        {
#if DEBUG
            Trace.WriteLine("ContentRendered started");
#endif
            ConfigColors.UpdateColor();

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

            MonitorInfo = MonitorSize.GetMonitorSize();
            AutoFitWindow = Properties.Settings.Default.AutoFitWindow;

            Pics = new List<string>();

            // Load image if possible
            var arg = Application.Current.Properties["ArbitraryArgName"];
            if (arg == null)
            {
                Unload();
            }
            else
            {
                if (!AutoFitWindow)
                {
                    SetDefaultSize();
                }
                else if (!ScaleImage.TryFitImage(arg.ToString()))
                {
                    SetDefaultSize();
                }

                Pic(arg.ToString());
            }

            // Load UI and events
            AddUIElementsAndUpdateValues();

            // Change into prefered UI, if needed.
            if (Properties.Settings.Default.Fullscreen)
            {
                if (arg == null)
                {
                    // Don't start it in fullscreen with no image
                    Properties.Settings.Default.Fullscreen = false;
                }
                else
                {
                    Fullscreen_Restore(true);
                }
            }
            else if (Properties.Settings.Default.Maximized)
            {
                Maximize();
            }
            // Load PicGallery, if needed
            else if (Properties.Settings.Default.PicGallery == 2)
            {
                if (arg == null)
                {
                    // Reset PicGallery and don't allow it to run,
                    // if only 1 image
                    Properties.Settings.Default.PicGallery = 1;
                }
                else
                {
                    GalleryToggle.OpenFullscreenGallery();
                }
            }

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
                    TheMainWindow.Top = Properties.Settings.Default.Top;
                    TheMainWindow.Left = Properties.Settings.Default.Left;
                    TheMainWindow.Width = Properties.Settings.Default.Width;
                    TheMainWindow.Height = Properties.Settings.Default.Height;
                }
            }
        }

        private static void AddUIElementsAndUpdateValues()
        {
            // Update values
            ConfigColors.SetColors();
            TheMainWindow.AllowDrop = true;
            IsScrollEnabled = Properties.Settings.Default.ScrollEnabled;

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
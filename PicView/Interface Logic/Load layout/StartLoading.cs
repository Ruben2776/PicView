using PicView.UserControls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.AjaxLoader;
using static PicView.ContextMenus;
using static PicView.Error_Handling;
using static PicView.Fields;
using static PicView.Utilities;
using static PicView.LoadControls;
using static PicView.Navigation;
using static PicView.Resize_and_Zoom;
using static PicView.Scroll;
using static PicView.Timers;
using static PicView.WindowLogic;

namespace PicView
{
    internal static class StartLoading
    {
        internal static void PreStart()
        {
            freshStartup = true;

            if (!Properties.Settings.Default.ShowInterface)
            {
                mainWindow.TitleBar.Visibility =
                mainWindow.LowerBar.Visibility =
                mainWindow.LeftBorderRectangle.Visibility =
                mainWindow.RightBorderRectangle.Visibility
                = Visibility.Collapsed;
            }
            // If normal window style
            if (!Properties.Settings.Default.FitToWindow)
            {
                mainWindow.Top = Properties.Settings.Default.Top;
                mainWindow.Left = Properties.Settings.Default.Left;
                mainWindow.Height = Properties.Settings.Default.Height;
                mainWindow.Width = Properties.Settings.Default.Width;

                if (Properties.Settings.Default.Maximized)
                {
                    mainWindow.WindowState = WindowState.Maximized;
                }

                FitToWindow = false;
            }
            else
            {
                FitToWindow = true;
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
            // Update values
            mainWindow.AllowDrop = true;
            IsScrollEnabled = Properties.Settings.Default.ScrollEnabled;
            Pics = new List<string>();
            //DataContext = this;
            MonitorInfo = MonitorSize.GetMonitorSize();

            // Load image if possible
            if (Application.Current.Properties["ArbitraryArgName"] == null)
            {
                Unload();
                UpdateColor();
            }
            else
            {
                Pic(Application.Current.Properties["ArbitraryArgName"].ToString());

                if (Properties.Settings.Default.Fullscreen)
                {
                    Fullscreen_Restore(true);
                }
                else
                {
                    UpdateColor();
                }
            }

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

            mainColor = (Color)Application.Current.Resources["MainColor"];
            if (!Properties.Settings.Default.BgColorWhite)
            {
                mainWindow.imgBorder.Background = new SolidColorBrush(Colors.Transparent);
            }

            backgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorAlt"];

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

                if (Properties.Settings.Default.PicGallery == 2 && freshStartup)
                {
                    ToggleGallery.Toggle();
                }
            }

            // Add UserControls :)
            LoadTooltipStyle();
            LoadFileMenu();
            LoadImageSettingsMenu();
            LoadQuickSettingsMenu();
            LoadFunctionsMenu();
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
        }
    }
}

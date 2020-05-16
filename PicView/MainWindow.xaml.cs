using PicView.UserControls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.AjaxLoader;
using static PicView.ContextMenus;
using static PicView.Error_Handling;
using static PicView.Fields;
using static PicView.Helper;
using static PicView.LoadControls;
using static PicView.Navigation;
using static PicView.Resize_and_Zoom;
using static PicView.Scroll;
using static PicView.Timers;
using static PicView.WindowLogic;

namespace PicView
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => MainWindow_Loaded(s, e);
            ContentRendered += MainWindow_ContentRendered;
        }

        #region Loaded and Rendered

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            freshStartup = true;

            if (!Properties.Settings.Default.ShowInterface)
            {
                TitleBar.Visibility =
                LowerBar.Visibility =
                LeftBorderRectangle.Visibility =
                RightBorderRectangle.Visibility
                = Visibility.Collapsed;
            }
            // If normal window style
            if (!Properties.Settings.Default.FitToWindow)
            {
                Top = Properties.Settings.Default.Top;
                Left = Properties.Settings.Default.Left;
                Height = Properties.Settings.Default.Height;
                Width = Properties.Settings.Default.Width;

                if (Properties.Settings.Default.Maximized)
                {
                    WindowState = WindowState.Maximized;
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
            bg.Children.Add(ajaxLoading);
            AjaxLoadingStart();
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            // Update values
            AllowDrop = true;
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
                imgBorder.Background = new SolidColorBrush(Colors.Transparent);
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

                bg.Children.Add(picGallery);
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
            LoadAutoScrollSign();

            // Initilize Things!
            RecentFiles.Initialize();
            InitializeZoom();

            // Add things!
            AddEvents();
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

        #endregion

        #region Events

        private void AddEvents()
        {
            Eventshandling.Go();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo size)
        {
            if (size == null)
            {
                return;
            }

            if (!FitToWindow || !size.WidthChanged && !size.HeightChanged)
            {
                return;
            }

            //Keep position when size has changed
            Top += (size.PreviousSize.Height - size.NewSize.Height) / 2;
            Left += (size.PreviousSize.Width - size.NewSize.Width) / 2;

            // Move cursor after resize when the button has been pressed
            if (RightbuttonClicked)
            {
                Point p = RightButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RighButton
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                RightbuttonClicked = false;
            }
            else if (LeftbuttonClicked)
            {
                Point p = LeftButton.PointToScreen(new Point(50, 10));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                LeftbuttonClicked = false;
            }
            else if (clickArrowRightClicked)
            {
                Point p = clickArrowRight.PointToScreen(new Point(25, 30));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                clickArrowRightClicked = false;
            }
            else if (clickArrowLeftClicked)
            {
                Point p = clickArrowLeft.PointToScreen(new Point(25, 30));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                clickArrowLeftClicked = false;
            }

            base.OnRenderSizeChanged(size);
        }

        #endregion Add events
        
    }
}
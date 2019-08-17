using PicView.File_Logic;
using PicView.Native;
using PicView.ScreenLogic;
using PicView.UserControls;
using PicView.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.Drag_and_Drop.DragAndDrop;
using static PicView.Error_Handling.Error_Handling;
using static PicView.File_Logic.Copy_Paste;
using static PicView.File_Logic.DeleteFiles;
using static PicView.File_Logic.Open_Save;
using static PicView.Helpers.Helper;
using static PicView.Helpers.Variables;
using static PicView.Image_Logic.Navigation;
using static PicView.Image_Logic.Resize_and_Zoom;
using static PicView.Image_Logic.Rotate_and_Flip;
using static PicView.Image_Logic.SlideShow;
using static PicView.Interface_Logic.ContextMenus;
using static PicView.Interface_Logic.Interface;
using static PicView.Interface_Logic.PicGallery;
using static PicView.Shortcuts.Shortcuts;

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
            if (!Properties.Settings.Default.ShowInterface)
            {
                TitleBar.Visibility =
                LowerBar.Visibility =
                LeftBorderRectangle.Visibility =
                RightBorderRectangle.Visibility
                = Visibility.Collapsed;
            }
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            ajaxLoading = new AjaxLoading
            {
                Opacity = 0
            };
            bg.Children.Add(ajaxLoading);
            AjaxLoadingStart();

            // Update values
            var endLoading = false;
            AllowDrop = true;
            IsScrollEnabled = Properties.Settings.Default.ScrollEnabled;
            Pics = new List<string>();
            freshStartup = true;
            DataContext = this;
            MonitorInfo = MonitorSize.GetMonitorSize();

            if (!Properties.Settings.Default.BgColorWhite)
                imgBorder.Background = new SolidColorBrush(Colors.Transparent);

            // Load image if possible
            if (Application.Current.Properties["ArbitraryArgName"] == null)
            {
                Unload();
                endLoading = true;
            }
            else
            {
                var args = Application.Current.Properties["ArbitraryArgName"].ToString();
                Pic(args);
            }

            SetWindowBorderColor();

            // Add UserControls :)
            LoadTooltipStyle();
            LoadFileMenu();
            LoadImageSettingsMenu();
            LoadQuickSettingsMenu();
            LoadFunctionsMenu();
            LoadAutoScrollSign();
            LoadClickArrow(true);
            LoadClickArrow(false);
            Loadx2();
            LoadMinus();

            // Update WindowStyle
            if (!Properties.Settings.Default.ShowInterface)
            {
                clickArrowLeft.Opacity =
                clickArrowRight.Opacity =
                x2.Opacity =
                minus.Opacity =
                0;

                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                minus.Visibility =
                Visibility.Visible;
            }

            // Update UserControl values
            backgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorAlt"];
            mainColor = (Color)Application.Current.Resources["MainColor"];
            quickSettingsMenu.ToggleScroll.IsChecked = IsScrollEnabled;
            if (FitToWindow)
                quickSettingsMenu.SetFit.IsChecked = true;
            else
                quickSettingsMenu.SetCenter.IsChecked = true;

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

                if (Properties.Settings.Default.PicGallery == 2 && !endLoading)
                    PicGalleryFade();
            }

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

            if (endLoading)
                AjaxLoadingEnd();
        }

        #endregion

        #region Add events

        private void AddEvents()
        {
            // keyboard and Mouse_Keys Keys
            KeyDown += MainWindow_KeysDown;
            KeyUp += MainWindow_KeysUp;
            MouseDown += MainWindow_MouseDown;

            // Close Button
            CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;
            CloseButton.MouseEnter += CloseButtonMouseOver;
            CloseButton.MouseLeave += CloseButtonMouseLeave;
            CloseButton.Click += (s, x) => Close();

            // MinButton
            MinButton.PreviewMouseLeftButtonDown += MinButtonMouseButtonDown;
            MinButton.MouseEnter += MinButtonMouseOver;
            MinButton.MouseLeave += MinButtonMouseLeave;
            MinButton.Click += (s, x) => SystemCommands.MinimizeWindow(this);

            // MaxButton
            MaxButton.PreviewMouseLeftButtonDown += MaxButtonMouseButtonDown;
            MaxButton.MouseEnter += MaxButtonMouseOver;
            MaxButton.MouseLeave += MaxButtonMouseLeave;
            MaxButton.Click += (s, x) => Fullscreen_Restore();

            // FileMenuButton
            FileMenuButton.PreviewMouseLeftButtonDown += OpenMenuButtonMouseButtonDown;
            FileMenuButton.MouseEnter += OpenMenuButtonMouseOver;
            FileMenuButton.MouseLeave += OpenMenuButtonMouseLeave;
            FileMenuButton.Click += Toggle_open_menu;

            fileMenu.Open.Click += (s, x) => Open();
            fileMenu.Open_File_Location.Click += (s, x) => Open_In_Explorer();
            fileMenu.Print.Click += (s, x) => Print(PicPath);
            fileMenu.Save_File.Click += (s, x) => SaveFiles();

            fileMenu.Open_Border.MouseLeftButtonUp += (s, x) => Open();
            fileMenu.Open_File_Location_Border.MouseLeftButtonUp += (s, x) => Open_In_Explorer();
            fileMenu.Print_Border.MouseLeftButtonUp += (s, x) => Print(PicPath);
            fileMenu.Save_File_Location_Border.MouseLeftButtonUp += (s, x) => SaveFiles();

            fileMenu.CloseButton.Click += Close_UserControls;
            fileMenu.PasteButton.Click += (s, x) => Paste();
            fileMenu.CopyButton.Click += (s, x) => CopyPic();

            // image_button
            image_button.PreviewMouseLeftButtonDown += ImageButtonMouseButtonDown;
            image_button.MouseEnter += ImageButtonMouseOver;
            image_button.MouseLeave += ImageButtonMouseLeave;
            image_button.Click += Toggle_image_menu;

            // imageSettingsMenu Buttons
            imageSettingsMenu.CloseButton.Click += Close_UserControls;
            imageSettingsMenu.Rotation0Button.Click += (s, x) => Rotate(0);
            imageSettingsMenu.Rotation90Button.Click += (s, x) => Rotate(90);
            imageSettingsMenu.Rotation180Button.Click += (s, x) => Rotate(180);
            imageSettingsMenu.Rotation270Button.Click += (s, x) => Rotate(270);
            imageSettingsMenu.Rotation0Border.MouseLeftButtonDown += (s, x) => Rotate(0);
            imageSettingsMenu.Rotation90Border.MouseLeftButtonDown += (s, x) => Rotate(90);
            imageSettingsMenu.Rotation180Border.MouseLeftButtonDown += (s, x) => Rotate(180);
            imageSettingsMenu.Rotation270Border.MouseLeftButtonDown += (s, x) => Rotate(270);

            // LeftButton
            LeftButton.PreviewMouseLeftButtonDown += LeftButtonMouseButtonDown;
            LeftButton.MouseEnter += LeftButtonMouseOver;
            LeftButton.MouseLeave += LeftButtonMouseLeave;
            LeftButton.Click += (s, x) => { LeftbuttonClicked = true; Pic(false, false); };

            // RightButton
            RightButton.PreviewMouseLeftButtonDown += RightButtonMouseButtonDown;
            RightButton.MouseEnter += RightButtonMouseOver;
            RightButton.MouseLeave += RightButtonMouseLeave;
            RightButton.Click += (s, x) => { RightbuttonClicked = true; Pic(); };

            // QuickSettingsMenu
            SettingsButton.PreviewMouseLeftButtonDown += SettingsButtonButtonMouseButtonDown;
            SettingsButton.MouseEnter += SettingsButtonButtonMouseOver;
            SettingsButton.MouseLeave += SettingsButtonButtonMouseLeave;
            SettingsButton.Click += Toggle_quick_settings_menu;
            quickSettingsMenu.CloseButton.Click += Toggle_quick_settings_menu;

            quickSettingsMenu.ToggleScroll.Checked += (s, x) => IsScrollEnabled = true;
            quickSettingsMenu.ToggleScroll.Unchecked += (s, x) => IsScrollEnabled = false;
            quickSettingsMenu.ToggleScroll.Click += Toggle_quick_settings_menu;

            quickSettingsMenu.SetFit.Click += (s, x) => { FitToWindow = true; };
            quickSettingsMenu.SetCenter.Click += (s, x) => { FitToWindow = false; };
            quickSettingsMenu.SettingsButton.Click += (s, x) => AllSettingsWindow();

            //FunctionMenu
            FunctionMenuButton.PreviewMouseLeftButtonDown += FunctionMenuButtonButtonMouseButtonDown;
            FunctionMenuButton.MouseEnter += FunctionMenuButtonButtonMouseOver;
            FunctionMenuButton.MouseLeave += FunctionMenuButtonButtonMouseLeave;
            FunctionMenuButton.Click += Toggle_Functions_menu;
            functionsMenu.CloseButton.Click += Toggle_Functions_menu;
            functionsMenu.Help.Click += (s, x) => HelpWindow();
            functionsMenu.About.Click += (s, x) => AboutWindow();
            functionsMenu.FileDetailsButton.Click += (s, x) => NativeMethods.ShowFileProperties(PicPath);
            functionsMenu.DeleteButton.Click += (s, x) => DeleteFile(PicPath, true);
            functionsMenu.DeletePermButton.Click += (s, x) => DeleteFile(PicPath, false);
            functionsMenu.ReloadButton.Click += (s, x) => Reload();
            functionsMenu.ResetZoomButton.Click += (s, x) => ResetZoom();
            functionsMenu.SlideshowButton.Click += (s, x) => LoadSlideshow();
            functionsMenu.BgButton.Click += (s, x) =>
            {
                if (imgBorder == null)
                    return;

                if (!(imgBorder.Background is SolidColorBrush cc))
                    return;

                if (cc.Color == Colors.White)
                {
                    imgBorder.Background = new SolidColorBrush(Colors.Transparent);
                    Properties.Settings.Default.BgColorWhite = false;
                }
                    
                else
                {
                    imgBorder.Background = new SolidColorBrush(Colors.White);
                    Properties.Settings.Default.BgColorWhite = true;
                }
                    
            };

            // FlipButton
            imageSettingsMenu.FlipButton.Click += (s, x) => Flip();

            // ClickArrows
            clickArrowLeft.MouseLeftButtonUp += (s, x) =>
            {
                clickArrowLeftClicked = true;
                Pic(false, false);
            };
            clickArrowRight.MouseLeftButtonUp += (s, x) =>
            {
                clickArrowRightClicked = true;
                Pic();
            };

            // x2
            x2.MouseLeftButtonUp += (x, xx) => Close();

            // Minus
            minus.MouseLeftButtonUp += (s, x) => SystemCommands.MinimizeWindow(this);

            // Bar
            Bar.MouseLeftButtonDown += Move;

            // img
            img.MouseLeftButtonDown += Zoom_img_MouseLeftButtonDown;
            img.MouseLeftButtonUp += Zoom_img_MouseLeftButtonUp;
            img.MouseMove += Zoom_img_MouseMove;
            img.MouseWheel += Zoom_img_MouseWheel;

            // bg
            bg.MouseLeftButtonDown += Bg_MouseLeftButtonDown;
            bg.Drop += Image_Drop;
            bg.DragOver += Image_DraOver;
            bg.DragEnter += Image_DraEnter;
            bg.DragLeave += Image_DragLeave;

            // TooltipStyle
            sexyToolTip.MouseWheel += Zoom_img_MouseWheel;

            // TitleBar
            TitleBar.MouseLeftButtonDown += Move;
            TitleBar.MouseLeave += Restore_From_Move;

            // Logobg
            //Logobg.MouseEnter += LogoMouseOver;
            //Logobg.MouseLeave += LogoMouseLeave;
            //Logobg.PreviewMouseLeftButtonDown += LogoMouseButtonDown;

            // Lower Bar
            LowerBar.Drop += Image_Drop;

            // PicGallery
            if (Properties.Settings.Default.PicGallery > 0)
            {
                picGallery.PreviewItemClick += PicGallery_PreviewItemClick;
                picGallery.ItemClick += PicGallery_ItemClick;
            }

            // This
            Closing += Window_Closing;
            MouseMove += MainWindow_MouseMove;
            MouseLeave += MainWindow_MouseLeave;
            StateChanged += MainWindow_StateChanged;

            //LocationChanged += MainWindow_LocationChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;          
        }

        #endregion Add events

        #region Changed Events

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    FitToWindow = FitToWindow ? false : true;
                    break;
                case WindowState.Minimized:
                    break;
                case WindowState.Maximized:
                    FitToWindow = false;
                    break;
                default:
                    break;
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // Update size when screen resulution changes

            MonitorInfo = MonitorSize.GetMonitorSize();
            ZoomFit(img.Width, img.Height);
        }

        //private void MainWindow_LocationChanged(object sender, EventArgs e)
        //{
        //    // Need to figure out a way to handle user dragging app to a monitor, with different resolution.

        //    //MonitorInfo = MonitorSize.GetMonitorSize();
        //    //ZoomFit(img.Width, img.Height);
        //}

        protected override void OnRenderSizeChanged(SizeChangedInfo size)
        {
            base.OnRenderSizeChanged(size);

            if (!FitToWindow)
                return;

            //Keep position when size has changed
            if (size.HeightChanged)
                Top += (size.PreviousSize.Height - size.NewSize.Height) / 2;
            if (size.WidthChanged)
                Left += (size.PreviousSize.Width - size.NewSize.Width) / 2;

            // Move cursor after resize when the button has been pressed
            if (RightbuttonClicked)
            {
                Point p = RightButton.PointToScreen(new Point(50, 30)); //Points cursor to center of RighButton
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                RightbuttonClicked = false;
            }
            else if (LeftbuttonClicked)
            {
                Point p = LeftButton.PointToScreen(new Point(50, 30));
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
        }

        #endregion Changed Events

        #region Window Functions

        /// <summary>
        /// Move window and maximize on double click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void Move(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (e.ClickCount == 2)
            {
                // Prevent method from being called twice
                var bar = sender as TextBlock;
                if (bar != null)
                {
                    if (bar.Name == "Bar")
                        return;
                }
                Maximize_Restore();
            }
            else
            {
                try
                {
                    DragMove();

                    // Update info for possible new screen, needs more engineering
                    MonitorInfo = MonitorSize.GetMonitorSize();
                }
                catch (InvalidOperationException)
                {
                    //Supress "Can only call DragMove when primary mouse button is down"
                }
            }
        }


        /// <summary>
        /// Function made to restore and drag window from maximized windowstate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Restore_From_Move(object sender, MouseEventArgs e)
        {
            if (WindowState == WindowState.Maximized && e.LeftButton == MouseButtonState.Pressed)
            {
                //Maximize_Restore();
                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    //Supress "Can only call DragMove when primary mouse button is down"
                }
            }
        }

        /// <summary>
        /// Maximizes/restore window
        /// </summary>
        internal void Maximize_Restore()
        {
            // Maximize
            if (WindowState == WindowState.Normal)
            {
                // Update new setting and sizing
                FitToWindow = false;

                // Tell Windows that it's maximized
                WindowState = WindowState.Maximized;
                SystemCommands.MaximizeWindow(this);

                //// Update button to reflect change
                //MaxButton.ToolTip = "Restore";
                //MaxButtonPath.Data = Geometry.Parse("M143-7h428v286h-428v-286z m571 286h286v428h-429v-143h54q37 0 63-26t26-63v-196z m429 482v-536q0-37-26-63t-63-26h-340v-197q0-37-26-63t-63-26h-536q-36 0-63 26t-26 63v536q0 37 26 63t63 26h340v197q0 37 26 63t63 26h536q36 0 63-26t26-63z");
            }

            // Restore
            else if (WindowState == WindowState.Maximized)
            {
                // Update new setting and sizing
                FitToWindow = true;

                // Tell Windows that it's normal
                WindowState = WindowState.Normal;
                SystemCommands.RestoreWindow(this);

                //// Update button to reflect change
                //MaxButton.ToolTip = "Maximize";
                //MaxButtonPath.Data = Geometry.Parse("M143 64h714v429h-714v-429z m857 625v-678q0-37-26-63t-63-27h-822q-36 0-63 27t-26 63v678q0 37 26 63t63 27h822q37 0 63-27t26-63z");
            }
        }

        /// <summary>
        /// Fullscreen/restore window
        /// </summary>
        internal void Fullscreen_Restore()
        {
            // Update new setting and sizing
            FitToWindow = false;

            HideInterface(false, false);           

            Width = bg.Width = SystemParameters.PrimaryScreenWidth + 2;
            Height = bg.Height = SystemParameters.PrimaryScreenHeight + 2;

            Top = 0;
            Left = 0;

            Topmost = true;
        }

        /// <summary>
        /// Centers on the current monitor
        /// </summary>
        internal void CenterWindowOnScreen()
        {
            if (!FitToWindow)
                return;

            //move to the centre
            Left = (((MonitorInfo.WorkArea.Width - (Width * MonitorInfo.DpiScaling)) / 2) + (MonitorInfo.WorkArea.Left * MonitorInfo.DpiScaling));
            Top = ((MonitorInfo.WorkArea.Height - (Height * MonitorInfo.DpiScaling)) / 2) + (MonitorInfo.WorkArea.Top * MonitorInfo.DpiScaling);

        }

        #endregion

        /// <summary>
        /// Save settings when closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Close Extra windows when closing
            if (Application.Current.Windows.OfType<FakeWindow>().Any())
                Application.Current.Windows[1].Close();

            Hide(); // Make it feel faster

            Properties.Settings.Default.Save();
            DeleteTempFiles();
            RecentFiles.WriteToFile();
            Environment.Exit(0);
        }
    }
}
using PicView.lib;
using PicView.lib.UserControls;
using PicView.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.lib.FileFunctions;
using static PicView.lib.Helper;
using static PicView.lib.ImageManager;
using static PicView.lib.Variables;
using static PicView.lib.WindowFunctions;

namespace PicView
{
    public partial class MainWindow : Window
    {
        #region Window Logic

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => MainWindow_Loaded(s, e);
            ContentRendered += MainWindow_ContentRendered;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.Resources["ChosenColor"] = AnimationHelper.GetPrefferedColorOver();
            ajaxLoading = new AjaxLoading
            {
                Opacity = 0
            };
            bg.Children.Add(ajaxLoading);
            AjaxLoadingStart();

            if (Properties.Settings.Default.WindowStyle == "Alt")
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
            var endLoading = false;

            // Update values
            AllowDrop = true;
            IsScrollEnabled = Properties.Settings.Default.ScrollEnabled;
            Pics = new List<string>();
            freshStartup = true;
            DataContext = this;

            // Load image if possible
            if (Application.Current.Properties["ArbitraryArgName"] == null)
            {
                Unload();
                endLoading = true;
            }
            else
            {
                var file = Application.Current.Properties["ArbitraryArgName"].ToString();
                if (File.Exists(file))
                    Pic(file);
                else if (Uri.IsWellFormedUriString(file, UriKind.Absolute))
                    PicWeb(file);
                else
                {
                    Unload();
                    endLoading = true;
                }
            }

            // Add UserControls :)
            LoadTooltipStyle();
            LoadFileMenu();
            LoadImageSettingsMenu();
            LoadQuickSettingsMenu();
            LoadAutoScrollSign();
            LoadClickArrow(true);
            LoadClickArrow(false);
            Loadx2();

            // Update UserControl values
            backgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorFade"];
            mainColor = (Color)Application.Current.Resources["MainColor"];
            quickSettingsMenu.ToggleScroll.IsChecked = IsScrollEnabled;

            // Update WindowStyle
            if (Properties.Settings.Default.WindowStyle == "Alt")
            {
                clickArrowLeft.Opacity =
                clickArrowRight.Opacity =
                x2.Opacity =
                0;

                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                Visibility.Visible;
            }

            // Do updates in seperate task
            var task = new Task(() =>
            {
                // Initilize Most Recently used
                //mruList = new RecentFiles();

                #region Add events

                // keyboard and Mouse_Keys Keys
                //PreviewKeyDown += previewKeys;
                KeyDown += MainWindow_KeyDown;
                KeyUp += MainWindow_KeyUp;
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
                MinButton.Click += (s, x) => Minimize(this);

                // MaxButton
                MaxButton.PreviewMouseLeftButtonDown += MaxButtonMouseButtonDown;
                MaxButton.MouseEnter += MaxButtonMouseOver;
                MaxButton.MouseLeave += MaxButtonMouseLeave;
                MaxButton.Click += (s, x) => Maximize(this);

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

                // Settings and QuickSettingsMenu
                SettingsButton.PreviewMouseLeftButtonDown += SettingsButtonButtonMouseButtonDown;
                SettingsButton.MouseEnter += SettingsButtonButtonMouseOver;
                SettingsButton.MouseLeave += SettingsButtonButtonMouseLeave;
                SettingsButton.Click += Toggle_quick_settings_menu;
                quickSettingsMenu.CloseButton.Click += Toggle_quick_settings_menu;
                quickSettingsMenu.ToggleScroll.Checked += (s, x) =>
                {
                    IsScrollEnabled = true;
                    Close_UserControls();
                };
                quickSettingsMenu.ToggleScroll.Unchecked += (s, x) =>
                {
                    IsScrollEnabled = false;
                    Close_UserControls();
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

                // Bar
                Bar.MouseLeftButtonDown += Move;

                // img
                img.MouseLeftButtonDown += Zoom_img_MouseLeftButtonDown;
                img.MouseLeftButtonUp += Zoom_img_MouseLeftButtonUp;
                img.MouseMove += Zoom_img_MouseMove;
                img.MouseWheel += Zoom_img_MouseWheel;

                // bg
                bg.Drop += Image_Drop;
                bg.DragOver += Image_DraOver;
                bg.DragLeave += Image_DragLeave;

                // TooltipStyle
                sexyToolTip.MouseWheel += Zoom_img_MouseWheel;

                // TitleBar
                TitleBar.MouseLeftButtonDown += Move;

                // Logobg
                //Logobg.MouseEnter += LogoMouseOver;
                //Logobg.MouseLeave += LogoMouseLeave;
                //Logobg.PreviewMouseLeftButtonDown += LogoMouseButtonDown;

                // Lower Bar
                LowerBar.Drop += Image_Drop;

                // This
                Closing += Window_Closing;
                MouseMove += MainWindow_MouseMove;
                MouseLeave += MainWindow_MouseLeave;

                #endregion

                // Add Timers
                autoScrollTimer = new System.Timers.Timer()
                {
                    Interval = 7,
                    AutoReset = true,
                    Enabled = false
                };
                autoScrollTimer.Elapsed += AutoScrollTimerEvent;

                activityTimer = new System.Timers.Timer()
                {
                    Interval = 2500,
                    AutoReset = true,
                    Enabled = false
                };
                activityTimer.Elapsed += ActivityTimer_Elapsed;

                fastPicTimer = new System.Timers.Timer()
                {
                    Interval = 85,
                    AutoReset = true,
                    Enabled = false
                };
                fastPicTimer.Elapsed += FastPic;

                // Updates settings from older version to newer version
                if (Properties.Settings.Default.CallUpgrade)
                {
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.CallUpgrade = false;
                }

            });
            task.Start();

            InitializeZoom();

            #region Add ContextMenus

            // Add main contextmenu
            cm = new ContextMenu();
            var scbf = (SolidColorBrush)Application.Current.Resources["MainColorFadedBrush"];

            var opencm = new MenuItem
            {
                Header = "Open",
                InputGestureText = "Ctrl + O"
            };
            var opencmIcon = new System.Windows.Shapes.Path();
            opencmIcon.Data = Geometry.Parse("M1717 931q0-35-53-35h-1088q-40 0-85.5 21.5t-71.5 52.5l-294 363q-18 24-18 40 0 35 53 35h1088q40 0 86-22t71-53l294-363q18-22 18-39zm-1141-163h768v-160q0-40-28-68t-68-28h-576q-40 0-68-28t-28-68v-64q0-40-28-68t-68-28h-320q-40 0-68 28t-28 68v853l256-315q44-53 116-87.5t140-34.5zm1269 163q0 62-46 120l-295 363q-43 53-116 87.5t-140 34.5h-1088q-92 0-158-66t-66-158v-960q0-92 66-158t158-66h320q92 0 158 66t66 158v32h544q92 0 158 66t66 158v160h192q54 0 99 24.5t67 70.5q15 32 15 68z");
            opencmIcon.Stretch = Stretch.Fill;
            opencmIcon.Width = opencmIcon.Height = 12;
            opencmIcon.Fill = scbf;
            opencm.Icon = opencmIcon;
            opencm.Click += (s, x) => Open();
            cm.Items.Add(opencm);

            var savecm = new MenuItem()
            {
                Header = "Save",
                InputGestureText = "Ctrl + S"
            };
            var savecmIcon = new System.Windows.Shapes.Path();
            savecmIcon.Data = Geometry.Parse("M512 1536h768v-384h-768v384zm896 0h128v-896q0-14-10-38.5t-20-34.5l-281-281q-10-10-34-20t-39-10v416q0 40-28 68t-68 28h-576q-40 0-68-28t-28-68v-416h-128v1280h128v-416q0-40 28-68t68-28h832q40 0 68 28t28 68v416zm-384-928v-320q0-13-9.5-22.5t-22.5-9.5h-192q-13 0-22.5 9.5t-9.5 22.5v320q0 13 9.5 22.5t22.5 9.5h192q13 0 22.5-9.5t9.5-22.5zm640 32v928q0 40-28 68t-68 28h-1344q-40 0-68-28t-28-68v-1344q0-40 28-68t68-28h928q40 0 88 20t76 48l280 280q28 28 48 76t20 88z");
            savecmIcon.Stretch = Stretch.Fill;
            savecmIcon.Width = savecmIcon.Height = 12;
            savecmIcon.Fill = scbf;
            savecm.Icon = savecmIcon;
            savecm.Click += (s, x) => SaveFiles();
            cm.Items.Add(savecm);

            var printcm = new MenuItem
            {
                Header = "Print",
                InputGestureText = "Ctrl + P"
            };
            var printcmIcon = new System.Windows.Shapes.Path();
            printcmIcon.Data = Geometry.Parse("M448 1536h896v-256h-896v256zm0-640h896v-384h-160q-40 0-68-28t-28-68v-160h-640v640zm1152 64q0-26-19-45t-45-19-45 19-19 45 19 45 45 19 45-19 19-45zm128 0v416q0 13-9.5 22.5t-22.5 9.5h-224v160q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-160h-224q-13 0-22.5-9.5t-9.5-22.5v-416q0-79 56.5-135.5t135.5-56.5h64v-544q0-40 28-68t68-28h672q40 0 88 20t76 48l152 152q28 28 48 76t20 88v256h64q79 0 135.5 56.5t56.5 135.5z");
            printcmIcon.Stretch = Stretch.Fill;
            printcmIcon.Width = printcmIcon.Height = 12;
            printcmIcon.Fill = scbf;
            printcm.Icon = printcmIcon;
            printcm.Click += (s, x) => Print(PicPath);
            cm.Items.Add(printcm);

            var wallcm = new MenuItem
            {
                Header = "Set as wallpaper"
            };
            wallcm.Click += (s, x) => Wallpaper.SetWallpaper(PicPath, WallpaperStyle.Fill);
            cm.Items.Add(wallcm);
            cm.Items.Add(new Separator());

            var lcdcm = new MenuItem
            {
                Header = "Locate on disk",
                InputGestureText = "F3",
                ToolTip = "Opens the current image on your drive"
            };
            lcdcm.Click += (s, x) => Open_In_Explorer();
            cm.Items.Add(lcdcm);

            var fildecm = new MenuItem
            {
                Header = "File Details",
                InputGestureText = "Ctrl + I"
            };
            fildecm.Click += (s, x) => NativeMethods.ShowFileProperties(PicPath);
            cm.Items.Add(fildecm);

            var cppcm = new MenuItem
            {
                Header = "Copy picture",
                InputGestureText = "Ctrl + C"
            };
            var cppcmIcon = new System.Windows.Shapes.Path();
            cppcmIcon.Data = Geometry.Parse("M1696 384q40 0 68 28t28 68v1216q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-288h-544q-40 0-68-28t-28-68v-672q0-40 20-88t48-76l408-408q28-28 76-48t88-20h416q40 0 68 28t28 68v328q68-40 128-40h416zm-544 213l-299 299h299v-299zm-640-384l-299 299h299v-299zm196 647l316-316v-416h-384v416q0 40-28 68t-68 28h-416v640h512v-256q0-40 20-88t48-76zm956 804v-1152h-384v416q0 40-28 68t-68 28h-416v640h896z");
            cppcmIcon.Stretch = Stretch.Fill;
            cppcmIcon.Width = cppcmIcon.Height = 12;
            cppcmIcon.Fill = scbf;
            cppcm.Icon = cppcmIcon;
            cppcm.Click += (s, x) => CopyPic();
            cm.Items.Add(cppcm);

            var pastecm = new MenuItem
            {
                Header = "Paste picture",
                InputGestureText = "Ctrl + V"
            };
            var pastecmIcon = new System.Windows.Shapes.Path();
            pastecmIcon.Data = Geometry.Parse("M768 1664h896v-640h-416q-40 0-68-28t-28-68v-416h-384v1152zm256-1440v-64q0-13-9.5-22.5t-22.5-9.5h-704q-13 0-22.5 9.5t-9.5 22.5v64q0 13 9.5 22.5t22.5 9.5h704q13 0 22.5-9.5t9.5-22.5zm256 672h299l-299-299v299zm512 128v672q0 40-28 68t-68 28h-960q-40 0-68-28t-28-68v-160h-544q-40 0-68-28t-28-68v-1344q0-40 28-68t68-28h1088q40 0 68 28t28 68v328q21 13 36 28l408 408q28 28 48 76t20 88z");
            pastecmIcon.Stretch = Stretch.Fill;
            pastecmIcon.Width = pastecmIcon.Height = 12;
            pastecmIcon.Fill = scbf;
            pastecm.Icon = pastecmIcon;
            pastecm.Click += (s, x) => Paste();
            cm.Items.Add(pastecm);
            cm.Items.Add(new Separator());

            var unloadcm = new MenuItem
            {
                Header = "Clear picture"
            };
            unloadcm.Click += (s, x) => Unload();
            cm.Items.Add(unloadcm);
            cm.Items.Add(new Separator());

            var abcm = new MenuItem
            {
                Header = "About",
                InputGestureText = "F2",
                ToolTip = "Shows version and copyright"
            };
            var abcmIcon = new System.Windows.Shapes.Path();
            abcmIcon.Data = Geometry.Parse("M1216 1344v128q0 26-19 45t-45 19h-512q-26 0-45-19t-19-45v-128q0-26 19-45t45-19h64v-384h-64q-26 0-45-19t-19-45v-128q0-26 19-45t45-19h384q26 0 45 19t19 45v576h64q26 0 45 19t19 45zm-128-1152v192q0 26-19 45t-45 19h-256q-26 0-45-19t-19-45v-192q0-26 19-45t45-19h256q26 0 45 19t19 45z");
            abcmIcon.Stretch = Stretch.Fill;
            abcmIcon.Height = 12;
            abcmIcon.Width = 9;
            abcmIcon.Fill = scbf;
            abcm.Icon = abcmIcon;
            abcm.Click += (s, x) => AboutWindow();
            cm.Items.Add(abcm);

            var helpcm = new MenuItem
            {
                Header = "Help",
                InputGestureText = "F1",
                ToolTip = "Shows keyboard shortcuts and general help"
            };
            var helpcmIcon = new System.Windows.Shapes.Path();
            helpcmIcon.Data = Geometry.Parse("M1088 1256v240q0 16-12 28t-28 12h-240q-16 0-28-12t-12-28v-240q0-16 12-28t28-12h240q16 0 28 12t12 28zm316-600q0 54-15.5 101t-35 76.5-55 59.5-57.5 43.5-61 35.5q-41 23-68.5 65t-27.5 67q0 17-12 32.5t-28 15.5h-240q-15 0-25.5-18.5t-10.5-37.5v-45q0-83 65-156.5t143-108.5q59-27 84-56t25-76q0-42-46.5-74t-107.5-32q-65 0-108 29-35 25-107 115-13 16-31 16-12 0-25-8l-164-125q-13-10-15.5-25t5.5-28q160-266 464-266 80 0 161 31t146 83 106 127.5 41 158.5z");
            helpcmIcon.Stretch = Stretch.Fill;
            helpcmIcon.Width = helpcmIcon.Height = 12;
            helpcmIcon.Fill = scbf;
            helpcm.Icon = helpcmIcon;
            helpcm.Click += (s, x) => HelpWindow();
            cm.Items.Add(helpcm);

            cm.Items.Add(new Separator());
            
            var mincm = new MenuItem
            {
                Header = "Minimize"
            };
            var mincmIcon = new System.Windows.Shapes.Path();
            mincmIcon.Data = Geometry.Parse("F1M0,6L0,9 9,9 9,6 0,6z");
            mincmIcon.Stretch = Stretch.Fill;
            mincmIcon.Width = 12;
            mincmIcon.Height = 5;
            mincmIcon.Fill = scbf;
            mincm.Icon = mincmIcon;
            mincm.Click += (s, x) => Minimize(this);
            cm.Items.Add(mincm);

            
            var maxcm = new MenuItem
            {
                Header = "Maximize"
            };
            var maxcmIcon = new System.Windows.Shapes.Path();
            maxcmIcon.Data = Geometry.Parse("F1M0,0L0,9 9,9 9,0 0,0 0,3 8,3 8,8 1,8 1,3z");
            maxcmIcon.Stretch = Stretch.Fill;
            maxcmIcon.Width = maxcmIcon.Height = 12;
            maxcmIcon.Fill = scbf;
            maxcm.Icon = maxcmIcon;
            maxcm.Click += (s, x) => Maximize(this);
            cm.Items.Add(maxcm);

            var clcm = new MenuItem
            {
                Header = "Close"
            };
            var mclcmIcon = new System.Windows.Shapes.Path();
            mclcmIcon.Data = Geometry.Parse("M443.6,387.1L312.4,255.4l131.5-130c5.4-5.4,5.4-14.2,0-19.6l-37.4-37.6c-2.6-2.6-6.1-4-9.8-4c-3.7,0-7.2,1.5-9.8,4  L256,197.8L124.9,68.3c-2.6-2.6-6.1-4-9.8-4c-3.7,0-7.2,1.5-9.8,4L68,105.9c-5.4,5.4-5.4,14.2,0,19.6l131.5,130L68.4,387.1  c-2.6,2.6-4.1,6.1-4.1,9.8c0,3.7,1.4,7.2,4.1,9.8l37.4,37.6c2.7,2.7,6.2,4.1,9.8,4.1c3.5,0,7.1-1.3,9.8-4.1L256,313.1l130.7,131.1  c2.7,2.7,6.2,4.1,9.8,4.1c3.5,0,7.1-1.3,9.8-4.1l37.4-37.6c2.6-2.6,4.1-6.1,4.1-9.8C447.7,393.2,446.2,389.7,443.6,387.1z");
            mclcmIcon.Stretch = Stretch.Fill;
            mclcmIcon.Width = mclcmIcon.Height = 12;
            mclcmIcon.Fill = scbf;
            clcm.Icon = mclcmIcon;
            clcm.Click += (s, x) => Close();
            cm.Items.Add(clcm);

            // Add to elements
            img.ContextMenu = bg.ContextMenu = cm;


            // Add left and right ContextMenus
            var cmLeft = new ContextMenu();
            var cmRight = new ContextMenu();

            var nextcm = new MenuItem
            {
                Header = "Next picture",
                InputGestureText = "ᗌ or D",
                ToolTip = "Go to Next image in folder",
                StaysOpenOnClick = true
            };
            nextcm.Click += (s, x) => Pic();
            cmRight.Items.Add(nextcm);

            var prevcm = new MenuItem
            {
                Header = "Previous picture",
                InputGestureText = "ᗏ or A",
                ToolTip = "Go to previous image in folder",
                StaysOpenOnClick = true
            };
            prevcm.Click += (s, x) => Pic(false);
            cmLeft.Items.Add(prevcm);

            var firstcm = new MenuItem
            {
                Header = "First picture",
                InputGestureText = "Ctrl + D or Ctrl + ᗌ",
                ToolTip = "Go to first image in folder"
            };
            firstcm.Click += (s, x) => Pic(false, true);
            cmLeft.Items.Add(firstcm);

            var lastcm = new MenuItem
            {
                Header = "Last picture",
                InputGestureText = "Ctrl + A or Ctrl + ᗏ",
                ToolTip = "Go to last image in folder"
            };
            lastcm.Click += (s, x) => Pic(true, true);
            cmRight.Items.Add(lastcm);

            // Add to elements
            RightButton.ContextMenu = cmRight;
            LeftButton.ContextMenu = cmLeft;

            clickArrowRight.ContextMenu = cmRight;
            clickArrowLeft.ContextMenu = cmLeft;


            // Add Title contextMenu
            var cmTitle = new ContextMenu();

            var clTc = new MenuItem
            {
                Header = "Copy path to clipboard"
            };
            clTc.Click += (s, x) => CopyText();
            cmTitle.Items.Add(clTc);

            Bar.ContextMenu = cmTitle;

            // Add Close x2 contextMenu
            var closeX2 = new ContextMenu();

            var clX2z = new MenuItem
            {
                Header = "Return to normal interface"
            };
            clX2z.Click += (s, x) => HideInterface();
            closeX2.Items.Add(clX2z);

            var clX2x = new MenuItem
            {
                Header = "Close"
            };
            clX2x.Click += (s, x) => Close();
            closeX2.Items.Add(clX2x);

            x2.ContextMenu = closeX2;

            #endregion           

            if (endLoading)
            {
                AjaxLoadingEnd();
            }
        }

        /// <summary>
        /// Reset to default state
        /// </summary>
        private void Unload()
        {
            Bar.ToolTip = Bar.Text = NoImage;
            Title = NoImage + " - " + AppName;
            canNavigate = false;
            img.Source = null;
            freshStartup = true;
            Pics.Clear();
            PreloadCount = 0;
            Preloader.Clear();
            PicPath = string.Empty;
            FolderIndex = 0;
            img.Width = Scroller.Width = Scroller.Height =
            img.Height = double.NaN;

            if (!string.IsNullOrWhiteSpace(TempZipPath))
            {
                DeleteTempFiles();
                TempZipPath = string.Empty;
            }

            AjaxLoadingEnd();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo size)
        {
            base.OnRenderSizeChanged(size);

            //Keep position when size has changed
            if (size.HeightChanged)
            {
                Top += (size.PreviousSize.Height - size.NewSize.Height) / 2;
            }

            if (size.WidthChanged)
            {
                Left += (size.PreviousSize.Width - size.NewSize.Width) / 2;
            }

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

        /// <summary>
        /// Centers on the primary monitor.. Needs multi monitor solution....
        /// </summary>
        private void CenterWindowOnScreen()
        {
            Top = (SystemParameters.WorkArea.Height - Height) / 2;
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
        }

        /// <summary>
        /// Move window and maximize on double click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Move(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (e.ClickCount == 2)
                Maximize(this);
            else
            {
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
        /// Save settings when closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            DeleteTempFiles();
            //mruList.WriteToFile();
        }

        #endregion

        #region Image Logic

        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        private async void Pic(string path)
        {
            // Set Loading
            Title = Bar.Text = Loading;
            Bar.ToolTip = Loading;
            if (img.Source == null)
            {
                AjaxLoadingStart();
            }

            //if (!string.IsNullOrWhiteSpace(TempZipPath) && mruList != null)
            //    mruList.SetZipped(PicPath);
            
            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            if (!string.IsNullOrWhiteSpace(PicPath) && Path.GetDirectoryName(path) != Path.GetDirectoryName(PicPath)
                || string.IsNullOrWhiteSpace(TempZipPath))
            {
                ChangeFolder();
                await GetValues(path);
            }
            else if (freshStartup)
                await GetValues(path);
            else
            {
                Pics = FileList(Path.GetDirectoryName(path));
                FolderIndex = Pics.IndexOf(path);
            }
            


            Pic(FolderIndex);

            // Set freshStartup
            if (freshStartup)
                freshStartup = false;

            // Fix loading bug
            if (ajaxLoading.Opacity != 1 && canNavigate)
            {
                AjaxLoadingEnd();
            }
        }


        /// <summary>
        /// Loads image based on overloaded int.
        /// Possible out of range error if used inappropriately.
        /// </summary>
        /// <param name="x"></param>
        private async void Pic(int x)
        {
            // Error Handling
            if (Pics.Count == 0)
            {
                bool foo = await RecoverFailedArchiveAsync();
                if (!foo)
                    return;
            }
            if (!File.Exists(Pics[x]))
            {
                PicErrorFix(x);
                return;
            }

            // Add "pic" as local variable used for the image.
            // Use the Load() function load image from memory if available
            // if not, it will be null
            BitmapSource pic = Preloader.Load(Pics[x]);
            var Extension = Path.GetExtension(Pics[x]);

            // if (pic == null)  On demand loading
            // Failed to load image from preloader
            if (pic == null)
            {
                Title = Bar.Text = Loading;
                Bar.ToolTip = Loading;

                if (Properties.Settings.Default.WindowStyle == "Alt")
                    AjaxLoadingStart();

                // Dissallow changing image while loading
                canNavigate = false;

                // Load new value manually
                await Task.Run(() => pic = RenderToBitmapSource(Pics[x], Extension));

                if (pic == null)
                {
                    PicErrorFix(x);
                    return;
                }
            }

            // Scroll to top if scroll enabled
            if (IsScrollEnabled)
                Scroller.ScrollToTop();

            //Prevent the next pichure to be flipped if previous is.
            if (Flipped)
                Flip();

            // Fit window to new values
            ZoomFit(pic.PixelWidth, pic.PixelHeight);

            // If gif, use XamlAnimatedGif to animate it
            if (Extension == ".gif")
                XamlAnimatedGif.AnimationBehavior.SetSourceUri(img, new Uri(Pics[x]));
            else
                img.Source = pic;


            // Update Title to reflect new image
            var titleString = TitleString(pic.PixelWidth, pic.PixelHeight, x);
            Title = titleString[0];
            Bar.Text = titleString[1];
            Bar.ToolTip = titleString[2];

            // Preload images           
            bool? reverse;

            if (PreloadCount > 1 || freshStartup)
                reverse = false;
            else if (PreloadCount < 0)
                reverse = true;
            else
                reverse = null;

            if (reverse.HasValue)
            {
                var t = new Task(() =>
                {
                    if (!Preloader.Contains(Pics[x]))
                        Preloader.Add(pic, Pics[x]);

                    Preloader.PreLoad(x, reverse.Value);
                    PreloadCount = 0;
                });
                t.Start();
            }

            // Update values
            PicPath = Pics[x];
            canNavigate = true;
            Progress(x, Pics.Count);
            FolderIndex = x;
            //if (mruList != null)
            //    mruList.Add(Pics[x]);

            // Loses position gradually if not forced to center       
            CenterWindowOnScreen();

            // Stop AjaxLoading if it's shown
            AjaxLoadingEnd();
        }


        /// <summary>
        /// Load a picture from a prepared bitmap
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        private void Pic(BitmapSource pic, string imageName)
        {
            Unload();

            if (IsScrollEnabled)
                Scroller.ScrollToTop();

            img.Source = pic;

            ZoomFit(pic.PixelWidth, pic.PixelHeight);
            CloseToolTipStyle();
            canNavigate = true;

            var titleString = TitleString(pic.PixelWidth, pic.PixelHeight, imageName);
            Title = titleString[0];
            Bar.Text = titleString[1];
            Bar.ToolTip = titleString[1];

            NoProgress();
            PicPath = string.Empty;
            xWidth = img.ActualWidth;
            xHeight = img.ActualHeight;
            canNavigate = false;
            CenterWindowOnScreen();

            canNavigate = false;
        }


        /// <summary>
        /// Goes to next, previous, first or last file in folder
        /// </summary>
        /// <param name="next">Whether it's forward or not</param>
        /// <param name="end">Whether to go to last or first,
        /// depending on the next value</param>
        private void Pic(bool next = true, bool end = false)
        {
            // Exit if not intended to change picture
            if (!canNavigate)
                return;

            if (end)
            {
                FolderIndex = next ? Pics.Count - 1 : 0;

                if (!Preloader.Contains(Pics[FolderIndex]))
                {
                    PreloadCount = 0;
                    Preloader.Clear();
                }
                else
                {
                    if (next)
                        PreloadCount++;
                    else
                        PreloadCount--;
                }
            }
            else
            {
                if (next)
                {
                    FolderIndex = FolderIndex == Pics.Count - 1 ? 0 : FolderIndex + 1;
                    PreloadCount++;
                }
                else
                {
                    FolderIndex = FolderIndex == 0 ? Pics.Count - 1 : FolderIndex - 1;
                    PreloadCount--;
                }
            }
            Pic(FolderIndex);
        }


        /// <summary>
        /// Only load image from preload or thumbnail without resizing
        /// </summary>
        private async void FastPic(object sender, EventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                Bar.ToolTip = Title = Bar.Text = "Image " + (FolderIndex + 1) + " of " + Pics.Count;

                img.Width = xWidth;
                img.Height = xHeight;
                //Width = img.Width = 465;
                //Height = img.Height = 515;

                img.Source = Preloader.Contains(Pics[FolderIndex]) ? Preloader.Load(Pics[FolderIndex]) : GetWindowsThumbnail(Pics[FolderIndex]);
                FastPicRunning = true;

            }));
            Progress(FolderIndex, Pics.Count);
        }


        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        private void FastPicUpdate()
        {
            fastPicTimer.Stop();
            FastPicRunning = false;

            if (!Preloader.Contains(Pics[FolderIndex]))
            {
                PreloadCount = 0;
                Preloader.Clear();
            }

            Pic(FolderIndex);
        }


        /// <summary>
        /// Attemps to download image and display it
        /// </summary>
        /// <param name="path"></param>
        private async void PicWeb(string path)
        {
            if (ajaxLoading.Opacity != 1)
            {
                AjaxLoadingStart();
            }
            var backUp = Bar.Text;
            Bar.Text = Loading;

            BitmapSource pic = null;
            try
            {
                pic = await LoadImageWebAsync(path);
            }
            catch (WebException)
            {
                pic = null;
            }

            if (pic == null)
            {
                if (backUp == Loading)
                {
                    backUp = NoImage;
                }
                Bar.Text = backUp;
                ToolTipStyle("Unable to load image");
                AjaxLoadingEnd();
                return;
            }

            ZoomFit(pic.PixelWidth, pic.PixelHeight);
            img.Source = pic;

            if (IsScrollEnabled)
                Scroller.ScrollToTop();

            var titleString = TitleString(pic.PixelWidth, pic.PixelHeight, path);
            Title = titleString[0];
            Bar.Text = titleString[1];
            Bar.ToolTip = titleString[0];
            PicPath = path;
            Pics.Clear();
            NoProgress();
            canNavigate = false;
            AjaxLoadingEnd();
            if (freshStartup)
                freshStartup = false;
        }


        /// <summary>
        /// Downloads image from web and returns as BitmapSource
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private async Task<BitmapSource> LoadImageWebAsync(string address)
        {
            BitmapSource pic = null;
            await Task.Run(async () =>
            {
                var client = new WebClient();
                client.DownloadProgressChanged += (sender, e) =>
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        Title = Bar.Text = e.BytesReceived + "/" + e.TotalBytesToReceive + ". " + e.ProgressPercentage + "% complete...";
                    }));

                var bytes = await client.DownloadDataTaskAsync(new Uri(address));
                var stream = new MemoryStream(bytes);
                pic = GetMagickImage(stream);
            });
            return pic;
        }


        /// <summary>
        /// Attemps to fix list by removing invalid files
        /// </summary>
        /// <param name="x"></param>
        private bool PicErrorFix(int x)
        {
            if (Pics.Count < 0 || x >= Pics.Count)
            {
                ToolTipStyle("Unexpected error", true, TimeSpan.FromSeconds(3));
                Unload();
                return false;
            }

            var file = Pics[x];

            if (file == null)
            {
                ToolTipStyle("Unexpected error", true, TimeSpan.FromSeconds(3));
                Unload();
                return false;
            }

            // Retry if exists, fixes rare error
            if (File.Exists(file))
            {
                Preloader.Add(file);
                BitmapSource pic = Preloader.Load(file);
                if (pic != null)
                {
                    Pic(file);
                    return true;
                }
            }

            // Continue to remove file if can't be rendered
            Pics.Remove(file);

            if (Pics.Count < 0)
            {
                ToolTipStyle("No images in folder", true, TimeSpan.FromSeconds(3));
                Unload();
                return false;
            }

            ToolTipStyle("File not found or unable to render, " + file, false, TimeSpan.FromSeconds(2.5));

            // Go to next image
            FolderIndex = FolderIndex == Pics.Count - 1 ? 0 : FolderIndex + 1;


           if (File.Exists(Pics[FolderIndex]))
            {
                Pic(FolderIndex);
                PreloadCount++;
                return true;
            }

            // Repeat process if the next image was not found
            PicErrorFix(FolderIndex);
            return false;
        }


        /// <summary>
        /// Attemps to recover from failed archive extraction
        /// </summary>
        private async Task<bool> RecoverFailedArchiveAsync()
        {
            if (Pics.Count > 0)
                return true;

            if (string.IsNullOrWhiteSpace(TempZipPath))
            {
                // Unexped result, return to clear state.
                Unload();
                return false;
            }

            // TempZipPath is not null = images being extracted
            short count = 0;
            Bar.Text = "Unzipping...";
            do
            {
                try
                {
                    // If there are no pictures, but a folder when TempZipPath has a value,
                    // we should open the folder
                    var directory = Directory.GetDirectories(TempZipPath);
                    if (directory.Length > -1)
                        TempZipPath = directory[0];

                    Pics = FileList(TempZipPath);
                }
                catch (Exception) { }

                if (count > 3)
                {
                    Unload();
                    return false;
                }
                switch (count)
                {
                    case 0:
                        break;
                    case 1:
                        await Task.Delay(700);
                        break;
                    case 2:
                        await Task.Delay(1500);
                        break;
                    default:
                        await Task.Delay(3000);
                        break;
                }
                count++;

            } while (Pics.Count < 1);
            return true;
        }


        /// <summary>
        /// Clears data, to free objects no longer necessary to store in memory and allow changing folder without error.
        /// </summary>
        private void ChangeFolder()
        {
            Pics.Clear();
            Preloader.Clear();
            DeleteTempFiles();
            PreloadCount = 0;
        }

        #endregion

        #region Drag and Drop

        /// <summary>
        /// Check if dragged file is valid,
        /// returns false for valid file with no thumbnail,
        /// true for valid file with thumbnail
        /// and null for invalid file
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        bool? Drag_Drop_Check(string[] files)
        {
            // Return if file strings are null
            if (files == null) return true;
            if (files[0] == null) return true;

            // Return status of useable file
            switch (Path.GetExtension(files[0]))
            {
                // Archives
                case ".zip":
                case ".7zip":
                case ".7z":
                case ".rar":
                case ".cbr":
                case ".cb7":
                case ".cbt":
                case ".cbz":
                case ".xz":
                case ".bzip2":
                case ".gzip":
                case ".tar":
                case ".wim":
                case ".iso":
                case ".cab":

                // Non-standards
                case ".svg":
                case ".psd":
                case ".psb":
                case ".orf":
                case ".cr2":
                case ".crw":
                case ".dng":
                case ".raf":
                case ".raw":
                case ".mrw":
                case ".nef":
                case ".x3f":
                case ".arw":
                case ".webp":
                case ".aai":
                case ".ai":
                case ".art":
                case ".bgra":
                case ".bgro":
                case ".canvas":
                case ".cin":
                case ".cmyk":
                case ".cmyka":
                case ".cur":
                case ".cut":
                case ".dcm":
                case ".dcr":
                case ".dcx":
                case ".dds":
                case ".dfont":
                case ".dlib":
                case ".dpx":
                case ".dxt1":
                case ".dxt5":
                case ".emf":
                case ".epi":
                case ".eps":
                case ".ept":
                case ".ept2":
                case ".ept3":
                case ".exr":
                case ".fax":
                case ".fits":
                case ".flif":
                case ".g3":
                case ".g4":
                case ".gif87":
                case ".gradient":
                case ".gray":
                case ".group4":
                case ".hald":
                case ".hdr":
                case ".hrz":
                case ".icb":
                case ".icon":
                case ".ipl":
                case ".jc2":
                case ".j2k":
                case ".jng":
                case ".jnx":
                case ".jpm":
                case ".jps":
                case ".jpt":
                case ".kdc":
                case ".label":
                case ".map":
                case ".nrw":
                case ".otb":
                case ".otf":
                case ".pbm":
                case ".pcd":
                case ".pcds":
                case ".pcl":
                case ".pct":
                case ".pcx":
                case ".pfa":
                case ".pfb":
                case ".pfm":
                case ".picon":
                case ".pict":
                case ".pix":
                case ".pjpeg":
                case ".png00":
                case ".png24":
                case ".png32":
                case ".png48":
                case ".png64":
                case ".png8":
                case ".pnm":
                case ".ppm":
                case ".ps":
                case ".radialgradient":
                case ".ras":
                case ".rgb":
                case ".rgba":
                case ".rgbo":
                case ".rla":
                case ".rle":
                case ".scr":
                case ".screenshot":
                case ".sgi":
                case ".srf":
                case ".sun":
                case ".svgz":
                case ".tiff64":
                case ".ttf":
                case ".vda":
                case ".vicar":
                case ".vid":
                case ".viff":
                case ".vst":
                case ".vmf":
                case ".wpg":
                case ".xbm":
                case ".xcf":
                case ".yuv":
                    return false;

                // Standards
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".png":
                case ".bmp":
                case ".tif":
                case ".tiff":
                case ".gif":
                case ".ico":
                case ".wdp":
                    return true;

                // Non supported
                default:
                    return null;
            }
        }

        /// <summary>
        /// Logic for handling DragOver event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_DraOver(object sender, DragEventArgs e)
        {
            // Error handling
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

            if (!Drag_Drop_Check(files).HasValue)
            {
                // Tell user drop not supported
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            // Tell that it's succeeded
            e.Effects = DragDropEffects.Copy;
            isDraggedOver = e.Handled = true;
            ToolTipStyle(DragOverString);

            // If standard image, show thumbnail preview
            if (!Drag_Drop_Check(files).Value)
                return;

            // If no image, fix it to container
            if (img.Source == null)
            {
                img.Width = Scroller.ActualWidth;
                img.Height = Scroller.ActualHeight;
            }
            else
            {
                // Save our image so we can swap back to it later if neccesary
                prevPicResource = img.Source;
            }

            // Load from preloader or Windows thumbnails
            img.Source = Preloader.Contains(files[0]) ? Preloader.Load(files[0]) : GetWindowsThumbnail(files[0]);

        }

        /// <summary>
        /// Logic for handling when the cursor leaves drag area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Image_DragLeave(object sender, DragEventArgs e)
        {
            // Error handling
            if (!isDraggedOver)
                return;

            // Switch to previous image if available
            if (prevPicResource != null)
            {
                img.Source = prevPicResource;
                prevPicResource = null;
            }
            else if (!canNavigate && !Uri.IsWellFormedUriString(PicPath, UriKind.Absolute))
            {
                img.Source = null;
            }

            // Update status
            isDraggedOver = false;
        }

        /// <summary>
        /// Logic for handling the drop event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_Drop(object sender, DragEventArgs e)
        {
            // Error handling
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            // Get files as strings
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

            // check if valid
            if (!Drag_Drop_Check(files).HasValue)
                return;

            // Load it
            Pic(files[0]);

            // Don't show drop message any longer
            CloseToolTipStyle();

            // Start multiple clients if user drags multiple files
            if (files.Length > 0)
            {
                Parallel.For(1, files.Length, x =>
                {
                    var myProcess = new Process
                    {
                        StartInfo = { FileName = Assembly.GetExecutingAssembly().Location, Arguments = files[x] }
                    };
                    myProcess.Start();
                });
            }

            // Save memory, make prevPicResource null
            if (prevPicResource != null)
            {
                prevPicResource = null;
            }
        }

        #endregion

        #region Keyboard & Mouse Shortcuts

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                // Next             
                case Key.BrowserForward:
                case Key.Right:
                case Key.D:
                    if (!e.IsRepeat)
                    {
                        // Go to first if Ctrl held down
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                            Pic(true, true);
                        else
                            Pic();
                    }
                    else if (canNavigate)
                    {
                        if (FolderIndex == Pics.Count - 1)
                            FolderIndex = 0;
                        else
                            FolderIndex++;

                        fastPicTimer.Start();
                        e.Handled = true;
                    }
                    break;

                // Prev
                case Key.BrowserBack:
                case Key.Left:
                case Key.A:
                    if (!e.IsRepeat)
                    {
                        // Go to first if Ctrl held down
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                            Pic(false, true);
                        else
                            Pic(false);

                        FastPicRunning = false;
                    }
                    else if (canNavigate)
                    {
                        if (FolderIndex == 0)
                            FolderIndex = Pics.Count - 1;
                        else
                            FolderIndex--;

                        fastPicTimer.Start();
                    }
                    break;

                // Scroll
                case Key.PageUp:
                    if (Properties.Settings.Default.ScrollEnabled)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 30);
                    break;
                case Key.PageDown:
                    if (Properties.Settings.Default.ScrollEnabled)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 30);
                    break;

                // Rotate or Scroll
                case Key.Up:
                case Key.W:
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        if (Scroller.ComputedVerticalScrollBarVisibility == Visibility.Collapsed && !e.IsRepeat)
                            Rotate(false);
                        else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !e.IsRepeat)
                            Rotate(false);
                        else
                            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 30);
                    }
                    else if (!e.IsRepeat)
                        Rotate(false);
                    break;

                case Key.Down:
                case Key.S:
                    if (Properties.Settings.Default.ScrollEnabled)
                    {
                        if (Scroller.ComputedVerticalScrollBarVisibility == Visibility.Collapsed && !e.IsRepeat)
                            Rotate(true);
                        else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !e.IsRepeat)
                            Rotate(true);
                        else
                            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 30);
                    }
                    else if (!e.IsRepeat)
                        Rotate(true);
                    break;

                // Zoom
                case Key.Add:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Zoom(1, false);
                    else
                        Zoom(1, true);
                    break;
                case Key.Subtract:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Zoom(-1, false);
                    else
                        Zoom(-1, true);
                    break;
            }
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            // FastPicUpdate()
            if (e.Key == Key.Left || e.Key == Key.A || e.Key == Key.Right || e.Key == Key.D)
            {
                if (!FastPicRunning)
                    return;
                FastPicUpdate();
            }

            // Esc
            else if (e.Key == Key.Escape)
            {
                if (UserControls_Open())
                    Close_UserControls();
                else
                    Close();
            }

            // Ctrl + Q
            else if (e.Key == Key.Q && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Close();
            }

            // O, Ctrl + O
            else if (e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || e.Key == Key.O)
            {
                Open();
            }

            // X
            else if (e.Key == Key.X)
            {
                IsScrollEnabled = IsScrollEnabled ? false : true;
            }

            // F
            else if (e.Key == Key.F)
            {
                Flip();
            }

            // Shift + Delete
            else if (e.Key == Key.Delete && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                //DeleteFile(PicPath);
            }

            // Ctrl + C
            else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                CopyPic();
            }

            // Ctrl + V
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Paste();

            }

            // Ctrl + I
            else if (e.Key == Key.I && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                NativeMethods.ShowFileProperties(PicPath);
            }

            // Ctrl + P
            else if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Print(PicPath);
            }

            // Alt + Enter
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Enter))
            {
                //FullScreen();
            }

            // Space
            else if (e.Key == Key.Space)
            {
                CenterWindowOnScreen();
            }

            // F1
            else if (e.Key == Key.F1)
            {
                HelpWindow();
            }

            //F2
            else if (e.Key == Key.F2)
            {
                AboutWindow();
            }

            // F3
            else if (e.Key == Key.F3)
            {
                Open_In_Explorer();
            }

            // F6
            else if (e.Key == Key.F6)
            {
                ResetZoom();
            }

            // Home
            else if (e.Key == Key.Home)
            {
                Scroller.ScrollToHome();
            }

            // End
            else if (e.Key == Key.End)
            {
                Scroller.ScrollToEnd();
            }

            // Alt + Z
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && (e.SystemKey == Key.Z))
            {
                HideInterface();
            }
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                case MouseButton.Left:
                    if (autoScrolling)
                        StopAutoScroll();
                    break;
                case MouseButton.Middle:
                    if (!autoScrolling)
                        StartAutoScroll(e);
                    else
                        StopAutoScroll();
                    break;
                case MouseButton.XButton1:
                    Pic(false);
                    break;
                case MouseButton.XButton2:
                    Pic();
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Zoom, Scroll, Rotate and Flip

        private void StartAutoScroll(MouseButtonEventArgs e)
        {
            // Don't scroll if not scrollable
            if (Scroller.ComputedVerticalScrollBarVisibility == Visibility.Collapsed)
                return;

            autoScrolling = true;
            autoScrollOrigin = e.GetPosition(Scroller);

            ShowAutoScrollSign();
        }

        private void StopAutoScroll()
        {
            autoScrollTimer.Stop();
            //window.ReleaseMouseCapture();
            autoScrollTimer.Enabled = false;
            autoScrolling = false;
            autoScrollOrigin = null;
            HideAutoScrollSign();
        }

        /// <summary>
        /// Uses timer to scroll vertical up/down every seventh milisecond
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="E"></param>
        private async void AutoScrollTimerEvent(object sender, System.Timers.ElapsedEventArgs E)
        {
            if (autoScrollPos == null || autoScrollOrigin == null)
            {
                return;
            }
            await Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (autoScrollOrigin.HasValue)
                {
                    var offset = (autoScrollPos.Y - autoScrollOrigin.Value.Y) / 15;
                    //ToolTipStyle("pos = " + autoScrollPos.Y.ToString() + " origin = " + autoScrollOrigin.Value.Y.ToString()
                    //    + Environment.NewLine + "offset = " + offset, false);

                    if (autoScrolling)
                    {
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + offset);
                    }
                }
            }));
        }

        /// <summary>
        /// Pan and Zoom, reset zoom and double click to reset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Zoom_img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (autoScrolling)
            {
                //window.CaptureMouse();
                autoScrollOrigin = e.GetPosition(this);
                autoScrollTimer.Enabled = true;
                return;
            }
            if (e.ClickCount == 2)
            {
                ResetZoom();
                return;
            }
            if (!IsScrollEnabled)
            {
                img.CaptureMouse();
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
            }
        }

        /// <summary>
        /// Occurs when the users clicks on the img control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Zoom_img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (autoScrolling)
            {
                StopAutoScroll();
            }
            else
                img.ReleaseMouseCapture();
        }

        /// <summary>
        /// Used to drag image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Zoom_img_MouseMove(object sender, MouseEventArgs e)
        {
            if (autoScrolling)
            {
                autoScrollPos = e.GetPosition(Scroller);
                autoScrollTimer.Start();
            }

            if (!img.IsMouseCaptured || st.ScaleX == 1)
                return;

            // Needs solution to not drag image away from visible area
            var v = start - e.GetPosition(this);
            tt.X = origin.X - v.X;
            tt.Y = origin.Y - v.Y;
            e.Handled = true;
        }

        /// <summary>
        /// Zooms or scrolls with mousewheel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Zoom_img_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Properties.Settings.Default.ScrollEnabled && !autoScrolling)
            {
                if (e.Delta > 0) Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 45);
                else if (e.Delta < 0) Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 45);
            }

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !autoScrolling)
                Zoom(e.Delta, true);
            else if (!autoScrolling)
                Zoom(e.Delta, false);
        }

        /// <summary>
        /// Manipulates the required elements to allow zooming
        /// </summary>
        private void InitializeZoom()
        {
            img.RenderTransformOrigin = new Point(0.5, 0.5);
            img.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection {
                    new ScaleTransform(),
                    new TranslateTransform()
                }
            };

            imgBorder.IsManipulationEnabled = true;
            Scroller.ClipToBounds = img.ClipToBounds = true;

            st = (ScaleTransform)((TransformGroup)img.RenderTransform).Children.First(tr => tr is ScaleTransform);
            tt = (TranslateTransform)((TransformGroup)img.RenderTransform).Children.First(tr => tr is TranslateTransform);
        }

        /// <summary>
        /// Resets element values to their loaded values
        /// </summary>
        private void ResetZoom()
        {
            var scaletransform = new ScaleTransform();
            scaletransform.ScaleX = scaletransform.ScaleY = 1.0;
            img.LayoutTransform = scaletransform;

            st.ScaleX = st.ScaleY = 1;
            tt.X = tt.Y = 0;
            img.RenderTransformOrigin = new Point(0.5, 0.5);

            CloseToolTipStyle();
            isZoomed = false;

            ZoomFit(img.Source.Width, img.Source.Height);
            var titleString = TitleString((int)img.Source.Width, (int)img.Source.Height, FolderIndex);
            Title = titleString[0];
            Bar.Text = titleString[1];
            Bar.ToolTip = titleString[2];
        }

        /// <summary>
        /// Scales or zooms, depending on given values
        /// </summary>
        /// <param name="i"></param>
        /// <param name="zoomMode"></param>
        private void Zoom(int i, bool zoomMode)
        {

            #region Scale size

            // Scales the window with img.LayoutTransform
            if (zoomMode)
            {
                AspectRatio += i > 0 ? .01 : -.01;

                var scaletransform = new ScaleTransform();

                scaletransform.ScaleX = scaletransform.ScaleY = AspectRatio;
                img.LayoutTransform = scaletransform;
            }

            #endregion

            #region Pan and zoom

            else
            {

                // Get position where user points cursor
                var position = Mouse.GetPosition(img);

                // Use our position as starting point for zoom
                img.RenderTransformOrigin = new Point(position.X / img.ActualWidth, position.Y / img.ActualHeight);

                // Determine zoom speed
                var x = st.ScaleX > 1.3 ? .04 : .01;
                if (st.ScaleX > 1.5)
                    x += .007;
                if (st.ScaleX > 1.7)
                    x += .009;


                if (st.ScaleX >= 1.0 && st.ScaleX + x >= 1.0 || st.ScaleX - x >= 1.0)
                {
                    // Start zoom
                    st.ScaleY = st.ScaleX = AspectRatio += i > 0 ? x : -x;
                }

                if (st.ScaleX < 1.0)
                {
                    // Don't zoom less than 1.0, does not work so good...
                    st.ScaleX = st.ScaleY = AspectRatio = 1.0;
                }

            }

            #endregion

            isZoomed = true;

            #region Display updated values

            // Displays zoompercentage in the center window
            ToolTipStyle(ZoomPercentage, true);

            var titleString = TitleString((int)img.Source.Width, (int)img.Source.Height, FolderIndex);
            Title = titleString[0];
            Bar.Text = titleString[1];
            Bar.ToolTip = titleString[2];

            #endregion

        }

        /// <summary>
        /// Fits image size based on users screen resolution
        /// </summary>
        /// <param name="width">The pixel width of the image</param>
        /// <param name="height">The pixel height of the image</param>
        private void ZoomFit(double width, double height)
        {
            // Get max width and height, based on user's screen
            var maxWidth = Math.Min(SystemParameters.PrimaryScreenWidth - ComfySpace, width);
            var maxHeight = Math.Min((SystemParameters.FullPrimaryScreenHeight - 72), height);

            AspectRatio = Math.Min((maxWidth / width), (maxHeight / height));

            if (IsScrollEnabled)
            {
                // Calculate height based on width
                img.Width = maxWidth;
                img.Height = maxWidth * height / width;

                // Set scroller height to aspect ratio calculation
                Scroller.Height = (height * AspectRatio);

                // Update values
                xWidth = img.Width;
                xHeight = Scroller.Height;

            }
            else
            {
                // Reset Scroller's height to auto
                Scroller.Height = double.NaN;

                // Fit image by aspect ratio calculation
                // and update values
                img.Height = xHeight = (height * AspectRatio);
                img.Width = xWidth = (width * AspectRatio);

            }

            // Update TitleBar width to fit new size
            if (xWidth - 220 < 220)
                Bar.MaxWidth = 220;
            else
                Bar.MaxWidth = xWidth - 220;

            isZoomed = false;



            /*

                            _.._   _..---.
                         .-"    ;-"       \
                        /      /           |
                       |      |       _=   |
                       ;   _.-'\__.-')     |
                        `-'      |   |    ;
                                 |  /;   /      _,
                               .-.;.-=-./-""-.-` _`
                              /   |     \     \-` `,
                             |    |      |     |
                             |____|______|     |
                              \0 / \0   /      /
                           .--.-""-.`--'     .'
                          (#   )          ,  \
                          ('--'          /\`  \
                           \       ,,  .'      \
                            `-._    _.'\        \
                   jgs          `""`    \        \


                   So much math!
            */
        }

        /// <summary>
        /// Rotates the image the specified degrees and updates imageSettingsMenu value
        /// </summary>
        /// <param name="r"></param>
        private void Rotate(int r)
        {
            if (img.Source == null)
            {
                return;
            }
            var rt = new RotateTransform { Angle = Rotateint = r };

            // If it's flipped, keep it flipped when rotating
            if (Flipped)
            {
                var tg = new TransformGroup();
                var flip = new ScaleTransform { ScaleX = -1 };
                tg.Children.Add(flip);
                tg.Children.Add(rt);
                img.LayoutTransform = tg;
            }
            else
                img.LayoutTransform = rt;

            switch (r)
            {
                case 0:
                    imageSettingsMenu.Rotation0Button.IsChecked = true;
                    imageSettingsMenu.Rotation90Button.IsChecked = false;
                    imageSettingsMenu.Rotation180Button.IsChecked = false;
                    imageSettingsMenu.Rotation270Button.IsChecked = false;
                    break;

                case 180:
                    imageSettingsMenu.Rotation180Button.IsChecked = true;
                    imageSettingsMenu.Rotation90Button.IsChecked = false;
                    imageSettingsMenu.Rotation0Button.IsChecked = false;
                    imageSettingsMenu.Rotation270Button.IsChecked = false;
                    break;

                case 90:
                    imageSettingsMenu.Rotation90Button.IsChecked = true;
                    imageSettingsMenu.Rotation0Button.IsChecked = false;
                    imageSettingsMenu.Rotation180Button.IsChecked = false;
                    imageSettingsMenu.Rotation270Button.IsChecked = false;
                    break;

                case 270:
                    imageSettingsMenu.Rotation270Button.IsChecked = true;
                    imageSettingsMenu.Rotation90Button.IsChecked = false;
                    imageSettingsMenu.Rotation180Button.IsChecked = false;
                    imageSettingsMenu.Rotation0Button.IsChecked = false;
                    break;
                default:
                    imageSettingsMenu.Rotation0Button.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// Rotates left or right
        /// </summary>
        /// <param name="right"></param>
        private void Rotate(bool right)
        {
            if (img.Source == null)
            {
                return;
            }

            switch (Rotateint)
            {
                case 0:
                    if (right)
                        Rotate(270);
                    else
                        Rotate(90);
                    break;

                case 90:
                    if (right)
                        Rotate(0);
                    else
                        Rotate(180);
                    break;

                case 180:
                    if (right)
                        Rotate(90);
                    else
                        Rotate(270);
                    break;

                case 270:
                    if (right)
                        Rotate(180);
                    else
                        Rotate(0);
                    break;
            }
        }

        /// <summary>
        /// Flips the image
        /// </summary>
        private void Flip()
        {
            if (img.Source == null)
            {
                return;
            }

            var rt = new RotateTransform();
            var flip = new ScaleTransform();
            var tg = new TransformGroup();

            if (!Flipped)
            {
                flip.ScaleX = -1;
                Flipped = true;
            }
            else
            {
                flip.ScaleX = +1;
                Flipped = false;
            }

            switch (Rotateint)
            {
                case 0:
                    rt.Angle = 0;
                    break;

                case 90:
                    rt.Angle = 90;
                    break;

                case 180:
                    rt.Angle = 180;
                    break;

                case 270:
                    rt.Angle = 270;
                    break;
            }

            tg.Children.Add(flip);
            tg.Children.Add(rt);
            img.LayoutTransform = tg;
        }

        #endregion

        #region Interface logic

        #region UserControl Specifics

        /// <summary>
        /// Loads ClickArrow and adds it to the window
        /// </summary>
        private void LoadClickArrow(bool right)
        {
            if (right)
            {
                clickArrowRight = new ClickArrow(true)
                {
                    Focusable = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                bg.Children.Add(clickArrowRight);
            }
            else
            {
                clickArrowLeft = new ClickArrow(false)
                {
                    Focusable = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                bg.Children.Add(clickArrowLeft);
            }

        }

        /// <summary>
        /// Loads x2 and adds it to the window
        /// </summary>
        private void Loadx2()
        {
            x2 = new X2()
            {
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            bg.Children.Add(x2);

        }

        /// <summary>
        /// Loads FileMenu and adds it to the window
        /// </summary>
        private void LoadFileMenu()
        {
            fileMenu = new FileMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 152, 0)
            };
            
            bg.Children.Add(fileMenu);
        }

        /// <summary>
        /// Loads ImageSettingsMenu and adds it to the window
        /// </summary>
        private void LoadImageSettingsMenu()
        {
            imageSettingsMenu = new ImageSettings
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 112, 0)
            };

            bg.Children.Add(imageSettingsMenu);
        }

        /// <summary>
        /// Loads QuickSettingsMenu and adds it to the window
        /// </summary>
        private void LoadQuickSettingsMenu()
        {
            quickSettingsMenu = new QuickSettingsMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(17, 0, 0, 0)
            };

            bg.Children.Add(quickSettingsMenu);
        }

        private void LoadQuickSettingsSecondMenu()
        {
            
            SettingsButton = new lib.UserControls.Menus.QuickSettingsSecondMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(17, 0, 0, 0)
            };

            
        }

        // Tooltip

        /// <summary>
        /// Loads TooltipStyle and adds it to the window
        /// </summary>
        private void LoadTooltipStyle()
        {
            sexyToolTip = new SexyToolTip
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden
            };

            bg.Children.Add(sexyToolTip);
        }

        /// <summary>
        /// Shows a black tooltip on screen in a given time
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="center">If centered or on bottom</param>
        /// <param name="time">How long until it fades away</param>
        private void ToolTipStyle(object message, bool center, TimeSpan time)
        {
            sexyToolTip.Visibility = Visibility.Visible;

            if (center)
            {
                sexyToolTip.Margin = new Thickness(0, 0, 0, 0);
                sexyToolTip.VerticalAlignment = VerticalAlignment.Center;
            }
            else
            {
                sexyToolTip.Margin = new Thickness(0, 0, 0, 15);
                sexyToolTip.VerticalAlignment = VerticalAlignment.Bottom;
            }

            sexyToolTip.SexyToolTipText.Text = message.ToString();
            var anim = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            anim.Completed += (s, _) => AnimationHelper.Fade(sexyToolTip, TimeSpan.FromSeconds(1.5), time, 1, 0);

            sexyToolTip.BeginAnimation(OpacityProperty, anim);
        }

        /// <summary>
        /// Shows a black tooltip on screen for a small time
        /// </summary>
        /// <param name="message">The message to display</param>
        private void ToolTipStyle(object message, bool center = false)
        {
            ToolTipStyle(message, center, TimeSpan.FromSeconds(1));
        }

        private void CloseToolTipStyle()
        {
            sexyToolTip.Visibility = Visibility.Hidden;
        }


        // AjaxLoading

        /// <summary>
        /// Loads AjaxLoading and adds it to the window
        /// </summary>
        private void LoadAjaxLoading()
        {
            ajaxLoading = new AjaxLoading
            {
                Focusable = false,
                Opacity = 0
            };

            bg.Children.Add(ajaxLoading);
        }
        /// <summary>
        /// Start loading animation
        /// </summary>
        private void AjaxLoadingStart()
        {
            if (ajaxLoading.Opacity != 1)
            {
                AnimationHelper.Fade(ajaxLoading, 1, TimeSpan.FromSeconds(.2));
            }
        }

        /// <summary>
        /// End loading animation
        /// </summary>
        private void AjaxLoadingEnd()
        {
            if (ajaxLoading.Opacity != 0)
            {
                AnimationHelper.Fade(ajaxLoading, 0, TimeSpan.FromSeconds(.2));
            }
        }


        // AutoScrollSign

        /// <summary>
        /// Loads AutoScrollSign and adds it to the window
        /// </summary>
        private void LoadAutoScrollSign()
        {
            autoScrollSign = new AutoScrollSign
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                Width = 20,
                Height = 35
            };

            topLayer.Children.Add(autoScrollSign);
        }

        private void HideAutoScrollSign()
        {
            autoScrollSign.Visibility = Visibility.Collapsed;
            autoScrollSign.Opacity = 0;
        }

        private void ShowAutoScrollSign()
        {
            Canvas.SetTop(autoScrollSign, autoScrollOrigin.Value.Y);
            Canvas.SetLeft(autoScrollSign, autoScrollOrigin.Value.X);
            autoScrollSign.Visibility = Visibility.Visible;
            autoScrollSign.Opacity = 1;
        }


        /// <summary>
        /// Toggles whether ImageSettingsMenu is open or not with a fade animation 
        /// </summary>
        private static bool ImageSettingsMenuOpen
        {
            get { return imageSettingsMenuOpen; }
            set
            {
                imageSettingsMenuOpen = value;
                imageSettingsMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { imageSettingsMenu.Visibility = Visibility.Hidden; };
                }
                else
                    da.To = 1;
                if (imageSettingsMenu != null)
                    imageSettingsMenu.BeginAnimation(OpacityProperty, da);
            }
        }

        /// <summary>
        /// Toggles whether FileMenu is open or not with a fade animation 
        /// </summary>
        private static bool FileMenuOpen
        {
            get { return fileMenuOpen; }
            set
            {
                fileMenuOpen = value;
                fileMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { fileMenu.Visibility = Visibility.Hidden; };
                }
                else
                    da.To = 1;
                if (fileMenu != null)
                    fileMenu.BeginAnimation(OpacityProperty, da);
            }
        }

        /// <summary>
        /// Toggles whether QuickSettingsMenu is open or not with a fade animation 
        /// </summary>
        private static bool QuickSettingsMenuOpen
        {
            get { return quickSettingsMenuOpen; }
            set
            {
                quickSettingsMenuOpen = value;
                quickSettingsMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    Application.Current.Resources["ChosenColor"] = AnimationHelper.GetPrefferedColorOver();
                    da.To = 0;
                    da.Completed += delegate { quickSettingsMenu.Visibility = Visibility.Hidden; };
                }
                else
                    da.To = 1;
                if (quickSettingsMenu != null)
                    quickSettingsMenu.BeginAnimation(OpacityProperty, da);
            }
        }


        /// <summary>
        /// Check if any UserControls are open
        /// </summary>
        /// <returns></returns>
        private bool UserControls_Open()
        {
            if (ImageSettingsMenuOpen)
                return true;

            if (FileMenuOpen)
                return true;

            if (QuickSettingsMenuOpen)
                return true;

            return false;
        }

        /// <summary>
        /// Closes usercontrol menus
        /// </summary>
        private void Close_UserControls()
        {
            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;
        }

        private void Close_UserControls(object sender, RoutedEventArgs e)
        {
            Close_UserControls();
        }

        private void Toggle_open_menu(object sender, RoutedEventArgs e)
        {
            FileMenuOpen = !FileMenuOpen;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;
        }

        private void Toggle_image_menu(object sender, RoutedEventArgs e)
        {
            ImageSettingsMenuOpen = !ImageSettingsMenuOpen;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;
        }

        private void Toggle_quick_settings_menu(object sender, RoutedEventArgs e)
        {
            QuickSettingsMenuOpen = !QuickSettingsMenuOpen;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

        }

        #endregion

        #region Manipulate Interface

        /// <summary>
        /// Toggle between "Alt" interface and default
        /// </summary>
        private void HideInterface()
        {
            if (Properties.Settings.Default.WindowStyle == "Default")
            {
                TitleBar.Visibility =
                LowerBar.Visibility =
                LeftBorderRectangle.Visibility =
                RightBorderRectangle.Visibility
                = Visibility.Collapsed;

                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                Visibility.Visible;

                Properties.Settings.Default.WindowStyle = "Alt";

                activityTimer.Start();

            }
            else
            {
                TitleBar.Visibility =
                LowerBar.Visibility =
                LeftBorderRectangle.Visibility =
                RightBorderRectangle.Visibility
                = Visibility.Visible;

                clickArrowLeft.Visibility =
                clickArrowRight.Visibility =
                x2.Visibility =
                Visibility.Collapsed;

                Properties.Settings.Default.WindowStyle = "Default";
                activityTimer.Stop();
            }

        }

        private async void FadeControlsAsync(bool show)
        {
            var fadeTo = show ? 1 : 0;

            await Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (Properties.Settings.Default.WindowStyle == "Alt")
                {
                    if (clickArrowRight != null && clickArrowLeft != null && x2 != null)
                    {
                        AnimationHelper.Fade(clickArrowLeft, fadeTo, TimeSpan.FromSeconds(.5));
                        AnimationHelper.Fade(clickArrowRight, fadeTo, TimeSpan.FromSeconds(.5));
                        AnimationHelper.Fade(x2, fadeTo, TimeSpan.FromSeconds(.5));
                    }
                }

                // Hide/show Scrollbar
                ScrollbarFade(show);
            }));
        }

        private void ActivityTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            FadeControlsAsync(false);
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            activityTimer.Stop();
            FadeControlsAsync(true);
        }

        private void MainWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            activityTimer.Start();
        }

        private void ScrollbarFade(bool show)
        {
            var s = Scroller.Template.FindName("PART_VerticalScrollBar", Scroller) as System.Windows.Controls.Primitives.ScrollBar;

            if (show)
            {
                AnimationHelper.Fade(s, 1, TimeSpan.FromSeconds(.7));
            }
            else
            {
                AnimationHelper.Fade(s, 0, TimeSpan.FromSeconds(1));
            }
        }

        /// <summary>
        /// Returns string with file name, folder position,
        /// zoom, aspect ratio, resolution and file size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static string[] TitleString(int width, int height, int index)
        {
            var s1 = new StringBuilder();
            s1.Append(AppName).Append(" - ").Append(Path.GetFileName(Pics[index])).Append(" ").Append(index + 1).Append("/").Append(Pics.Count).Append(" files")
                    .Append(" (").Append(width).Append(" x ").Append(height).Append(StringAspect(width, height)).Append(GetSizeReadable(new FileInfo(Pics[index]).Length)).Append(Zoomed);

            var array = new string[3];
            array[0] = s1.ToString();
            s1.Remove(0, AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            s1.Replace(Path.GetFileName(Pics[index]), Pics[index]);
            array[2] = s1.ToString();
            return array;
        }

        /// <summary>
        /// Returns string with file name,
        /// zoom, aspect ratio and resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string[] TitleString(int width, int height, string path)
        {
            var s1 = new StringBuilder();
            s1.Append(AppName).Append(" - ").Append(path).Append(" (").Append(width).Append(" x ").Append(height).Append(", ").Append(StringAspect(width, height)).Append(") ").Append(Zoomed);

            var array = new string[2];
            array[0] = s1.ToString();
            s1.Remove(0, AppName.Length + 3);   // Remove AppName + " - "
            array[1] = s1.ToString();
            return array;
        }

        /// <summary>
        /// Toggles scroll and displays it with TooltipStle
        /// </summary>
        private bool IsScrollEnabled
        {
            get { return Properties.Settings.Default.ScrollEnabled; }
            set
            {
                Properties.Settings.Default.ScrollEnabled = value;
                Scroller.VerticalScrollBarVisibility = value ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
                if (!freshStartup && !string.IsNullOrEmpty(PicPath))
                {
                    ZoomFit(img.Source.Width, img.Source.Height);
                    ToolTipStyle(value ? "Scrolling enabled" : "Scrolling disabled");
                }
            }
        }

        #endregion

        #region Windows

        private void AboutWindow()
        {
            Window window = new About
            {
                Width = Width,
                Height = Height,
                Opacity = 0,
                Owner = Application.Current.MainWindow,
            };

            var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            window.BeginAnimation(OpacityProperty, animation);

            window.ShowDialog();
        }

        private void HelpWindow()
        {
            Window window = new Help
            {
                Width = Width,
                Height = Height,
                Opacity = 0,
                Owner = Application.Current.MainWindow,
            };

            var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            window.BeginAnimation(OpacityProperty, animation);
            window.Show();
        }

        #endregion

        #region MouseOver Button Events
        /*
        
            Adds MouseOver events for the given elements with the AnimationHelper.
            Changes color depending on the users settings.

        */

        // Logo Mouse Over
        private void LogoMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, pBrush, false);
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, iBrush, false);
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, cBrush, false);
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, vBrush, false);
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, iiBrush, false);
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, eBrush, false);
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, wBrush, false);
        }

        private void LogoMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, pBrush, false);
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, iBrush, false);
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, cBrush, false);
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, vBrush, false);
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, iiBrush, false);
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, eBrush, false);
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, wBrush, false);
        }

        private void LogoMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(pBrush, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(iBrush, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(cBrush, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(vBrush, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(iiBrush, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(eBrush, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(wBrush, false);
        }

        // Close Button
        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, false);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
        }

        // MaxButton
        private void MaxButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, MaxButtonBrush, false);
        }

        private void MaxButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(MaxButtonBrush, false);
        }

        private void MaxButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, MaxButtonBrush, false);
        }

        // MinButton
        private void MinButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, MinButtonBrush, false);
        }

        private void MinButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(MinButtonBrush, false);
        }

        private void MinButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, MinButtonBrush, false);
        }

        // LeftButton
        private void LeftButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                LeftArrowFill,
                false
            );
        }

        private void LeftButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(LeftArrowFill, false);
        }

        private void LeftButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                LeftArrowFill,
                false
            );
        }

        // RightButton
        private void RightButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                RightArrowFill,
                false
            );
        }

        private void RightButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(RightArrowFill, false);
        }


        private void RightButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                RightArrowFill,
                false
            );
        }

        // OpenMenuButton
        private void OpenMenuButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                FolderFill,
                false
            );
        }

        private void OpenMenuButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FolderFill, false);
        }

        private void OpenMenuButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                FolderFill,
                false
            );
        }

        // ImageButton
        private void ImageButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ImagePath1Fill,
                false
            );
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ImagePath2Fill,
                false
            );
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ImagePath3Fill,
                false
            );
            //AnimationHelper.MouseEnterColorEvent(
            //    mainColor.A,
            //    mainColor.R,
            //    mainColor.G,
            //    mainColor.B,
            //    ImagePath4Fill,
            //    false
            //);
        }

        private void ImageButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath1Fill, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath2Fill, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath3Fill, false);
            //AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath4Fill, false);
        }

        private void ImageButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ImagePath1Fill,
                false
            );
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ImagePath2Fill,
                false
            );
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                ImagePath3Fill,
                false
            );
            //AnimationHelper.MouseLeaveColorEvent(
            //    mainColor.A,
            //    mainColor.R,
            //    mainColor.G,
            //    mainColor.B,
            //    ImagePath4Fill,
            //    false
            //);
        }

        // SettingsButton
        private void SettingsButtonButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                SettingsButtonFill,
                false
            );
        }

        private void SettingsButtonButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SettingsButtonFill, false);
        }

        private void SettingsButtonButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                SettingsButtonFill,
                false
            );
        }

        #endregion

        #endregion

        #region File Methods

        /// <summary>
        /// Copy image location to clipboard
        /// </summary>
        private void CopyText()
        {
            Clipboard.SetText(PicPath);
            ToolTipStyle(TxtCopy);
        }

        /// <summary>
        /// Add image to clipboard
        /// </summary>
        private void CopyPic()
        {
            // Copy pic if from web
            if (string.IsNullOrWhiteSpace(PicPath) || Uri.IsWellFormedUriString(PicPath, UriKind.Absolute))
            {
                var source = img.Source as BitmapImage;
                if (source != null)
                    Clipboard.SetImage(source);
                else
                    return;
            }
            else
            {
                var paths = new System.Collections.Specialized.StringCollection { PicPath };
                Clipboard.SetFileDropList(paths);
            }
            ToolTipStyle(FileCopy);
        }

        /// <summary>
        /// Retrieves the data from the clipboard and attemps to load image, if possible
        /// </summary>
        private void Paste()
        {
            #region file

            if (Clipboard.ContainsFileDropList()) // If Clipboard has one or more files
            {
                var files = Clipboard.GetFileDropList().Cast<string>().ToArray();

                if (!string.IsNullOrWhiteSpace(PicPath) &&
                    Path.GetDirectoryName(files[0]) == Path.GetDirectoryName(PicPath))
                    Pic(Pics.IndexOf(files[0]));
                else
                    Pic(files[0]);

                if (files.Length > 0)
                {
                    Parallel.For(1, files.Length, x =>
                    {
                        var myProcess = new Process
                        {
                            StartInfo = { FileName = Assembly.GetExecutingAssembly().Location, Arguments = files[x] }
                        };
                        myProcess.Start();
                    });
                }

                return;
            }

            #endregion

            #region Clipboard Image

            if (Clipboard.ContainsImage())
            {
                Pic(Clipboard.GetImage(), "Clipboard Image");
                return;
            }

            #endregion

            #region text/string/adddress

            var s = Clipboard.GetText(TextDataFormat.Text);

            if (string.IsNullOrEmpty(s))
                return;

            if (FilePathHasInvalidChars(s))
                MakeValidFileName(s);

            s = s.Replace("\"", "");
            s = s.Trim();

            if (File.Exists(s))
            {
                if (Path.GetDirectoryName(s) == Path.GetDirectoryName(PicPath))
                    Pic(Pics.IndexOf(s));
                else
                    Pic(s);
            }
            else if (Directory.Exists(s))
            {
                ChangeFolder();
                Pics = FileList(s);
                Pic(Pics[0]);
            }
            else if (Uri.IsWellFormedUriString(s, UriKind.Absolute)) // Check if from web
                PicWeb(s);

            else ToolTipStyle("An error occured while trying to paste file");

            #endregion
        }

        /// <summary>
        /// Opens image in File Explorer
        /// </summary>
        private void Open_In_Explorer()
        {
            if (!File.Exists(PicPath) || img.Source == null)
            {
                ToolTipStyle("Error, File does not exist, or something went wrong...", true);
                return;
            }
            try
            {
                Close_UserControls();
                ToolTipStyle(ExpFind);
                Process.Start("explorer.exe", "/select,\"" + PicPath + "\"");
            }
            catch (InvalidCastException) { }
        }

        /// <summary>
        /// Open a file dialog where user can select a supported file
        /// </summary>
        private void Open()
        {


            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = FilterFiles,
                Title = "Open image - PicView"
            };
            if (dlg.ShowDialog() == true)
            {
                Pic(dlg.FileName);

                if (string.IsNullOrWhiteSpace(PicPath))
                    PicPath = dlg.FileName;
            }
            else return;

            Close_UserControls();
        }

        /// <summary>
        /// Open a file dialog where user can save the selected file in a supported filtype.
        /// </summary>
        private void SaveFiles()
        {
            var Savedlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = FilterFiles,
                Title = "Save image - PicView",
                FileName = Path.GetFileName(PicPath)
            };

            if (!string.IsNullOrEmpty(PicPath))
            {
                if (Savedlg.ShowDialog() == true)
                {
                    if (TrySaveImage(Rotateint, Flipped, PicPath, Savedlg.FileName) == false)
                    {
                        ToolTipStyle("Error, File didnt get saved - File not Found.", true);
                    }
                }
                else
                    return;

                if (Path.GetDirectoryName(Savedlg.FileName) == Path.GetDirectoryName(PicPath))
                    Pics = FileList(PicPath);

                Close_UserControls();
            }
            else
            {
                ToolTipStyle("Error, File does not exist, or something went wrong...", true);
            }

        }

        #endregion     
    }
}
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

            if (Properties.Settings.Default.WindowStyle == 2)
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
            LoadFunctionsMenu();
            LoadAutoScrollSign();
            LoadClickArrow(true);
            LoadClickArrow(false);
            Loadx2();

            if (Properties.Settings.Default.PicGalleryEnabled)
            {
                LoadPicGallery();
            }

            // Update UserControl values
            backgroundBorderColor = (Color)Application.Current.Resources["BackgroundColorFade"];
            mainColor = (Color)Application.Current.Resources["MainColor"];
            quickSettingsMenu.ToggleScroll.IsChecked = IsScrollEnabled;
            if (SizeMode)
                quickSettingsMenu.SetFit.IsChecked = true;
            else
                quickSettingsMenu.SetCenter.IsChecked = true;

            // Update WindowStyle
            if (Properties.Settings.Default.WindowStyle == 2)
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
                RecentFiles.Initialize();

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
                MinButton.Click += (s, x) => SystemCommands.MinimizeWindow(this);

                // MaxButton
                MaxButton.PreviewMouseLeftButtonDown += MaxButtonMouseButtonDown;
                MaxButton.MouseEnter += MaxButtonMouseOver;
                MaxButton.MouseLeave += MaxButtonMouseLeave;
                MaxButton.Click += (s, x) => Maximize_Restore();

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
                    //Close_UserControls();
                };
                quickSettingsMenu.ToggleScroll.Unchecked += (s, x) =>
                {
                    IsScrollEnabled = false;
                    //Close_UserControls();
                };
                quickSettingsMenu.SetFit.Click += (s, x) => { SizeMode = true; };
                quickSettingsMenu.SetCenter.Click += (s, x) => { SizeMode = false; };
                quickSettingsMenu.SettingsButton.Click += (s, x) => AllSettingsWindow();

                //FunctionMenu
                FunctionMenuButton.PreviewMouseLeftButtonDown += FunctionMenuButtonButtonMouseButtonDown;
                FunctionMenuButton.MouseEnter += FunctionMenuButtonButtonMouseOver;
                FunctionMenuButton.MouseLeave += FunctionMenuButtonButtonMouseLeave;
                FunctionMenuButton.Click += Toggle_Functions_menu;
                functionsMenu.CloseButton.Click += Toggle_Functions_menu;
                functionsMenu.Help.Click += (s, x) => HelpWindow();
                functionsMenu.About.Click += (s, x) => AboutWindow();
                functionsMenu.ClearButton.Click += (s, x) => Unload();
                functionsMenu.ClearButton.Click += Toggle_Functions_menu;
                functionsMenu.FileDetailsButton.Click += (s, x) => NativeMethods.ShowFileProperties(PicPath);
                functionsMenu.FileDetailsButton.Click += Toggle_Functions_menu;
                functionsMenu.DeleteButton.Click += (s, x) => DeleteFile(PicPath, true);
                functionsMenu.DeletePermButton.Click += (s, x) => DeleteFile(PicPath, false);
                functionsMenu.ReloadButton.Click += (s, x) => Reload();
                functionsMenu.ReloadButton.Click += Toggle_Functions_menu;
                functionsMenu.RenameFileButton.Click += (s, x) => RenameFile();
                functionsMenu.RenameFileButton.Click += Toggle_Functions_menu;
                functionsMenu.ResetZoomButton.Click += (s, x) => ResetZoom();


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
                picGallery.PreviewItemClick += PicGallery_PreviewItemClick;
                picGallery.ItemClick += PicGallery_ItemClick;

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
                    Interval = 100,
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


            cm.Items.Add(new Separator());
            var recentcm = new MenuItem
            {
                Header = "Recent files"
            };
            var recentcmIcon = new System.Windows.Shapes.Path();
            recentcmIcon.Data = Geometry.Parse("M288,48H136c-22.092,0-40,17.908-40,40v336c0,22.092,17.908,40,40,40h240c22.092,0,40-17.908,40-40V176L288,48z M272,192 V80l112, 112H272z");
            recentcmIcon.Stretch = Stretch.Fill;
            recentcmIcon.Width = recentcmIcon.Height = 12;
            recentcmIcon.Fill = scbf;
            recentcm.Icon = recentcmIcon;
            recentcm.MouseEnter += (xx, xxx) => Recentcm_MouseEnter(recentcm);
            cm.Items.Add(recentcm);

            var sortcm = new MenuItem
            {
                Header = "Sort files by"
            };
            var sortcmIcon = new System.Windows.Shapes.Path();
            sortcmIcon.Data = Geometry.Parse("M666 481q-60 92-137 273-22-45-37-72.5t-40.5-63.5-51-56.5-63-35-81.5-14.5h-224q-14 0-23-9t-9-23v-192q0-14 9-23t23-9h224q250 0 410 225zm1126 799q0 14-9 23l-320 320q-9 9-23 9-13 0-22.5-9.5t-9.5-22.5v-192q-32 0-85 .5t-81 1-73-1-71-5-64-10.5-63-18.5-58-28.5-59-40-55-53.5-56-69.5q59-93 136-273 22 45 37 72.5t40.5 63.5 51 56.5 63 35 81.5 14.5h256v-192q0-14 9-23t23-9q12 0 24 10l319 319q9 9 9 23zm0-896q0 14-9 23l-320 320q-9 9-23 9-13 0-22.5-9.5t-9.5-22.5v-192h-256q-48 0-87 15t-69 45-51 61.5-45 77.5q-32 62-78 171-29 66-49.5 111t-54 105-64 100-74 83-90 68.5-106.5 42-128 16.5h-224q-14 0-23-9t-9-23v-192q0-14 9-23t23-9h224q48 0 87-15t69-45 51-61.5 45-77.5q32-62 78-171 29-66 49.5-111t54-105 64-100 74-83 90-68.5 106.5-42 128-16.5h256v-192q0-14 9-23t23-9q12 0 24 10l319 319q9 9 9 23z");
            sortcmIcon.Stretch = Stretch.Fill;
            sortcmIcon.Width = sortcmIcon.Height = 12;
            sortcmIcon.Fill = scbf;
            sortcm.Icon = sortcmIcon;
            var sortcmChild0 = new RadioButton();
            sortcmChild0.Content = "File name";
            sortcmChild0.Click += (s, x) => 
            {
                Properties.Settings.Default.SortPreference = 0;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild0);
            var sortcmChild1 = new RadioButton();
            sortcmChild1.Content = "File Size";
            sortcmChild1.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 1;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild1);
            var sortcmChild2 = new RadioButton();
            sortcmChild2.Content = "Creation time";
            sortcmChild2.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 2;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild2);
            var sortcmChild3 = new RadioButton();
            sortcmChild3.Content = "File extension";
            sortcmChild3.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 3;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild3);
            var sortcmChild4 = new RadioButton();
            sortcmChild4.Content = "Last access time";
            sortcmChild4.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 4;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild4);
            var sortcmChild5 = new RadioButton();
            sortcmChild5.Content = "Last write time";
            sortcmChild5.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 5;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild5);
            var sortcmChild6 = new RadioButton();
            sortcmChild6.Content = "Random";
            sortcmChild6.Click += (s, x) =>
            {
                Properties.Settings.Default.SortPreference = 6;
                if (!string.IsNullOrWhiteSpace(PicPath))
                    Pics = FileList(Path.GetDirectoryName(PicPath));
            };
            sortcm.Items.Add(sortcmChild6);
            cm.Items.Add(sortcm);
            cm.Items.Add(new Separator());

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
            cm.Items.Add(new Separator());

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

            var MovetoRecycleBin = new MenuItem
            {
                Header = "Delete picture",
                InputGestureText = "Delete"
            };
            var MovetoRecycleBinIcon = new System.Windows.Shapes.Path();
            MovetoRecycleBinIcon.Data = Geometry.Parse("M836 1169l-15 368-2 22-420-29q-36-3-67-31.5t-47-65.5q-11-27-14.5-55t4-65 12-55 21.5-64 19-53q78 12 509 28zm-387-586l180 379-147-92q-63 72-111.5 144.5t-72.5 125-39.5 94.5-18.5 63l-4 21-190-357q-17-26-18-56t6-47l8-18q35-63 114-188l-140-86zm1231 517l-188 359q-12 29-36.5 46.5t-43.5 20.5l-18 4q-71 7-219 12l8 164-230-367 211-362 7 173q170 16 283 5t170-33zm-785-924q-47 63-265 435l-317-187-19-12 225-356q20-31 60-45t80-10q24 2 48.5 12t42 21 41.5 33 36 34.5 36 39.5 32 35zm655 307l212 363q18 37 12.5 76t-27.5 74q-13 20-33 37t-38 28-48.5 22-47 16-51.5 14-46 12q-34-72-265-436l313-195zm-143-226l142-83-220 373-419-20 151-86q-34-89-75-166t-75.5-123.5-64.5-80-47-46.5l-17-13 405 1q31-3 58 10.5t39 28.5l11 15q39 61 112 190z");
            MovetoRecycleBinIcon.Stretch = Stretch.Fill;
            MovetoRecycleBinIcon.Width = MovetoRecycleBinIcon.Height = 12;
            MovetoRecycleBinIcon.Fill = scbf;
            MovetoRecycleBin.Icon = MovetoRecycleBinIcon;
            MovetoRecycleBin.Click += (s, x) => DeleteFile(PicPath, true);
            cm.Items.Add(MovetoRecycleBin);

            cm.Items.Add(new Separator());
            var clcm = new MenuItem
            {
                Header = "Close",
                InputGestureText = "Esc"
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
                Header = "Close",
                InputGestureText = "Esc"
            };
            clX2x.Click += (s, x) => Close();
            closeX2.Items.Add(clX2x);

            x2.ContextMenu = closeX2;

            switch (Properties.Settings.Default.SortPreference)
            {
                case 0:
                    sortcmChild0.IsChecked = true;
                    break;
                case 1:
                    sortcmChild1.IsChecked = true;
                    break;
                case 2:
                    sortcmChild2.IsChecked = true;
                    break;
                case 3:
                    sortcmChild3.IsChecked = true;
                    break;
                case 4:
                    sortcmChild4.IsChecked = true;
                    break;
                case 5:
                    sortcmChild5.IsChecked = true;
                    break;
                case 6:
                    sortcmChild6.IsChecked = true;
                    break;
                default:
                    sortcmChild0.IsChecked = true;
                    break;
            }

            #endregion           

            Recentcm_MouseEnter(recentcm);
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

            NoProgress();
            AjaxLoadingEnd();
        }


        protected override void OnRenderSizeChanged(SizeChangedInfo size)
        {
            base.OnRenderSizeChanged(size);

            if (Properties.Settings.Default.WindowStyle == 0 || Properties.Settings.Default.WindowStyle == 2)
            {
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
                Maximize_Restore();
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
            if (Properties.Settings.Default.WindowStyle == 4)
                Properties.Settings.Default.WindowStyle = 0;

            Properties.Settings.Default.Save();
            DeleteTempFiles();
            RecentFiles.WriteToFile();
            Environment.Exit(0);
        }


        /// <summary>
        /// Maximizes/restore window
        /// </summary>
        private void Maximize_Restore()
        {
            // Maximize
            if (WindowState == WindowState.Normal)
            {
                // Update new setting and sizing
                SizeMode = false;

                // Tell Windows that it's maximized
                WindowState = WindowState.Maximized;
                SystemCommands.MaximizeWindow(this);

                // Update button to reflect change
                MaxButton.ToolTip = "Restore";
                MaxButtonPath.Data = Geometry.Parse("M143-7h428v286h-428v-286z m571 286h286v428h-429v-143h54q37 0 63-26t26-63v-196z m429 482v-536q0-37-26-63t-63-26h-340v-197q0-37-26-63t-63-26h-536q-36 0-63 26t-26 63v536q0 37 26 63t63 26h340v197q0 37 26 63t63 26h536q36 0 63-26t26-63z");
            }

            // Restore
            else if (WindowState == WindowState.Maximized)
            {
                // Update new setting and sizing
                SizeMode = true;

                // Tell Windows that it's normal
                WindowState = WindowState.Normal;
                SystemCommands.RestoreWindow(this);

                // Update button to reflect change
                MaxButton.ToolTip = "Maximize";
                MaxButtonPath.Data = Geometry.Parse("M143 64h714v429h-714v-429z m857 625v-678q0-37-26-63t-63-27h-822q-36 0-63 27t-26 63v678q0 37 26 63t63 27h822q37 0 63-27t26-63z");
            }

        }


        /// <summary>
        /// Set whether to fit window to image or image to window
        /// </summary>
        private bool SizeMode
        {
            get
            {
                return Properties.Settings.Default.WindowStyle == 0 || Properties.Settings.Default.WindowStyle == 2;
            }
            set
            {
                if (value)
                {
                    SizeToContent = SizeToContent.WidthAndHeight;

                    if (Properties.Settings.Default.WindowStyle != 2)
                        Properties.Settings.Default.WindowStyle = 0;
                    quickSettingsMenu.SetFit.IsChecked = true;
                }
                else
                {
                    SizeToContent = SizeToContent.Manual;

                    Properties.Settings.Default.WindowStyle = 4;
                    quickSettingsMenu.SetCenter.IsChecked = true;
                }
                if (img.Source != null)
                    ZoomFit(img.Source.Width, img.Source.Height);
            }
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

            // Handle if from web
            if (!File.Exists(path))
            {
                if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                    PicWeb(path);
                else
                    Unload();
                return;
            }

            // If count not correct or just started, get values
            if (Pics.Count <= FolderIndex || FolderIndex < 0 || freshStartup)
            {
                await GetValues(path);
            }

            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            else if (!string.IsNullOrWhiteSpace(PicPath) && Path.GetDirectoryName(path) != Path.GetDirectoryName(PicPath))
            {
                // Reset zipped values
                if (!string.IsNullOrWhiteSpace(TempZipPath))
                {
                    DeleteTempFiles();
                    TempZipPath = string.Empty;
                    RecentFiles.SetZipped(string.Empty, false);
                }

                // Reset old values and get new
                ChangeFolder();
                await GetValues(path);
            }

            // If no need to reset values, get index
            else
            {
                FolderIndex = Pics.IndexOf(path);
            }

            // Fix large archive extraction error
            if (Pics.Count == 0)
            {
                bool foo = await RecoverFailedArchiveAsync();
                if (!foo)
                    return;
            }

            // Navigate to picture using obtained index
            Pic(FolderIndex);


            if (freshStartup)
                freshStartup = false;

            // Fix loading bug
            if (ajaxLoading.Opacity != 1 && canNavigate)
            {
                AjaxLoadingEnd();
            }

            if (Properties.Settings.Default.PicGalleryEnabled)
            {
                if (picGallery == null)
                    LoadPicGallery();

                if (!picGallery.LoadComplete)
                    picGallery.Load();
            }
        }


        /// <summary>
        /// Loads image based on overloaded int.
        /// Possible out of range error if used inappropriately.
        /// </summary>
        /// <param name="x"></param>
        private async void Pic(int x)
        {
            //var stopWatch = new Stopwatch();
            //stopWatch.Start();

            // If file was removed, fix it
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

            if (pic == null)
            {
                Title = Bar.Text = Loading;
                Bar.ToolTip = Loading;

                if (Properties.Settings.Default.WindowStyle == 2)
                    AjaxLoadingStart();

                // Dissallow changing image while loading
                canNavigate = false;

                // Load new value manually
                await Task.Run(() => pic = RenderToBitmapSource(Pics[x], Extension));

                // If pic is still null, image can't be rendered
                if (pic == null)
                {
                    PicErrorFix(x);
                    return;
                }
            }

            // Scroll to top if scroll enabled
            if (IsScrollEnabled)
                Scroller.ScrollToTop();

            // Prevent picture from being flipped if previous is
            if (Flipped)
                Flip();

            // Fit image to new values
            ZoomFit(pic.PixelWidth, pic.PixelHeight);

            // If gif, use XamlAnimatedGif to animate it
            if (Extension == ".gif")
                XamlAnimatedGif.AnimationBehavior.SetSourceUri(img, new Uri(Pics[x]));
            else
                img.Source = pic;

            // Update values
            var titleString = TitleString(pic.PixelWidth, pic.PixelHeight, x);
            Title = titleString[0];
            Bar.Text = titleString[1];
            Bar.ToolTip = titleString[2];

            
            PicPath = Pics[x];
            canNavigate = true;
            Progress(x, Pics.Count);
            FolderIndex = x;
            if (!freshStartup)
                RecentFiles.Add(Pics[x]);

            // Preload images           
            bool? reverse;

            // Only preload every second entry
            // and determine if going backwards or forwards
            if (PreloadCount > 1 || freshStartup)
                reverse = false;
            else if (PreloadCount < 0)
                reverse = true;
            else
                reverse = null;

            if (reverse.HasValue)
            {
                await Task.Run(() =>
                {
                    Preloader.PreLoad(x, reverse.Value);
                    PreloadCount = 0;

                    if (x < Pics.Count)
                    {
                        if (!Preloader.Contains(Pics[x]))
                            Preloader.Add(pic, Pics[x]);
                    }
                });
            }

            // Stop AjaxLoading if it's shown
            AjaxLoadingEnd();

            //stopWatch.Stop();
            //ToolTipStyle(stopWatch.Elapsed);
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

            var titleString = TitleString(pic.PixelWidth, pic.PixelHeight, imageName);
            Title = titleString[0];
            Bar.Text = titleString[1];
            Bar.ToolTip = titleString[1];

            NoProgress();
            PicPath = string.Empty;

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

            if (picGallery != null)
            {
                if (picGallery.open)
                    return;
            }

            // Go to first or last
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
            // Go to next or previous
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
                Bar.ToolTip =
                Title =
                Bar.Text = "Image " + (FolderIndex + 1) + " of " + Pics.Count;

                img.Width = xWidth;
                img.Height = xHeight;

                img.Source = GetBitmapSourceThumb(Pics[FolderIndex]);
            }));
            Progress(FolderIndex, Pics.Count);
            FastPicRunning = true;
        }

        private void FastPic()
        {
            Bar.ToolTip =
            Title =
            Bar.Text = "Image " + (FolderIndex + 1) + " of " + Pics.Count;

            img.Width = xWidth;
            img.Height = xHeight;

            img.Source = GetBitmapSourceThumb(Pics[FolderIndex]);

            Progress(FolderIndex, Pics.Count);
            FastPicRunning = true;
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
            catch (Exception)
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

            Pic(pic, path);
            PicPath = path;
            RecentFiles.Add(path);
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
        /// Attemps to fix Pics list by removing invalid files
        /// </summary>
        /// <param name="x">The index to start from</param>
        private bool PicErrorFix(int x)
        {
            if (Pics.Count < 0)
            {
                ToolTipStyle("Unexpected error", true, TimeSpan.FromSeconds(3));
                Unload();
                return false;
            }
            else if (x >= Pics.Count)
            {
                if (Pics.Count >= 1)
                {
                    Pic(Pics[0]);
                    return true;
                }
                else
                {
                    Unload();
                    return false;
                }
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

            if (FolderIndex < Pics.Count)
            {
                if (File.Exists(Pics[FolderIndex]))
                {
                    Pic(FolderIndex);
                    PreloadCount++;
                    return true;
                }
            }
            try
            {
                var dir = Path.GetDirectoryName(PicPath);
                if (Directory.Exists(dir))
                {
                    Pics = FileList(dir);
                }
            }
            catch (Exception)
            {
                return false;
            }
            if (Pics.Count < 0)
            {
                Unload();
                return false;
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
                // Unexped result
                Reload(true);
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
                    if (!string.IsNullOrWhiteSpace(xPicPath))
                    {
                        Reload(true);
                        return false;
                    }
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


        /// <summary>
        /// Refresh the current list of pics and reload them if there is some missing or changes.
        /// </summary>
        private void Reload(bool fromBackup = false)
        {
            if (img.Source == null)
                return;

            if (fromBackup && string.IsNullOrWhiteSpace(xPicPath))
            {
                Unload();
                return;
            }

            var x = fromBackup ? xPicPath : PicPath;

            if (File.Exists(x))
            {
                // Force reloading values by setting freshStartup to true
                freshStartup = true;
                Pic(x);

                // Reset
                if (isZoomed)
                    ResetZoom();
                if (Flipped)
                    Flip();
                if (Rotateint != 0)
                    Rotate(0);
            }
            else
            {
                var y = fromBackup ? xFolderIndex : FolderIndex;
                PicErrorFix(y);
            }
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
        /// Determine if supported files for drag and drop operation
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
        }


        /// <summary>
        /// Show image or thumbnail preview on drag enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_DraEnter(object sender, DragEventArgs e)
        {
            // Error handling
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

            // Do nothing for invalid files
            if (!Drag_Drop_Check(files).HasValue)
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
            img.Source = Preloader.Contains(files[0]) ? Preloader.Load(files[0]) : GetBitmapSourceThumb(files[0]);
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
                //case Key.LWin:
                //case Key.RWin:
                //    SizeMode = false;
                //    break;

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
                        //FastPic();
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
                    }
                    else if (canNavigate)
                    {
                        if (FolderIndex == 0)
                            FolderIndex = Pics.Count - 1;
                        else
                            FolderIndex--;

                        fastPicTimer.Start();
                        //FastPic();
                    }
                    break;

                // Scroll
                case Key.PageUp:
                    if (picGallery != null)
                    {
                        if (picGallery.open)
                        {
                            picGallery.ScrollTo(true, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    if (Properties.Settings.Default.ScrollEnabled)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 30);
                    break;
                case Key.PageDown:
                    if (picGallery != null)
                    {
                        if (picGallery.open)
                        {
                            picGallery.ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
                    if (Properties.Settings.Default.ScrollEnabled)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 30);
                    break;

                // Rotate or Scroll
                case Key.Up:
                case Key.W:
                    if (picGallery != null)
                    {
                        if (picGallery.open)
                        {
                            picGallery.ScrollTo(true, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
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
                    if (picGallery != null)
                    {
                        if (picGallery.open)
                        {
                            picGallery.ScrollTo(false, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
                            return;
                        }
                    }
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
                case Key.OemPlus:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || IsScrollEnabled)
                        Zoom(1, true);
                    else
                        Zoom(1, false);
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ||IsScrollEnabled)
                        Zoom(-1, true);
                    else
                        Zoom(-1, false);
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

            // Delete, Shift + Delete
            else if (e.Key == Key.Delete)
            {
                var x = e.KeyboardDevice.Modifiers == ModifierKeys.Shift;
                DeleteFile(PicPath, !x);
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

            // Ctrl + R, F5
            else if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
                || e.Key == Key.F5)
            {
                Reload();
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





            // DEBUG!!!!!
            // F7
            else if (e.Key == Key.F7)
            {
                var x = Properties.Settings.Default.PicGalleryEnabled ? false : true;
                Properties.Settings.Default.PicGalleryEnabled = x;
                ToolTipStyle(x);
            }
            // F8
            else if (e.Key == Key.F8)
            {
                if (picGallery != null)
                    PicGalleryFade(picGallery.Visibility == Visibility.Collapsed);
                else
                    ToolTipStyle("null");
            }
            // DEBUG!!!!!
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

        // Auto scroll

        /// <summary>
        /// Starts the auto scroll feature and shows the sign on the ui
        /// </summary>
        /// <param name="e"></param>
        private void StartAutoScroll(MouseButtonEventArgs e)
        {
            // Don't scroll if not scrollable
            if (Scroller.ComputedVerticalScrollBarVisibility == Visibility.Collapsed)
                return;

            autoScrolling = true;
            autoScrollOrigin = e.GetPosition(Scroller);

            ShowAutoScrollSign();
        }


        /// <summary>
        /// Stop auto scroll feature and remove sign from the ui
        /// </summary>
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


        // Zoom
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
            // Disable normal scroll
            e.Handled = true;

            if (Properties.Settings.Default.ScrollEnabled && !autoScrolling)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    Zoom(e.Delta, true);
                }
                else
                {
                    if (e.Delta > 0)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 45);
                    else if (e.Delta < 0)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 45);
                }

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
            if (img.Source == null)
                return;

            var scaletransform = new ScaleTransform();
            scaletransform.ScaleX = scaletransform.ScaleY = 1.0;
            img.LayoutTransform = scaletransform;

            st.ScaleX = st.ScaleY = 1;
            tt.X = tt.Y = 0;
            img.RenderTransformOrigin = new Point(0.5, 0.5);

            CloseToolTipStyle();
            isZoomed = false;

            ZoomFit(img.Source.Width, img.Source.Height);

            string[] titleString;

            if (canNavigate)
            {
                titleString = TitleString((int)img.Source.Width, (int)img.Source.Height, FolderIndex);
                Title = titleString[0];
                Bar.Text = titleString[1];
                Bar.ToolTip = titleString[2];
            }
            else
            {
                // Display values from web
                titleString = TitleString((int)img.Source.Width, (int)img.Source.Height, PicPath);
                Title = titleString[0];
                Bar.Text = titleString[1];
                Bar.ToolTip = titleString[1];
            }
        }


        /// <summary>
        /// Scales or zooms, depending on given values
        /// </summary>
        /// <param name="i"></param>
        /// <param name="zoomMode"></param>
        private void Zoom(int i, bool zoomMode)
        {
            // Don't zoom when gallery is open
            if (picGallery != null)
            {
                if (picGallery.open)
                {
                    return;
                }
            }

            // Scales the window with img.LayoutTransform
            if (zoomMode)
            {
                // Start from zero or zoom value
                if (isZoomed)
                    AspectRatio += i > 0 ? .01 : -.01;
                else
                    AspectRatio = 1;

                var scaletransform = new ScaleTransform();

                scaletransform.ScaleX = scaletransform.ScaleY = AspectRatio;
                img.LayoutTransform = scaletransform;
            }

            // Pan and zoom
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

            isZoomed = true;

            // Display updated values

            // Displays zoompercentage in the center window
            ToolTipStyle(ZoomPercentage, true);

            string[] titleString;

            if (canNavigate)
            {
                titleString = TitleString((int)img.Source.Width, (int)img.Source.Height, FolderIndex);
                Title = titleString[0];
                Bar.Text = titleString[1];
                Bar.ToolTip = titleString[2];
            }
            else
            {
                // Display values from web
                titleString = TitleString((int)img.Source.Width, (int)img.Source.Height, PicPath);
                Title = titleString[0];
                Bar.Text = titleString[1];
                Bar.ToolTip = titleString[1];
            }

        }


        /// <summary>
        /// Fits image size based on users screen resolution
        /// </summary>
        /// <param name="width">The pixel width of the image</param>
        /// <param name="height">The pixel height of the image</param>
        private void ZoomFit(double width, double height)
        {
            // Determine whether to center image or fit window to image
            var windowstyle = Properties.Settings.Default.WindowStyle == 0 || Properties.Settings.Default.WindowStyle == 2;

            double maxWidth, maxHeight;

            if (windowstyle)
            {
                // Get max width and height, based on user's screen
                maxWidth = Math.Min(SystemParameters.PrimaryScreenWidth - ComfySpace, width);
                maxHeight = Math.Min((SystemParameters.FullPrimaryScreenHeight - 93), height);
            }
            else
            {
                // 93 = interface height
                maxWidth = Math.Min(Width, width);
                maxHeight = Math.Min(Height - 93, height);
            }

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
            if (windowstyle)
            {
                if (xWidth - 220 < 220)
                    Bar.MaxWidth = 220;
                else
                    Bar.MaxWidth = xWidth - 220;

                // Loses position gradually if not forced to center       
                CenterWindowOnScreen();
            }
            else
            {
                if (Width - 220 < 220)
                    Bar.MaxWidth = 220;
                else
                    Bar.MaxWidth = Width - 220;
            }


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


        // Rotate and flip
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

        // Load controls

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

        /// <summary>
        /// Loads FunctionsMenu and adds it to the window
        /// </summary>
        private void LoadFunctionsMenu()
        {
            functionsMenu = new lib.UserControls.Menus.FunctionsMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };

            bg.Children.Add(functionsMenu);
        }


        // PicGallery

        /// <summary>
        /// Loads PicGallery and adds it to the window
        /// </summary>
        private void LoadPicGallery()
        {
            picGallery = new PicGallery
            {
                Opacity = 0,
                Visibility = Visibility.Collapsed,
                Width = bg.Width,
                Height = bg.Height
            };

            bg.Children.Add(picGallery);
            Panel.SetZIndex(picGallery, 999);
        }


        private void PicGalleryFade(bool show = true)
        {
            picGallery.Width = Width - 15; // 15 = borders width
            picGallery.Height = Height - 95; // 95 = top + bottom bar height

            if (!picGallery.LoadComplete)
                picGallery.Load();

            picGallery.Visibility = Visibility.Visible;
            var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.5) };
            if (!show)
            {
                da.To = 0;
                da.Completed += delegate
                {
                    picGallery.Visibility = Visibility.Collapsed;
                    picGallery.open = false;
                };
            }
            else
            {
                da.To = 1;
                picGallery.open = true;
            }

            if (picGallery != null)
                picGallery.BeginAnimation(OpacityProperty, da);
        }


        private void PicGallery_PreviewItemClick(object source, MyEventArgs e)
        {
            Task.Run(() =>
            {
                Preloader.Clear();
                Preloader.Add(e.GetId());
            });
        }

        private async void PicGallery_ItemClick(object source, MyEventArgs e)
        {
            while (!Preloader.Contains(Pics[e.GetId()]))
            {
                if (Bar.Text != Loading)
                {
                    Bar.Text = Loading;
                    img.Source = e.GetImage();
                }
                await Task.Delay(50);
            }
            Pic(e.GetId());
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


        // Toggle open close menus

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
        /// Toggles whether FunctionsMenu is open or not with a fade animation 
        /// </summary>
        private static bool FunctionsMenuOpen
        {
            get { return functionsMenuOpen; }
            set
            {
                functionsMenuOpen = value;
                functionsMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { functionsMenu.Visibility = Visibility.Hidden; };
                }
                else
                    da.To = 1;
                if (functionsMenu != null)
                    functionsMenu.BeginAnimation(OpacityProperty, da);
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

            if (FunctionsMenuOpen)
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

            if (FunctionsMenuOpen)
                FunctionsMenuOpen = false;
        }


        /// <summary>
        /// Closes usercontrol menus
        /// </summary>
        private void Close_UserControls(object sender, RoutedEventArgs e)
        {
            Close_UserControls();
        }

        /// <summary>
        /// Toggles whether open menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Toggle_open_menu(object sender, RoutedEventArgs e)
        {
            FileMenuOpen = !FileMenuOpen;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;

            if (FunctionsMenuOpen)
                FunctionsMenuOpen = false;
        }


        /// <summary>
        /// Toggles whether image menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Toggle_image_menu(object sender, RoutedEventArgs e)
        {
            ImageSettingsMenuOpen = !ImageSettingsMenuOpen;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;

            if (FunctionsMenuOpen)
                FunctionsMenuOpen = false;
        }


        /// <summary>
        /// Toggles whether quick settings menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Toggle_quick_settings_menu(object sender, RoutedEventArgs e)
        {
            QuickSettingsMenuOpen = !QuickSettingsMenuOpen;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (FunctionsMenuOpen)
                FunctionsMenuOpen = false;

        }


        /// <summary>
        /// Toggles whether functions menu is open or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Toggle_Functions_menu(object sender, RoutedEventArgs e)
        {
            FunctionsMenuOpen = !FunctionsMenuOpen;

            if (FileMenuOpen)
                FileMenuOpen = false;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (QuickSettingsMenuOpen)
                QuickSettingsMenuOpen = false;

        }

        #endregion

        #region Manipulate Interface

        /// <summary>
        /// Toggle between hidden interface and default
        /// </summary>
        private void HideInterface()
        {
            if (Properties.Settings.Default.WindowStyle == 0)
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

                Properties.Settings.Default.WindowStyle = 2;

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

                Properties.Settings.Default.WindowStyle = 0;
                activityTimer.Stop();
            }

        }


        /// <summary>
        /// Hides/shows interface elements with a fade animation
        /// </summary>
        /// <param name="show"></param>
        private async void FadeControlsAsync(bool show)
        {
            var fadeTo = show ? 1 : 0;

            await Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (Properties.Settings.Default.WindowStyle == 2)
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


        /// <summary>
        /// Timer starts FadeControlsAsync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActivityTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            FadeControlsAsync(false);
        }


        /// <summary>
        /// Logic for mouse movements on MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            // Stop timer if mouse moves on mainwindow and show elements
            activityTimer.Stop();
            FadeControlsAsync(true);
        }


        /// <summary>
        /// Logic for mouse leave mainwindow event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            // Start timer when mouse leaves mainwindow
            activityTimer.Start();
        }


        /// <summary>
        /// Find scrollbar and start fade animation
        /// </summary>
        /// <param name="show"></param>
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
            s1.Append(AppName).Append(" - ").Append(path).Append(" (").Append(width).Append(" x ").Append(height).Append(StringAspect(width, height)).Append(" ").Append(Zoomed);

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

        /// <summary>
        /// Show About window in a dialog
        /// </summary>
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


        /// <summary>
        /// Show Help window in a dialog
        /// </summary>
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


        /// <summary>
        /// Show All Settings window in a dialog
        /// </summary>
        public void AllSettingsWindow()
        {
            Window window = new AllSettings
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

        /// <summary>
        /// Show YesNoDialogBox
        /// </summary>
        private void RenameFile()
        {
            if (!File.Exists(PicPath))
                return;

            string Picname = Path.GetFileName(PicPath);
            string RenamedFilePath = Path.GetDirectoryName(PicPath);
            string RenamedFileExt = Path.GetExtension(PicPath);

            lib.Windows.YesNoDialogWindow YesNoDialog = new lib.Windows.YesNoDialogWindow("Are you sure you wanna rename \r\n" + Picname + " to ")
            {
                Width = Width,
                Height = Height,
                Owner = Application.Current.MainWindow,
            };

            var animation = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            window.BeginAnimation(OpacityProperty, animation);

            if ((bool)YesNoDialog.ShowDialog())
            {

                if (!File.Exists(PicPath) || img.Source == null)
                    return;

                if (string.IsNullOrWhiteSpace(YesNoDialog.NameForRename))
                    return;

                if (File.Exists(RenamedFilePath + "\\" + YesNoDialog.NameForRename + RenamedFileExt))
                    ToolTipStyle(YesNoDialog.NameForRename + RenamedFileExt + " allready exists");
                    

                if (FileFunctions.RenameFile(PicPath, RenamedFilePath + "\\" + YesNoDialog.NameForRename + RenamedFileExt))
                {
                    string Fullpath = RenamedFilePath + "\\" + YesNoDialog.NameForRename + RenamedFileExt;
                    Pics[FolderIndex] = Fullpath;

                    var titleString = TitleString((int)img.Source.Width, (int)img.Source.Height, FolderIndex);
                    Title = titleString[0];
                    Bar.Text = titleString[1];
                    Bar.ToolTip = titleString[2];
                }
                Reload();
            }
            else
            {
                ToolTipStyle("Something went wrong under renamening of " + Picname);
            }
        }

        #endregion

        #region MouseOver Button Events
        /*
        
            Adds MouseOver events for the given elements with the AnimationHelper.
            Changes color depending on the users settings.

        */

        // Logo Mouse Over
        //private void LogoMouseOver(object sender, MouseEventArgs e)
        //{
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, pBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, iBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, cBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, vBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, iiBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, eBrush, false);
        //    AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, wBrush, false);
        //}

        //private void LogoMouseLeave(object sender, MouseEventArgs e)
        //{
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, pBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, iBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, cBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, vBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, iiBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, eBrush, false);
        //    AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, wBrush, false);
        //}

        //private void LogoMouseButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(pBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(iBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(cBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(vBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(iiBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(eBrush, false);
        //    AnimationHelper.PreviewMouseLeftButtonDownColorEvent(wBrush, false);
        //}

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

        // FunctionMenu
        private void FunctionMenuButtonButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                QuestionButtonFill1,
                false
            );
        }

        private void FunctionMenuButtonButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(QuestionButtonFill1, false);
        }

        private void FunctionMenuButtonButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                mainColor.A,
                mainColor.R,
                mainColor.G,
                mainColor.B,
                QuestionButtonFill1,
                false
            );
        }

        #endregion

        #endregion

        #region File Methods

        /// <summary>
        /// Adds events and submenu items to recent items in the context menu
        /// </summary>
        /// <param name="sender"></param>
        private void Recentcm_MouseEnter(object sender)
        {
            // Need to register the object as a MenuItem to use it
            var RecentFilesMenuItem = (MenuItem)sender;

            // Load values and check if succeeded
            var fileNames = RecentFiles.LoadValues();
            if (fileNames == null)
                return;

            // If items exist: replace them, else add them 
            if (RecentFilesMenuItem.Items.Count >= fileNames.Length)
            {
                for (int i = fileNames.Length - 1; i >= 0; i--)
                {
                    // Don't add the same item more than once
                    var item = fileNames[i];
                    if (i != 0 && fileNames[i - 1] == item)
                        continue;

                    // Change values
                    var menuItem = (MenuItem)RecentFilesMenuItem.Items[i];
                    menuItem.Header = Path.GetFileName(item);
                    menuItem.ToolTip = item;
                }
                return;
            }

            for (int i = fileNames.Length - 1; i >= 0; i--)
            {
                // Don't add the same item more than once
                var item = fileNames[i];
                if (i != 0 && fileNames[i - 1] == item)
                    continue;

                // Add items
                var menuItem = new MenuItem()
                {
                    Header = Path.GetFileName(item),
                    ToolTip = item
                };
                // Set tooltip as argument to avoid subscribing and unsubscribing to events
                menuItem.Click += (x, xx) => Pic(menuItem.ToolTip.ToString());
                RecentFilesMenuItem.Items.Add(menuItem);
            }
        }


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
            // file

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

            // Clipboard Image
            if (Clipboard.ContainsImage())
            {
                Pic(Clipboard.GetImage(), "Clipboard Image");
                return;
            }

            // text/string/adddress

            var s = Clipboard.GetText(TextDataFormat.Text);

            if (string.IsNullOrEmpty(s))
                return;

            if (FilePathHasInvalidChars(s))
                MakeValidFileName(s);

            s = s.Replace("\"", "");
            s = s.Trim();

            if (File.Exists(s))
            {
                Pic(s);
            }
            else if (Directory.Exists(s))
            {
                ChangeFolder();
                Pics = FileList(s);
                if (Pics.Count > 0)
                    Pic(Pics[0]);
                else if (!string.IsNullOrWhiteSpace(PicPath))
                    Pic(PicPath);
                else
                    Unload();
            }
            else if (Uri.IsWellFormedUriString(s, UriKind.Absolute)) // Check if from web
                PicWeb(s);

            else
                ToolTipStyle("An error occured while trying to paste file");
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
        /// Open a File Dialog, where the user can save a supported file type.
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

                //Force freshed the list of pichures.
                Reload();

                Close_UserControls();
            }
            else
            {
                ToolTipStyle("Error, File does not exist, or something went wrong...", true);
            }
        }


        /// <summary>
        /// Delete file or move it to recycle bin
        /// </summary>
        /// <param name="Recyclebin"></param>
        private void DeleteFile(string file, bool Recyclebin)
        {
            if (FileFunctions.DeleteFile(file, Recyclebin))
            {
                Pic();
                Close_UserControls();
                var filename = Path.GetFileName(file);
                if (filename.Length >= 25)
                {
                    filename = filename.Substring(0, 21);
                    filename += "...";
                }
                var y = Recyclebin ? "Sent " + filename + " to the recyle bin" : "Deleted " + filename;
                ToolTipStyle(y);
            }
            else
            {
                ToolTipStyle("An error occured when deleting " + file);
            }
        }


        

        #endregion     
    }
}
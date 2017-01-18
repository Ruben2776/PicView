using PicView.lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.lib.Helper;
using static PicView.lib.ImageManager;
using System.Reflection;
using ImageMagick;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Net;
using System.Threading;
using PicView.lib.UserControls;

namespace PicView
{
    public partial class MainWindow : Window
    {
        #region Window Logic

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => MainWindow_Loaded(s, e);
            ContentRendered += MainWindow_ContentRendered;
        }

        #endregion

        #region Load Events

        #region ContentRendered
        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            #region Extra settings
            AllowDrop = true;
            Scroller.MaxHeight = SystemParameters.PrimaryScreenHeight - ComfySpace;
            Scroller.MaxWidth = SystemParameters.PrimaryScreenWidth - 8;
            IsScrollEnabled = Properties.Settings.Default.ScrollEnabled;

            //var WindowBrush = FindResource("BorderBrush") as SolidColorBrush;
            //WindowBrush.Color = AnimationHelper.GetPrefferedColorDown();
            //var MenuBrush = FindResource("MenuHighlightBrushKey") as SolidColorBrush;
            //MenuBrush.Color = AnimationHelper.GetPrefferedColorDown();
            #endregion

            #region Set required stuff
            Pics = new List<string>();
            freshStartup = true;
            DataContext = this;
            #endregion

            #region To load or not to load image, that is the question...
            if (Application.Current.Properties["ArbitraryArgName"] == null)
                Unload();
            else
            {
                var file = Application.Current.Properties["ArbitraryArgName"].ToString();
                if (File.Exists(file))
                    Pic(file);
                else
                    if (Uri.IsWellFormedUriString(file, UriKind.Absolute))
                    PicWeb(file);
                else
                    Unload();
            }
            #endregion   

            #region Add ContextMenu

            // Add main contextmenu
            cm = new ContextMenu();

            var opencm = new MenuItem
            {
                Header = "Open",
                InputGestureText = "Ctrl + O"
            };
            opencm.Click += (s, x) => Open();
            cm.Items.Add(opencm);

            var printcm = new MenuItem
            {
                Header = "Print",
                InputGestureText = "Ctrl + P"
            };
            printcm.Click += (s, x) => Print(PicPath);
            cm.Items.Add(printcm);

            var wallcm = new MenuItem
            {
                Header = "Set as wallpaper"
            };
            wallcm.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Fill);
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
            cppcm.Click += (s, x) => CopyPic();
            cm.Items.Add(cppcm);

            var pastecm = new MenuItem
            {
                Header = "Paste picture",
                InputGestureText = "Ctrl + V"
            };
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
            abcm.Click += (s, x) => AboutWindow();
            cm.Items.Add(abcm);

            var helpcm = new MenuItem
            {
                Header = "Help",
                InputGestureText = "F1",
                ToolTip = "Shows keyboard shortcuts and general help"
            };
            helpcm.Click += (s, x) => HelpWindow();
            cm.Items.Add(helpcm);

            //var toolscm = new MenuItem
            //{
            //    Header = "Tools",
            //    InputGestureText = "F6",
            //    ToolTip = "Tools and stuff"
            //};
            ////toolscm.Click += tools_window;
            //cm.Items.Add(toolscm);
            cm.Items.Add(new Separator());

            var mincm = new MenuItem
            {
                Header = "Minimize"
            };
            mincm.Click += (s, x) => Minimize(this);
            cm.Items.Add(mincm);

            var maxcm = new MenuItem
            {
                Header = "Fullscreen/Restore"
            };
            maxcm.Click += (s, x) => Maximize(this);
            cm.Items.Add(maxcm);

            var clcm = new MenuItem
            {
                Header = "Close"
            };
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
            nextcm.Click += (s, x) => Pic(true, false);
            cmRight.Items.Add(nextcm);

            var prevcm = new MenuItem
            {
                Header = "Previous picture",
                InputGestureText = "ᗏ or A",
                ToolTip = "Go to previous image in folder",
                StaysOpenOnClick = true
            };
            prevcm.Click += (s, x) => Pic(false, false);
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

            // Add Title contextMenu
            var cmTitle = new ContextMenu();

            var clTc = new MenuItem
            {
                Header = "Copy path to clipboard"
            };
            clTc.Click += (s, x) => CopyText();
            cmTitle.Items.Add(clTc);

            Bar.ContextMenu = cmTitle;

            #endregion

            #region Initilize Zoom
            InitializeZoom();
            #endregion

            #region Add UserControls :)
            LoadTooltipStyle();
            LoadAboutWindow();
            LoadHelpWindow();
            LoadOpenMenu();
            LoadImageSettingsMenu();
            #endregion           

            #region Do updates in seperate thread
            var task = new Task(() => {
                #region Add events
                Closing += Window_Closing;

                #region keyboard and Mouse_Keys Keys
                //PreviewKeyDown += previewKeys;
                KeyDown += Keys;
                KeyUp += MainWindow_KeyUp;
                MouseDown += MainWindow_MouseDown;
                #endregion

                #region Button Events

                #region closebutton
                CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;
                CloseButton.MouseEnter += CloseButtonMouseOver;
                CloseButton.MouseLeave += CloseButtonMouseLeave;
                CloseButton.Click += (s, x) => Close();
                #endregion

                #region MinButton
                MinButton.PreviewMouseLeftButtonDown += MinButtonMouseButtonDown;
                MinButton.MouseEnter += MinButtonMouseOver;
                MinButton.MouseLeave += MinButtonMouseLeave;
                MinButton.Click += (s, x) => Minimize(this);
                #endregion

                #region MaxButton
                MaxButton.PreviewMouseLeftButtonDown += MaxButtonMouseButtonDown;
                MaxButton.MouseEnter += MaxButtonMouseOver;
                MaxButton.MouseLeave += MaxButtonMouseLeave;
                MaxButton.Click += (s, x) => Maximize(this);
                #endregion

                #region OpenMenuButton

                OpenMenuButton.PreviewMouseLeftButtonDown += OpenMenuButtonMouseButtonDown;
                OpenMenuButton.MouseEnter += OpenMenuButtonMouseOver;
                OpenMenuButton.MouseLeave += OpenMenuButtonMouseLeave;
                OpenMenuButton.Click += Toggle_open_menu;

                Helper.openMenu.Open.Click += (s, x) => Open();
                Helper.openMenu.Open_File_Location.Click += (s, x) => Open_In_Explorer();
                Helper.openMenu.Print.Click += (s, x) => Print(PicPath);

                Helper.openMenu.Open_Border.MouseLeftButtonUp += (s, x) => Open();
                Helper.openMenu.Open_File_Location_Border.MouseLeftButtonUp += (s, x) => Open_In_Explorer();
                Helper.openMenu.Print_Border.MouseLeftButtonUp += (s, x) => Print(PicPath);

                Helper.openMenu.CloseButton.Click += Close_UserControls;
                Helper.openMenu.PasteButton.Click += (s, x) => Paste();
                Helper.openMenu.CopyButton.Click += (s, x) => CopyPic();
                #endregion

                #region image_button

                image_button.PreviewMouseLeftButtonDown += ImageButtonMouseButtonDown;
                image_button.MouseEnter += ImageButtonMouseOver;
                image_button.MouseLeave += ImageButtonMouseLeave;
                image_button.Click += Toggle_image_menu;

                #region CloseButton
                imageSettingsMenu.CloseButton.Click += Close_UserControls;
                #endregion

                #region FlipButton
                imageSettingsMenu.FlipButton.Click += (s,x) => Flip();
                #endregion

                #region Rotation RadioButtons
                imageSettingsMenu.ro0.Click += (s, x) => Rotate(0);
                imageSettingsMenu.ro90.Click += (s, x) => Rotate(90);
                imageSettingsMenu.ro180.Click += (s, x) => Rotate(180);
                imageSettingsMenu.ro270.Click += (s, x) => Rotate(270);

                imageSettingsMenu.ro0Border.MouseLeftButtonDown += (s, x) => Rotate(0);
                imageSettingsMenu.ro90Border.MouseLeftButtonDown += (s, x) => Rotate(90);
                imageSettingsMenu.ro180Border.MouseLeftButtonDown += (s, x) => Rotate(180);
                imageSettingsMenu.ro270Border.MouseLeftButtonDown += (s, x) => Rotate(270);

                #endregion

                #endregion

                #region LeftButton

                LeftButton.PreviewMouseLeftButtonDown += LeftButtonMouseButtonDown;
                LeftButton.MouseEnter += LeftButtonMouseOver;
                LeftButton.MouseLeave += LeftButtonMouseLeave;
                LeftButton.Click += (s, x) => { LeftbuttonClicked = true; Pic(false, false); };

                #endregion

                #region RightButton

                RightButton.PreviewMouseLeftButtonDown += RightButtonMouseButtonDown;
                RightButton.MouseEnter += RightButtonMouseOver;
                RightButton.MouseLeave += RightButtonMouseLeave;
                RightButton.Click += (s, x) => { RightbuttonClicked = true; Pic(true, false); };

                #endregion

                #region SettingsWindow
                //SettingsButton.PreviewMouseLeftButtonDown += SettingsButtonButtonMouseButtonDown;
                SettingsButton.MouseEnter += SettingsButtonButtonMouseOver;
                SettingsButton.MouseLeave += SettingsButtonButtonMouseLeave;
                #endregion

                #endregion

                #region Bar
                Bar.MouseLeftButtonDown += Move;
                #endregion

                #region img

                img.MouseLeftButtonDown += Zoom_img_MouseLeftButtonDown;
                img.MouseLeftButtonUp += Zoom_img_MouseLeftButtonUp;

                img.MouseMove += Zoom_img_MouseMove;
                img.MouseWheel += Zoom_img_MouseWheel;
                //img.MouseEnter += img_MouseEnter;
                //img.MouseLeave += img_MouseLeave;

                #endregion

                #region bg
                //bg.MouseMove += MouseMoves;
                //bg.MouseLeave += MouseLeaves;
                bg.Drop += Image_Drop;
                bg.DragEnter += Image_DragEnter;
                bg.DragLeave += Image_DragLeave;
                //bg.MouseEnter += bg_MouseEnter;
                //bg.MouseLeave += bg_MouseLeave;
                //bg.MouseLeftButtonDown += bg_MouseLeftButtonDown;
                #endregion

                #region Scroller
                //Scroller.MouseEnter += Scroller_MouseEnter;
                //Scroller.MouseLeave += Scroller_MouseLeave;
                #endregion

                #region TitleBar
                TitleBar.MouseLeftButtonDown += Move;
                #endregion

                #region Logobg
                //Logobg.MouseEnter += LogoMouseOver;
                //Logobg.MouseLeave += LogoMouseLeave;
                //Logobg.PreviewMouseLeftButtonDown += LogoMouseButtonDown;
                #endregion

                #region Lower Bar
                LowerBar.Drop += Image_Drop;
                #endregion

                #region Update settings if needed
                if (Properties.Settings.Default.CallUpgrade)
                {
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.CallUpgrade = false;
                }
                #endregion
                #endregion
            });
            task.Start();
            #endregion            

            if (ajaxLoading.Opacity > 0)
            {
                AjaxLoadingEnd();
            }
        }
        #endregion

        #region Loaded
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ajaxLoading = new AjaxLoading
            {
                Opacity = 0
            };
            bg.Children.Add(ajaxLoading);
            AjaxLoadingStart();
        }
        #endregion

        #endregion

        #region Size changed
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
            //if (size.HeightChanged || size.WidthChanged)
            //{
            //    CenterWindowOnScreen();
            //}
        }
        #endregion

        #region Center window
        /// <summary>
        /// Centers on the primary monitor.. Needs multi monitor solution....
        /// </summary>
        private void CenterWindowOnScreen()
        {
            Top = (SystemParameters.WorkArea.Height - Height) / 2;
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
        }
        #endregion

        #region Move window
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

        #endregion

        #region Window_Closing

        /// <summary>
        /// Save settings when closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            DeleteTempFiles();
        }

        #endregion

        #endregion

        #region Image Logic

        #region Pic

        #region Pic(string path)

        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        private async void Pic(string path)
        {
            #region Set Loading
            Title = Bar.Text = Loading;
            Bar.ToolTip = Loading;
            AjaxLoadingStart();
            #endregion

            #region Check if folder has changed
            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            if (!string.IsNullOrWhiteSpace(PicPath) && Path.GetDirectoryName(path) != Path.GetDirectoryName(PicPath))
            {
                ChangeFolder();
                await GetValues(path);
            }
            else if (freshStartup)
                await GetValues(path);
            else FolderIndex = Pics.IndexOf(path);
            #endregion

            #region Check if failed archive
            RecoverFailedArchiveAsync();
            #endregion

            #region Get image
            if (Pics.Count > 0)
                Pic(FolderIndex);
            else
                Unload();
            #endregion

            #region Set freshStartup
            if (freshStartup)
                freshStartup = false;

            // Fix possible loading bug
            if (ajaxLoading.Visibility == Visibility.Visible)
            {
                AjaxLoadingEnd();
            }
            #endregion
        }

        #endregion

        #region Pic(int x)
        /// <summary>
        /// Loads image based on overloaded int.
        /// Possible out of range error if used inappropriately.
        /// </summary>
        /// <param name="x"></param>
        private async void Pic(int x)
        {
            #region fields
            // Add "pic" as local variable used for the image.
            // Use the Load() function load image from memory if available
            // if not, it will be null
            BitmapSource pic = Preloader.Load(Pics[x]);
            #endregion

            #region if (pic == null)  On demand loading
            // Failed to load image from memory
            if (pic == null)
            {
                if (File.Exists(Pics[x]))
                {
                    #region Set Loading
                    Title = Bar.Text = Loading;
                    Bar.ToolTip = Loading;
                    canNavigate = false;
                    if (img.Source != null)
                        img.Source = GetWindowsThumbnail(Pics[x]);
                    #endregion

                    if (freshStartup || PreloadCount < 2 || Preloader.Count() < 0)
                    {
                        // If preloader is not running, load picture manually
                        await Task.Run(() => pic = RenderToBitmapSource(Pics[x], Extension));
                    }
                    else
                    {
                        // Preloader is running, wait for it to decode image
                        var spin = new SpinWait();
                        await Task.Run(() =>
                        {
                            do
                            {
                                spin.SpinOnce();
                                if (spin.Count > 2700)
                                {
                                    pic = RenderToBitmapSource(Pics[x], Extension);
                                    break;
                                }
                            } while (!Preloader.Contains(Pics[x]));
                        });
                        if (spin.Count < 2700)
                            pic = Preloader.Load(Pics[x]);
                    }
                    if (pic == null)
                    {
                        PicErrorFix(x);
                        return;
                    }
                }
                else
                {
                    PicErrorFix(x);
                    return;
                }
            }
            #endregion

            #region Update source, size, animated gif and scroll
            if (IsScrollEnabled)
                Scroller.ScrollToTop();

            if (Extension == ".gif")
                XamlAnimatedGif.AnimationBehavior.SetSourceUri(img, new Uri(Pics[x]));
            else
                img.Source = pic;

            ZoomFit(pic.PixelWidth, pic.PixelHeight);
            PicPath = Pics[x];
            canNavigate = true;
            #endregion

            #region Update ui stuff
            var titleString = TitleString(pic.PixelWidth, pic.PixelHeight, x);
            Title = titleString[0];
            Bar.Text = titleString[1];
            Bar.ToolTip = titleString[2];
            #endregion

            #region Preload images           
            bool? reverse;

            if (PreloadCount > 5 || freshStartup)
                reverse = false;
            else if (PreloadCount < 3)
                reverse = true;
            else
                reverse = null;

            if (reverse.HasValue)
            {
                var t = new Thread(() =>
                {
                    if (!Preloader.Contains(Pics[x]))
                        Preloader.Add(pic, Pics[x]);

                    Preloader.PreLoad(x, reverse.Value);
                    PreloadCount = 4;
                });
                t.Start();
            }

            #endregion

            #region Update the rest
            Progress(x, Pics.Count);
            // Loses position gradually if not forced to center
            CenterWindowOnScreen();
            if (freshStartup)
                freshStartup = false;
            #endregion

        }
        #endregion

        #region Pic(BitmapSource, imageName)
        /// <summary>
        /// Load a picture from a prepared bitmap
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        private void Pic(BitmapSource pic, string imageName)
        {
            Unload();

            #region Update source, size, animated gif and scroll
            if (IsScrollEnabled)
                Scroller.ScrollToTop();

            img.Source = pic;

            ZoomFit(pic.PixelWidth, pic.PixelHeight);
            CloseToolTipStyle();
            canNavigate = true;
            #endregion

            #region Update ui stuff
            var titleString = TitleString(pic.PixelWidth, pic.PixelHeight, imageName);
            Title = titleString[0];
            Bar.Text = titleString[1];
            Bar.ToolTip = titleString[1];
            #endregion

            #region Update the rest
            NoProgress();
            PicPath = string.Empty;
            xWidth = img.ActualWidth;
            xHeight = img.ActualHeight;
            canNavigate = false;
            CenterWindowOnScreen();
            #endregion

            canNavigate = false;
        }
        #endregion

        #region Pic(bool next, bool end)
        /// <summary>
        /// Goes to next, previous, first or last image
        /// next = true, false;
        /// previous = false, false;
        /// last = true, true;
        /// first = false, true;
        /// </summary>
        /// <param name="next"></param>
        /// <param name="end"></param>
        private void Pic(bool next, bool end)
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
            CloseToolTipStyle();
        }

        #endregion

        #region FastPic
        /// <summary>
        /// Only load image from preload or thumbnail without resizing
        /// </summary>
        private void FastPic()
        {
            Bar.ToolTip = Title = Bar.Text = "Image " + (FolderIndex + 1) + " of " + Pics.Count;

            img.Width = xWidth;
            img.Height = xHeight;

            img.Source = Preloader.Contains(Pics[FolderIndex]) ? Preloader.Load(Pics[FolderIndex]) : GetWindowsThumbnail(Pics[FolderIndex]);

            Progress(FolderIndex, Pics.Count);

            GoToPic = true;
        }

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        private void FastPicUpdate()
        {
            if (!Preloader.Contains(Pics[FolderIndex]))
            {
                PreloadCount = 0;
                Preloader.Clear();
            }

            GoToPic = false;
            Pic(FolderIndex);
        }

        #endregion

        #endregion

        #region PicErrorFix

        /// <summary>
        /// Attemps to fix list by removing invalid files
        /// </summary>
        /// <param name="x"></param>
        private void PicErrorFix(int x)
        {
            var file = Pics[x];
            Pics.Remove(Pics[x]);
            if (Pics.Count < 1)
            { 
                ToolTipStyle("Unexpected error", false, TimeSpan.FromSeconds(3));
                Unload();
                return;
            }

            if (FolderIndex + 1 == Pics.Count)
                FolderIndex = 0;
            else
                FolderIndex++;

            if (FolderIndex == 0 || Pics.Count < 1)
                Unload();
            else
            {
                Pic(FolderIndex);
                PreloadCount++;
            }

            ToolTipStyle("File not found or unable to render, " + file, true, TimeSpan.FromSeconds(3));
        }

        #endregion

        #region RecoverFailedArchiveAsync()
        /// <summary>
        /// Attemps to recover from failed archive extraction
        /// </summary>
        private async void RecoverFailedArchiveAsync()
        {
            // If there are no pictures, but a folder when TempZipPath has a value,
            // we should open the folder
            if (Pics.Count > 1)
                return;

            if (string.IsNullOrWhiteSpace(TempZipPath))
            {
                // Unexped result, return to clear state.
                Unload();
                return;
            }

            //TempZipPath is not null = images being extracted
            short count = 0;
            Bar.Text = "Unzipping...";
            do
            {
                if (count == 0 || count == 2)
                {
                    try
                    {
                        var directory = Directory.GetDirectories(TempZipPath);
                        if (directory.Length == 1)
                        {
                            TempZipPath = directory[0];
                            Pics = FileList(TempZipPath);
                        }
                        else
                        {
                            //Fix non working archive
                            ToolTipStyle("Non working zip file, reloading...", false);
                            PicPath = File.Exists(xPicPath) ? xPicPath : string.Empty;
                            FolderIndex = xFolderIndex;
                            if (!File.Exists(PicPath) || String.IsNullOrWhiteSpace(PicPath))
                                Unload();
                            else
                                Pic(PicPath);
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        Unload();
                        return;
                    }

                }
                else
                {
                    try
                    {
                        if (Directory.Exists(TempZipPath))
                        {
                            try
                            {
                                var test = Directory.EnumerateFileSystemEntries(TempZipPath);
                                if (test.Count() > -1)
                                    Pics = FileList(TempZipPath);
                            }
                            catch (Exception e)
                            {
                                ToolTipStyle(e.Message, true, TimeSpan.FromSeconds(5));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Unload();
                        return;
                    }
                }

                if (count > 0)
                {
                    Bar.Text = "Still " + Loading + " Attempt " + count + " of 3";
                }

                if (count > 3)
                {
                    Unload();
                    return;
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
        }

        #endregion

        #region PicWeb
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
                ToolTipStyle("Unable to load image", false);
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

        #endregion

        #region ChangeFolder + Unload
        /// <summary>
        /// Clears data, to free objects no longer necessary to store in memory and allow changing folder without error.
        /// </summary>
        private void ChangeFolder()
        {
            Pics.Clear();
            Preloader.Clear();
            DeleteTempFiles();
        }

        #region unLoad

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
            img.Width = 0;
            img.Height = 0;
        }

        #endregion

        #endregion

        #endregion

        #region Drag and Drop
        /// <summary>
        /// Check if dragged file is valid
        /// Returns null if not useable thumbnail,
        /// false if it is, true if invalid
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        bool? Drag_Drop_Check(string[] files)
        {
            // Return if file strings are null
            if (files == null) return true;
            if (files[0] == null) return true;

            // Return if not useable file
            switch (Path.GetExtension(files[0]))
            {
                case ".zip":
                case ".7zip":
                case ".7z":
                case ".rar":
                case ".cbr":
                case ".cb7":
                case ".cbt":
                case ".cbz":
                case ".cba":
                case ".xz":
                case ".bzip2":
                case ".gzip":
                case ".tar":
                case ".wim":
                case ".dds":
                case ".svg":
                case ".psd":
                case ".psb":
                case ".orf":
                case ".cr2":
                case ".crw":
                case ".dng":
                case ".raf":
                case ".ppm":
                case ".raw":
                case ".mrw":
                case ".nef":
                case ".pef":
                    return null;
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
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Logic for handling drag over event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_DragEnter(object sender, DragEventArgs e)
        {
            // Error handling
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (Drag_Drop_Check(files).HasValue && Drag_Drop_Check(files).Value)
                return;

            if (Drag_Drop_Check(files) == null)
                return;

            // Tell that it's succeeded
            isDraggedOver = true;
            ToolTipStyle(DragOverString, false);

            // Use the images dimensions if available, else fix it to container
            if (img.Source == null)
            {
                img.Width = Scroller.ActualWidth;
                img.Height = Scroller.ActualHeight;
            }
            else
            {
                // Save our image so we can swap back to it later if neccesary
                prevPicResource = img.Source;

                if (xWidth > 0 && xHeight > 0)
                {
                    img.Width = xWidth;
                    img.Height = xHeight;
                }
                else
                {
                    img.Width = Scroller.ActualWidth;
                    img.Height = Scroller.ActualHeight;
                }
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

            // Switch to previous image if available, else display no image
            if (prevPicResource != null)
            {
                img.Source = prevPicResource;
                prevPicResource = null;
            }
            else
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
            if (!Drag_Drop_Check(files).HasValue && Drag_Drop_Check(files).Value)
                return;

            // Load it
            Pic(files[0]);

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

        #region Keyboard Shortcuts

        #region KeyDown
        private void Keys(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                #region Close
                case Key.Escape:
                    Close();
                    break;
                #endregion

                #region CenterWindowOnScreen()
                case Key.Space:
                    CenterWindowOnScreen();
                    break;
                #endregion

                #region Next/last
                case Key.BrowserForward:
                case Key.Right:
                case Key.D:
                    if (!e.IsRepeat) //If the key is (not) held down 
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                            Pic(true, true); // Go to first if Ctrl held down
                        else
                            Pic(true, false);
                    }
                    else if (canNavigate)
                    {
                        if (FolderIndex == Pics.Count - 1)
                            FolderIndex = 0;
                        else
                            FolderIndex++;

                        FastPic();
                    }
                    break;
                #endregion

                #region Prev/first
                case Key.BrowserBack:
                case Key.Left:
                case Key.A:
                    if (!e.IsRepeat) //If the key is (not) held down 
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                            Pic(false, true); // Go to last if Ctrl held down
                        else
                            Pic(false, false);

                        GoToPic = false;
                    }
                    else if (canNavigate)
                    {
                        if (FolderIndex == 0)
                            FolderIndex = Pics.Count - 1;
                        else
                            FolderIndex--;

                        FastPic();
                    }
                    break;
                #endregion

                #region Toggle Scroll
                case Key.X:
                    IsScrollEnabled = IsScrollEnabled ? false : true;
                    break;
                #endregion

                #region Scroll
                case Key.PageUp:
                    if (Properties.Settings.Default.ScrollEnabled)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 30);
                    break;
                case Key.PageDown:
                    if (Properties.Settings.Default.ScrollEnabled)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 30);
                    break;
                #endregion

                #region Rotate ||Scroll
                case Key.Up:
                case Key.W:
                    if (Properties.Settings.Default.ScrollEnabled)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 30);
                    else if (!e.IsRepeat)
                        Rotate(false);
                    break;

                case Key.Down:
                case Key.S:
                    if (Properties.Settings.Default.ScrollEnabled)
                        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 30);
                    else if (!e.IsRepeat)
                        Rotate(true);
                    break;

                #endregion

                #region Flip
                case Key.F:
                    Flip();
                    break;

                #endregion

                #region Zoom
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
                #endregion

                #region Print
                case Key.P:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Print(PicPath);
                    break;
                #endregion

                #region Copy
                case Key.C:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        CopyPic();
                    break;
                #endregion

                #region Paste
                case Key.V:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Paste();
                    break;
                #endregion

                #region Open
                case Key.O:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Open();
                    break;
                    #endregion,
            }
        }
        #endregion

        #region KeyUp
        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.A || e.Key == Key.Right || e.Key == Key.D)
            {
                if (!GoToPic)
                    return;
                FastPicUpdate();
            }
        }
        #endregion

        #region MouseDown
        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    break;
                case MouseButton.Middle:
                    break;
                case MouseButton.Right:
                    break;
                case MouseButton.XButton1:
                    Pic(false, false);
                    break;
                case MouseButton.XButton2:
                    Pic(true, false);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #endregion

        #region Zoom and Scroll
        /// <summary>
        /// Pan and Zoom, reset zoom and double click to go to next picture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Zoom_img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.ClickCount == 2)
                    ResetZoom();
                else
                {
                    img.CaptureMouse();
                    start = e.GetPosition(this);
                    origin = new Point(tt.X, tt.Y);
                }

            }

            else if (e.ClickCount == 2)
                Pic(true, false);

            //else
            //    try
            //    {
            //        DragMove();
            //    }
            //    catch (InvalidOperationException)
            //    {
            //        //return;
            //    }
        }

        private void Zoom_img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            img.ReleaseMouseCapture();
        }

        /// <summary>
        /// Tracks where the mouse drags to
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Zoom_img_MouseMove(object sender, MouseEventArgs e)
        {
            if (!img.IsMouseCaptured) return;
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
            if (Properties.Settings.Default.ScrollEnabled)
            {
                if (e.Delta > 0) Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 30);
                else if (e.Delta < 0) Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 30);
            }

            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                Zoom(e.Delta, false);
            else
                Zoom(e.Delta, true);
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
            if (IsScrollEnabled)
            {
                var scaletransform = new ScaleTransform();
                scaletransform.ScaleX = scaletransform.ScaleY = 1.0;
                img.LayoutTransform = scaletransform;
            }
            else
            {
                st.ScaleX = st.ScaleY = 1;
                tt.X = tt.Y = 0;
                img.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            CloseToolTipStyle();
            isZoomed = false;

            int width, height;
            if (Preloader.Contains(PicPath))
            {
                width = Preloader.Load(PicPath).PixelWidth;
                height = Preloader.Load(PicPath).PixelHeight;
            }
            else
            {
                var info = new MagickImageInfo(PicPath);
                width = info.Width;
                height = info.Height;
            }
            ZoomFit(width, height);
            var titleString = TitleString(width, height, FolderIndex);
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
            if (!isZoomed)
                AspectRatio = 1.0;

            AspectRatio += i > 0 ? .01 : -.01;

            // Scales the window with img.LayoutTransform
            if (zoomMode)
            {
                var scaletransform = new ScaleTransform();

                if (!isZoomed)
                    if (scaletransform.ScaleX == 1.0)
                        scaletransform.ScaleX = scaletransform.ScaleY += i > 0 ? .01 : -.01;
                    else
                        scaletransform.ScaleY = scaletransform.ScaleX = i > 0 ? .01 : -.01;
                else
                    scaletransform.ScaleY = scaletransform.ScaleX += i > 0 ? .01 : -.01;

                scaletransform.ScaleX = scaletransform.ScaleY = AspectRatio;
                img.LayoutTransform = scaletransform;
            }

            // Pan and zoom
            else
            {
                Point position;

                //if (ZoomLevel > 100)
                position = Mouse.GetPosition(img);
                //else
                //    position = Mouse.GetPosition(imgBorder);

                img.RenderTransformOrigin = new Point(position.X / img.ActualWidth, position.Y / img.ActualHeight);

                if (!isZoomed)
                    if (st.ScaleX == 1.0)
                        st.ScaleX = st.ScaleY += i > 0 ? .01 : -.01;
                    else
                        st.ScaleY = st.ScaleX = i > 0 ? .01 : -.01;
                else
                    st.ScaleY = st.ScaleX += i > 0 ? .01 : -.01;

                ////if (st.ScaleX < MinZoom)
                ////{
                ////    st.ScaleX = st.ScaleY =
                ////    ZoomLevel = MinZoom;
                ////}

                if (st.ScaleX < 1.0)
                {
                    st.ScaleX = st.ScaleY =
                    AspectRatio = 1.0;
                }
            }

            // Displays zoompercentage in the center window
            ToolTipStyle(ZoomPercentage, true);

            if (Preloader.Contains(PicPath))
            {
                var titleString = TitleString(Preloader.Load(PicPath).PixelWidth, Preloader.Load(PicPath).PixelHeight, FolderIndex);
                Title = titleString[0];
                Bar.Text = titleString[1];
                Bar.ToolTip = titleString[2];
            }

            isZoomed = true;
        }

        /// <summary>
        /// Fits image size based on users screen resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void ZoomFit(double width, double height)
        {
            //Aspect ratio calculation
            var maxWidth = Math.Min(SystemParameters.PrimaryScreenWidth, width);
            var maxHeight = Math.Min((SystemParameters.FullPrimaryScreenHeight - 98), height); // 38 = Titlebar height, 60 = lowerbar height

            AspectRatio = Math.Min((maxWidth / width), (maxHeight / height));

            img.Width = xWidth = (width * AspectRatio);
            img.Height = xHeight = (height * AspectRatio);

            if (IsScrollEnabled)
            {
                img.Width = img.Height = double.NaN;
                img.MaxWidth = width;
            }
            else
            {
                img.Width = xWidth = (width * AspectRatio);
                img.Height = xHeight = (height * AspectRatio);
            }

            //Buttons (38 * 3 = 87) logo (canvas width 80 + margin right 7 = 87) = 179 (Bar.MinWidth 444) 444 - 179 = 270 - (comfy space) = 210
            if (xWidth - 221 < 220)
                Bar.MaxWidth = 210;
            else
                Bar.MaxWidth = xWidth - 220;

            isZoomed = false;
        }

        #endregion

        #region Interface stuff

        #region TitleString
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

        #endregion

        #region Toggle scroll || IsScrollEnabled
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
                    ToolTipStyle(value ? "Scrolling enabled" : "Scrolling disabled", false);
                }
            }
        }

        #endregion

        #region UserControl Specifics

        #region Toggle Menu booleans || ImageSettingsMenuOpen, OpenMenuOpen
        /// <summary>
        /// Toggles whether ImageSettingsMenu is open or not with a fade animatiomn 
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
        /// Toggles whether OpenMenu is open or not with a fade animatiomn 
        /// </summary>
        private static bool OpenMenuOpen
        {
            get { return openMenuOpen; }
            set
            {
                openMenuOpen = value;
                openMenu.Visibility = Visibility.Visible;
                var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
                if (!value)
                {
                    da.To = 0;
                    da.Completed += delegate { openMenu.Visibility = Visibility.Hidden; };
                }
                else
                    da.To = 1;
                if (openMenu != null)
                    openMenu.BeginAnimation(OpacityProperty, da);
            }
        }

        #endregion

        #region ToolTipStyle
        private void LoadTooltipStyle()
        {
            sexyToolTip = new SexyToolTip
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden
            };

            bg.Children.Add(sexyToolTip);
            sexyToolTip.MouseWheel += Zoom_img_MouseWheel;
        }

        /// <summary>
        /// Shows a black tooltip on screen in a given time
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="center">If centered or on bottom</param>
        /// <param name="time">How long until it fades away</param>
        private void ToolTipStyle(string message, bool center, TimeSpan time)
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

            sexyToolTip.SexyToolTipText.Text = message;
            var anim = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            anim.Completed += (s, _) => AnimationHelper.Fade(sexyToolTip, TimeSpan.FromSeconds(1.5), time, 1, 0);

            sexyToolTip.BeginAnimation(OpacityProperty, anim);
        }

        /// <summary>
        /// Shows a black tooltip on screen for a small time
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="center">If centered or on bottom</param>
        private void ToolTipStyle(string message, bool center)
        {
            ToolTipStyle(message, center, TimeSpan.FromSeconds(1));
        }

        private void CloseToolTipStyle()
        {
            sexyToolTip.Visibility = Visibility.Hidden;
        }
        #endregion

        #region AboutWindow
        private void LoadAboutWindow()
        {
            about_uc = new UserControls.About()
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden
            };

            about_uc.CloseButton.MouseEnter += (s, e) =>
            {
                AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, about_uc.CloseButtonBrush, true);
            };
            about_uc.CloseButton.MouseLeave += (s, e) =>
            {
                AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, about_uc.CloseButtonBrush, true);
            };
            about_uc.CloseButton.PreviewMouseLeftButtonDown += (s, e) =>
            {
                AnimationHelper.PreviewMouseLeftButtonDownColorEvent(about_uc.CloseButtonBrush, true);
            };
            bg.Children.Add(about_uc);
        }

        private void AboutWindow()
        {
            about_uc.Visibility = Visibility.Visible;
            var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
            if (about_uc.Opacity > 0)
            {
                da.To = 0;
                da.Completed += delegate { about_uc.Visibility = Visibility.Hidden; };
            }
            else
                da.To = 1;
            if (about_uc != null)
                about_uc.BeginAnimation(OpacityProperty, da); ;
        }
        #endregion

        #region HelpWindow
        private void LoadHelpWindow()
        {
            help_uc = new UserControls.Help()
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden
            };

            help_uc.CloseButton.MouseEnter += (s, e) =>
            {
                AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, help_uc.CloseButtonBrush, true);
            };
            help_uc.CloseButton.MouseLeave += (s, e) =>
            {
                AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, help_uc.CloseButtonBrush, true);
            };
            help_uc.CloseButton.PreviewMouseLeftButtonDown += (s, e) =>
            {
                AnimationHelper.PreviewMouseLeftButtonDownColorEvent(help_uc.CloseButtonBrush, true);
            };
            bg.Children.Add(help_uc);
        }

        private void HelpWindow()
        {
            help_uc.Visibility = Visibility.Visible;
            var da = new DoubleAnimation { Duration = TimeSpan.FromSeconds(.3) };
            if (help_uc.Opacity > 0)
            {
                da.To = 0;
                da.Completed += delegate { help_uc.Visibility = Visibility.Hidden; };
            }
            else
                da.To = 1;
            if (help_uc != null)
                help_uc.BeginAnimation(OpacityProperty, da); ;
        }
        #endregion

        #region OpenMenu
        private void LoadOpenMenu()
        {
            Helper.openMenu = new OpenMenu
            {
                Focusable = false,
                Opacity = 0,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 152, 0)
            };

            bg.Children.Add(Helper.openMenu);
        }
        #endregion

        #region ImageSettingsMenu
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
        #endregion

        #region AjaxLoading
        private void LoadAjaxLoading()
        {
            ajaxLoading = new AjaxLoading
            {
                Focusable = false,
                Opacity = 0
            };

            bg.Children.Add(ajaxLoading);
        }
        private void AjaxLoadingStart()
        {
            AnimationHelper.Fade(ajaxLoading, 1, TimeSpan.FromSeconds(.2));
        }

        private void AjaxLoadingEnd()
        {
            AnimationHelper.Fade(ajaxLoading, 0, TimeSpan.FromSeconds(.2));
        }
        #endregion

        #region Close UserControls!!

        private void Close_UserControls()
        {
            //if (EffectsWindowOpen)
            //    EffectsWindowOpen = false;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;

            if (OpenMenuOpen)
                OpenMenuOpen = false;

            //if (SettingsWindowOpen)
            //    SettingsWindowOpen = false;
        }

        private void Close_UserControls(object sender, RoutedEventArgs e)
        {
            Close_UserControls();
        }

        #endregion

        #region Toggle Usercontrols

        private void Toggle_open_menu(object sender, RoutedEventArgs e)
        {
            OpenMenuOpen = !OpenMenuOpen;

            if (ImageSettingsMenuOpen)
                ImageSettingsMenuOpen = false;
        }

        private void Toggle_image_menu(object sender, RoutedEventArgs e)
        {
            ImageSettingsMenuOpen = !ImageSettingsMenuOpen;

            if (OpenMenuOpen)
                OpenMenuOpen = false;
        }

        #endregion

        #endregion

        #region MouseOver Button Events
        /*
        
            Adds MouseOver events for the given elements with the AnimationHelper.
            Changes color depending on the users settings.

        */
        #region Logo Mouse Over

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

        #endregion

        #region Close Button

        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 20, 20, 20, CloseButtonBrush, false);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, false);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 20, 20, 20, CloseButtonBrush, false);
        }

        #endregion

        #region MaxButton

        private void MaxButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 20, 20, 20, MaxButtonBrush, false);
        }

        private void MaxButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(MaxButtonBrush, false);
        }

        private void MaxButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 20, 20, 20, MaxButtonBrush, false);
        }

        #endregion

        #region MinButton

        private void MinButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 20, 20, 20, MinButtonBrush, false);
        }

        private void MinButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(MinButtonBrush, false);
        }

        private void MinButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 20, 20, 20, MinButtonBrush, false);
        }

        #endregion

        #region LeftButton

        private void LeftButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, LeftArrowFill, false);
        }

        private void LeftButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(LeftArrowFill, false);
        }

        private void LeftButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, LeftArrowFill, false);
        }

        #endregion

        #region RightButton

        private void RightButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, RightArrowFill, false);
        }

        private void RightButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(RightArrowFill, false);
        }


        private void RightButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, RightArrowFill, false);
        }

        #endregion

        #region OpenMenuButton

        private void OpenMenuButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, FolderFill, false);
        }

        private void OpenMenuButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FolderFill, false);
        }

        private void OpenMenuButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, FolderFill, false);
        }

        #endregion

        #region ImageButton

        private void ImageButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, ImagePath1Fill, false);
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, ImagePath2Fill, false);
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, ImagePath3Fill, false);
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, ImagePath4Fill, false);
        }

        private void ImageButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath1Fill, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath2Fill, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath3Fill, false);
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ImagePath4Fill, false);
        }

        private void ImageButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, ImagePath1Fill, false);
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, ImagePath2Fill, false);
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, ImagePath3Fill, false);
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, ImagePath4Fill, false);
        }

        #endregion

        #region SettingsButton

        private void SettingsButtonButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 245, 245, 245, SettingsButtonFill, false);
        }

        private void SettingsButtonButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SettingsButtonFill, false);
        }

        private void SettingsButtonButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 245, 245, 245, SettingsButtonFill, false);
        }

        #endregion

        #endregion

        #region ScrollViewer

        //private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    var border = VisualTreeHelper.GetChild(listbox, 0) as Decorator;

        //    // Get scrollviewer
        //    var scrollviewer = border.Child as ScrollViewer;

        //    if (e.Delta > 0)
        //        scrollviewer.LineRight();
        //    else
        //        scrollviewer.LineLeft();
        //    e.Handled = true;
        //}

        #region Scroller events
        //private async void Scroller_MouseLeave(object sender, MouseEventArgs e)
        //{
        //    if (img.Source == null)
        //        return;

        //    await Task.Delay(TimeSpan.FromSeconds(2.4));
        //    var s = Scroller.Template.FindName("PART_VerticalScrollBar", Scroller) as System.Windows.Controls.Primitives.ScrollBar;
        //    AnimationHelper.Fade(s, 0, TimeSpan.FromSeconds(1));
        //}

        //void Scroller_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    if (img.Source == null)
        //        return;

        //    var s = Scroller.Template.FindName("PART_VerticalScrollBar", Scroller) as System.Windows.Controls.Primitives.ScrollBar;
        //    AnimationHelper.Fade(s, 1, TimeSpan.FromSeconds(.7));
        //}
        #endregion

        #endregion

        #endregion

        #region Rotation and Flipping
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
                    imageSettingsMenu.ro0.IsChecked = true;
                    imageSettingsMenu.ro90.IsChecked = false;
                    imageSettingsMenu.ro180.IsChecked = false;
                    imageSettingsMenu.ro270.IsChecked = false;
                    break;

                case 180:
                    imageSettingsMenu.ro180.IsChecked = true;
                    imageSettingsMenu.ro90.IsChecked = false;
                    imageSettingsMenu.ro0.IsChecked = false;
                    imageSettingsMenu.ro270.IsChecked = false;
                    break;

                case 90:
                    imageSettingsMenu.ro90.IsChecked = true;
                    imageSettingsMenu.ro0.IsChecked = false;
                    imageSettingsMenu.ro180.IsChecked = false;
                    imageSettingsMenu.ro270.IsChecked = false;
                    break;

                case 270:
                    imageSettingsMenu.ro270.IsChecked = true;
                    imageSettingsMenu.ro90.IsChecked = false;
                    imageSettingsMenu.ro180.IsChecked = false;
                    imageSettingsMenu.ro0.IsChecked = false;
                    break;
                default: imageSettingsMenu.ro0.IsChecked = true;
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

        #region Open + Copy/Paste

        #region copy/Paste
        /// <summary>
        /// Copy image location to clipboard
        /// </summary>
        private void CopyText()
        {
            Clipboard.SetText(PicPath);
            ToolTipStyle(TxtCopy, false);
        }

        /// <summary>
        /// Add image to clipboard
        /// </summary>
        private void CopyPic()
        {
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
            ToolTipStyle(FileCopy, false);
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

            else ToolTipStyle("An error occured while trying to paste file", false);

            #endregion
        }

        #endregion

        #region Open and Open_In_Eplorer
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
                ToolTipStyle(ExpFind, false);
                Process.Start("explorer.exe", "/select,\"" + PicPath + "\"");
            }
            catch (InvalidCastException)
            {
                //error_window("Error", e.Message);
            }
        }

        /// <summary>
        /// Open a file dialog where usr can select a supported file
        /// </summary>
        private void Open()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                // Needs support for not being case sensitive 
                Filter = "All Supported files|*.bmp;*.jpg;*.png;*.tif;*.gif;*.ico;*.jpeg;*.wdp;*.psd;*.psb;*.cbr;*.cb7;*.cbt;"
                + "*.cbz;*.xz;*.orf;*.cr2;*.crw;*.dng;*.raf;*.ppm;*.raw;*.mrw;*.nef;*.pef;*.3xf;*.arw;"
                ////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
                + "|Pictures|*.bmp;*.jpg;*.png;.tif;*.gif;*.ico;*.jpeg*.wdp*"                                   // Common pics
                + "|jpg| *.jpg;*.jpeg;*"                                                                        // JPG
                + "|bmp|*.bmp;*"                                                                                // BMP
                + "|png|*.png;*"                                                                                // PNG
                + "|gif|*.gif;*"                                                                                // GIF
                + "|ico|*.ico;*"                                                                                // ICO
                + "|wdp|*.wdp;*"                                                                                // WDP
                + "|svg|*.svg;*"                                                                                // SVG
                + "|tif|*.tif;*"                                                                                // Tif
                + "|Photoshop|*.psd;*.psb"                                                                      // PSD
                + "|Archives|*.zip;*.7zip;*.7z;*.rar;*.bzip2;*.tar;*.wim;*.iso;*.cab"                           // Archives
                + "|Comics|*.cbr;*.cb7;*.cbt;*.cbz;*.xz"                                                        // Comics
                + "|Camera files|*.orf;*.cr2;*.crw;*.dng;*.raf;*.ppm;*.raw;*.mrw;*.nef;*.pef;*.3xf;*.arw",      // Camera files
                ////////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
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
        #endregion

        #endregion     
    }
}
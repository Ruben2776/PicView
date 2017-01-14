using Nepharia.lib;
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
using static Nepharia.lib.Helper;
using static Nepharia.lib.ImageManager;
using Microsoft.WindowsAPICodePack.Shell;
using System.Reflection;
using ImageMagick;
using PicView.lib;
using Nepharia.lib.UserControls;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Net;
using System.Threading;

namespace Nepharia
{
    public partial class MainWindow : Window
    {
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (s, e) => MainWindow_Loaded(s,e);

            ContentRendered += MainWindow_ContentRendered;
        }

        #endregion

        #region Private, Public and Internal fields

        #region Strings
        internal const string Loading = "Loading...";
        private const string TxtCopy = "Filename copied to Clipboard";
        private const string FileCopy = "File copied to Clipboard";
        private const string ExpFind = "Locating in file explorer";
        private const string NoImage = "No image loaded";
        private const string DragOverString = "Drop to load image";
        private const string SevenZipFiles = " *.jpg *jpeg. *.png *.gif *.jpe *.bmp *.tiff *.tif *.ico *.wdp *.dds *.svg";
        /// <summary>
        /// File path of current  image
        /// </summary>
        internal static string PicPath { get; set; }
        /// <summary>
        /// Backup of PicPath
        /// </summary>
        internal static string xPicPath { get; set; }
        /// <summary>
        /// File path for the extracted folder
        /// </summary>
        private static string TempZipPath { get; set; }
        /// <summary>
        /// Returns string with zoom %
        /// </summary>
        private static string ZoomPercentage { get { return Math.Round(AspectRatio * 100) + "%"; } }
        /// <summary>
        /// Returns zoom % if not zero. Empty string for zero
        /// </summary>
        private static string Zoomed
        {
            get
            {
                var zoom = Math.Round(AspectRatio * 100);
                if (zoom == 100)
                    return string.Empty;

                return " - " + zoom + "%";
            }
        }
        /// <summary>
        /// Returns aspect ratio as a formatted string
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static string StringAspect(int width, int height)
        {
            var gcd = GCD(width, height);
            var x = (width / gcd);
            var y = (height / gcd);

            if (x == width && y == height)
                return ") ";

            return ", " + x + ":" + y + ") ";
        }

        #endregion

        #region Integers and Doubles
        /// <summary>
        /// Used as comfortable space for standard viewing
        /// </summary>
        private const int ComfySpace = 90;
        private static double xWidth { get; set; }
        private static double xHeight { get; set; }

        /// <summary>
        /// Counter used to get/set current index
        /// </summary>
        private static int FolderIndex { get; set; }
        private static int xFolderIndex { get; set; }

        /// <summary>
        /// Counter used to check if preloading is neccesary
        /// </summary>
        private static short PreloadCount { get; set; }

        private const double MinZoom = 0.3;
        private static double AspectRatio { get; set; }
        private static int Rotateint { get; set; }

        #endregion

        #region Booleans
        //private static bool LeftbuttonClicked;
        //private static bool RightbuttonClicked;
        private static bool GoToPic;
        //private static bool cursorHidden;
        private static bool isZoomed;
        private static bool Flipped;
        private static bool canNavigate;
        //private static bool mouseIsOnArrow;
        private static bool isDraggedOver;
        private static bool freshStartup;

        private bool IsScrollEnabled
        {
            get { return Properties.Settings.Default.ScrollEnabled; }
            set
            {
                Properties.Settings.Default.ScrollEnabled = value;
                Scroller.VerticalScrollBarVisibility = value ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
                if (!freshStartup && !string.IsNullOrEmpty(PicPath))
                {
                    ZoomFit(Preloader.Load(PicPath).PixelWidth, Preloader.Load(PicPath).PixelHeight);
                    ToolTipStyle(value ? "Scrolling enabled" : "Scrolling disabled", false);
                }
            }
        }

        #endregion

        #region Controls
        //private static Effects EffectsWindow;
        //private static ImageSettings ImageSettingsWindow;
        //private static OpenMenu OpenWindow;
        //private static Settings SettingsWindow;
        //private static RightArrow FullScreenRightArrow;
        //private static LeftArrow FullScreenLeftArrow;
        //private static Alpha_X Alpha_X2;
        private static AjaxLoading ajaxLoading;
        private static SexyToolTip sexyToolTip;
        private static UserControls.About about_uc;
        private static UserControls.Help help_uc;
        #endregion

        #region Points + Scaletransform & TranslateTransform
        private static Point origin;
        private static Point start;

        private static ScaleTransform st;
        private static TranslateTransform tt;
        #endregion

        #region Lists
        public static List<string> Pics { get; set; }
        //public static List<MyBitmaps> MyImages { get; set; }
        #endregion

        #region ... And the rest!
        private static readonly Color bgBackColor = Color.FromArgb(242, 20, 20, 20); //#F2141414
        //http://www.netgfx.com/RGBaZR/
        private static ImageSource prevPicResource;
        //private static System.Timers.Timer activityTimer;
        private static ContextMenu cm;
        #endregion

        #endregion

        #region Window Logic

        #region Load Events
        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            #region Extra settings
            Scroller.MaxHeight = SystemParameters.PrimaryScreenHeight - ComfySpace;
            Scroller.MaxWidth = SystemParameters.PrimaryScreenWidth - 8;
            IsScrollEnabled = Properties.Settings.Default.ScrollEnabled;
            AllowDrop = true;

            var WindowBrush = FindResource("BorderBrush") as SolidColorBrush;
            WindowBrush.Color = AnimationHelper.GetPrefferedColorDown();
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
                UnLoad();
            else
            {
                var file = Application.Current.Properties["ArbitraryArgName"].ToString();
                if (File.Exists(file))
                    Pic(file);
                else
                    UnLoad();
            }
            #endregion   

            #region Add ContextMenu

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
            wallcm.Click += (s, x) => setWallpaper(PicPath, WallpaperStyle.Fill);
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

            var nextcm = new MenuItem
            {
                Header = "Next picture",
                InputGestureText = "ᗌ or D",
                ToolTip = "Go to Next image in folder",
                StaysOpenOnClick = true
            };
            nextcm.Click += (s, x) => Pic(true, false);
            cm.Items.Add(nextcm);

            var prevcm = new MenuItem
            {
                Header = "Previous picture",
                InputGestureText = "ᗏ or A",
                ToolTip = "Go to previous image in folder",
                StaysOpenOnClick = true
            };
            prevcm.Click += (s, x) => Pic(false, false);
            cm.Items.Add(prevcm);

            var firstcm = new MenuItem
            {
                Header = "First picture",
                InputGestureText = "Ctrl + D or Ctrl + ᗌ",
                ToolTip = "Go to first image in folder"
            };
            firstcm.Click += (s, x) => Pic(false, true);
            cm.Items.Add(firstcm);

            var lastcm = new MenuItem
            {
                Header = "Last picture",
                InputGestureText = "Ctrl + A or Ctrl + ᗏ",
                ToolTip = "Go to last image in folder"
            };
            lastcm.Click += (s, x) => Pic(true, true);
            cm.Items.Add(lastcm);

            var unloadcm = new MenuItem
            {
                Header = "Clear picture"
            };
            unloadcm.Click += (s, x) => UnLoad();
            cm.Items.Add(unloadcm);
            cm.Items.Add(new Separator());

            var abcm = new MenuItem
            {
                Header = "About",
                InputGestureText = "F2",
                ToolTip = "Shows version and copyright"
            };
            abcm.Click += (s, x) => about_window();
            cm.Items.Add(abcm);

            var helpcm = new MenuItem
            {
                Header = "Help",
                InputGestureText = "F1",
                ToolTip = "Shows keyboard shortcuts and general help"
            };
            helpcm.Click += (s, x) => help_window();
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

            img.ContextMenu = bg.ContextMenu = cm;

            #endregion

            #region Initilize Zoom
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

                #endregion

                #region Bar
                Bar.MouseLeftButtonDown += Move;
                #endregion

                #region img

                img.MouseLeftButtonDown += img_MouseLeftButtonDown;
                img.MouseLeftButtonUp += img_MouseLeftButtonUp;

                img.MouseMove += img_MouseMove;
                img.MouseWheel += img_MouseWheel;
                //img.MouseEnter += img_MouseEnter;
                //img.MouseLeave += img_MouseLeave;

                #endregion

                #region bg
                //bg.MouseMove += MouseMoves;
                //bg.MouseLeave += MouseLeaves;
                bg.Drop += image_Drop_1;
                bg.DragEnter += image_DragEnter_1;
                bg.DragLeave += bg_DragLeave;
                //bg.MouseEnter += bg_MouseEnter;
                //bg.MouseLeave += bg_MouseLeave;
                //bg.MouseLeftButtonDown += bg_MouseLeftButtonDown;
                #endregion

                #region Scroller
                //Scroller.MouseEnter += Scroller_MouseEnter;
                //Scroller.MouseLeave += Scroller_MouseLeave;
                #endregion

                #endregion

                #region Update settings if needed
                if (Properties.Settings.Default.CallUpgrade)
                {
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.CallUpgrade = false;
                }
                #endregion
            });
            task.Start();
            #endregion

            #region Add UserControls :)
            LoadTooltipStyle();
            LoadAboutWindow();
            LoadHelpWindow();
            #endregion           

            if (img.Source == null)
            {
                AjaxLoadingEnd();
            }
        }

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

            if (size.HeightChanged)
            {
                Top += (size.PreviousSize.Height - size.NewSize.Height) / 2;
            }

            if (size.WidthChanged)
            {
                Left += (size.PreviousSize.Width - size.NewSize.Width) / 2;


                //if (size.NewSize.Width - 220 < 220)
                //    Bar.MaxWidth = 220;
                //else
                //    Bar.MaxWidth = size.NewSize.Width - 220;
            }
            //if (size.HeightChanged || size.WidthChanged)
            //{
            //    CenterWindowOnScreen();
            //}
        }
        #endregion

        #region Center window
        private void CenterWindowOnScreen() //Centers on the primary monitor.. Needs multi monitor solution....
        {
            Top = (SystemParameters.WorkArea.Height - Height) / 2;
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
        }
        #endregion

        #region Move window
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
                    //return;
                }
            }
        }

        #endregion

        #region Window_Closing
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
        private async void Pic(string path)
        {
            #region Set Loading
            Title = Bar.Text = Loading;
            Bar.ToolTip = Loading;
            AjaxLoadingStart();
            #endregion

            #region Check if folder has changed
            if (!string.IsNullOrWhiteSpace(PicPath) && Path.GetDirectoryName(path) != Path.GetDirectoryName(PicPath))
            {
                ChangeFolder();
                freshStartup = true;
                await GetValues(path);
            }
            else if (freshStartup)
                await GetValues(path);
            else FolderIndex = Pics.IndexOf(path);
            #endregion

            #region Check if images exists
            // If there are no pictures, but a folder when TempZipPath has a value,
            // we should open the folder
            if (Pics.Count < 1)
            {
                if (string.IsNullOrWhiteSpace(TempZipPath))
                {
                    // Unexped result, return to clear state.
                    UnLoad();
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
                                if (!File.Exists(PicPath) || Directory.Exists(PicPath) || String.IsNullOrWhiteSpace(PicPath))
                                    UnLoad();
                                else
                                    Pic(PicPath);
                                return;
                            }
                        }
                        catch (Exception)
                        {
                            UnLoad();
                            return;
                        }

                    }
                    else
                    {
                        try
                        {
                            if (Directory.Exists(TempZipPath))
                            {
                                var test = Directory.EnumerateFileSystemEntries(TempZipPath);
                                if (test.Count() > -1)
                                    Pics = FileList(TempZipPath);
                            }
                        }
                        catch (Exception)
                        {
                            UnLoad();
                            return;
                        }
                    }

                    if (count > 0)
                    {
                        Bar.Text = "Still " + Loading + " Attempt " + count + " of 3";
                    }

                    if (count > 3)
                    {
                        UnLoad();
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

            #region Get image
            Pic(FolderIndex);
            #endregion

            #region Set NeedsRefreshStartup
            AjaxLoadingEnd();

            //if (!NeedsRefreshStartup) return;         
            //NeedsRefreshStartup = false;

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
            if (x < 0)
            {
                return;
            }
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
                        img.Source = ShellFile.FromFilePath(Pics[x]).Thumbnail.BitmapSource;
                    #endregion

                    if (freshStartup || PreloadCount < 2 || Preloader.Count() < 0)
                    {
                        // If preloader is not running, load it manually
                        await Task.Run(() => pic = RenderToBitmapSource(Pics[x]));
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
                                    pic = RenderToBitmapSource(Pics[x]);
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

            if (Path.GetExtension(Pics[x]) == ".gif")
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

                    PreLoad(x, reverse.Value);
                    PreloadCount = 4;
                });
                t.Start();
            }

            #endregion

            #region Update the rest
            freshStartup = freshStartup ? false : true;
            Progress(x, Pics.Count);
            CenterWindowOnScreen();
            #endregion

        }
        #endregion

        #region Pic(BitmapSource, imageName)
        private void Pic(BitmapSource pic, string imageName)
        {
            UnLoad();

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

        private void FastPic()
        {
            Bar.ToolTip = Title = Bar.Text = "Image " + (FolderIndex + 1) + " of " + Pics.Count;

            img.Width = xWidth;
            img.Height = xHeight;

            img.Source = Preloader.Contains(Pics[FolderIndex]) ? Preloader.Load(Pics[FolderIndex]) : ShellFile.FromFilePath(Pics[FolderIndex]).Thumbnail.BitmapSource;

            Progress(FolderIndex, Pics.Count);

            GoToPic = true;
        }

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
        private async void PicErrorFix(int x)
        {
            var file = Pics[x];
            Pics.Remove(Pics[x]);
            if (Pics.Count > 0)
            {
                if (FolderIndex + 1 == Pics.Count)
                    FolderIndex = 0;
                else
                    FolderIndex++;

                PreloadCount++;
                Pic(FolderIndex);
                await Task.Delay(300);
                ToolTipStyle("File not found or unable to render, " + file, true, TimeSpan.FromSeconds(3));
            }
        }
        #endregion

        #region load frem web

        private async void PicWeb(string path)
        {
            var backUp = Bar.Text;
            Bar.Text = Loading;

            var pic = await LoadImageWebAsync(path);

            if (pic == null)
            {
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
        }

        private async Task<BitmapSource> LoadImageWebAsync(string address)
        {
            BitmapSource pic = null;
            await Task.Run(async () =>
            {
                var client = new System.Net.WebClient();
                client.DownloadProgressChanged += (sender, e) =>
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        Bar.Text = e.BytesReceived + "/" + e.TotalBytesToReceive + ". " + e.ProgressPercentage + "% complete...";
                    }));

                var bytes = await client.DownloadDataTaskAsync(new Uri(address));
                var stream = new MemoryStream(bytes);
                pic = GetMagickImage(stream);
            });
            return pic;
        }

        #endregion

        #endregion

        #region Preloading
        /// <summary>
        /// Starts decoding images into memory,
        /// based on current index and if reverse or not
        /// </summary>
        /// <param name="index"></param>
        /// <param name="reverse"></param>
        private static void PreLoad(int index, bool reverse)
        {
            #region Forward
            if (!reverse)
            {
                //Add first three
                var i = index + 1 >= Pics.Count ? (index + 1) - Pics.Count : index + 1;
                Preloader.Add(i);
                i = i + 1 >= Pics.Count ? (i + 1) - Pics.Count : i + 1;
                Preloader.Add(i);
                i = i + 1 >= Pics.Count ? (i + 1) - Pics.Count : i + 1;
                Preloader.Add(i);

                //Add two behind
                i = index - 1 < 0 ? Pics.Count - index : index - 1;
                Preloader.Add(i);
                i = i - 1 < 0 ? Pics.Count - i : i - 1;
                Preloader.Add(i);

                //Add one more infront
                i = index + 4 >= Pics.Count ? (index + 4) - Pics.Count : index + 4;
                Preloader.Add(i);

                if (!freshStartup)
                {
                    //Clean up behind
                    var arr = new string[3];
                    i = index - 3 < 0 ? (Pics.Count - index) - 3 : index - 3;
                    if (i > -1 && i < Pics.Count)
                        arr[0] = Pics[i];
                    i = i - 1 < 0 ? (Pics.Count - index) - 1 : i - 1;
                    if (i > -1 && i < Pics.Count)
                        arr[1] = Pics[i];
                    i = i - 1 < 0 ? (Pics.Count - index) - 1 : i - 1;
                    if (i > -1 && i < Pics.Count)
                        arr[2] = Pics[i];
                    Preloader.Clear(arr);
                }
            }
            #endregion

            #region Backwards
            else
            {
                //Add first three
                var i = index - 1 < 0 ? Pics.Count : index - 1;
                Preloader.Add(i);
                i = i - 1 < 0 ? Pics.Count : i - 1;
                Preloader.Add(i);
                i = i - 1 < 0 ? Pics.Count : i - 1;
                Preloader.Add(i);

                //Add two behind
                i = index + 1 >= Pics.Count ? (i + 1) - Pics.Count : index + 1;
                Preloader.Add(i);
                i = i + 1 >= Pics.Count ? (i + 1) - Pics.Count : i + 1;
                Preloader.Add(i);

                //Add one more infront
                i = index - 4 < 0 ? (index + 4) - Pics.Count : index - 4;
                Preloader.Add(i);

                if (!freshStartup)
                {
                    //Clean up behind
                    var arr = new string[3];
                    i = index + 3 > Pics.Count - 1 ? Pics.Count - 1 : index + 3;
                    arr[0] = Pics[i];
                    i = index + 4 > Pics.Count - 1 ? Pics.Count - 1 : index + 4;
                    arr[1] = Pics[i];
                    i = index + 5 > Pics.Count - 1 ? Pics.Count - 1 : index + 5;
                    arr[2] = Pics[i];
                    Preloader.Clear(arr);
                }
            }
            #endregion
        }

        #endregion

        #region GetValues

        private static Task GetValues(string path)
        {
            return Task.Run(() =>
            {
                bool zipped = false;
                var extension = Path.GetExtension(path);
                if (extension != null)
                    switch (extension.ToLower())
                    {
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
                            var sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\7-Zip\\7z.exe";
                            if (!File.Exists(sevenZip))
                                sevenZip = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\7-Zip\\7z.exe";
                            if (File.Exists(sevenZip))
                            {
                                TempZipPath = Path.GetTempPath() + Path.GetRandomFileName();
                                Directory.CreateDirectory(TempZipPath);

                                var x = Process.Start(new ProcessStartInfo
                                {
                                    FileName = sevenZip,
                                    Arguments = "x \"" + path + "\" -o" + TempZipPath + SevenZipFiles + " -r -aou",
                                    WindowStyle = ProcessWindowStyle.Hidden
                                });
                                if (x == null) goto default;
                                x.EnableRaisingEvents = true;
                                x.Exited += (s, e) => Pics = FileList(TempZipPath);
                                x.WaitForExit(200);
                                zipped = true;
                            }
                            else goto default;
                            break;
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
                        case ".dds":
                        case ".svg":
                        case ".psd":
                        case ".psb":
                            break;
                        default:
                            Pics = new List<string>();
                            FolderIndex = -1;
                            TempZipPath = string.Empty;
                            return;
                    }

                if (zipped)
                {
                    if (FolderIndex > -1)
                    {
                        xFolderIndex = FolderIndex;
                    }
                    if (!string.IsNullOrWhiteSpace(PicPath))
                    {
                        xPicPath = PicPath;
                    }
                    FolderIndex = 0;
                    if (Directory.Exists(TempZipPath))
                    {
                        var test = Directory.EnumerateFileSystemEntries(TempZipPath);
                        if (test.Count() > -1)
                            Pics = FileList(TempZipPath);
                    }
                    //else
                    //    Pics = new List<string>();
                }
                else
                {
                    Pics = FileList(Path.GetDirectoryName(path));
                    FolderIndex = Pics.IndexOf(path);
                }

                PicPath = path;
            });
            //.ContinueWith((t) => Parallel.Invoke(LoadImages, () => preLoad(10, FolderIndex, false)));
            //.ContinueWith((t) =>
            //    {
            //        if (Pics.Count > 1)
            //        {
            //            //var thread = new Thread(new ThreadStart(() => LoadImages()));
            //            //thread.Priority = ThreadPriority.Lowest;
            //            //thread.IsBackground = true;
            //            //thread.Start();

            //            if (Pics.Count > 100)
            //                Task.Factory.StartNew(() => LoadImages(), TaskCreationOptions.LongRunning);
            //            else
            //                Task.Run(() => LoadImages());
            //        }
            //    });
        }

        #endregion

        #region ChangeFolder + re/unLoad
        /// <summary>
        /// Clears data, to free objects no longer necessary to store in memory and allow changing folder without error.
        /// </summary>
        private void ChangeFolder()
        {
            Pics.Clear();
            Preloader.Clear();
            DeleteTempFiles();
        }

        #region Reload and Unload

        //private void Reload()
        //{
        //    Pics.Clear();
        //    Pics = FileList(Path.GetDirectoryName(PicPath));

        //    Pic(FolderIndex);
        //}

        //private void Reload(object sender, RoutedEventArgs e)
        //{
        //    Reload();
        //}

        private void UnLoad()
        {
            Bar.Text = Title = NoImage;
            Bar.ToolTip = NoImage;
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

        #region TitleString

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

        #region Drag and Drop

        bool? drag_drop_check(string[] files)
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

        private void image_DragEnter_1(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (drag_drop_check(files).HasValue && drag_drop_check(files).Value)
                return;

            isDraggedOver = true;

            ToolTipStyle(DragOverString, false);

            if (drag_drop_check(files) == null)
                return;

            if (img.Source == null)
            {
                img.Width = Scroller.ActualWidth;
                img.Height = Scroller.ActualHeight;
            }
            else
            {
                prevPicResource = img.Source;

                if (xWidth > 0 && xHeight > 0)
                {
                    img.Width = xWidth;
                    img.Height = xHeight;
                }
            }

            img.Source = Preloader.Contains(files[0]) ? Preloader.Load(files[0]) : ShellFile.FromFilePath(files[0]).Thumbnail.BitmapSource;

        }

        void bg_DragLeave(object sender, DragEventArgs e)
        {
            if (!isDraggedOver)
                return;

            if (prevPicResource != null)
            {
                img.Source = prevPicResource;
                prevPicResource = null;
            }
            else
            {
                img.Source = null;
            }

            isDraggedOver = false;

            CloseToolTipStyle();
        }

        private void image_Drop_1(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            // Get files as strings
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            // check if valid
            if (drag_drop_check(files).HasValue && drag_drop_check(files).Value)
                return;

            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            if (!string.IsNullOrWhiteSpace(PicPath) && Path.GetDirectoryName(files[0]) == Path.GetDirectoryName(PicPath))
                Pic(Pics.IndexOf(files[0]));
            else
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
                            Pic(true, true); // Go to last if Ctrl held down
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

        #region Zoom and Mouse Buttons

        private void img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Close_UserControls();

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

        private void img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            img.ReleaseMouseCapture();
        }

        private void img_MouseMove(object sender, MouseEventArgs e)
        {
            //if (Properties.Settings.Default.Fullscreen)
            //{
            //    if (cursorHidden)
            //        Cursor = Cursors.Arrow;

            //    activityTimer.Start();
            //}

            if (!img.IsMouseCaptured) return;
            var v = start - e.GetPosition(this);
            tt.X = origin.X - v.X;
            tt.Y = origin.Y - v.Y;
            e.Handled = true;
        }

        private void img_MouseWheel(object sender, MouseWheelEventArgs e)
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

        private static void imgBorder_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            st.ScaleX *= e.DeltaManipulation.Scale.X;
            st.ScaleY *= e.DeltaManipulation.Scale.X;
            tt.X += e.DeltaManipulation.Translation.X;
            tt.Y += e.DeltaManipulation.Translation.Y;

            e.Handled = true;
        }

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

        private void Zoom(int i, bool zoomMode)
        {
            if (!isZoomed)
                AspectRatio = 1.0;

            AspectRatio += i > 0 ? .01 : -.01;

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
            //Buttons (38 * 3 = 87) logo (canvas width 80 + margin right 7 = 87) = 179 (Bar.MinWidth 444) 444 - 179 = 270 - (comfy space) = 210

            //Exit early if ScrollEnabled. Aspectratio won't work in that case.
            if (IsScrollEnabled)
            {
                if (width - 221 < 220)
                    Bar.MaxWidth = 210;
                else
                    Bar.MaxWidth = width - 220;

                img.Width = img.Height = double.NaN;
                img.MaxWidth = width;
                return;
            }

            img.MaxWidth = double.PositiveInfinity;

            //Aspect ratio calculation
            var maxWidth = Math.Min(SystemParameters.PrimaryScreenWidth, width);
            var maxHeight = Math.Min((SystemParameters.FullPrimaryScreenHeight - 38), height); // 38 = Titlebar height

            AspectRatio = Math.Min((maxWidth / width), (maxHeight / height));

            img.Width = xWidth = (width * AspectRatio);
            img.Height = xHeight = (height * AspectRatio);

            if (xWidth - 221 < 220)
                Bar.MaxWidth = 210;
            else
                Bar.MaxWidth = xWidth - 220;

            isZoomed = false;
        }

        #endregion

        #region Interface stuff

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
            sexyToolTip.MouseWheel += img_MouseWheel;
        }

        private void ToolTipStyle(string path, bool center, TimeSpan time)
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

            sexyToolTip.SexyToolTipText.Text = path;
            var anim = new DoubleAnimation(1, TimeSpan.FromSeconds(.5));
            anim.Completed += (s, _) => AnimationHelper.Fade(sexyToolTip, TimeSpan.FromSeconds(1.5), time, 1, 0);

            sexyToolTip.BeginAnimation(OpacityProperty, anim);
        }

        private void ToolTipStyle(string path, bool center)
        {
            ToolTipStyle(path, center, TimeSpan.FromSeconds(1));
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

        private void about_window()
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

        private void help_window()
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

        #region MouseOver Button Events

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

        #endregion

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



        #endregion

        #endregion

        #region DeleteTempFiles
        private static void DeleteTempFiles()
        {
            if (!Directory.Exists(TempZipPath))
                return;
            try
            {
                Array.ForEach(Directory.GetFiles(TempZipPath), File.Delete);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                Directory.Delete(TempZipPath);
            }
            catch (Exception)
            {
                return;
            }

        }
        #endregion

        #region Rotation and Flipping

        private void Rotate(int r)
        {
            if (img.Source == null)
            {
                return;
            }
            var rt = new RotateTransform { Angle = Rotateint = r };

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
        }

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

        private void CopyText() // Copy image location to clipboard
        {
            Clipboard.SetText(PicPath);
            ToolTipStyle(TxtCopy, false);
        }

        private void CopyPic() // Add image to clipboard
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

            if (Clipboard.ContainsImage())
            {
                Pic(Clipboard.GetImage(), "Clipboard Image");
                return;
            }

            #region text/string/adddress

            var s = Clipboard.GetText(TextDataFormat.Text);

            if (string.IsNullOrEmpty(s))
                return;

            if (FilePathHasInvalidChars(s))
                MakeValidFileName(s);

            s = s.Replace("\"", "");
            s = s.Trim(); // Removes spaces

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

        private void Open_In_Explorer()
        {
            if (!File.Exists(PicPath) || img.Source == null)
            {
                ToolTipStyle("Error, File does not exist, or something went wrong...", true);
                return;
            }
            try
            {
                //Close_UserControls();
                ToolTipStyle(ExpFind, false);
                Process.Start("explorer.exe", "/select,\"" + PicPath + "\"");
            }
            catch (InvalidCastException)
            {
                //error_window("Error", e.Message);
            }
        }

        private void Open()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter =
                "Pictures|*.bmp;*.jpg;*.png;.tif;*.gif;*.ico;*.jpeg*.wdp*"                  // Common pics
                + "|jpg| *.jpg *.jpeg *"                                                    // JPG
                + "|bmp|*.bmp*"                                                             // BMP
                + "|png|*.png*"                                                             // PNG
                + "|gif|*.gif*"                                                             // GIF
                + "|ico|*.ico*"                                                             // ICO
                + "|wdp|*.wdp*"                                                             // WDP
                + "|svg|*.svg*"                                                             // SVG
                + "|tif|*.tif*"                                                             // Tif
                + "|Photoshop|*.psd *.psb"                                                  // PSD
                + "|Archives|*.zip *.7zip *.7z *.rar *.bzip2 *.tar *.wim *.iso *.cab"       // Archives
                + "|Comics|*.cbr *.cb7 *.cbt *.cbz *.xz";                                   // Comics
            dlg.Title = "Open image - PicView";
            if (dlg.ShowDialog() == true)
            {
                Pic(dlg.FileName);

                if (string.IsNullOrWhiteSpace(PicPath))
                    PicPath = dlg.FileName;
            }
            else return;
        }
        #endregion

        #endregion

        #region Wallpaper

        private void setWallpaper(string path, WallpaperStyle style)
        {
            if (canNavigate)
            {
                if (File.Exists(path))
                    Task.Run(() => Wallpaper.SetDesktopWallpaper(path, style));
            }
            else
            {
                Task.Run(() =>
                {
                    //Handle if file from web, need clipboard image solution
                    var tempPath = Path.GetTempPath();
                    var randomName = Path.GetRandomFileName();
                    var webClient = new WebClient();
                    Directory.CreateDirectory(tempPath);
                    webClient.DownloadFile(path, tempPath + randomName);
                    Wallpaper.SetDesktopWallpaper(tempPath + randomName, style);
                    File.Delete(tempPath + randomName);
                    Thread.Sleep(2000);
                    Directory.Delete(tempPath);
                });
            }
        }
        #endregion
    }
}
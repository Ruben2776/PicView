using ImageMagick;
using PicView.WPF.ChangeImage;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.ImageHandling;
using PicView.WPF.Properties;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic.Sizing;
using PicView.WPF.Views.UserControls.Misc;
using PicView.WPF.Views.Windows;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.Loading.LoadContextMenus;
using static PicView.WPF.UILogic.Loading.LoadControls;
using static PicView.WPF.UILogic.Sizing.WindowSizing;
using static PicView.WPF.UILogic.TransformImage.ZoomLogic;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.UILogic.Loading
{
    internal static class StartLoading
    {
        internal static async Task LoadedEvent(Startup_Window startupWindow)
        {
            var spinner = GetSpinWaiter;
            spinner = new SpinWaiter();
            startupWindow.TheGrid.Children.Add(spinner);
            Panel.SetZIndex(spinner, 99);
            BitmapSource? bitmapSource = null;
            var image = new Image
            {
                Stretch = Stretch.Uniform,
            };
            // Load sizing properties
            MonitorInfo = MonitorSize.GetMonitorSize(startupWindow);
            if (Settings.Default.AutoFitWindow == false && Settings.Default.Fullscreen == false)
            {
                SetLastWindowSize(startupWindow);
            }

            var args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
            {
                await Task.Run(async () =>
                {
                    using var magickImage = new MagickImage();
                    magickImage.Ping(args[1]);
                    var thumb = magickImage.GetExifProfile()?.CreateThumbnail();
                    var bitmapThumb = thumb?.ToBitmapSource();
                    bitmapThumb?.Freeze();
                    await startupWindow.Dispatcher.InvokeAsync(() =>
                    {
                        image.Source = bitmapThumb;
                        startupWindow.TheGrid.Children.Add(image);
                    });
                });
                bitmapSource = await Image2BitmapSource.ReturnBitmapSourceAsync(new FileInfo(args[1])).ConfigureAwait(false);
                await startupWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (Settings.Default.AutoFitWindow)
                    {
                        var size = Core.Calculations.ImageSizeCalculationHelper.GetImageSize(
                            width: bitmapSource.PixelWidth * MonitorInfo.DpiScaling,
                            height: bitmapSource.PixelHeight * MonitorInfo.DpiScaling,
                            monitorWidth: MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling,
                            monitorHeight: MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling,
                            rotationAngle: 0,
                            stretch: Settings.Default.FillImage,
                            padding: 20 * MonitorInfo.DpiScaling,
                            dpiScaling: MonitorInfo.DpiScaling,
                            fullscreen: Settings.Default.Fullscreen,
                            uiTopSize: ConfigureWindows.GetMainWindow.TitleBar.Height * MonitorInfo.DpiScaling,
                            uiBottomSize: ConfigureWindows.GetMainWindow.LowerBar.Height * MonitorInfo.DpiScaling,
                            galleryHeight: Settings.Default.IsBottomGalleryShown ? Settings.Default.BottomGalleryItemSize : 0,
                            autoFit: true,
                            containerWidth: ConfigureWindows.GetMainWindow.ParentContainer.Width * MonitorInfo.DpiScaling,
                            containerHeight: ConfigureWindows.GetMainWindow.ParentContainer.Height * MonitorInfo.DpiScaling,
                            Settings.Default.ScrollEnabled
                            );
                        image.Stretch = Stretch.Fill;
                        image.Width = startupWindow.Width = size.Width;
                        image.Height = startupWindow.Height = size.Height;
                        CenterWindowOnScreen(startupWindow);
                        ScaleImage.AspectRatio = size.AspectRatio;
                    }
                    image.Source = bitmapSource;
                });
            }

            await startupWindow.Dispatcher.InvokeAsync(() =>
            {
                startupWindow.TheGrid.Children.Remove(spinner);
                var mainWindow = new MainWindow();
                ConfigureWindows.GetMainWindow = mainWindow;

                if (Settings.Default.AutoFitWindow == false)
                {
                    SetLastWindowSize(mainWindow);
                }
                mainWindow.MainImage.Source = image.Source;
                GetSpinWaiter = spinner;
                mainWindow.ParentContainer.Children.Add(spinner);
            });
            Pics = new List<string>();
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                // Set min size to DPI scaling
                ConfigureWindows.GetMainWindow.MinWidth *= MonitorInfo.DpiScaling;
                ConfigureWindows.GetMainWindow.MinHeight *= MonitorInfo.DpiScaling;

                SetWindowBehavior();
                if (Settings.Default.AutoFitWindow == false)
                    SetLastWindowSize(ConfigureWindows.GetMainWindow);

                ConfigureWindows.GetMainWindow.Scroller.VerticalScrollBarVisibility = Settings.Default.ScrollEnabled
                    ? ScrollBarVisibility.Auto
                    : ScrollBarVisibility.Disabled;

                if (!Settings.Default.ShowInterface)
                {
                    ConfigureWindows.GetMainWindow.TitleBar.Visibility =
                        ConfigureWindows.GetMainWindow.LowerBar.Visibility
                            = Visibility.Collapsed;
                }
                else if (!Settings.Default.ShowBottomNavBar)
                {
                    ConfigureWindows.GetMainWindow.LowerBar.Visibility
                        = Visibility.Collapsed;
                }
            });
            if (args.Length > 1)
            {
                await QuickLoad.QuickLoadAsync(args[1], null, bitmapSource).ConfigureAwait(false);
            }
            else
            {
                ErrorHandling.Unload(true);
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                ConfigureWindows.GetMainWindow.Show();
                if (Settings.Default.Fullscreen)
                {
                    if (args.Length < 2)
                    {
                        Settings.Default.Fullscreen = false;
                    }
                    else
                    {
                        Fullscreen_Restore(true);
                    }
                }
                if (Settings.Default.AutoFitWindow)
                {
                    ScaleImage.TryFitImage();
                }
                startupWindow.Close();
            });

            await CustomKeybindings.LoadKeybindings().ConfigureAwait(false);

            ConfigColors.UpdateColor();
        }

        internal static void AddDictionaries()
        {
            // Add dictionaries
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

            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    Source = new Uri(@"/PicView;component/Views/Resources/MainContextMenu.xaml", UriKind.Relative)
                }
            );

            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    Source = new Uri(@"/PicView;component/Views/Resources/WindowContextMenu.xaml", UriKind.Relative)
                }
            );

            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    Source = new Uri(@"/PicView;component/Views/Resources/NavigationContextMenu.xaml", UriKind.Relative)
                }
            );

            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    Source = new Uri(@"/PicView;component/Views/Resources/Icons.xaml", UriKind.Relative)
                }
            );

            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    Source = new Uri(@"/PicView;component/Views/Resources/GalleryContextMenu.xaml", UriKind.Relative)
                }
            );
        }

        internal static void AddUiElementsAndUpdateValues()
        {
            // Update values
            ConfigureWindows.GetMainWindow.FolderButton.BackgroundEvents();
            ConfigureWindows.GetMainWindow.AllowDrop = true;
            ConfigColors.SetColors();

            LoadClickArrow(true);
            LoadClickArrow(false);
            Loadx2();
            LoadMinus();
            LoadRestoreButton();
            LoadGalleryShortcut();

            // Update WindowStyle
            if (!Settings.Default.ShowInterface)
            {
                GetClickArrowLeft.Opacity =
                    GetClickArrowRight.Opacity =
                        GetX2.Opacity =
                            GetMinus.Opacity =
                                GetRestoreButton.Opacity =
                                    GetGalleryShortcut.Opacity =
                                        0;

                GetClickArrowLeft.Visibility =
                    GetClickArrowRight.Visibility =
                        GetX2.Visibility =
                            GetMinus.Visibility =
                                GetGalleryShortcut.Visibility =
                                    GetRestoreButton.Visibility =
                                        Visibility.Visible;
            }
            else if (Settings.Default.Fullscreen)
            {
                GetClickArrowLeft.Opacity =
                    GetClickArrowRight.Opacity =
                        GetX2.Opacity =
                            GetMinus.Opacity =
                                GetRestoreButton.Opacity =
                                        1;

                GetClickArrowLeft.Visibility =
                    GetClickArrowRight.Visibility =
                        GetX2.Visibility =
                            GetMinus.Visibility =
                                    GetRestoreButton.Visibility =
                                        Visibility.Visible;

                if (!Settings.Default.IsBottomGalleryShown)
                {
                    GetGalleryShortcut.Opacity = 1;
                    GetGalleryShortcut.Visibility = Visibility.Visible;
                }
            }

            // Add UserControls :)
            LoadFileMenu();
            LoadImageSettingsMenu();
            LoadQuickSettingsMenu();
            LoadToolsAndEffectsMenu();
            LoadTooltipStyle();

            // Initialize things!
            InitializeZoom();
            GetFileHistory ??= new FileHistory();

            // Add things!
            Timers.AddTimers();
            AddContextMenus();
        }
    }
}
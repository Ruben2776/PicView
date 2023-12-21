using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.ImageHandling;
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
using static PicView.WPF.UILogic.Loading.LoadContextMenus;
using static PicView.WPF.UILogic.Loading.LoadControls;
using static PicView.WPF.UILogic.Sizing.WindowSizing;
using static PicView.WPF.UILogic.TransformImage.ZoomLogic;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.UILogic.Loading;

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
        await SettingsHelper.LoadSettingsAsync().ConfigureAwait(false);
        if (SettingsHelper.Settings.WindowProperties.AutoFit == false && SettingsHelper.Settings.WindowProperties.Fullscreen == false)
        {
            SetLastWindowSize(startupWindow);
        }

        var args = Environment.GetCommandLineArgs();
        ImageSizeCalculationHelper.ImageSize? size = null;
        MainWindow? mainWindow = null;

        if (args.Length > 1)
        {
            await Task.Run(async () =>
            {
                bitmapSource = await Image2BitmapSource.ReturnBitmapSourceAsync(new FileInfo(args[1]))
                    .ConfigureAwait(false);
            });

            await startupWindow.Dispatcher.InvokeAsync(() =>
            {
                size = ImageSizeCalculationHelper.GetImageSize(
                    width: bitmapSource.PixelWidth * MonitorInfo.DpiScaling,
                    height: bitmapSource.PixelHeight * MonitorInfo.DpiScaling,
                    monitorWidth: MonitorInfo.WorkArea.Width * MonitorInfo.DpiScaling,
                    monitorHeight: MonitorInfo.WorkArea.Height * MonitorInfo.DpiScaling,
                    rotationAngle: 0,
                    stretch: SettingsHelper.Settings.ImageScaling.StretchImage,
                    padding: 20 * MonitorInfo.DpiScaling,
                    dpiScaling: MonitorInfo.DpiScaling,
                    fullscreen: SettingsHelper.Settings.ImageScaling.StretchImage,
                    uiTopSize: 30 * MonitorInfo.DpiScaling,
                    uiBottomSize: 25 * MonitorInfo.DpiScaling,
                    galleryHeight: SettingsHelper.Settings.Gallery.IsBottomGalleryShown
                        ? SettingsHelper.Settings.Gallery.BottomGalleryItemSize
                        : 0,
                    autoFit: SettingsHelper.Settings.WindowProperties.AutoFit,
                    containerWidth: startupWindow.Width * MonitorInfo.DpiScaling,
                    containerHeight: startupWindow.Height * MonitorInfo.DpiScaling,
                    SettingsHelper.Settings.Zoom.ScrollEnabled
                );

                image.Stretch = Stretch.Fill;
                image.Width = startupWindow.Width = size.Value.Width;
                image.Height = startupWindow.Height = size.Value.Height;
                ScaleImage.AspectRatio = size.Value.AspectRatio;

                image.Source = bitmapSource;
                startupWindow.TheGrid.Children.Add(image);

                if (SettingsHelper.Settings.WindowProperties.AutoFit)
                {
                    CenterWindowOnScreen(startupWindow);
                }

                image.Source = bitmapSource;
                mainWindow = new MainWindow();
                ConfigureWindows.GetMainWindow = mainWindow;
                startupWindow.TheGrid.Children.Remove(spinner);
                mainWindow.MainImage.Source = image.Source;
                GetSpinWaiter = spinner;
                mainWindow.ParentContainer.Children.Add(spinner);
            });
        }

        if (args.Length > 1)
        {
            await QuickLoad.QuickLoadAsync(args[1], null, bitmapSource).ConfigureAwait(false);
        }
        else
        {
            ErrorHandling.Unload(true);
        }

        await CustomKeybindings.LoadKeybindings().ConfigureAwait(false);

        await startupWindow.Dispatcher.InvokeAsync(() =>
        {
            if (SettingsHelper.Settings.WindowProperties.AutoFit == false)
            {
                SetLastWindowSize(mainWindow);
            }

            // Set min size to DPI scaling
            mainWindow.MinWidth *= MonitorInfo.DpiScaling;
            mainWindow.MinHeight *= MonitorInfo.DpiScaling;

            SetWindowBehavior();
            if (SettingsHelper.Settings.WindowProperties.AutoFit == false)
                SetLastWindowSize(ConfigureWindows.GetMainWindow);

            mainWindow.Scroller.VerticalScrollBarVisibility = SettingsHelper.Settings.Zoom.ScrollEnabled
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled;

            if (!SettingsHelper.Settings.UIProperties.ShowInterface)
            {
                mainWindow.TitleBar.Visibility =
                    mainWindow.LowerBar.Visibility
                        = Visibility.Collapsed;
            }
            else if (!SettingsHelper.Settings.UIProperties.ShowBottomNavBar)
            {
                mainWindow.LowerBar.Visibility
                    = Visibility.Collapsed;
            }

            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                ConfigureWindows.GetMainWindow.MainImage.Width = startupWindow.ActualWidth;
                ConfigureWindows.GetMainWindow.MainImage.Height = startupWindow.ActualHeight;
            }
            else
            {
                ConfigureWindows.GetMainWindow.Width = startupWindow.ActualWidth;
                ConfigureWindows.GetMainWindow.Height = startupWindow.ActualHeight;
            }

            ConfigureWindows.GetMainWindow.Show();
            if (SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                if (args.Length < 2)
                {
                    SettingsHelper.Settings.WindowProperties.Fullscreen = false;
                }
                else
                {
                    Fullscreen_Restore(true);
                }
            }

            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                ScaleImage.TryFitImage();
            }

            startupWindow.Close();
        });

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
        if (!SettingsHelper.Settings.UIProperties.ShowInterface)
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
        else if (SettingsHelper.Settings.WindowProperties.Fullscreen)
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

            if (!SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
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

        // Add things!
        Timers.AddTimers();
        AddContextMenus();
    }
}
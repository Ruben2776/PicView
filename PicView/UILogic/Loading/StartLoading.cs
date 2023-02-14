using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Loading.LoadContextMenus;
using static PicView.UILogic.Loading.LoadControls;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.TransformImage.ZoomLogic;
using static PicView.UILogic.UC;

namespace PicView.UILogic.Loading
{
    internal static class StartLoading
    {
        internal static void LoadedEvent()
        {
            FreshStartup = true;
            Pics = new List<string>();

            // Load sizing properties
            MonitorInfo = MonitorSize.GetMonitorSize();

            // Set min size to DPI scaling
            ConfigureWindows.GetMainWindow.MinWidth *= MonitorInfo.DpiScaling;
            ConfigureWindows.GetMainWindow.MinHeight *= MonitorInfo.DpiScaling;

            if (Settings.Default.AutoFitWindow == false)
            {
                SetWindowBehavior();
            }

            ConfigureWindows.GetMainWindow.Scroller.VerticalScrollBarVisibility = Settings.Default.ScrollEnabled ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;

            // Set min size to DPI scaling
            ConfigureWindows.GetMainWindow.MinWidth *= MonitorInfo.DpiScaling;
            ConfigureWindows.GetMainWindow.MinHeight *= MonitorInfo.DpiScaling;

            if (!Settings.Default.ShowInterface)
            {
                ConfigureWindows.GetMainWindow.TitleBar.Visibility =
                   ConfigureWindows.GetMainWindow.LowerBar.Visibility
                   = Visibility.Collapsed;
            }
        }

        internal static async Task ContentRenderedEventAsync()
        {
            var args = Environment.GetCommandLineArgs();

            // Determine prefered UI for startup
            if (Settings.Default.StartInFullscreenGallery)
            {
                if (args.Length <= 1)
                {
                    Settings.Default.StartInFullscreenGallery = false;
                }
                else
                {
                    await GalleryToggle.OpenFullscreenGalleryAsync(true).ConfigureAwait(false);
                }
            }

            else if (Settings.Default.Fullscreen)
            {
                if (args.Length <= 1)
                {
                    Settings.Default.Fullscreen = false;
                }
                else
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        Fullscreen_Restore(true);
                    }));
                }
            }

            else if (Settings.Default.Width > 0 && Settings.Default.AutoFitWindow == false)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    SetLastWindowSize();
                }));
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    SetWindowBehavior();
                }));
            }

            // Load image if possible

            if (args.Length <= 1)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    Unload(true); // Load clean setup when starting up without arguments       
                }));
            }
            else
            {
                await QuickLoad.QuickLoadAsync(args[1]).ConfigureAwait(false);
                // TODO maybe load extra images if multiple arguments
            }

            ConfigColors.UpdateColor();

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
                    Source = new Uri(@"/PicView;component/Views/UserControls/Misc/MainContextMenu.xaml", UriKind.Relative)
                }
            );

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                // Load UI and events
                AddUIElementsAndUpdateValues();
            }));
        }

        private static void AddUIElementsAndUpdateValues()
        {
            // Update values
            ConfigureWindows.GetMainWindow.FolderButton.BackgroundEvents();
            ConfigureWindows.GetMainWindow.AllowDrop = true;
            ConfigureSettings.ConfigColors.SetColors();

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
            else if (Settings.Default.Fullscreen)
            {
                GetClickArrowLeft.Opacity =
                GetClickArrowRight.Opacity =
                Getx2.Opacity =
                GetMinus.Opacity =
                GetRestorebutton.Opacity =
                GetGalleryShortcut.Opacity =
                1;

                GetClickArrowLeft.Visibility =
                GetClickArrowRight.Visibility =
                Getx2.Visibility =
                GetMinus.Visibility =
                GetGalleryShortcut.Visibility =
                GetRestorebutton.Visibility =
                Visibility.Visible;
            }

            // Add UserControls :)
            LoadFileMenu();
            LoadImageSettingsMenu();
            LoadQuickSettingsMenu();
            LoadToolsAndEffectsMenu();
            LoadAutoScrollSign();
            LoadTooltipStyle();

            // Initilize Things!
            InitializeZoom();
            GetFileHistory = new FileHistory();

            // Add things!
            Timers.AddTimers();
            AddContextMenus();
        }
    }
}
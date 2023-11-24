using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using System.Windows;
using System.Windows.Controls;
using PicView.Shortcuts;
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
            Pics = new List<string>();

            // Load sizing properties
            MonitorInfo = MonitorSize.GetMonitorSize();

            // Set min size to DPI scaling
            ConfigureWindows.GetMainWindow.MinWidth *= MonitorInfo.DpiScaling;
            ConfigureWindows.GetMainWindow.MinHeight *= MonitorInfo.DpiScaling;

            SetWindowBehavior();
            if (Settings.Default.AutoFitWindow == false)
                SetLastWindowSize();

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

            Task.Run(() =>
            {
                _ = CustomKeybindings.LoadKeybindings().ConfigureAwait(false);
                ConfigColors.UpdateColor();
            });

            var args = Environment.GetCommandLineArgs();

            // Determine preferred UI for startup
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

            if (Settings.Default.IsBottomGalleryShown)
            {
                GalleryToggle.ShowBottomGallery();
            }

            // Load image if possible
            if (args.Length < 2)
            {
                ErrorHandling.Unload(true);

                // Make sure to fix loading spinner showing up
                if (ConfigureWindows.GetMainWindow.MainImage.Source is null)
                {
                    GetSpinWaiter.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                Task.Run(() =>
                {
                    _ = QuickLoad.QuickLoadAsync(args[1]).ConfigureAwait(false);
                });
            }
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
using PicView.Core.Config;
using PicView.Core.ProcessHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.ProcessHandling;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic.Sizing;
using PicView.WPF.Views.UserControls.Misc;
using PicView.WPF.Views.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        GetSpinWaiter = new SpinWaiter();
        startupWindow.TheGrid.Children.Add(GetSpinWaiter);
        Panel.SetZIndex(GetSpinWaiter, 99);

        // Load sizing properties
        MonitorInfo = MonitorSize.GetMonitorSize(startupWindow);
        var settingsExist = await SettingsHelper.LoadSettingsAsync().ConfigureAwait(false);
        if (settingsExist)
        {
            if (SettingsHelper.Settings.WindowProperties.AutoFit == false && SettingsHelper.Settings.WindowProperties.Fullscreen == false)
            {
                SetLastWindowSize(startupWindow);
            }
            else
            {
                CenterWindowOnScreen(startupWindow);
            }
        }
        else
        {
            CenterWindowOnScreen(startupWindow);
        }

        var args = Environment.GetCommandLineArgs();

        if (SettingsHelper.Settings.UIProperties.OpenInSameWindow)
        {
            var pipeName = "PicViewPipe";

            if (ProcessHelper.CheckIfAnotherInstanceIsRunning())
            {
                if (args.Length > 1)
                {
                    // Another instance is running, send arguments and exit
                    var argument = await IPCHelper.SendArgumentToRunningInstance(args[1], pipeName).ConfigureAwait(false);
                    if (argument)
                    {
                        Environment.Exit(0);
                    }
                }
            }
            else
            {
                // No other instance is running, create named pipe server
                _ = IPCHelper.StartListeningForArguments(pipeName);
            }
        }

        if (args.Length > 1)
        {
            // Apply lockscreen and close
            var arg = args[1];
            if (arg.StartsWith("lockscreen"))
            {
                var path = arg[(arg.LastIndexOf(',') + 1)..];
                path = Path.GetFullPath(path);
                Windows.Lockscreen.LockscreenHelper.SetLockScreenImage(path);
                Environment.Exit(0);
            }
        }

        MainWindow? mainWindow = null;
        var language = SettingsHelper.Settings.UIProperties.UserLanguage;
        await Core.Localization.TranslationHelper.LoadLanguage(language).ConfigureAwait(false);

        await startupWindow.Dispatcher.InvokeAsync(() =>
        {
            mainWindow = new MainWindow();
            ConfigureWindows.GetMainWindow = mainWindow;
            GetSpinWaiter = GetSpinWaiter;

            // Set min size to DPI scaling
            mainWindow.MinWidth *= MonitorInfo.DpiScaling;
            mainWindow.MinHeight *= MonitorInfo.DpiScaling;

            SetWindowBehavior();
            if (SettingsHelper.Settings.WindowProperties.AutoFit == false)
            {
                if (!settingsExist)
                {
                    CenterWindowOnScreen(mainWindow);
                }
                else
                {
                    SetLastWindowSize(mainWindow);
                }
            }

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
                Fullscreen_Restore(true);
            }

            if (SettingsHelper.Settings.WindowProperties.AutoFit)
            {
                ScaleImage.TryFitImage();
            }

            startupWindow.TheGrid.Children.Remove(GetSpinWaiter);
            mainWindow.ParentContainer.Children.Add(GetSpinWaiter);
        });

        Navigation.Pics = new List<string>();
        _ = CustomKeybindings.LoadKeybindings().ConfigureAwait(false);

        if (args.Length > 1)
        {
           _ = Task.Run(() => QuickLoad.QuickLoadAsync(args[1]));
        }
        else
        {
            if (SettingsHelper.Settings.StartUp.OpenLastFile)
            {
                var file = FileHistoryNavigation.Contains(SettingsHelper.Settings.StartUp.LastFile)
                    ? SettingsHelper.Settings.StartUp.LastFile
                    : FileHistoryNavigation.GetLastFile();

                if (!string.IsNullOrWhiteSpace(file))
                {
                    _ = Task.Run(() => QuickLoad.QuickLoadAsync(file));
                }
            }
            else if (SettingsHelper.Settings.StartUp.OpenSpecificFile)
            {
                _ = Task.Run(() => LoadPic.LoadPicFromStringAsync(SettingsHelper.Settings.StartUp.OpenSpecificString));
            }
            else
            {
                ErrorHandling.Unload(true);
            }
        }

        try
        {
            ConfigColors.UpdateColor();
            await mainWindow.Dispatcher.InvokeAsync(startupWindow.Close);
            // Try catch to prevent task canceled exception
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadedEvent)}: {nameof(startupWindow)} exception,\n{e.Message}");
#endif
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

        AddDictionaries();
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
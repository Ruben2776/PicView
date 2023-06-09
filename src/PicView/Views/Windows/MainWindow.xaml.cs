using Microsoft.Win32;
using PicView.Animations;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.ConfigureSettings;
using PicView.PicGallery;
using PicView.Properties;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.Translations;
using PicView.UILogic;
using PicView.UILogic.DragAndDrop;
using PicView.UILogic.Loading;
using PicView.UILogic.TransformImage;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.UC;

namespace PicView.Views.Windows;

public partial class MainWindow
{
    public MainWindow()
    {
        // Updates settings from older version to newer version
        if (Settings.Default.CallUpgrade)
        {
            Settings.Default.Upgrade();
            Settings.Default.CallUpgrade = false;
            LoadLanguage.DetermineLanguage(Settings.Default.UserLanguage != "en");
        }
        else if (Settings.Default.UserLanguage != "en")
        {
            LoadLanguage.DetermineLanguage(false);
        }

        if (Settings.Default.DarkTheme == false)
        {
            ConfigColors.ChangeTheme(false);
        }

        InitializeComponent();

        if (Settings.Default.AutoFitWindow == false)
        {
            // Need to change startup location after initialize component
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
        Topmost = Settings.Default.TopMost;

        Loaded += (_, _) =>
        {
            // Subscribe to Windows resized event || Need to be exactly on load
            HwndSource.FromHwnd(new WindowInteropHelper(ConfigureWindows.GetMainWindow).Handle)
                ?.AddHook(NativeMethods.WndProc);
            ConfigColors.MainWindowUnfocusOrFocus(true);
            StartLoading.LoadedEvent();
        };

        ContentRendered += delegate
        {
            WindowBlur.EnableBlur(this);
            Task.Run(async () =>
            {
                var args = Environment.GetCommandLineArgs();

                // Determine preferred UI for startup
                if (Settings.Default.FullscreenGallery)
                {
                    if (args.Length <= 1)
                    {
                        Settings.Default.FullscreenGallery = false;
                    }
                    else
                    {
                        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(GalleryLoad.LoadLayout, DispatcherPriority.Send);
                        await GalleryToggle.OpenFullscreenGalleryAsync().ConfigureAwait(false);
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
                        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() => Fullscreen_Restore(true), DispatcherPriority.Send);
                    }
                }

                // Load image if possible
                if (args.Length <= 1)
                {
                    ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() => ErrorHandling.Unload(true), DispatcherPriority.Send);
                }
                else
                {
                    await QuickLoad.QuickLoadAsync(args[1]).ConfigureAwait(false);
                    // TODO maybe load extra images if multiple arguments
                }
            });
            StartLoading.AddDictionaries();
            StartLoading.AddUiElementsAndUpdateValues();

            // keyboard and Mouse_Keys Keys
            KeyDown += async (sender, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(sender, e).ConfigureAwait(false);
            KeyUp += MainKeyboardShortcuts.MainWindow_KeysUp;
            MouseLeftButtonDown += MainMouseKeys.MouseLeftButtonDown;
            MouseDown += (sender, e) => MainMouseKeys.MouseButtonDownAsync(sender, e).ConfigureAwait(false);

            // Lowerbar
            LowerBar.Drop += async (sender, e) => await ImageDragAndDrop.Image_Drop(sender, e).ConfigureAwait(false);
            LowerBar.MouseLeftButtonDown += MoveAlt;

            MouseMove += (_, _) => HideInterfaceLogic.Interface_MouseMove();
            MouseLeave += (_, _) => HideInterfaceLogic.Interface_MouseLeave();

            // MainImage
            ConfigureWindows.GetMainWindow.MainImage.MouseLeftButtonUp += MainMouseKeys.MainImage_MouseLeftButtonUp;
            ConfigureWindows.GetMainWindow.MainImage.MouseMove += MainMouseKeys.MainImage_MouseMove;
            ConfigureWindows.GetMainWindow.MainImage.MouseLeftButtonDown += DragToExplorer.DragFile;

            // ClickArrows
            GetClickArrowLeft.MouseLeftButtonDown += async (_, _) => await Navigation.PicButtonAsync(true, false).ConfigureAwait(false);
            GetClickArrowRight.MouseLeftButtonDown += async (_, _) => await Navigation.PicButtonAsync(true, true).ConfigureAwait(false);

            // image_button
            MouseOverAnimations.SetButtonIconMouseOverAnimations(ImageButton, ImageMenuBg, (SolidColorBrush)Resources["ImageBrush"], true);
            ImageButton.Click += Toggle_image_menu;

            // SettingsButton
            MouseOverAnimations.SetButtonIconMouseOverAnimations(SettingsButton, SettingsMenuBg, SettingsButtonFill, true);
            SettingsButton.Click += Toggle_quick_settings_menu;

            //FunctionButton
            var magicBrush = TryFindResource("MagicBrush") as SolidColorBrush;
            MouseOverAnimations.SetButtonIconMouseOverAnimations(FunctionMenuButton, EffectsMenuBg, magicBrush, true);
            FunctionMenuButton.Click += Toggle_Functions_menu;

            //GalleryButton
            MouseOverAnimations.SetButtonIconMouseOverAnimations(GalleryButton, GalleryBg, GalleryBrush, true);
            GalleryButton.Click += async (_, _) =>
            {
                if (GalleryFunctions.IsGalleryOpen)
                {
                    GalleryToggle.CloseHorizontalGallery();
                }
                else if (Settings.Default.FullscreenGallery == false)
                {
                    await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
                }
            };

            // RotateButton
            MouseOverAnimations.SetButtonIconMouseOverAnimations(RotateButton, RotateBg, RotateBrush, true);
            RotateButton.Click += async (_, _) => await Rotation.RotateAndMoveCursor(false, RotateButton).ConfigureAwait(false);

            // FlipButton
            MouseOverAnimations.SetButtonIconMouseOverAnimations(FlipButton, FlipBg, FlipBrush, true);
            FlipButton.Click += (_, _) => Rotation.Flip();
            FlipButton.Checked += (_, _) => UpdateUIValues.ChangeFlipButton(true);
            FlipButton.Unchecked += (_, _) => UpdateUIValues.ChangeFlipButton(false);

            // TitleText
            TitleText.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
            TitleText.InnerTextBox.PreviewKeyDown += async (_, e) => await CustomTextBoxShortcuts.CustomTextBox_KeyDownAsync(e).ConfigureAwait(false);
            TitleText.PreviewMouseLeftButtonDown += EditTitleBar.Bar_PreviewMouseLeftButtonDown;
            TitleText.PreviewMouseRightButtonDown += EditTitleBar.Bar_PreviewMouseRightButtonDown;

            // ParentContainer
            ParentContainer.Drop += async (sender, e) => await ImageDragAndDrop.Image_Drop(sender, e).ConfigureAwait(false);
            ParentContainer.DragEnter += ImageDragAndDrop.Image_DragEnter;
            ParentContainer.DragLeave += ImageDragAndDrop.Image_DragLeave;
            ParentContainer.PreviewMouseWheel += async (sender, e) => await MainMouseKeys.MainImage_MouseWheelAsync(sender, e).ConfigureAwait(false);

            CloseButton.TheButton.Click += (_, _) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);

            Closing += (_, _) => Window_Closing();
            StateChanged += (_, _) => MainWindow_StateChanged();

            Deactivated += (_, _) => ConfigColors.MainWindowUnfocusOrFocus(false);
            Activated += (_, _) => ConfigColors.MainWindowUnfocusOrFocus(true);

            SystemEvents.DisplaySettingsChanged += (_, _) => SystemEvents_DisplaySettingsChanged();

            TitleBar.MouseLeftButtonDown += (_, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            Logo.MouseLeftButtonDown += (_, _) => ConfigureWindows.WindowContextMenu.IsOpen = true;
        };
    }

    #region OnRenderSizeChanged override

    protected override void OnRenderSizeChanged(SizeChangedInfo? sizeInfo)
    {
        if (sizeInfo == null || sizeInfo is { WidthChanged: false, HeightChanged: false } || Settings.Default.AutoFitWindow == false)
        {
            Navigation.RightButtonClicked = false;
            Navigation.LeftButtonClicked = false;
            Navigation.ClickArrowRightClicked = false;
            Navigation.ClickArrowLeftClicked = false;
            return;
        }

        //Keep position when size has changed
        Top += ((sizeInfo.PreviousSize.Height / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Height / MonitorInfo.DpiScaling)) / 2;
        Left += ((sizeInfo.PreviousSize.Width / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Width / MonitorInfo.DpiScaling)) / 2;

        // Move cursor after resize when the button has been pressed
        if (Navigation.RightButtonClicked)
        {
            var p = RightButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RightButton
            NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
            Navigation.RightButtonClicked = false;
        }
        else if (Navigation.LeftButtonClicked)
        {
            var p = LeftButton.PointToScreen(new Point(50, 10));
            NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
            Navigation.LeftButtonClicked = false;
        }
        else if (Navigation.ClickArrowRightClicked)
        {
            var p = GetClickArrowRight.PointToScreen(new Point(25, 30));
            NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
            Navigation.ClickArrowRightClicked = false;

            FadeControls.Fade(true);
        }
        else if (Navigation.ClickArrowLeftClicked)
        {
            var p = GetClickArrowLeft.PointToScreen(new Point(25, 30));
            NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
            Navigation.ClickArrowLeftClicked = false;

            FadeControls.Fade(true);
        }

        base.OnRenderSizeChanged(sizeInfo);
    }

    #endregion OnRenderSizeChanged override
}
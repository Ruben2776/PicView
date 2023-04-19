using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.UC;

namespace PicView.Views.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Updates settings from older version to newer version
            if (Settings.Default.CallUpgrade)
            {
                Settings.Default.Upgrade();
                Settings.Default.CallUpgrade = false;
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
                LoadLanguage.DetermineLanguage();
                StartLoading.LoadedEvent();
            };

            ContentRendered += async delegate
            {
                WindowBlur.EnableBlur(this);

                await StartLoading.ContentRenderedEventAsync().ConfigureAwait(false);

                // keyboard and Mouse_Keys Keys
                KeyDown += async (sender, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(sender, e).ConfigureAwait(false);
                KeyUp += (sender, e) => MainKeyboardShortcuts.MainWindow_KeysUp(sender, e);
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
                ImageButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(ImagePath1Fill, ImagePath2Fill, ImagePath3Fill);
                ImageButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ImageMenuBg);
                ImageButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(ImagePath1Fill, ImagePath2Fill, ImagePath3Fill);
                ImageButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ImageMenuBg);

                // SettingsButton
                SettingsButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(SettingsButtonFill);
                SettingsButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(SettingsMenuBg);
                SettingsButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(SettingsButtonFill);
                SettingsButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(SettingsMenuBg);
                SettingsButton.Click += Toggle_quick_settings_menu;
                ImageButton.Click += Toggle_image_menu;

                //FunctionButton
                var magicBrush = TryFindResource("MagicBrush") as SolidColorBrush;
                FunctionMenuButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(magicBrush);
                FunctionMenuButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(EffectsMenuBg);
                FunctionMenuButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(magicBrush);
                FunctionMenuButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(EffectsMenuBg);
                FunctionMenuButton.Click += Toggle_Functions_menu;
                
                //GalleryButton
                GalleryButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(GalleryBrush);
                GalleryButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(GalleryBg);
                GalleryButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(GalleryBrush);
                GalleryButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(GalleryBg);
                GalleryButton.Click += async (_, _) =>
                {
                    if (GalleryFunctions.IsHorizontalOpen)
                    {
                        GalleryToggle.CloseHorizontalGallery();
                    }
                    else if (GalleryFunctions.IsHorizontalFullscreenOpen == false)
                    {
                        await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
                    }
                };

                // RotateButton
                RotateButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(RotateBrush);
                RotateButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(RotateBg);
                RotateButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(RotateBrush);
                RotateButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(RotateBg);
                RotateButton.Click += async (_, _) =>
                    await Rotation.RotateAndMoveCursor(false, RotateButton).ConfigureAwait(false);

                // FlipButton
                FlipButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(FlipBrush);
                FlipButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(FlipBg);
                FlipButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(FlipBrush);
                FlipButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(FlipBg);
                FlipButton.Click += (_, _) => Rotation.Flip();
                FlipButton.Checked += (_, _) => UpdateUIValues.ChangeFlipButton(true);
                FlipButton.Unchecked += (_, _) => UpdateUIValues.ChangeFlipButton(false);

                // TitleText
                TitleText.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
                TitleText.InnerTextBox.PreviewKeyDown += async (sender, e) => await CustomTextBoxShortcuts.CustomTextBox_KeyDownAsync(sender, e).ConfigureAwait(false);
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
                Navigation.RightbuttonClicked = false;
                Navigation.LeftbuttonClicked = false;
                Navigation.ClickArrowRightClicked = false;
                Navigation.ClickArrowLeftClicked = false;
                return;
            }

            //Keep position when size has changed
            Top += ((sizeInfo.PreviousSize.Height / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Height / MonitorInfo.DpiScaling)) / 2;
            Left += ((sizeInfo.PreviousSize.Width / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Width / MonitorInfo.DpiScaling)) / 2;

            // Move cursor after resize when the button has been pressed
            if (Navigation.RightbuttonClicked)
            {
                var p = RightButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RightButton
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.RightbuttonClicked = false;
            }
            else if (Navigation.LeftbuttonClicked)
            {
                var p = LeftButton.PointToScreen(new Point(50, 10));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.LeftbuttonClicked = false;
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
}
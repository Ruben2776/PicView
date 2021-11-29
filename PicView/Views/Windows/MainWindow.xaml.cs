using PicView.Animations;
using PicView.ChangeImage;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Loading;
using System.Windows;
using System.Windows.Interop;
using static PicView.UILogic.Sizing.WindowSizing;
using static PicView.UILogic.UC;

namespace PicView.Views.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Updates settings from older version to newer version
            if (Properties.Settings.Default.CallUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.CallUpgrade = false;
            }

            if (Properties.Settings.Default.DarkTheme == false)
            {
                ConfigureSettings.ConfigColors.ChangeToLightTheme();
            }
            InitializeComponent();

            if (Properties.Settings.Default.AutoFitWindow == false)
            {
                // Need to change startup location after initialize component
                WindowStartupLocation = WindowStartupLocation.Manual;
                if (Properties.Settings.Default.Width > 0)
                {
                    SetLastWindowSize();
                }
            }
            Topmost = Properties.Settings.Default.TopMost;

            Loaded += (_, _) =>
            {
                // Subscribe to Windows resized event || Need to be exactly on load
                HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(ConfigureWindows.GetMainWindow).Handle);
                source.AddHook(new HwndSourceHook(NativeMethods.WndProc));
                Translations.LoadLanguage.DetermineLanguage();
                StartLoading.LoadedEvent();
            };
            ContentRendered += delegate
            {
                StartLoading.ContentRenderedEvent();

                // keyboard and Mouse_Keys Keys
                KeyDown += async (sender, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(sender, e).ConfigureAwait(false);
                KeyUp += (sender, e) => MainKeyboardShortcuts.MainWindow_KeysUp(sender, e);
                MouseLeftButtonDown += async (sender, e) => await MainMouseKeys.MouseLeftButtonDownAsync(sender, e).ConfigureAwait(false);
                MouseDown += (sender, e) => MainMouseKeys.MouseButtonDownAsync(sender, e).ConfigureAwait(false);

                // Lowerbar
                LowerBar.Drop += async (sender, e) => await UILogic.DragAndDrop.Image_DragAndDrop.Image_Drop(sender, e).ConfigureAwait(false);
                LowerBar.MouseLeftButtonDown += UILogic.Sizing.WindowSizing.MoveAlt;

                MouseMove += async (_, _) => await HideInterfaceLogic.Interface_MouseMove().ConfigureAwait(false);
                MouseLeave += async (_, _) => await HideInterfaceLogic.Interface_MouseLeave().ConfigureAwait(false);

                // MainImage
                ConfigureWindows.GetMainWindow.MainImage.MouseLeftButtonUp += MainMouseKeys.MainImage_MouseLeftButtonUp;
                ConfigureWindows.GetMainWindow.MainImage.MouseMove += MainMouseKeys.MainImage_MouseMove;

                // ClickArrows
                GetClickArrowLeft.MouseLeftButtonDown += async (_, _) => await ChangeImage.Navigation.PicButtonAsync(true, false).ConfigureAwait(false);
                GetClickArrowRight.MouseLeftButtonDown += async (_, _) => await ChangeImage.Navigation.PicButtonAsync(true, true).ConfigureAwait(false);

                // image_button
                image_button.PreviewMouseLeftButtonDown += (_, _) => MouseOverAnimations.PreviewMouseButtonDownAnim(ImagePath1Fill, ImagePath2Fill, ImagePath3Fill);
                image_button.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(ImagePath1Fill, ImagePath2Fill, ImagePath3Fill);
                image_button.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ImageMenuBg);
                image_button.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(ImagePath1Fill, ImagePath2Fill, ImagePath3Fill);
                image_button.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ImageMenuBg);

                // SettingsButton
                SettingsButton.PreviewMouseLeftButtonDown += (_, _) => MouseOverAnimations.PreviewMouseButtonDownAnim(SettingsButtonFill);
                SettingsButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(SettingsButtonFill);
                SettingsButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(SettingsMenuBg);
                SettingsButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(SettingsButtonFill);
                SettingsButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(SettingsMenuBg);
                SettingsButton.Click += Toggle_quick_settings_menu;
                image_button.Click += Toggle_image_menu;

                //FunctionButton
                var MagicBrush = TryFindResource("MagicBrush") as System.Windows.Media.SolidColorBrush;
                FunctionMenuButton.PreviewMouseLeftButtonDown += (_, _) => MouseOverAnimations.PreviewMouseButtonDownAnim(MagicBrush);
                FunctionMenuButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(MagicBrush);
                FunctionMenuButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(EffectsMenuBg);
                FunctionMenuButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(MagicBrush);
                FunctionMenuButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(EffectsMenuBg);
                FunctionMenuButton.Click += Toggle_Functions_menu;

                // TitleText
                TitleText.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
                TitleText.InnerTextBox.PreviewKeyDown += async (sender, e) => await CustomTextBoxShortcuts.CustomTextBox_KeyDownAsync(sender, e).ConfigureAwait(false);
                TitleText.PreviewMouseLeftButtonDown += EditTitleBar.Bar_PreviewMouseLeftButtonDown;
                TitleText.PreviewMouseRightButtonDown += EditTitleBar.Bar_PreviewMouseRightButtonDown;

                // ParentContainer
                ParentContainer.Drop += async (sender, e) => await UILogic.DragAndDrop.Image_DragAndDrop.Image_Drop(sender, e).ConfigureAwait(false);
                ParentContainer.DragEnter += UILogic.DragAndDrop.Image_DragAndDrop.Image_DragEnter;
                ParentContainer.DragLeave += UILogic.DragAndDrop.Image_DragAndDrop.Image_DragLeave;
                ParentContainer.PreviewMouseWheel += async (sender, e) => await MainMouseKeys.MainImage_MouseWheelAsync(sender, e).ConfigureAwait(false);

                CloseButton.TheButton.Click += (_, _) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);

                Closing += (_, _) => UILogic.Sizing.WindowSizing.Window_Closing();
                StateChanged += (_, _) => UILogic.Sizing.WindowSizing.MainWindow_StateChanged();

                Deactivated += (_, _) => ConfigureSettings.ConfigColors.MainWindowUnfocus();
                Activated += (_, _) => ConfigureSettings.ConfigColors.MainWindowFocus();

                Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (_, _) => UILogic.Sizing.WindowSizing.SystemEvents_DisplaySettingsChanged();

                TitleBar.MouseLeftButtonDown += (_, e) =>
                {
                    if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    {
                        DragMove();
                    }
                };
            };
        }

        #region OnRenderSizeChanged override

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo == null || !sizeInfo.WidthChanged && !sizeInfo.HeightChanged || Properties.Settings.Default.AutoFitWindow == false)
            {
                return;
            }

            //Keep position when size has changed
            Top += ((sizeInfo.PreviousSize.Height / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Height / MonitorInfo.DpiScaling)) / 2;
            Left += ((sizeInfo.PreviousSize.Width / MonitorInfo.DpiScaling) - (sizeInfo.NewSize.Width / MonitorInfo.DpiScaling)) / 2;

            // Move cursor after resize when the button has been pressed
            if (Navigation.RightbuttonClicked)
            {
                Point p = RightButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RighButton
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.RightbuttonClicked = false;
            }
            else if (Navigation.LeftbuttonClicked)
            {
                Point p = LeftButton.PointToScreen(new Point(50, 10));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.LeftbuttonClicked = false;
            }
            else if (Navigation.ClickArrowRightClicked)
            {
                Point p = GetClickArrowRight.PointToScreen(new Point(25, 30));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.ClickArrowRightClicked = false;

                _ = FadeControls.FadeAsync(true);
            }
            else if (Navigation.ClickArrowLeftClicked)
            {
                Point p = GetClickArrowLeft.PointToScreen(new Point(25, 30));
                NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                Navigation.ClickArrowLeftClicked = false;

                _ = FadeControls.FadeAsync(true);
            }

            base.OnRenderSizeChanged(sizeInfo);
        }

        #endregion OnRenderSizeChanged override
    }
}
﻿using Microsoft.Win32;
using PicView.Core.Config;
using PicView.WPF.Animations;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.PicGallery;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.DragAndDrop;
using PicView.WPF.UILogic.Loading;
using PicView.WPF.UILogic.Sizing;
using PicView.WPF.UILogic.TransformImage;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.WPF.UILogic.Sizing.WindowSizing;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.Views.Windows;

public partial class MainWindow
{
    public MainWindow()
    {
        // Updates settings from older version to newer version
        //if (SettingsHelper.Settings.CallUpgrade)
        //{
        //    SettingsHelper.Settings.Upgrade();
        //    SettingsHelper.Settings.CallUpgrade = false;
        //    LoadLanguage.DetermineLanguage(SettingsHelper.Settings.UIProperties.UserLanguage != "en");
        //}
        //else if (SettingsHelper.Settings.UIProperties.UserLanguage; != "en")
        //{
        //    LoadLanguage.DetermineLanguage(false);
        //}

        if (SettingsHelper.Settings.Theme.Dark == false)
        {
            ConfigColors.ChangeTheme(false);
        }

        InitializeComponent();

        if (SettingsHelper.Settings.WindowProperties.AutoFit == false)
        {
            // Need to change startup location after initialize component
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        Topmost = SettingsHelper.Settings.WindowProperties.TopMost;

        ContentRendered += delegate
        {
            WindowBlur.EnableBlur(this);

            StartLoading.AddUiElementsAndUpdateValues();

            // keyboard and Mouse_Keys Keys
            KeyDown += async (sender, e) =>
                await MainKeyboardShortcuts.MainWindow_KeysDownAsync(sender, e).ConfigureAwait(false);
            KeyUp += async (sender, e) =>
                await MainKeyboardShortcuts.MainWindow_KeysUp(sender, e).ConfigureAwait(false);
            MouseLeftButtonDown += MainMouseKeys.MouseLeftButtonDown;
            MouseDown += (sender, e) => MainMouseKeys.MouseButtonDownAsync(sender, e).ConfigureAwait(false);

            // Lowerbar
            LowerBar.Drop += async (sender, e) =>
                await ImageDragAndDrop.Image_Drop(sender, e).ConfigureAwait(false);
            LowerBar.MouseLeftButtonDown += MoveAlt;

            MouseMove += (_, _) => HideInterfaceLogic.Interface_MouseMove();
            MouseLeave += (_, _) => HideInterfaceLogic.Interface_MouseLeave();

            // MainImage
            ConfigureWindows.GetMainWindow.MainImage.MouseLeftButtonUp += MainMouseKeys.MainImage_MouseLeftButtonUp;
            ConfigureWindows.GetMainWindow.MainImage.MouseMove += MainMouseKeys.MainImage_MouseMove;

            // ClickArrows
            GetClickArrowLeft.MouseLeftButtonDown += async (_, _) =>
                await Navigation.PicButtonAsync(true, false).ConfigureAwait(false);
            GetClickArrowRight.MouseLeftButtonDown += async (_, _) =>
                await Navigation.PicButtonAsync(true, true).ConfigureAwait(false);

            // image_button
            MouseOverAnimations.SetButtonIconMouseOverAnimations(ImageButton, ImageMenuBg,
                (SolidColorBrush)Resources["ImageBrush"], true);
            ImageButton.Click += Toggle_image_menu;

            // SettingsButton
            MouseOverAnimations.SetButtonIconMouseOverAnimations(SettingsButton, SettingsMenuBg, SettingsButtonFill,
                true);
            SettingsButton.Click += Toggle_quick_settings_menu;

            //FunctionButton
            var magicBrush = TryFindResource("MagicBrush") as SolidColorBrush;
            MouseOverAnimations.SetButtonIconMouseOverAnimations(FunctionMenuButton, EffectsMenuBg, magicBrush,
                true);
            FunctionMenuButton.Click += Toggle_Functions_menu;

            //GalleryButton
            MouseOverAnimations.SetButtonIconMouseOverAnimations(GalleryButton, GalleryBg, GalleryBrush, true);
            GalleryButton.Click += async (_, _) => await GalleryToggle.ToggleGalleryAsync().ConfigureAwait(false);

            // RotateButton
            MouseOverAnimations.SetButtonIconMouseOverAnimations(RotateButton, RotateBg, RotateBrush, true);
            RotateButton.Click += async (_, _) =>
                await Rotation.RotateAndMoveCursor(false, RotateButton).ConfigureAwait(false);

            // FlipButton
            MouseOverAnimations.SetButtonIconMouseOverAnimations(FlipButton, FlipBg, FlipBrush, true);
            FlipButton.Click += (_, _) => Rotation.Flip();

            // TitleText
            TitleText.GotKeyboardFocus += EditTitleBar.EditTitleBar_Text;
            TitleText.InnerTextBox.PreviewKeyDown += async (_, e) =>
                await CustomTextBoxShortcuts.CustomTextBox_KeyDownAsync(e).ConfigureAwait(false);
            TitleText.PreviewMouseLeftButtonDown += EditTitleBar.Bar_PreviewMouseLeftButtonDown;
            TitleText.PreviewMouseRightButtonDown += EditTitleBar.Bar_PreviewMouseRightButtonDown;

            // ParentContainer
            ParentContainer.Drop += async (sender, e) =>
                await ImageDragAndDrop.Image_Drop(sender, e).ConfigureAwait(false);
            ParentContainer.DragEnter += async (sender, e) =>
                await ImageDragAndDrop.Image_DragEnter(sender, e).ConfigureAwait(false);
            ParentContainer.DragLeave += ImageDragAndDrop.Image_DragLeave;
            ParentContainer.PreviewMouseWheel += async (sender, e) =>
                await MainMouseKeys.MainImage_MouseWheelAsync(sender, e).ConfigureAwait(false);

            CloseButton.TheButton.Click += (_, _) => SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);

            Closing += (_, _) => Window_Closing();
            StateChanged += (_, _) => MainWindow_StateChanged();

            Deactivated += async (_, _) => await ConfigColors.MainWindowUnfocusOrFocus(false);
            Activated += async (_, _) => await ConfigColors.MainWindowUnfocusOrFocus(true);

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
        if (sizeInfo == null || sizeInfo is { WidthChanged: false, HeightChanged: false } ||
            SettingsHelper.Settings.WindowProperties.AutoFit == false)
        {
            // Resize Gallery
            if (GetPicGallery != null && GalleryFunctions.IsGalleryOpen ||
                GetPicGallery != null && SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                if (GalleryFunctions.IsGalleryOpen)
                {
                    GetPicGallery.Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight;
                }

                GetPicGallery.Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
            }

            var w = ConfigureWindows.GetMainWindow;
            if (w is null)
            {
                return;
            }
            GetStartUpUC?.ResponsiveSize(w.Width);
            if (w.WindowState == WindowState.Maximized)
            {
                Restore_From_Move();
            }

            if (w.MainImage.Source is not null)
                ScaleImage.FitImage(w.MainImage.Source.Width, w.MainImage.Source.Height);

            Navigation.RightButtonClicked = false;
            Navigation.LeftButtonClicked = false;
            Navigation.ClickArrowRightClicked = false;
            Navigation.ClickArrowLeftClicked = false;
            return;
        }

        //Keep position when size has changed
        Top += (sizeInfo.PreviousSize.Height / MonitorInfo.DpiScaling -
                sizeInfo.NewSize.Height / MonitorInfo.DpiScaling) / 2;
        Left += (sizeInfo.PreviousSize.Width / MonitorInfo.DpiScaling -
                 sizeInfo.NewSize.Width / MonitorInfo.DpiScaling) / 2;

        // Move cursor after resize when the button has been pressed
        if (Navigation.RightButtonClicked)
        {
            var p = RightButton.PointToScreen(new Point(50, 10)); //Points cursor to center of RightButton
            PicView.Windows.NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
            Navigation.RightButtonClicked = false;
        }
        else if (Navigation.LeftButtonClicked)
        {
            var p = LeftButton.PointToScreen(new Point(50, 10));
            PicView.Windows.NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
            Navigation.LeftButtonClicked = false;
        }
        else if (Navigation.ClickArrowRightClicked)
        {
            var p = GetClickArrowRight.PointToScreen(new Point(25, 30));
            PicView.Windows.NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
            Navigation.ClickArrowRightClicked = false;

            FadeControls.Fade(true);
        }
        else if (Navigation.ClickArrowLeftClicked)
        {
            var p = GetClickArrowLeft.PointToScreen(new Point(25, 30));
            PicView.Windows.NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
            Navigation.ClickArrowLeftClicked = false;

            FadeControls.Fade(true);
        }

        base.OnRenderSizeChanged(sizeInfo);
    }

    #endregion OnRenderSizeChanged override
}
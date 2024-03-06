using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.WPF.Animations;
using PicView.WPF.ChangeImage;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.PicGallery;
using PicView.WPF.ProcessHandling;
using PicView.WPF.Shortcuts;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Loading;
using PicView.WPF.UILogic.Sizing;
using PicView.WPF.UILogic.TransformImage;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static PicView.WPF.Animations.MouseOverAnimations;
using static PicView.WPF.ConfigureSettings.ConfigColors;

namespace PicView.WPF.Views.Windows;

public partial class SettingsWindow
{
    public SettingsWindow()
    {
        MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
        Width *= WindowSizing.MonitorInfo.DpiScaling;
        if (double.IsNaN(Width)) // Fixes if user opens window when loading from startup
        {
            WindowSizing.MonitorInfo = MonitorSize.GetMonitorSize(this);
            MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
            Width *= WindowSizing.MonitorInfo.DpiScaling;
        }

        InitializeComponent();

        ContentRendered += delegate
        {
            WindowBlur.EnableBlur(this);
            UpdateLanguage();
            Owner = null; // Remove owner, so that minimizing main-window will not minimize this

            foreach (var language in Enum.GetValues(typeof(Languages)))
            {
                try
                {
                    LanguageBox.Items.Add(new ComboBoxItem
                    {
                        Content = new CultureInfo(language.ToString().Replace('_', '-')).DisplayName,
                        IsSelected = language.ToString().Replace('_', '-') ==
                                     SettingsHelper.Settings.UIProperties.UserLanguage
                    });
                }
                catch (Exception e)
                {
                    // Fix weird crash https://github.com/Ruben2776/PicView/issues/51
#if DEBUG
                    Trace.WriteLine($"{nameof(SettingsWindow)} Add language caught exception: \n {e.Message}");
#endif
                }
            }

            Deactivated += (_, _) => WindowUnfocusOrFocus(TitleBar, TitleText, null, false);
            Activated += (_, _) => WindowUnfocusOrFocus(TitleBar, TitleText, null, true);
            var colorAnimation = new ColorAnimation { Duration = TimeSpan.FromSeconds(.1) };
            AddGenericEvents(colorAnimation);

            // AvoidZoomRadio
            AvoidZoomRadio.IsChecked = SettingsHelper.Settings.Zoom.AvoidZoomingOut;
            AvoidZoomRadio.Click += async delegate
            {
                SettingsHelper.Settings.Zoom.AvoidZoomingOut = !SettingsHelper.Settings.Zoom.AvoidZoomingOut;
                await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
            };

            // SubDirRadio
            SubDirRadio.IsChecked = SettingsHelper.Settings.Sorting.IncludeSubDirectories;
            SubDirRadio.Click += async delegate
            {
                await Task.Run(async () =>
                {
                    await UpdateUIValues.ToggleIncludeSubdirectoriesAsync().ConfigureAwait(false);
                });
            };

            // Slideshow
            SlideshowSlider.Value = SettingsHelper.Settings.UIProperties.SlideShowTimer / 1000;
            SlideshowSlider.ValueChanged += (_, e) =>
                SettingsHelper.Settings.UIProperties.SlideShowTimer = e.NewValue * 1000;

            // Zoom slider
            ZoomSlider.Value = SettingsHelper.Settings.Zoom.ZoomSpeed;
            txtZoomSlide.Text = Math.Round(ZoomSlider.Value * 100).ToString(CultureInfo.CurrentCulture);
            ZoomSlider.ValueChanged += async (_, e) =>
            {
                SettingsHelper.Settings.Zoom.ZoomSpeed = e.NewValue;
                txtZoomSlide.Text =
                    Math.Round(e.NewValue * 100).ToString(CultureInfo.CurrentCulture);
                await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
            };

            // Nav speed
            NavSlider.Value = SettingsHelper.Settings.UIProperties.NavSpeed;
            NavTxt.Text = NavSlider.Value.ToString("0.#", CultureInfo.CurrentCulture);
            NavSlider.ValueChanged += async (_, e) =>
            {
                SettingsHelper.Settings.UIProperties.NavSpeed = e.NewValue;
                NavTxt.Text = e.NewValue.ToString("0.#", CultureInfo.CurrentCulture);
                await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
            };

            // SetExpandedGallerySlider
            SetExpandedGallerySlider.Value = SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize;
            SetExpandedGalleryText.Text =
                SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize.ToString(CultureInfo.CurrentCulture);
            SetExpandedGallerySlider.ValueChanged += async (_, e) =>
            {
                SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize = (int)e.NewValue;

                if (GalleryFunctions.IsGalleryOpen)
                {
                    GalleryToggle.OpenLayout();
                }
                else
                {
                    GalleryNavigation.SetSize(SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize);
                }

                SetExpandedGalleryText.Text = SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize.ToString(CultureInfo.CurrentCulture);
            };

            // SetBottomGallerySlider
            SetBottomGallerySlider.Value = SettingsHelper.Settings.Gallery.BottomGalleryItemSize;
            SetBottomGalleryText.Text = SettingsHelper.Settings.Gallery.BottomGalleryItemSize.ToString(CultureInfo.CurrentCulture);
            SetBottomGallerySlider.ValueChanged += (_, e) =>
            {
                SettingsHelper.Settings.Gallery.BottomGalleryItemSize = e.NewValue;
                if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
                {
                    GalleryToggle.OpenLayout();
                    ScaleImage.TryFitImage();
                }
                else
                {
                    GalleryNavigation.SetSize(SettingsHelper.Settings.Gallery.BottomGalleryItemSize);
                }

                SetBottomGalleryText.Text = SettingsHelper.Settings.Gallery.BottomGalleryItemSize.ToString(CultureInfo.CurrentCulture);
            };

            // Themes
            LightThemeRadio.IsChecked = !SettingsHelper.Settings.Theme.Dark;
            DarkThemeRadio.IsChecked = SettingsHelper.Settings.Theme.Dark;

            DarkThemeRadio.Click += async delegate
            {
                if (SettingsHelper.Settings.Theme.Dark)
                {
                    DarkThemeRadio.IsChecked = true;
                    LightThemeRadio.IsChecked = false;
                    return;
                }

                SettingsHelper.Settings.Theme.Dark = true;
                LightThemeRadio.IsChecked = false;
                await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
            };
            LightThemeRadio.Click += async delegate
            {
                if (!SettingsHelper.Settings.Theme.Dark)
                {
                    DarkThemeRadio.IsChecked = false;
                    LightThemeRadio.IsChecked = true;
                    return;
                }

                SettingsHelper.Settings.Theme.Dark = false;
                DarkThemeRadio.IsChecked = false;
                await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
            };

            LanguageBox.SelectionChanged += async delegate
            {
                await TranslationHelper.ChangeLanguage(LanguageBox.SelectedIndex).ConfigureAwait(false);

                await Dispatcher?.InvokeAsync(UpdateLanguage);
                if (ConfigureWindows.GetAboutWindow is not null)
                {
                    await ConfigureWindows.GetAboutWindow?.Dispatcher?.InvokeAsync(() =>
                    {
                        ConfigureWindows.GetAboutWindow?.UpdateLanguage();
                    });
                }

                if (UC.GetStartUpUC is not null)
                {
                    await ConfigureWindows.GetMainWindow?.Dispatcher?.InvokeAsync(() =>
                    {
                        UC.GetStartUpUC.UpdateLanguage();
                    });
                }

                if (UC.GetQuickResize is not null)
                {
                    await ConfigureWindows.GetMainWindow?.Dispatcher?.InvokeAsync(() =>
                    {
                        UC.GetQuickResize.UpdateLanguage();
                    });
                }

                if (ConfigureWindows.GetResizeWindow is not null)
                {
                    await ConfigureWindows.GetResizeWindow?.Dispatcher?.InvokeAsync(() =>
                    {
                        ConfigureWindows.GetResizeWindow?.UpdateLanguage();
                    });
                }

                if (ConfigureWindows.GetEffectsWindow is not null)
                {
                    await ConfigureWindows.GetEffectsWindow?.Dispatcher?.InvokeAsync(() =>
                    {
                        ConfigureWindows.GetEffectsWindow?.UpdateLanguage();
                    });
                }

                if (ConfigureWindows.GetImageInfoWindow is not null)
                {
                    await ConfigureWindows.GetImageInfoWindow?.Dispatcher?.InvokeAsync(() =>
                    {
                        ConfigureWindows.GetImageInfoWindow?.UpdateLanguage();
                    });
                }

                await ConfigureWindows.GetMainWindow?.Dispatcher?.InvokeAsync(() =>
                {
                    UC.GetFileMenu.UpdateLanguage();
                    UC.GetImageSettingsMenu.UpdateLanguage();
                    UC.GetQuickSettingsMenu.UpdateLanguage();
                    UC.GetToolsAndEffectsMenu.UpdateLanguage();
                    LoadContextMenus.AddContextMenus();
                });
            };

            // ScrollDirection
            Reverse.IsSelected = SettingsHelper.Settings.Zoom.HorizontalReverseScroll;
            Reverse.Selected += (_, _) =>
                SettingsHelper.Settings.Zoom.HorizontalReverseScroll = !SettingsHelper.Settings.Zoom.HorizontalReverseScroll;

            Forward.IsSelected = !SettingsHelper.Settings.Zoom.HorizontalReverseScroll;
            Forward.Selected += (_, _) =>
                SettingsHelper.Settings.Zoom.HorizontalReverseScroll = !SettingsHelper.Settings.Zoom.HorizontalReverseScroll;

            AltUIRadio.IsChecked = SettingsHelper.Settings.UIProperties.ShowAltInterfaceButtons;
            AltUIRadio.Click += delegate
            {
                SettingsHelper.Settings.UIProperties.ShowAltInterfaceButtons = !SettingsHelper.Settings.UIProperties.ShowAltInterfaceButtons;
            };

            TaskbarRadio.IsChecked = SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled;
            TaskbarRadio.Click += delegate
            {
                SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled = !SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled;
                if (!SettingsHelper.Settings.UIProperties.IsTaskbarProgressEnabled)
                {
                    Taskbar.NoProgress();
                }
                else
                {
                    Taskbar.Progress((double)Navigation.FolderIndex / Navigation.Pics.Count);
                }
            };

            SaveDialogRadio.IsChecked = SettingsHelper.Settings.UIProperties.ShowFileSavingDialog;
            SaveDialogRadio.Click += delegate
            {
                SettingsHelper.Settings.UIProperties.ShowFileSavingDialog = !SettingsHelper.Settings.UIProperties.ShowFileSavingDialog;
            };

            OpenInSameWindowRadio.IsChecked = SettingsHelper.Settings.UIProperties.OpenInSameWindow;
            OpenInSameWindowRadio.Click += delegate
            {
                SettingsHelper.Settings.UIProperties.OpenInSameWindow = !SettingsHelper.Settings.UIProperties.OpenInSameWindow;
                if (SettingsHelper.Settings.UIProperties.OpenInSameWindow)
                {
                    _ = IPCHelper.StartListeningForArguments("PicViewPipe").ConfigureAwait(false);
                }
            };

            ShowBottomRadio.IsChecked = SettingsHelper.Settings.UIProperties.ShowBottomNavBar;
            ShowBottomRadio.Click += delegate
            {
                SettingsHelper.Settings.UIProperties.ShowBottomNavBar = !SettingsHelper.Settings.UIProperties.ShowBottomNavBar;
                if (!SettingsHelper.Settings.UIProperties.ShowInterface) return;
                HideInterfaceLogic.IsTopAndBottomShown(true);
                ScaleImage.TryFitImage();
            };

            ShowBottomWhenHiddenRadio.IsChecked = SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI;
            ShowBottomWhenHiddenRadio.Click += async delegate
            {
                SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI =
                    !SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI;
                if (SettingsHelper.Settings.UIProperties.ShowInterface == false && SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
                {
                    if (SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI)
                    {
                        GalleryToggle.ShowBottomGallery();
                        await Task.Delay(TimeSpan.FromSeconds(.5)); // Need to delay to cause calculation to be correct
                        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ScaleImage.TryFitImage);

                        if (GalleryLoad.IsLoading == false)
                        {
                            await GalleryLoad.LoadAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        GalleryToggle.CloseBottomGallery();
                    }
                }
            };

            CtrlZoom.IsChecked = SettingsHelper.Settings.Zoom.CtrlZoom;
            ScrollZoom.IsChecked = !SettingsHelper.Settings.Zoom.CtrlZoom;

            CtrlZoom.Checked += (_, _) => UpdateUIValues.SetCtrlToZoom(true);
            ScrollZoom.Checked += (_, _) => UpdateUIValues.SetCtrlToZoom(false);

            ThemeRestart.MouseLeftButtonDown += (_, _) => ProcessLogic.RestartApp();

            TopmostRadio.Checked += (_, _) => ConfigureWindows.IsMainWindowTopMost = !SettingsHelper.Settings.WindowProperties.TopMost;
            TopmostRadio.Unchecked += (_, _) => ConfigureWindows.IsMainWindowTopMost = false;
            TopmostRadio.IsChecked = SettingsHelper.Settings.WindowProperties.TopMost;

            CenterRadio.Checked += (_, _) => SettingsHelper.Settings.WindowProperties.KeepCentered = true;
            CenterRadio.Unchecked += (_, _) => SettingsHelper.Settings.WindowProperties.KeepCentered = false;
            CenterRadio.IsChecked = SettingsHelper.Settings.WindowProperties.KeepCentered;

            AliasingBoxHighQuality.IsSelected = !SettingsHelper.Settings.ImageScaling.IsScalingSetToNearestNeighbor;
            AliasingBoxHighQuality.Selected += delegate
            {
                SettingsHelper.Settings.ImageScaling.IsScalingSetToNearestNeighbor = false;
                ZoomLogic.TriggerScalingModeUpdate();
            };

            AliasingNearestNeighbor.IsSelected = SettingsHelper.Settings.ImageScaling.IsScalingSetToNearestNeighbor;
            AliasingNearestNeighbor.Selected += delegate
            {
                SettingsHelper.Settings.ImageScaling.IsScalingSetToNearestNeighbor = true;
                ZoomLogic.TriggerScalingModeUpdate();
            };

            StartUpNone.IsSelected = SettingsHelper.Settings.StartUp.OpenLastFile == false;
            StartUpLastFile.IsSelected = SettingsHelper.Settings.StartUp.OpenLastFile;

            StartUpNone.Selected += delegate
            {
                SettingsHelper.Settings.StartUp.OpenLastFile = false;
            };
            StartUpLastFile.Selected += delegate
            {
                SettingsHelper.Settings.StartUp.OpenLastFile = true;
            };

            switch (SettingsHelper.Settings.Theme.ColorTheme)
            {
                case 1:
                    BlueRadio.IsChecked = true;
                    break;

                case 2:
                    PinkRadio.IsChecked = true;
                    break;

                default:
                    OrangeRadio.IsChecked = true;
                    break;

                case 4:
                    GreenRadio.IsChecked = true;
                    break;

                case 5:
                    RedRadio.IsChecked = true;
                    break;

                case 6:
                    TealRadio.IsChecked = true;
                    break;

                case 7:
                    AquaRadio.IsChecked = true;
                    break;

                case 8:
                    GoldenRadio.IsChecked = true;
                    break;

                case 9:
                    PurpleRadio.IsChecked = true;
                    break;

                case 10:
                    CyanRadio.IsChecked = true;
                    break;

                case 11:
                    MagentaRadio.IsChecked = true;
                    break;

                case 12:
                    LimeRadio.IsChecked = true;
                    break;
            }
        };
    }

    internal void UpdateLanguage()
    {
        Title = TitleText.Text = TranslationHelper.GetTranslation("Settings");
        GeneralSettingsTextBlock.Text = TranslationHelper.GetTranslation("GeneralSettings");
        MiscSettingsLabel.Content = MiscSettingsLabel2.Content =
            TranslationHelper.GetTranslation("MiscSettings");
        SearchSubdirectoryTextBlock.Text = TranslationHelper.GetTranslation("SearchSubdirectory");
        StayTopMostTextBlock.Text = TranslationHelper.GetTranslation("StayTopMost");
        StayCenteredTextBlock.Text = TranslationHelper.GetTranslation("StayCentered");
        AllowZoomOutTextBlock.Text = TranslationHelper.GetTranslation("AllowZoomOut");
        ShowFileSavingDialogTextBlock.Text = TranslationHelper.GetTranslation("ShowFileSavingDialog");
        LanguageLabel.Content = TranslationHelper.GetTranslation("Language");
        MouseWheelLabel.Content = TranslationHelper.GetTranslation("MouseWheel");
        CtrlToZoomTextBlock.Text = TranslationHelper.GetTranslation("CtrlToZoom");
        ScrollToZoomTextBlock.Text = TranslationHelper.GetTranslation("ScrollToZoom");
        ScrollDirectionLabel.Content = TranslationHelper.GetTranslation("ScrollDirection");
        Reverse.Content = TranslationHelper.GetTranslation("Reverse");
        Forward.Content = TranslationHelper.GetTranslation("Forward");
        AdjustTimingForSlideshowLabel.Text = TranslationHelper.GetTranslation("AdjustTimingForSlideshow");
        SecAbbreviationTextBlock.Text = SecAbbreviationTextBlock2.Text =
            TranslationHelper.GetTranslation("SecAbbreviation");
        AdjustTimingForZoomLabel.Text = TranslationHelper.GetTranslation("AdjustTimingForZoom");
        AdjustNavSpeedLabel.Text = TranslationHelper.GetTranslation("AdjustNavSpeed");
        UiSettingsTextBlock.Text = TranslationHelper.GetTranslation("UISettings");
        ShowButtonsInHiddenUiTextBlock.Text = TranslationHelper.GetTranslation("ShowButtonsInHiddenUI");
        ToggleTaskbarProgressTextBlock.Text = TranslationHelper.GetTranslation("ToggleTaskbarProgress");
        ShowBottomToolbarTextBlock.Text = TranslationHelper.GetTranslation("ShowBottomToolbar");
        ShowBottomToolbarTextBlock.Text = TranslationHelper.GetTranslation("ShowBottomToolbar");
        ShowBottomGalleryWhenUiIsHiddenTextBlock.Text = TranslationHelper.GetTranslation("ShowBottomGalleryWhenUiIsHidden");
        HighlightColorLabel.Content = TranslationHelper.GetTranslation("HighlightColor");
        BlueRadio.Content = TranslationHelper.GetTranslation("Blue");
        CyanRadio.Content = TranslationHelper.GetTranslation("Cyan");
        AquaRadio.Content = TranslationHelper.GetTranslation("Aqua");
        TealRadio.Content = TranslationHelper.GetTranslation("Teal");
        LimeRadio.Content = TranslationHelper.GetTranslation("Lime");
        GreenRadio.Content = TranslationHelper.GetTranslation("Green");
        GoldenRadio.Content = TranslationHelper.GetTranslation("Golden");
        OrangeRadio.Content = TranslationHelper.GetTranslation("Orange");
        RedRadio.Content = TranslationHelper.GetTranslation("Red");
        PinkRadio.Content = TranslationHelper.GetTranslation("Pink");
        MagentaRadio.Content = TranslationHelper.GetTranslation("Magenta");
        PurpleRadio.Content = TranslationHelper.GetTranslation("Purple");
        ImageAliasingLabel.Content = TranslationHelper.GetTranslation("ImageAliasing");
        AliasingNearestNeighbor.Content = TranslationHelper.GetTranslation("NearestNeighbor");
        AliasingBoxHighQuality.Content = TranslationHelper.GetTranslation("HighQuality");
        ExpandedGalleryItemSizeLabel.Content = TranslationHelper.GetTranslation("ExpandedGalleryItemSize");
        BottomGalleryItemSizeLabel.Content = TranslationHelper.GetTranslation("BottomGalleryItemSize");
        ThemeLabel.Content = TranslationHelper.GetTranslation("Theme");
        DarkThemeRadio.Content = TranslationHelper.GetTranslation("DarkTheme");
        LightThemeRadio.Content = TranslationHelper.GetTranslation("LightTheme");
        ThemeRestart.Text = TranslationHelper.GetTranslation("ChangingThemeRequiresRestart");
        ThemeRestart.ToolTip = TranslationHelper.GetTranslation("RestartApp");
        StartUpLabel.Content = TranslationHelper.GetTranslation("ApplicationStartup");
        StartUpNone.Content = TranslationHelper.GetTranslation("None");
        StartUpLastFile.Content = TranslationHelper.GetTranslation("OpenLastFile");
        OpenInSameWindowTextBlock.Text = TranslationHelper.GetTranslation("OpenInSameWindow");
    }

    #region EventHandlers

    private void AddGenericEvents(ColorAnimation colorAnimation)
    {
        KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(null, e, this);

        // CloseButton
        CloseButton.TheButton.Click += delegate { Hide(); };

        // MinButton
        MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

        TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

        // BlueRadio
        BlueRadio.MouseEnter += BlueRadio_MouseEnter;
        BlueRadio.MouseLeave += BlueRadio_MouseLeave;
        BlueRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Blue);

        // PinkRadio
        PinkRadio.MouseEnter += PinkRadio_MouseEnter;
        PinkRadio.MouseLeave += PinkRadio_MouseLeave;
        PinkRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Pink);

        // OrangeRadio
        OrangeRadio.MouseEnter += OrangeRadio_MouseEnter;
        OrangeRadio.MouseLeave += OrangeRadio_MouseLeave;
        OrangeRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Orange);

        // GreenRadio
        GreenRadio.MouseEnter += GreenRadio_MouseEnter;
        GreenRadio.MouseLeave += GreenRadio_MouseLeave;
        GreenRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Green);

        // RedRadio
        RedRadio.MouseEnter += RedRadio_MouseEnter;
        RedRadio.MouseLeave += RedRadio_MouseLeave;
        RedRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Red);

        // TealRadio
        TealRadio.MouseEnter += TealRadio_MouseEnter;
        TealRadio.MouseLeave += TealRadio_MouseLeave;
        TealRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Teal);

        // AquaRadio
        AquaRadio.MouseEnter += AquaRadio_MouseEnter;
        AquaRadio.MouseLeave += AquaRadio_MouseLeave;
        AquaRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Aqua);

        // GoldenRadio
        GoldenRadio.MouseEnter += GoldenRadio_MouseEnter;
        GoldenRadio.MouseLeave += GoldenRadio_MouseLeave;
        GoldenRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Golden);

        // PurpleRadio
        PurpleRadio.MouseEnter += PurpleRadio_MouseEnter;
        PurpleRadio.MouseLeave += PurpleRadio_MouseLeave;
        PurpleRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Purple);

        // CyanRadio
        CyanRadio.MouseEnter += CyanRadio_MouseEnter;
        CyanRadio.MouseLeave += CyanRadio_MouseLeave;
        CyanRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Cyan);

        // MagentaRadio
        MagentaRadio.MouseEnter += MagentaRadio_MouseEnter;
        MagentaRadio.MouseLeave += MagentaRadio_MouseLeave;
        MagentaRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Magenta);

        // LimeRadio
        LimeRadio.Click += (_, _) => UpdateColorThemeTo(ColorOption.Lime);
        LimeRadio.MouseEnter += LimeRadio_MouseEnter;
        LimeRadio.MouseLeave += Lime_MouseLeave;

        // RestartTheme
        ThemeRestart.MouseEnter += delegate
        {
            colorAnimation.From = MainColor;
            colorAnimation.To = AnimationHelper.GetPreferredColor();
            ThemeRestartTxt.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        };
        ThemeRestart.MouseLeave += delegate
        {
            colorAnimation.From = AnimationHelper.GetPreferredColor();
            colorAnimation.To = MainColor;
            ThemeRestartTxt.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        };

        // DarkThemeRadio
        DarkThemeRadio.MouseEnter += delegate { ButtonMouseOverAnim(DarkThemeText); };
        DarkThemeRadio.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(DarkThemeBrush); };
        DarkThemeRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(DarkThemeText); };
        DarkThemeRadio.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(DarkThemeBrush); };

        // LightThemeRadio
        LightThemeRadio.MouseEnter += delegate { ButtonMouseOverAnim(LightThemeText); };
        LightThemeRadio.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(LightThemeBrush); };
        LightThemeRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(LightThemeText); };
        LightThemeRadio.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(LightThemeBrush); };

        // SubDirRadio
        SubDirRadio.MouseEnter += delegate { ButtonMouseOverAnim(SubDirText); };
        SubDirRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(SubDirText); };

        // TopmostRadio
        TopmostRadio.MouseEnter += delegate { ButtonMouseOverAnim(TopMostDirText); };
        TopmostRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(TopMostDirText); };

        // CenterRadio
        CenterRadio.MouseEnter += delegate { ButtonMouseOverAnim(CenterubDirText); };
        CenterRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(CenterubDirText); };

        // AltUIRadio
        AltUIRadio.MouseEnter += delegate { ButtonMouseOverAnim(AltUIText); };
        AltUIRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(AltUIText); };

        // TaskbarRadio
        TaskbarRadio.MouseEnter += delegate { ButtonMouseOverAnim(TaskbarText); };
        TaskbarRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(TaskbarText); };

        // SaveDialogRadio
        SaveDialogRadio.MouseEnter += delegate { ButtonMouseOverAnim(SaveDialogText); };
        SaveDialogRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(SaveDialogText); };

        // OpenInSameWindowRadio
        OpenInSameWindowRadio.MouseEnter += delegate { ButtonMouseOverAnim(OpenInSameWindowText); };
        OpenInSameWindowRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(OpenInSameWindowText); };

        // ShowBottomRadio
        ShowBottomRadio.MouseEnter += delegate { ButtonMouseOverAnim(ShowBottomText); };
        ShowBottomRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(ShowBottomText); };

        // ShowBottomWhenHiddenRadio
        ShowBottomWhenHiddenRadio.MouseEnter += delegate { ButtonMouseOverAnim(ShowBottomWhenHiddenText); };
        ShowBottomWhenHiddenRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(ShowBottomWhenHiddenText); };

        // ScrollZoom
        ScrollZoom.MouseEnter += delegate
        {
            colorAnimation.From =
                colorAnimation.To = AnimationHelper.GetPreferredColor();
            ScrollZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        };
        ScrollZoom.MouseLeave += delegate
        {
            colorAnimation.From = AnimationHelper.GetPreferredColor();
            colorAnimation.To = MainColor;
            ScrollZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        };

        // CtrlZoom
        CtrlZoom.MouseEnter += delegate
        {
            colorAnimation.From = MainColor;
            colorAnimation.To = AnimationHelper.GetPreferredColor();
            CtrlZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        };
        CtrlZoom.MouseLeave += delegate
        {
            colorAnimation.From = AnimationHelper.GetPreferredColor();
            colorAnimation.To = MainColor;
            CtrlZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        };

        // AvoidZoom
        AvoidZoomRadio.MouseEnter += delegate
        {
            colorAnimation.From = MainColor;
            colorAnimation.To = AnimationHelper.GetPreferredColor();
            AvoidZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        };
        AvoidZoomRadio.MouseLeave += delegate
        {
            colorAnimation.From = AnimationHelper.GetPreferredColor();
            colorAnimation.To = MainColor;
            AvoidZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        };

        if (!SettingsHelper.Settings.Theme.Dark) // Add white hover text on light theme
        {
            BlueRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                BlueText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            BlueRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                BlueText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            CyanRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                CyanText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            CyanRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                CyanText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            AquaRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                AquaText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            AquaRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                AquaText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            TealRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                TealText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            TealRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                TealText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            LimeRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                LimeText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            LimeRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                LimeText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            GreenRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                GreenText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            GreenRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                GreenText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            GoldenRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                GoldenText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            GoldenRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                GoldenText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            OrangeRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                OrangeText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            OrangeRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                OrangeText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            RedRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                RedText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            RedRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                RedText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            PinkRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                PinkText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            PinkRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                PinkText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            MagentaRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                MagentaText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            MagentaRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                MagentaText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            PurpleRadio.MouseEnter += delegate
            {
                colorAnimation.From = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                colorAnimation.To = Colors.White;
                PurpleText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            PurpleRadio.MouseLeave += delegate
            {
                colorAnimation.From = Colors.White;
                colorAnimation.To = Color.FromArgb(MainColor.A, MainColor.R, MainColor.G, MainColor.B);
                PurpleText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
        }
    }

    // Blue
    private void BlueRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            BlueBrush,
            1
        );
    }

    private void BlueRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            BlueBrush,
            1
        );
    }

    // Pink
    private void PinkRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            PinkBrush,
            2
        );
    }

    private void PinkRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            PinkBrush,
            2
        );
    }

    // Orange
    private void OrangeRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            OrangeBrush,
            3
        );
    }

    private void OrangeRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            OrangeBrush,
            3
        );
    }

    // Green
    private void GreenRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            GreenBrush,
            4
        );
    }

    private void GreenRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            GreenBrush,
            4
        );
    }

    // Red
    private void RedRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            RedBrush,
            5
        );
    }

    private void RedRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            RedBrush,
            5
        );
    }

    // Teal
    private void TealRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            TealBrush,
            6
        );
    }

    private void TealRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            TealBrush,
            6
        );
    }

    // Aqua
    private void AquaRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            AquaBrush,
            7
        );
    }

    private void AquaRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            AquaBrush,
            7
        );
    }

    // Golden
    private void GoldenRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            GoldenBrush,
            8
        );
    }

    private void GoldenRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            GoldenBrush,
            8
        );
    }

    // Purple
    private void PurpleRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            PurpleBrush,
            9
        );
    }

    private void PurpleRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            PurpleBrush,
            9
        );
    }

    // Cyan
    private void CyanRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            CyanBrush,
            10
        );
    }

    private void CyanRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            CyanBrush,
            10
        );
    }

    // Magenta
    private void MagentaRadio_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            MagentaBrush,
            11
        );
    }

    private void MagentaRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            MagentaBrush,
            11
        );
    }

    // Lime
    private void Lime_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseLeaveColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            LimeBrush,
            12
        );
    }

    private void LimeRadio_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimationHelper.MouseEnterColorEvent(
            BackgroundBorderColor.A,
            BackgroundBorderColor.R,
            BackgroundBorderColor.G,
            BackgroundBorderColor.B,
            LimeBrush,
            12
        );
    }

    #endregion EventHandlers
}
using PicView.Animations;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.ConfigureSettings;
using PicView.FileHandling;
using PicView.ProcessHandling;
using PicView.Properties;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.Translations;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static PicView.Animations.MouseOverAnimations;
using static PicView.ConfigureSettings.ConfigColors;
using static PicView.SystemIntegration.Wallpaper;

namespace PicView.Views.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            Title = Application.Current.Resources["SettingsWindow"] + " - PicView";
            MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
            Width *= WindowSizing.MonitorInfo.DpiScaling;
            if (double.IsNaN(Width)) // Fixes if user opens window when loading from startup
            {
                WindowSizing.MonitorInfo = MonitorSize.GetMonitorSize();
                MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
                Width *= WindowSizing.MonitorInfo.DpiScaling;
            }

            InitializeComponent();

            ContentRendered += delegate
            {
                WindowBlur.EnableBlur(this);
                ChangeColor();
                var colorAnimation = new ColorAnimation { Duration = TimeSpan.FromSeconds(.1) };

                AddGenericEvents(colorAnimation);

                // SubDirRadio
                SubDirRadio.IsChecked = Settings.Default.IncludeSubDirectories;
                SubDirRadio.Click += async delegate
                {
                    Settings.Default.IncludeSubDirectories = !Settings.Default.IncludeSubDirectories;
                    if (ErrorHandling.CheckOutOfRange()) { return; }
                    var preloadValue = Preloader.Get(Navigation.FolderIndex);
                    if (preloadValue is null) { return; }
                    Navigation.Pics = FileLists.FileList(preloadValue.FileInfo);
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        SetTitle.SetTitleString(preloadValue.BitmapSource.PixelWidth, preloadValue.BitmapSource.PixelHeight,
                            Navigation.FolderIndex, preloadValue.FileInfo);
                    });

                    Settings.Default.Save();
                };

                WallpaperApply.MouseLeftButtonDown += async delegate
                {
                    var x = WallpaperStyle.Fill;
                    if (Fit.IsSelected) { x = WallpaperStyle.Fit; }
                    if (Center.IsSelected) { x = WallpaperStyle.Center; }
                    if (Tile.IsSelected) { x = WallpaperStyle.Tile; }
                    if (Fit.IsSelected) { x = WallpaperStyle.Fit; }

                    await SetWallpaperAsync(x).ConfigureAwait(false);
                };

                SlideshowSlider.Value = Settings.Default.SlideTimer / 1000;
                SlideshowSlider.ValueChanged += (_, e) => Settings.Default.SlideTimer = e.NewValue * 1000;

                ZoomSlider.Value = Settings.Default.ZoomSpeed;
                txtZoomSlide.Text = Math.Round(ZoomSlider.Value * 100).ToString();
                ZoomSlider.ValueChanged += (_, e) => 
                {
                    Settings.Default.ZoomSpeed = e.NewValue; txtZoomSlide.Text = Math.Round(e.NewValue * 100).ToString();
                    Settings.Default.Save();
                };

                LightThemeRadio.IsChecked = !Settings.Default.DarkTheme;
                DarkThemeRadio.IsChecked = Settings.Default.DarkTheme;

                DarkThemeRadio.Click += delegate
                {
                    if (Properties.Settings.Default.DarkTheme)
                    {
                        DarkThemeRadio.IsChecked = true;
                        LightThemeRadio.IsChecked = false;
                        return;
                    }
                    ChangeTheme(true);
                    LightThemeRadio.IsChecked = false;
                    Settings.Default.Save();
                };
                LightThemeRadio.Click += delegate
                {
                    if (!Properties.Settings.Default.DarkTheme)
                    {
                        DarkThemeRadio.IsChecked = false;
                        LightThemeRadio.IsChecked = true;
                        return;
                    }
                    ChangeTheme(false);
                    DarkThemeRadio.IsChecked = false;
                    Settings.Default.Save();
                };

                foreach (var language in Enum.GetValues(typeof(Languages)))
                {
                    try
                    {
                        LanguageBox.Items.Add(new ComboBoxItem
                        {
                            Content = new CultureInfo(language.ToString()).DisplayName,
                            IsSelected = language.ToString() == Settings.Default.UserLanguage,
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

                LanguageBox.SelectionChanged += delegate
                {
                    LoadLanguage.ChangeLanguage(LanguageBox.SelectedIndex);
                };

                // ScrollDirection
                Reverse.IsSelected = Settings.Default.HorizontalReverseScroll;
                Reverse.Selected += (_, _) => Settings.Default.HorizontalReverseScroll = !Settings.Default.HorizontalReverseScroll;

                Forward.IsSelected = !Settings.Default.HorizontalReverseScroll;
                Forward.Selected += (_, _) => Settings.Default.HorizontalReverseScroll = !Settings.Default.HorizontalReverseScroll;

                AltUIRadio.IsChecked = Settings.Default.ShowAltInterfaceButtons;
                AltUIRadio.Click += delegate
                {
                    Settings.Default.ShowAltInterfaceButtons = !Settings.Default.ShowAltInterfaceButtons;
                };

                CtrlZoom.IsChecked = Settings.Default.CtrlZoom;
                ScrollZoom.IsChecked = !Settings.Default.CtrlZoom;

                CtrlZoom.Checked += (_, _) => Settings.Default.CtrlZoom = true;
                ScrollZoom.Checked += (_, _) => Settings.Default.CtrlZoom = false;

                ThemeRestart.MouseLeftButtonDown += (_, _) => ProcessLogic.RestartApp();
                LanguageRestart.MouseLeftButtonDown += (_, _) => ProcessLogic.RestartApp();

                TopmostRadio.Checked += (_, _) => ConfigureWindows.IsMainWindowTopMost = !Settings.Default.TopMost;
                TopmostRadio.Unchecked += (_, _) => ConfigureWindows.IsMainWindowTopMost = false;
                TopmostRadio.IsChecked = Settings.Default.TopMost;

                CenterRadio.Checked += (_, _) => Settings.Default.KeepCentered = true;
                CenterRadio.Unchecked += (_, _) => Settings.Default.KeepCentered = false;
                CenterRadio.IsChecked = Settings.Default.KeepCentered;

                switch (Settings.Default.ColorTheme)
                {
                    case 1:
                        BlueRadio.IsChecked = true;
                        break;

                    case 2:
                        PinkRadio.IsChecked = true;
                        break;

                    case 3:
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

                    default:
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

        public void ChangeColor()
        {
            Logo.ChangeColor();
        }

        #region EventHandlers

        private void AddGenericEvents(ColorAnimation colorAnimation)
        {
            KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(null, e, this);

            MouseLeftButtonDown += (_, e) =>
            { if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); } };

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            // BlueRadio
            BlueRadio.MouseEnter += BlueRadio_MouseEnter;
            BlueRadio.MouseLeave += BlueRadio_MouseLeave;
            BlueRadio.Click += (_,_) => UpdateColorThemeTo(ColorOption.Blue);

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
                colorAnimation.To = AnimationHelper.GetPrefferedColor();
                ThemeRestartTxt.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            ThemeRestart.MouseLeave += delegate
            {
                colorAnimation.From = AnimationHelper.GetPrefferedColor();
                colorAnimation.To = MainColor;
                ThemeRestartTxt.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            // LanguageRestart
            LanguageRestart.MouseEnter += delegate
            {
                colorAnimation.From = MainColor;
                colorAnimation.To = AnimationHelper.GetPrefferedColor();
                LanguageRestartTxt.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            LanguageRestart.MouseLeave += delegate
            {
                colorAnimation.From = AnimationHelper.GetPrefferedColor();
                colorAnimation.To = MainColor;
                LanguageRestartTxt.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
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

            // ScrollZoom
            ScrollZoom.MouseEnter += delegate
            {
                colorAnimation.From =
                colorAnimation.To = AnimationHelper.GetPrefferedColor();
                ScrollZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            ScrollZoom.MouseLeave += delegate
            {
                colorAnimation.From = AnimationHelper.GetPrefferedColor();
                colorAnimation.To = MainColor;
                ScrollZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            // CtrlZoom
            CtrlZoom.MouseEnter += delegate
            {
                colorAnimation.From = MainColor;
                colorAnimation.To = AnimationHelper.GetPrefferedColor();
                CtrlZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            CtrlZoom.MouseLeave += delegate
            {
                colorAnimation.From = AnimationHelper.GetPrefferedColor();
                colorAnimation.To = MainColor;
                CtrlZoomText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };


            if (!Settings.Default.DarkTheme) // Add white hover text on light theme
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
}
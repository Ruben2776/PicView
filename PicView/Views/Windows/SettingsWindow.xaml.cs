using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.Translations;
using PicView.UILogic;
using PicView.UILogic.Animations;
using PicView.UILogic.Sizing;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static PicView.ConfigureSettings.ConfigColors;
using static PicView.SystemIntegration.Wallpaper;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
            InitializeComponent();
            Width *= WindowSizing.MonitorInfo.DpiScaling;

            ContentRendered += delegate
            {
                var colorAnimation = new ColorAnimation { Duration = TimeSpan.FromSeconds(.1) };
                
                AddGenericEvents(colorAnimation);

                // SubDirRadio
                SubDirRadio.IsChecked = Properties.Settings.Default.IncludeSubDirectories;
                SubDirRadio.Click += delegate
                {
                    Properties.Settings.Default.IncludeSubDirectories = !Properties.Settings.Default.IncludeSubDirectories;
                    Error_Handling.Reload();
                };

                // BorderColorRadio
                BorderRadio.Click += UpdateUIValues.SetBorderColorEnabled;
                if (Properties.Settings.Default.WindowBorderColorEnabled)
                {
                    BorderRadio.IsChecked = true;
                }

                WallpaperApply.MouseLeftButtonDown += delegate 
                {
                    var x = WallpaperStyle.Fill;
                    if (Fit.IsSelected) { x = WallpaperStyle.Fit; }
                    if (Center.IsSelected) { x = WallpaperStyle.Center; }
                    if (Tile.IsSelected) { x = WallpaperStyle.Tile; }
                    if (Fit.IsSelected) { x = WallpaperStyle.Fit; }

                    SetWallpaper(x);
                };

                SlideshowSlider.Value = Properties.Settings.Default.SlideTimer / 1000;
                SlideshowSlider.ValueChanged += (_, e) => Properties.Settings.Default.SlideTimer = e.NewValue * 1000;

                ZoomSlider.Value = Properties.Settings.Default.ZoomSpeed;
                ZoomSlider.ValueChanged += (_,e) => Properties.Settings.Default.ZoomSpeed = e.NewValue;

                LightThemeRadio.IsChecked = !Properties.Settings.Default.DarkTheme;
                DarkThemeRadio.IsChecked = Properties.Settings.Default.DarkTheme;

                DarkThemeRadio.Click += delegate
                {
                    ChangeToDarkTheme();
                    LightThemeRadio.IsChecked = false;
                };
                LightThemeRadio.Click += delegate
                {
                    ChangeToLightTheme();
                    DarkThemeRadio.IsChecked = false;
                };

                foreach (var language in Enum.GetValues(typeof(Languages)))
                {
                    LanguageBox.Items.Add(new ComboBoxItem
                    {
                        Content = new CultureInfo(language.ToString()).DisplayName,
                        IsSelected = language.ToString() == Properties.Settings.Default.UserLanguage,
                    });
                }

                LanguageBox.SelectionChanged += delegate
                {
                    GeneralSettings.ChangeLanguage((LanguageBox.SelectedIndex));
                };

                AltUIRadio.IsChecked = Properties.Settings.Default.ShowAltInterfaceButtons;
                AltUIRadio.Click += delegate
                {
                    Properties.Settings.Default.ShowAltInterfaceButtons = !Properties.Settings.Default.ShowAltInterfaceButtons;
                };

                CtrlZoom.IsChecked = Properties.Settings.Default.CtrlZoom;
                ScrollZoom.IsChecked = !Properties.Settings.Default.CtrlZoom;

                CtrlZoom.Checked += (_, _) => Properties.Settings.Default.CtrlZoom = true;
                ScrollZoom.Checked += (_, _) => Properties.Settings.Default.CtrlZoom = false;

                RestartButton.Click += delegate
                {
                    GeneralSettings.RestartApp();
                };

                TopmostRadio.IsChecked = Properties.Settings.Default.TopMost;
                TopmostRadio.Checked += (_, _) => ConfigureWindows.IsMainWindowTopMost = !Properties.Settings.Default.TopMost;
                TopmostRadio.Unchecked += (_, _) => ConfigureWindows.IsMainWindowTopMost = false;

                CenterRadio.IsChecked = Properties.Settings.Default.KeepCentered;
                CenterRadio.Checked += (_, _) => Properties.Settings.Default.KeepCentered = true;
                CenterRadio.Unchecked += (_, _) => Properties.Settings.Default.KeepCentered = false;

                SetAsDefaultTxt.MouseLeftButtonDown += (_, _) => GeneralSettings.ElevateProcess(Process.GetCurrentProcess());


                switch (Properties.Settings.Default.ColorTheme)
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

        #region EventHandlers

        private void AddGenericEvents(ColorAnimation colorAnimation)
        {
            KeyDown += (_, e) => Shortcuts.GenericWindowShortcuts.KeysDown(null, e, this);

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            // BlueRadio
            BlueRadio.PreviewMouseLeftButtonDown += BlueRadio_PreviewMouseLeftButtonDown;
            BlueRadio.MouseEnter += BlueRadio_MouseEnter;
            BlueRadio.MouseLeave += BlueRadio_MouseLeave;
            BlueRadio.Click += Blue;

            // PinkRadio
            PinkRadio.PreviewMouseLeftButtonDown += PinkRadio_PreviewMouseLeftButtonDown;
            PinkRadio.MouseEnter += PinkRadio_MouseEnter;
            PinkRadio.MouseLeave += PinkRadio_MouseLeave;
            PinkRadio.Click += Pink;

            // OrangeRadio
            OrangeRadio.PreviewMouseLeftButtonDown += OrangeRadio_PreviewMouseLeftButtonDown;
            OrangeRadio.MouseEnter += OrangeRadio_MouseEnter;
            OrangeRadio.MouseLeave += OrangeRadio_MouseLeave;
            OrangeRadio.Click += Orange;

            // GreenRadio
            GreenRadio.PreviewMouseLeftButtonDown += GreenRadio_PreviewMouseLeftButtonDown;
            GreenRadio.MouseEnter += GreenRadio_MouseEnter;
            GreenRadio.MouseLeave += GreenRadio_MouseLeave;
            GreenRadio.Click += Green;

            // RedRadio
            RedRadio.PreviewMouseLeftButtonDown += RedRadio_PreviewMouseLeftButtonDown;
            RedRadio.MouseEnter += RedRadio_MouseEnter;
            RedRadio.MouseLeave += RedRadio_MouseLeave;
            RedRadio.Click += Red;

            // TealRadio
            TealRadio.PreviewMouseLeftButtonDown += TealRadio_PreviewMouseLeftButtonDown;
            TealRadio.MouseEnter += TealRadio_MouseEnter;
            TealRadio.MouseLeave += TealRadio_MouseLeave;
            TealRadio.Click += Teal;

            // AquaRadio
            AquaRadio.PreviewMouseLeftButtonDown += AquaRadio_PreviewMouseLeftButtonDown;
            AquaRadio.MouseEnter += AquaRadio_MouseEnter;
            AquaRadio.MouseLeave += AquaRadio_MouseLeave;
            AquaRadio.Click += Aqua;

            // GoldenRadio
            GoldenRadio.PreviewMouseLeftButtonDown += GoldenRadio_PreviewMouseLeftButtonDown;
            GoldenRadio.MouseEnter += GoldenRadio_MouseEnter;
            GoldenRadio.MouseLeave += GoldenRadio_MouseLeave;
            GoldenRadio.Click += Golden;

            // PurpleRadio
            PurpleRadio.PreviewMouseLeftButtonDown += PurpleRadio_PreviewMouseLeftButtonDown;
            PurpleRadio.MouseEnter += PurpleRadio_MouseEnter;
            PurpleRadio.MouseLeave += PurpleRadio_MouseLeave;
            PurpleRadio.Click += Purple;

            // CyanRadio
            CyanRadio.PreviewMouseLeftButtonDown += CyanRadio_PreviewMouseLeftButtonDown;
            CyanRadio.MouseEnter += CyanRadio_MouseEnter;
            CyanRadio.MouseLeave += CyanRadio_MouseLeave;
            CyanRadio.Click += Cyan;

            // MagentaRadio
            MagentaRadio.PreviewMouseLeftButtonDown += MagentaRadio_PreviewMouseLeftButtonDown;
            MagentaRadio.MouseEnter += MagentaRadio_MouseEnter;
            MagentaRadio.MouseLeave += MagentaRadio_MouseLeave;
            MagentaRadio.Click += Magenta;

            // LimeRadio
            LimeRadio.Click += Lime;
            LimeRadio.PreviewMouseLeftButtonDown += LimeRadio_PreviewMouseLeftButtonDown;
            LimeRadio.MouseEnter += LimeRadio_MouseEnter;
            LimeRadio.MouseLeave += Lime_MouseLeave;

            // WallpaperApply
            WallpaperApply.MouseEnter += delegate {
                colorAnimation.From = AnimationHelper.GetPrefferedColorOver();
                colorAnimation.To = AnimationHelper.GetPrefferedColorDown();
                WallpaperApplyBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            WallpaperApply.MouseLeave += delegate {
                colorAnimation.From = AnimationHelper.GetPrefferedColorDown();
                colorAnimation.To = AnimationHelper.GetPrefferedColorOver();
                WallpaperApplyBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            // SetAsDefaultTxt
            SetAsDefaultTxt.MouseEnter += delegate {
                colorAnimation.From = AnimationHelper.GetPrefferedColorOver();
                colorAnimation.To = AnimationHelper.GetPrefferedColorDown();
                SetAsDefaultTxtBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };
            SetAsDefaultTxt.MouseLeave += delegate {
                colorAnimation.From = AnimationHelper.GetPrefferedColorDown();
                colorAnimation.To = AnimationHelper.GetPrefferedColorOver();
                SetAsDefaultTxtBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            };

            // DarkThemeRadio
            DarkThemeRadio.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(DarkThemeText); };
            DarkThemeRadio.MouseEnter += delegate { ButtonMouseOverAnim(DarkThemeText); };
            DarkThemeRadio.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(DarkThemeBrush); };
            DarkThemeRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(DarkThemeText); };
            DarkThemeRadio.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(DarkThemeBrush); };

            // LightThemeRadio
            LightThemeRadio.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(LightThemeText); };
            LightThemeRadio.MouseEnter += delegate { ButtonMouseOverAnim(LightThemeText); };
            LightThemeRadio.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(LightThemeBrush); };
            LightThemeRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(LightThemeText); };
            LightThemeRadio.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(LightThemeBrush); };

            // SubDirRadio
            SubDirRadio.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SubDirText); };
            SubDirRadio.MouseEnter += delegate { ButtonMouseOverAnim(SubDirText); };
            SubDirRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(SubDirText); };

            // TopmostRadio
            TopmostRadio.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(TopMostDirText); };
            TopmostRadio.MouseEnter += delegate { ButtonMouseOverAnim(TopMostDirText); };
            TopmostRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(TopMostDirText); };

            // CenterRadio
            CenterRadio.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(CenterubDirText); };
            CenterRadio.MouseEnter += delegate { ButtonMouseOverAnim(CenterubDirText); };
            CenterRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(CenterubDirText); };

            // BorderRadio
            BorderRadio.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BorderBrushText); };
            BorderRadio.MouseEnter += delegate { ButtonMouseOverAnim(BorderBrushText); };
            BorderRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(BorderBrushText); };

            // AltUIRadio
            AltUIRadio.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(AltUIText); };
            AltUIRadio.MouseEnter += delegate { ButtonMouseOverAnim(AltUIText); };
            AltUIRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(AltUIText); };

            // ScrollZoom
            ScrollZoom.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ScrollZoomText); };
            ScrollZoom.MouseEnter += delegate { ButtonMouseOverAnim(ScrollZoomText); };
            ScrollZoom.MouseLeave += delegate { ButtonMouseLeaveAnim(ScrollZoomText); };

            // CtrlZoom
            CtrlZoom.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(CtrlZoomText); };
            CtrlZoom.MouseEnter += delegate { ButtonMouseOverAnim(CtrlZoomText); };
            CtrlZoom.MouseLeave += delegate { ButtonMouseLeaveAnim(CtrlZoomText); };

            // Restart
            RestartButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RestartText); };
            RestartButton.MouseEnter += delegate { ButtonMouseOverAnim(RestartText); };
            RestartButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(RestartBrush); };
            RestartButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RestartText); };
            RestartButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(RestartBrush); };

            if (!Properties.Settings.Default.DarkTheme)
            {
                BlueRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    BlueText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                BlueRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    BlueText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                CyanRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    CyanText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                CyanRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    CyanText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                AquaRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    AquaText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                AquaRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    AquaText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                TealRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    TealText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                TealRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    TealText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                LimeRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    LimeText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                LimeRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    LimeText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                GreenRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    GreenText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                GreenRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    GreenText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                GoldenRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    GoldenText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                GoldenRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    GoldenText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                OrangeRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    OrangeText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                OrangeRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    OrangeText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                RedRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    RedText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                RedRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    RedText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                PinkRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    PinkText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                PinkRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    PinkText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                MagentaRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    MagentaText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                MagentaRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    MagentaText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };

                PurpleRadio.MouseEnter += delegate {
                    colorAnimation.From = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    colorAnimation.To = Colors.White;
                    PurpleText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
                PurpleRadio.MouseLeave += delegate {
                    colorAnimation.From = Colors.White;
                    colorAnimation.To = Color.FromArgb(mainColor.A, mainColor.R, mainColor.G, mainColor.B);
                    PurpleText.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                };
            }
        }

        // Blue
        private void BlueRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                BlueBrush,
                1
            );
        }

        private void BlueRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                BlueBrush,
                1
            );
        }

        private void BlueRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(BlueBrush, 1);
        }

        // Pink
        private void PinkRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                PinkBrush,
                2
            );
        }

        private void PinkRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                PinkBrush,
                2
            );
        }

        private void PinkRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(PinkBrush, 2);
        }

        // Orange
        private void OrangeRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                OrangeBrush,
                3
            );
        }

        private void OrangeRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                OrangeBrush,
                3
            );
        }

        private void OrangeRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(OrangeBrush, 3);
        }

        // Green
        private void GreenRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                GreenBrush,
                4
            );
        }

        private void GreenRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                GreenBrush,
                4
            );
        }

        private void GreenRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(GreenBrush, 4);
        }

        // Red
        private void RedRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                RedBrush,
                5
            );
        }

        private void RedRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                RedBrush,
                5
            );
        }

        private void RedRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(RedBrush, 5);
        }

        // Teal
        private void TealRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                TealBrush,
                6
            );
        }

        private void TealRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                TealBrush,
                6
            );
        }

        private void TealRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(TealBrush, 6);
        }

        // Aqua
        private void AquaRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                AquaBrush,
                7
            );
        }

        private void AquaRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                AquaBrush,
                7
            );
        }

        private void AquaRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(AquaBrush, 7);
        }

        // Golden
        private void GoldenRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                GoldenBrush,
                8
            );
        }

        private void GoldenRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                GoldenBrush,
                8
            );
        }

        private void GoldenRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(GoldenBrush, 8);
        }

        // Purple
        private void PurpleRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                PurpleBrush,
                9
            );
        }

        private void PurpleRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                PurpleBrush,
                9
            );
        }

        private void PurpleRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(PurpleBrush, 9);
        }

        // Cyan
        private void CyanRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                CyanBrush,
                10
            );
        }

        private void CyanRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                CyanBrush,
                10
            );
        }

        private void CyanRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CyanBrush, 10);
        }

        // Magenta
        private void MagentaRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                MagentaBrush,
                11
            );
        }

        private void MagentaRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                MagentaBrush,
                11
            );
        }

        private void MagentaRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(MagentaBrush, 11);
        }

        // Lime
        private void Lime_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                LimeBrush,
                12
            );
        }

        private void LimeRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                LimeBrush,
                12
            );
        }

        private void LimeRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(LimeBrush, 12);
        }

        #endregion EventHandlers
    }
}
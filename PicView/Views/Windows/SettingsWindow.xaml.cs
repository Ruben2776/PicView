using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.Translations;
using PicView.UILogic.Animations;
using PicView.UILogic.Sizing;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.ConfigureSettings.ConfigColors;
using static PicView.SystemIntegration.Wallpaper;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            Width = 500 * WindowLogic.MonitorInfo.DpiScaling;

            InitializeComponent();

            ContentRendered += delegate
            {
                // Center vertically
                Top = ((WindowLogic.MonitorInfo.WorkArea.Height * WindowLogic.MonitorInfo.DpiScaling) - ActualHeight) / 2 + WindowLogic.MonitorInfo.WorkArea.Top;

                KeyDown += KeysDown;
                AddGenericEvents();

                SetCheckedColorEvent();

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

                // Fill
                Fill.Click += delegate
                {
                    SetWallpaper(WallpaperStyle.Fill);
                };

                // Fit
                Fit.Click += delegate
                {
                    SetWallpaper(WallpaperStyle.Fit);
                };

                // Center

                Center.Click += delegate
                {
                    SetWallpaper(WallpaperStyle.Center);
                };

                // Tile
                Tile.Click += delegate
                {
                    SetWallpaper(WallpaperStyle.Tile);
                };

                // Stretch
                Stretch.Click += delegate
                {
                    SetWallpaper(WallpaperStyle.Stretch);
                };

                SlideshowSlider.Value = Properties.Settings.Default.SlideTimer / 1000;
                SlideshowSlider.ValueChanged += SlideshowSlider_ValueChanged;

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

                RestartButton.Click += delegate
                {
                    GeneralSettings.RestartApp();
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
                SubDirRadio.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(SubDirBrush); };
                SubDirRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(SubDirText); };
                SubDirRadio.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(SubDirBrush); };

                // BorderRadio
                BorderRadio.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BorderBrushText); };
                BorderRadio.MouseEnter += delegate { ButtonMouseOverAnim(BorderBrushText); };
                BorderRadio.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(BorderBrushColor); };
                BorderRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(BorderBrushText); };
                BorderRadio.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(BorderBrushColor); };

                // AltUIRadio
                AltUIRadio.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(AltUIText); };
                AltUIRadio.MouseEnter += delegate { ButtonMouseOverAnim(AltUIText); };
                AltUIRadio.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(AltUIColor); };
                AltUIRadio.MouseLeave += delegate { ButtonMouseLeaveAnim(AltUIText); };
                AltUIRadio.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(AltUIColor); };

                // Fill
                Fill.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FillText); };
                Fill.MouseEnter += delegate { ButtonMouseOverAnim(FillText); };
                Fill.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(FillBrush); };
                Fill.MouseLeave += delegate { ButtonMouseLeaveAnim(FillText); };
                Fill.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(FillBrush); };

                // Center
                Center.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(CenterText); };
                Center.MouseEnter += delegate { ButtonMouseOverAnim(CenterText); };
                Center.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(CenterBrush); };
                Center.MouseLeave += delegate { ButtonMouseLeaveAnim(CenterText); };
                Center.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(CenterBrush); };

                // Fit
                Fit.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FitText); };
                Fit.MouseEnter += delegate { ButtonMouseOverAnim(FitText); };
                Fit.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(FitBrush); };
                Fit.MouseLeave += delegate { ButtonMouseLeaveAnim(FitText); };
                Fit.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(FitBrush); };

                // Tile
                Tile.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(TileText); };
                Tile.MouseEnter += delegate { ButtonMouseOverAnim(TileText); };
                Tile.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(TileBrush); };
                Tile.MouseLeave += delegate { ButtonMouseLeaveAnim(TileText); };
                Tile.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(TileBrush); };

                // Stretch
                Stretch.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(StretchText); };
                Stretch.MouseEnter += delegate { ButtonMouseOverAnim(StretchText); };
                Stretch.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(StretchBrush); };
                Stretch.MouseLeave += delegate { ButtonMouseLeaveAnim(StretchText); };
                Stretch.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(StretchBrush); };

                // Restart
                RestartButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RestartText); };
                RestartButton.MouseEnter += delegate { ButtonMouseOverAnim(RestartText); };
                RestartButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(RestartBrush); };
                RestartButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RestartText); };
                RestartButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(RestartBrush); };
            };
        }

        private void SetCheckedColorEvent()
        {
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
        }

        private void SlideshowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Properties.Settings.Default.SlideTimer = e.NewValue * 1000;
        }

        private void KeysDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    UILogic.Loading.LoadWindows.GetMainWindow.Focus();
                    break;

                case Key.S:
                case Key.Down:
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 10);
                    break;

                case Key.W:
                case Key.U:
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 10);
                    break;

                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        Environment.Exit(0);
                    }
                    break;
            }
        }

        #region EventHandlers

        private void AddGenericEvents()
        {
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
            LimeRadio.PreviewMouseLeftButtonDown += GreyRadio_PreviewMouseLeftButtonDown;
            LimeRadio.MouseEnter += GreyRadio_MouseEnter;
            LimeRadio.MouseLeave += GreyRadio_MouseLeave;
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

        // Beige
        private void GoldenRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                BeigeBrush,
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
                BeigeBrush,
                8
            );
        }

        private void GoldenRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(BeigeBrush, 8);
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

        // Grey
        private void GreyRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                GreyBrush,
                12
            );
        }

        private void GreyRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                GreyBrush,
                12
            );
        }

        private void GreyRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(GreyBrush, 12);
        }

        #endregion EventHandlers
    }
}
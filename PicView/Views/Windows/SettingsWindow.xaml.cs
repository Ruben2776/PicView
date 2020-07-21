using PicView.ChangeImage;
using PicView.ConfigureSettings;
using PicView.Translations;
using PicView.UILogic.Animations;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.ConfigureSettings.ConfigColors;
using static PicView.Library.Fields;
using static PicView.SystemIntegration.Wallpaper;

namespace PicView.Views.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            ContentRendered += (s, x) =>
            {
                KeyUp += KeysUp;
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
                BorderRadio.Click += ConfigureSettings.UpdateUIValues.SetBorderColorEnabled;
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

                LightThemeRadio.IsChecked = Properties.Settings.Default.LightTheme;
                DarkThemeRadio.IsChecked = !Properties.Settings.Default.LightTheme;

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
                        Content = new CultureInfo(language.ToString()).DisplayName
                    });
                }

                LanguageBox.SelectedIndex = 0;
                LanguageBox.SelectionChanged += delegate
                {
                    GeneralSettings.ChangeLanguage((LanguageBox.SelectedIndex));
                };

                RestartButton.Click += delegate
                {
                    GeneralSettings.RestartApp();
                };
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

        private void KeysUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    TheMainWindow.Focus();
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

            // DarkThemeRadio
            DarkThemeRadio.PreviewMouseLeftButtonDown += DarkTheme_PreviewMouseLeftButtonDown;
            DarkThemeRadio.MouseEnter += DarkTheme_MouseEnter;
            DarkThemeRadio.MouseLeave += DarkTheme_MouseLeave;

            // LightThemeRadio
            LightThemeRadio.PreviewMouseLeftButtonDown += LightTheme_PreviewMouseLeftButtonDown;
            LightThemeRadio.MouseEnter += LightTheme_MouseEnter;
            LightThemeRadio.MouseLeave += LightTheme_MouseLeave;

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

            SubDirRadio.PreviewMouseLeftButtonDown += SubDirRadio_PreviewMouseLeftButtonDown;
            SubDirRadio.MouseEnter += SubDirRadio_MouseEnter;
            SubDirRadio.MouseLeave += SubDirRadio_MouseLeave;

            BorderRadio.PreviewMouseLeftButtonDown += BorderRadio_PreviewMouseLeftButtonDown;
            BorderRadio.MouseEnter += BorderRadio_MouseEnter;
            BorderRadio.MouseLeave += BorderRadio_MouseLeave;

            Fill.PreviewMouseLeftButtonDown += Fill_PreviewMouseLeftButtonDown;
            Fill.MouseEnter += Fill_MouseEnter;
            Fill.MouseLeave += Fill_MouseLeave;

            Fit.PreviewMouseLeftButtonDown += Fit_PreviewMouseLeftButtonDown;
            Fit.MouseEnter += Fit_MouseEnter;
            Fit.MouseLeave += Fit_MouseLeave;

            Center.PreviewMouseLeftButtonDown += Center_PreviewMouseLeftButtonDown;
            Center.MouseEnter += Center_MouseEnter;
            Center.MouseLeave += Center_MouseLeave;

            Tile.PreviewMouseLeftButtonDown += Tile_PreviewMouseLeftButtonDown;
            Tile.MouseEnter += Tile_MouseEnter;
            Tile.MouseLeave += Tile_MouseLeave;

            Stretch.PreviewMouseLeftButtonDown += Stretch_PreviewMouseLeftButtonDown;
            Stretch.MouseEnter += Stretch_MouseEnter;
            Stretch.MouseLeave += Stretch_MouseLeave;

            RestartButton.PreviewMouseLeftButtonDown += Restart_PreviewMouseLeftButtonDown;
            RestartButton.MouseEnter += Restart_MouseEnter;
            RestartButton.MouseLeave += Restart_MouseLeave;


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

        // SubDirRadio
        private void SubDirRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SubDirBrush,
                false
            );
        }

        private void SubDirRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SubDirBrush,
                false
            );
        }

        private void SubDirRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, SubDirBrush, false);
        }

        // BorderColor
        private void BorderRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                BorderBrushColor,
                false
            );
        }

        private void BorderRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                BorderBrushColor,
                false
            );
        }

        private void BorderRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, BorderBrushColor, false);
        }

        #endregion EventHandlers 

        #region Mouseover Events

        // DarkTheme
        private void DarkTheme_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                DarkThemeBrush,
                false
            );
        }

        private void DarkTheme_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                DarkThemeBrush,
                false
            );
        }

        private void DarkTheme_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(DarkThemeBrush, false);
        }

        // LightTheme
        private void LightTheme_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                LightThemeBrush,
                false
            );
        }

        private void LightTheme_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                LightThemeBrush,
                false
            );
        }

        private void LightTheme_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(LightThemeBrush, false);
        }

        // Restart Button
        private void Restart_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                RestartBrush,
                false
            );
        }

        private void Restart_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                RestartBrush,
                false
            );
        }

        private void Restart_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(RestartBrush, false);
        }

        // Fill Button
        private void Fill_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                RestartBrush,
                false
            );
        }

        private void Fill_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FillBrush,
                false
            );
        }

        private void Fill_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FillBrush, false);
        }

        // Tile Button
        private void Tile_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                TileBrush,
                false
            );
        }

        private void Tile_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                TileBrush,
                false
            );
        }

        private void Tile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(TileBrush, false);
        }

        // Center Button
        private void Center_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                CenterBrush,
                false
            );
        }

        private void Center_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                CenterBrush,
                false
            );
        }

        private void Center_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CenterBrush, false);
        }

        // Fitbutton Mouse Event
        private void Fit_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FitBrush,
                false
            );
        }

        private void Fit_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FitBrush,
                false
            );
        }

        private void Fit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FitBrush, false);
        }

        // Stretch Button
        private void Stretch_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                StretchBrush,
                false
            );
        }

        private void Stretch_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                StretchBrush,
                false
            );
        }

        private void Stretch_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(StretchBrush, false);
        }

        #endregion Mouseover Events
    }
}
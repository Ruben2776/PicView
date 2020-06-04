using PicView.UserControls;
using System;
using System.Windows;
using System.Windows.Input;
using static PicView.Fields;
using static PicView.Wallpaper;

namespace PicView.Windows
{
    public partial class AllSettings : Window
    {
        public AllSettings()
        {
            InitializeComponent();

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
                    BeigeRadio.IsChecked = true;
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
                    GreyRadio.IsChecked = true;
                    break;
            }

            KeyUp += KeysUp;
            KeyDown += KeysDown;

            ContentRendered += (s, x) =>
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

                // BeigeRadio
                BeigeRadio.PreviewMouseLeftButtonDown += BeigeRadio_PreviewMouseLeftButtonDown;
                BeigeRadio.MouseEnter += BeigeRadio_MouseEnter;
                BeigeRadio.MouseLeave += BeigeRadio_MouseLeave;
                BeigeRadio.Click += Beige;

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

                // GreyRadio
                GreyRadio.PreviewMouseLeftButtonDown += GreyRadio_PreviewMouseLeftButtonDown;
                GreyRadio.MouseEnter += GreyRadio_MouseEnter;
                GreyRadio.MouseLeave += GreyRadio_MouseLeave;
                GreyRadio.Click += Grey;

                // LoopRadio
                LoopRadio.PreviewMouseLeftButtonDown += LoopRadio_PreviewMouseLeftButtonDown;
                LoopRadio.MouseEnter += LoopRadio_MouseEnter;
                LoopRadio.MouseLeave += LoopRadio_MouseLeave;
                LoopRadio.IsChecked = Properties.Settings.Default.Looping;
                LoopRadio.Click += Configs.SetLooping;

                // BorderColorRadio
                BorderRadio.PreviewMouseLeftButtonDown += BorderRadio_PreviewMouseLeftButtonDown;
                BorderRadio.MouseEnter += BorderRadio_MouseEnter;
                BorderRadio.MouseLeave += BorderRadio_MouseLeave;
                BorderRadio.Click += Configs.SetBorderColorEnabled;
                if (Properties.Settings.Default.WindowBorderColorEnabled)
                {
                    BorderRadio.IsChecked = true;
                }


                // Toggle Scroll
                ToggleScroll.PreviewMouseLeftButtonDown += ToggleScroll_PreviewMouseLeftButtonDown;
                ToggleScroll.MouseEnter += ToggleScroll_MouseEnter;
                ToggleScroll.MouseLeave += ToggleScroll_MouseLeave;
                ToggleScroll.IsChecked = Properties.Settings.Default.ScrollEnabled;

                // Set Fit
                SetFit.PreviewMouseLeftButtonDown += SetFit_PreviewMouseLeftButtonDown;
                SetFit.MouseEnter += SetFit_MouseEnter;
                SetFit.MouseLeave += SetFit_MouseLeave;
                SetFit.IsChecked = Properties.Settings.Default.AutoFit;

                // Fill
                Fill.PreviewMouseLeftButtonDown += Fill_PreviewMouseLeftButtonDown;
                Fill.MouseEnter += Fill_MouseEnter;
                Fill.MouseLeave += Fill_MouseLeave;
                Fill.Click += delegate
                {
                    SetWallpaper(Pics[FolderIndex], WallpaperStyle.Fill);
                };

                // Fit
                Fit.PreviewMouseLeftButtonDown += Fit_PreviewMouseLeftButtonDown;
                Fit.MouseEnter += Fit_MouseEnter;
                Fit.MouseLeave += Fit_MouseLeave;
                Fit.Click += delegate
                {
                    SetWallpaper(Pics[FolderIndex], WallpaperStyle.Fit);
                };

                // Center
                Center.PreviewMouseLeftButtonDown += Center_PreviewMouseLeftButtonDown;
                Center.MouseEnter += Center_MouseEnter;
                Center.MouseLeave += Center_MouseLeave;
                Center.Click += delegate
                {
                    SetWallpaper(Pics[FolderIndex], WallpaperStyle.Center);
                };

                // Tile
                Tile.PreviewMouseLeftButtonDown += Tile_PreviewMouseLeftButtonDown;
                Tile.MouseEnter += Tile_MouseEnter;
                Tile.MouseLeave += Tile_MouseLeave;
                Tile.Click += delegate
                {
                    SetWallpaper(Pics[FolderIndex], WallpaperStyle.Tile);
                };

                // Stretch
                Stretch.PreviewMouseLeftButtonDown += Stretch_PreviewMouseLeftButtonDown;
                Stretch.MouseEnter += Stretch_MouseEnter;
                Stretch.MouseLeave += Stretch_MouseLeave;
                Stretch.Click += delegate
                {
                    SetWallpaper(Pics[FolderIndex], WallpaperStyle.Stretch);
                };

                // Set Center
                SetCenter.PreviewMouseLeftButtonDown += SetCenter_PreviewMouseLeftButtonDown;
                SetCenter.MouseEnter += SetCenter_MouseEnter;
                SetCenter.MouseLeave += SetCenter_MouseLeave;

                SlideshowSlider.Value = Properties.Settings.Default.SlideTimer / 1000;
                SlideshowSlider.ValueChanged += SlideshowSlider_ValueChanged;
            };
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
                    mainWindow.Focus();
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

        // Blue
        void BlueRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void BlueRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void BlueRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(BlueBrush, 1);
        }

        // Pink
        void PinkRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void PinkRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void PinkRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(PinkBrush, 2);
        }

        // Orange
        void OrangeRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void OrangeRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void OrangeRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(OrangeBrush, 3);
        }

        // Green
        void GreenRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void GreenRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void GreenRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(GreenBrush, 4);
        }

        // Red
        void RedRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void RedRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void RedRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(RedBrush, 5);
        }

        // Teal
        void TealRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void TealRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void TealRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(TealBrush, 6);
        }

        // Aqua
        void AquaRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void AquaRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void AquaRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(AquaBrush, 7);
        }

        // Beige
        void BeigeRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void BeigeRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void BeigeRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(BeigeBrush, 8);
        }

        // Purple
        void PurpleRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void PurpleRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void PurpleRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(PurpleBrush, 9);
        }

        // Cyan
        void CyanRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void CyanRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void CyanRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CyanBrush, 10);
        }

        // Magenta
        void MagentaRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void MagentaRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void MagentaRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(MagentaBrush, 11);
        }

        // Grey
        void GreyRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void GreyRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void GreyRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(GreyBrush, 12);
        }

        // Loop
        void LoopRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                LoopBrush,
                false
            );
        }

        void LoopRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                LoopBrush,
                false
            );
        }

        void LoopRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, LoopBrush, false);
        }

        // BorderColor
        void BorderRadio_MouseLeave(object sender, MouseEventArgs e)
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

        void BorderRadio_MouseEnter(object sender, MouseEventArgs e)
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

        void BorderRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, BorderBrushColor, false);
        }

        #endregion

        #region Set ColorTheme

        private static void Blue(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 1;
            Utilities.UpdateColor();
        }

        private static void Pink(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 2;
            Utilities.UpdateColor();
        }

        private static void Orange(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 3;
            Utilities.UpdateColor();
        }

        private static void Green(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 4;
            Utilities.UpdateColor();
        }

        private static void Red(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 5;
            Utilities.UpdateColor();
        }

        private static void Teal(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 6;
            Utilities.UpdateColor();
        }

        private static void Aqua(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 7;
            Utilities.UpdateColor();
        }

        private static void Beige(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 8;
            Utilities.UpdateColor();
        }

        private static void Purple(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 9;
            Utilities.UpdateColor();
        }

        private static void Cyan(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 10;
            Utilities.UpdateColor();
        }

        private static void Magenta(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 11;
            Utilities.UpdateColor();
        }

        private static void Grey(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 12;
            Utilities.UpdateColor();
        }

        #endregion

        #region Mouseover Events

        // Fill Button
        void Fill_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                FillBrush,
                false
            );
        }

        void Fill_MouseEnter(object sender, MouseEventArgs e)
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

        void Fill_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FillBrush, false);
        }

        // Tile Button
        void Tile_MouseLeave(object sender, MouseEventArgs e)
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

        void Tile_MouseEnter(object sender, MouseEventArgs e)
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

        void Tile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(TileBrush, false);
        }

        // Center Button
        void Center_MouseLeave(object sender, MouseEventArgs e)
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

        void Center_MouseEnter(object sender, MouseEventArgs e)
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

        void Center_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CenterBrush, false);
        }

        // Fitbutton Mouse Event
        void Fit_MouseLeave(object sender, MouseEventArgs e)
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

        void Fit_MouseEnter(object sender, MouseEventArgs e)
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

        void Fit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FitBrush, false);
        }

        // Stretch Button
        void Stretch_MouseLeave(object sender, MouseEventArgs e)
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

        void Stretch_MouseEnter(object sender, MouseEventArgs e)
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

        void Stretch_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(StretchBrush, false);
        }

        // SetFitbutton Mouse Event
        void SetFit_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SetFitBrush,
                false
            );
        }

        void SetFit_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SetFitBrush,
                false
            );
        }

        void SetFit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SetFitBrush, false);
        }

        // SetCenter Button
        void SetCenter_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SetCenterBrush,
                false
            );
        }

        void SetCenter_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                SetCenterBrush,
                false
            );
        }

        void SetCenter_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SetCenterBrush, false);
        }

        // ToggleScroll
        void ToggleScroll_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                ToggleScrollBrush,
                false
            );
        }

        void ToggleScroll_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseOverColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                ToggleScrollBrush,
                false
            );
        }

        void ToggleScroll_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ToggleScrollBrush, false);
        }

        #endregion

    }
}

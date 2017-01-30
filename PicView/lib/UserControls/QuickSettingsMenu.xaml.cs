using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.lib.Helper;

namespace PicView.lib.UserControls
{
    /// <summary>
    /// Interaction logic for QuickSettingsMenu.xaml
    /// </summary>
    public partial class QuickSettingsMenu : UserControl
    {
        #region Window Logic

        #region Constructor

        public QuickSettingsMenu()
        {
            InitializeComponent();

            #region Register events

            #region CloseButton

            CloseButton.MouseEnter += CloseButtonMouseOver;
            CloseButton.MouseLeave += CloseButtonMouseLeave;
            CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;

            #endregion

            #region SettingsButton

            SettingsButton.MouseEnter += SettingsButtonMouseOver;
            SettingsButton.MouseLeave += SettingsButtonMouseLeave;
            SettingsButton.PreviewMouseLeftButtonDown += SettingsButtonMouseButtonDown;

            #endregion

            #region Toggle Scroll

            ToggleScroll.PreviewMouseLeftButtonDown += ToggleScroll_PreviewMouseLeftButtonDown;
            ToggleScroll.MouseEnter += ToggleScroll_MouseEnter;
            ToggleScroll.MouseLeave += ToggleScroll_MouseLeave;

            #endregion

            #region Set Fit

            SetFit.PreviewMouseLeftButtonDown += SetFit_PreviewMouseLeftButtonDown;
            SetFit.MouseEnter += SetFit_MouseEnter;
            SetFit.MouseLeave += SetFit_MouseLeave;

            #endregion

            #region Color Theme radio buttons

            #region BlueRadio

            BlueRadio.PreviewMouseLeftButtonDown += BlueRadio_PreviewMouseLeftButtonDown;
            BlueRadio.MouseEnter += BlueRadio_MouseEnter;
            BlueRadio.MouseLeave += BlueRadio_MouseLeave;

            BlueRadio.Click += Blue;

            #endregion

            #region PinkRadio

            PinkRadio.PreviewMouseLeftButtonDown += PinkRadio_PreviewMouseLeftButtonDown;
            PinkRadio.MouseEnter += PinkRadio_MouseEnter;
            PinkRadio.MouseLeave += PinkRadio_MouseLeave;

            PinkRadio.Click += Pink;

            #endregion

            #region OrangeRadio

            OrangeRadio.PreviewMouseLeftButtonDown += OrangeRadio_PreviewMouseLeftButtonDown;
            OrangeRadio.MouseEnter += OrangeRadio_MouseEnter;
            OrangeRadio.MouseLeave += OrangeRadio_MouseLeave;

            OrangeRadio.Click += Orange;

            #endregion

            #region GreenRadio

            GreenRadio.PreviewMouseLeftButtonDown += GreenRadio_PreviewMouseLeftButtonDown;
            GreenRadio.MouseEnter += GreenRadio_MouseEnter;
            GreenRadio.MouseLeave += GreenRadio_MouseLeave;

            GreenRadio.Click += Green;

            #endregion

            #region RedRadio

            RedRadio.PreviewMouseLeftButtonDown += RedRadio_PreviewMouseLeftButtonDown;
            RedRadio.MouseEnter += RedRadio_MouseEnter;
            RedRadio.MouseLeave += RedRadio_MouseLeave;

            RedRadio.Click += Red;

            #endregion

            #region TealRadio

            TealRadio.PreviewMouseLeftButtonDown += TealRadio_PreviewMouseLeftButtonDown;
            TealRadio.MouseEnter += TealRadio_MouseEnter;
            TealRadio.MouseLeave += TealRadio_MouseLeave;

            TealRadio.Click += Teal;

            #endregion

            #region AquaRadio

            AquaRadio.PreviewMouseLeftButtonDown += AquaRadio_PreviewMouseLeftButtonDown;
            AquaRadio.MouseEnter += AquaRadio_MouseEnter;
            AquaRadio.MouseLeave += AquaRadio_MouseLeave;

            AquaRadio.Click += Aqua;

            #endregion

            #region BeigeRadio

            BeigeRadio.PreviewMouseLeftButtonDown += BeigeRadio_PreviewMouseLeftButtonDown;
            BeigeRadio.MouseEnter += BeigeRadio_MouseEnter;
            BeigeRadio.MouseLeave += BeigeRadio_MouseLeave;

            BeigeRadio.Click += Beige;

            #endregion

            #region PurpleRadio

            PurpleRadio.PreviewMouseLeftButtonDown += PurpleRadio_PreviewMouseLeftButtonDown;
            PurpleRadio.MouseEnter += PurpleRadio_MouseEnter;
            PurpleRadio.MouseLeave += PurpleRadio_MouseLeave;

            PurpleRadio.Click += Purple;

            #endregion

            #region CyanRadio

            CyanRadio.PreviewMouseLeftButtonDown += CyanRadio_PreviewMouseLeftButtonDown;
            CyanRadio.MouseEnter += CyanRadio_MouseEnter;
            CyanRadio.MouseLeave += CyanRadio_MouseLeave;

            CyanRadio.Click += Cyan;

            #endregion

            #region MagentaRadio

            MagentaRadio.PreviewMouseLeftButtonDown += MagentaRadio_PreviewMouseLeftButtonDown;
            MagentaRadio.MouseEnter += MagentaRadio_MouseEnter;
            MagentaRadio.MouseLeave += MagentaRadio_MouseLeave;

            MagentaRadio.Click += Magenta;

            #endregion

            #region YellowRadio

            YellowRadio.PreviewMouseLeftButtonDown += YellowRadio_PreviewMouseLeftButtonDown;
            YellowRadio.MouseEnter += YellowRadio_MouseEnter;
            YellowRadio.MouseLeave += YellowRadio_MouseLeave;

            YellowRadio.Click += Yellow;

            #endregion

            #endregion

            #region Wallpaper buttons

            #region Fill

            Fill.PreviewMouseLeftButtonDown += Fill_PreviewMouseLeftButtonDown;
            Fill.MouseEnter += Fill_MouseEnter;
            Fill.MouseLeave += Fill_MouseLeave;

            Fill.Click += (s,x) => SetWallpaper(PicPath, WallpaperStyle.Fill);

            #endregion

            #region Fit

            Fit.PreviewMouseLeftButtonDown += Fit_PreviewMouseLeftButtonDown;
            Fit.MouseEnter += Fit_MouseEnter;
            Fit.MouseLeave += Fit_MouseLeave;

            Fit.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Fit);

            #endregion

            #region Center

            Center.PreviewMouseLeftButtonDown += Center_PreviewMouseLeftButtonDown;
            Center.MouseEnter += Center_MouseEnter;
            Center.MouseLeave += Center_MouseLeave;

            Center.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Center);

            #endregion

            #region Tile

            Tile.PreviewMouseLeftButtonDown += Tile_PreviewMouseLeftButtonDown;
            Tile.MouseEnter += Tile_MouseEnter;
            Tile.MouseLeave += Tile_MouseLeave;

            Tile.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Tile);

            #endregion

            #region Stretch

            Stretch.PreviewMouseLeftButtonDown += Stretch_PreviewMouseLeftButtonDown;
            Stretch.MouseEnter += Stretch_MouseEnter;
            Stretch.MouseLeave += Stretch_MouseLeave;

            Stretch.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Stretch);

            #endregion

            #endregion

            #endregion

            Loaded += (s, x) =>
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
                        YellowRadio.IsChecked = true;
                        break;
                }
            };

        }

        #endregion

        #region Add mouseover events

        #region Close Button

        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(250, 17, 17, 17, CloseButtonBrush, false);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, false);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(250, 17, 17, 17, CloseButtonBrush, false);
        }

        #endregion

        #region Settings Button

        private void SettingsButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(250, 17, 17, 17, SettingsButtonBrush, false);
        }

        private void SettingsButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SettingsButtonBrush, false);
        }

        private void SettingsButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(250, 17, 17, 17, SettingsButtonBrush, false);
        }

        #endregion

        #region Fillbutton Mouse Event
        void Fill_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, FillBrush, false);
        }

        void Fill_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, FillBrush, false);
        }

        void Fill_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FillBrush, false);
        }

        #endregion

        #region Tilebutton Mouse Event
        void Tile_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, TileBrush, false);
        }

        void Tile_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, TileBrush, false);
        }

        void Tile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(TileBrush, false);
        }

        #endregion

        #region Centerbutton Mouse Event
        void Center_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, CenterBrush, false);
        }

        void Center_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, CenterBrush, false);
        }

        void Center_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CenterBrush, false);
        }

        #endregion

        #region Fitbutton Mouse Event
        void Fit_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, FitBrush, false);
        }

        void Fit_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, FitBrush, false);
        }

        void Fit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(FitBrush, false);
        }

        #endregion

        #region Stretchutton Mouse Event
        void Stretch_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, StretchBrush, false);
        }

        void Stretch_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, StretchBrush, false);
        }

        void Stretch_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(StretchBrush, false);
        }

        #endregion

        #region SetFitbutton Mouse Event
        void SetFit_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, SetFitBrush, false);
        }

        void SetFit_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, SetFitBrush, false);
        }

        void SetFit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SetFitBrush, false);
        }

        #endregion

        #region ToggleScroll Mouse Event
        void ToggleScroll_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, ToggleScrollBrush, false);
        }

        void ToggleScroll_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, ToggleScrollBrush, false);
        }

        void ToggleScroll_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ToggleScrollBrush, false);
        }

        #endregion

        #region ColorTheme Mouse Events

        #region Blue

        void BlueRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, BlueBrush, 1);
        }

        void BlueRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, BlueBrush, 1);
        }

        void BlueRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(BlueBrush, 1);
        }

        #endregion

        #region Pink

        void PinkRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, PinkBrush, 2);
        }

        void PinkRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, PinkBrush, 2);
        }

        void PinkRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(PinkBrush, 2);
        }

        #endregion

        #region Orange

        void OrangeRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, OrangeBrush, 3);
        }

        void OrangeRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, OrangeBrush, 3);
        }

        void OrangeRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(OrangeBrush, 3);
        }

        #endregion

        #region Green

        void GreenRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, GreenBrush, 4);
        }

        void GreenRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, GreenBrush, 4);
        }

        void GreenRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(GreenBrush, 4);
        }

        #endregion

        #region Red

        void RedRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, RedBrush, 5);
        }

        void RedRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, RedBrush, 5);
        }

        void RedRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(RedBrush, 5);
        }

        #endregion

        #region Teal

        void TealRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, TealBrush, 6);
        }

        void TealRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, TealBrush, 6);
        }

        void TealRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(TealBrush, 6);
        }

        #endregion

        #region Aqua

        void AquaRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, AquaBrush, 7);
        }

        void AquaRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, AquaBrush, 7);
        }

        void AquaRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(AquaBrush, 7);
        }

        #endregion

        #region Beige

        void BeigeRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, BeigeBrush, 8);
        }

        void BeigeRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, BeigeBrush, 8);
        }

        void BeigeRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(BeigeBrush, 8);
        }

        #endregion

        #region Purple

        void PurpleRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, PurpleBrush, 9);
        }

        void PurpleRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, PurpleBrush, 9);
        }

        void PurpleRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(PurpleBrush, 9);
        }

        #endregion

        #region Cyan

        void CyanRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, CyanBrush, 10);
        }

        void CyanRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, CyanBrush, 10);
        }

        void CyanRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CyanBrush, 10);
        }

        #endregion

        #region Magenta

        void MagentaRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, MagentaBrush, 11);
        }

        void MagentaRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, MagentaBrush, 11);
        }

        void MagentaRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(MagentaBrush, 11);
        }

        #endregion

        #region Yellow

        void YellowRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, YellowBrush, 12);
        }

        void YellowRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, YellowBrush, 12);
        }

        void YellowRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(YellowBrush, 12);
        }

        #endregion

        #endregion

        #endregion

        #endregion

        #region Set ColorTheme

        private static void Blue(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 1;
        }

        private static void Pink(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 2;
        }

        private static void Orange(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 3;
        }

        private static void Green(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 4;
        }

        private static void Red(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 5;
        }

        private static void Teal(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 6;
        }

        private static void Aqua(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 7;
        }

        private static void Beige(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 8;
        }

        private static void Purple(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 9;
        }

        private static void Cyan(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 10;
        }

        private static void Magenta(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 11;
        }

        private static void Yellow(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 12;
        }

        #endregion

    }
}

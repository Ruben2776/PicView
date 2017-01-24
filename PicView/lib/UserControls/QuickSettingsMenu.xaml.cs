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

            #region Set fit

            SetFit.PreviewMouseLeftButtonDown += SetFit_PreviewMouseLeftButtonDown;
            SetFit.MouseEnter += SetFit_MouseEnter;
            SetFit.MouseLeave += SetFit_MouseLeave;

            #endregion

            #region Color Theme radio buttons

            #region blue radio

            blueradio.PreviewMouseLeftButtonDown += blueradio_PreviewMouseLeftButtonDown;
            blueradio.MouseEnter += blueradio_MouseEnter;
            blueradio.MouseLeave += blueradio_MouseLeave;

            blueradio.Click += Blue;

            #endregion

            #region pink radio

            pinkradio.PreviewMouseLeftButtonDown += pinkradio_PreviewMouseLeftButtonDown;
            pinkradio.MouseEnter += pinkradio_MouseEnter;
            pinkradio.MouseLeave += pinkradio_MouseLeave;

            pinkradio.Click += Pink;

            #endregion

            #region orange radio

            orangeradio.PreviewMouseLeftButtonDown += orangeradio_PreviewMouseLeftButtonDown;
            orangeradio.MouseEnter += orangeradio_MouseEnter;
            orangeradio.MouseLeave += orangeradio_MouseLeave;

            orangeradio.Click += Orange;

            #endregion

            #region green radio

            greenradio.PreviewMouseLeftButtonDown += greenradio_PreviewMouseLeftButtonDown;
            greenradio.MouseEnter += greenradio_MouseEnter;
            greenradio.MouseLeave += greenradio_MouseLeave;

            greenradio.Click += Green;

            #endregion

            #region red radio

            redradio.PreviewMouseLeftButtonDown += redradio_PreviewMouseLeftButtonDown;
            redradio.MouseEnter += redradio_MouseEnter;
            redradio.MouseLeave += redradio_MouseLeave;

            redradio.Click += Red;

            #endregion

            #region teal radio

            tealradio.PreviewMouseLeftButtonDown += tealradio_PreviewMouseLeftButtonDown;
            tealradio.MouseEnter += tealradio_MouseEnter;
            tealradio.MouseLeave += tealradio_MouseLeave;

            tealradio.Click += Teal;

            #endregion

            #region aqua radio

            aquaradio.PreviewMouseLeftButtonDown += aquaradio_PreviewMouseLeftButtonDown;
            aquaradio.MouseEnter += aquaradio_MouseEnter;
            aquaradio.MouseLeave += aquaradio_MouseLeave;

            aquaradio.Click += Aqua;

            #endregion

            #region sandy radio

            sandyradio.PreviewMouseLeftButtonDown += sandyradio_PreviewMouseLeftButtonDown;
            sandyradio.MouseEnter += sandyradio_MouseEnter;
            sandyradio.MouseLeave += sandyradio_MouseLeave;

            sandyradio.Click += Sandy;

            #endregion

            #region purple radio

            purpleradio.PreviewMouseLeftButtonDown += purpleradio_PreviewMouseLeftButtonDown;
            purpleradio.MouseEnter += purpleradio_MouseEnter;
            purpleradio.MouseLeave += purpleradio_MouseLeave;

            purpleradio.Click += Purple;

            #endregion

            #region cyan radio

            cyanradio.PreviewMouseLeftButtonDown += cyanradio_PreviewMouseLeftButtonDown;
            cyanradio.MouseEnter += cyanradio_MouseEnter;
            cyanradio.MouseLeave += cyanradio_MouseLeave;

            cyanradio.Click += Cyan;

            #endregion

            #region magenta radio

            magentaradio.PreviewMouseLeftButtonDown += magentaradio_PreviewMouseLeftButtonDown;
            magentaradio.MouseEnter += magentaradio_MouseEnter;
            magentaradio.MouseLeave += magentaradio_MouseLeave;

            magentaradio.Click += Magenta;

            #endregion

            #region yellow radio

            yellowradio.PreviewMouseLeftButtonDown += yellowradio_PreviewMouseLeftButtonDown;
            yellowradio.MouseEnter += yellowradio_MouseEnter;
            yellowradio.MouseLeave += yellowradio_MouseLeave;

            yellowradio.Click += Yellow;

            #endregion

            #endregion

            #region Wallpaper buttons

            #region fill

            fill.PreviewMouseLeftButtonDown += fill_PreviewMouseLeftButtonDown;
            fill.MouseEnter += fill_MouseEnter;
            fill.MouseLeave += fill_MouseLeave;

            fill.Click += (s,x) => SetWallpaper(PicPath, WallpaperStyle.Fill);

            #endregion

            #region fit

            fit.PreviewMouseLeftButtonDown += fit_PreviewMouseLeftButtonDown;
            fit.MouseEnter += fit_MouseEnter;
            fit.MouseLeave += fit_MouseLeave;

            fit.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Fit);

            #endregion

            #region center

            center.PreviewMouseLeftButtonDown += center_PreviewMouseLeftButtonDown;
            center.MouseEnter += center_MouseEnter;
            center.MouseLeave += center_MouseLeave;

            center.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Center);

            #endregion

            #region tile

            tile.PreviewMouseLeftButtonDown += tile_PreviewMouseLeftButtonDown;
            tile.MouseEnter += tile_MouseEnter;
            tile.MouseLeave += tile_MouseLeave;

            tile.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Tile);

            #endregion

            #region stretch

            stretch.PreviewMouseLeftButtonDown += stretch_PreviewMouseLeftButtonDown;
            stretch.MouseEnter += stretch_MouseEnter;
            stretch.MouseLeave += stretch_MouseLeave;

            stretch.Click += (s, x) => SetWallpaper(PicPath, WallpaperStyle.Stretch);

            #endregion

            #endregion

            #endregion

            Loaded += (s, x) =>
            {
                switch (Properties.Settings.Default.ColorTheme)
                {
                    case 1:
                        blueradio.IsChecked = true;
                        break;
                    case 2:
                        pinkradio.IsChecked = true;
                        break;
                    case 3:
                        orangeradio.IsChecked = true;
                        break;
                    case 4:
                        greenradio.IsChecked = true;
                        break;
                    case 5:
                        redradio.IsChecked = true;
                        break;
                    case 6:
                        tealradio.IsChecked = true;
                        break;
                    case 7:
                        aquaradio.IsChecked = true;
                        break;
                    case 8:
                        sandyradio.IsChecked = true;
                        break;
                    case 9:
                        purpleradio.IsChecked = true;
                        break;
                    case 10:
                        cyanradio.IsChecked = true;
                        break;
                    case 11:
                        magentaradio.IsChecked = true;
                        break;
                    case 12:
                        yellowradio.IsChecked = true;
                        break;
                }
            };

        }

        #endregion

        #region Add mouseover events

        #region Close Button

        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 17, 17, 17, CloseButtonBrush, false);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, false);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 17, 17, 17, CloseButtonBrush, false);
        }

        #endregion

        #region Settings Button

        private void SettingsButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(255, 17, 17, 17, SettingsButtonBrush, false);
        }

        private void SettingsButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(SettingsButtonBrush, false);
        }

        private void SettingsButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(255, 17, 17, 17, SettingsButtonBrush, false);
        }

        #endregion

        #region fillbutton Mouse Event
        void fill_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, fillBrush, false);
        }

        void fill_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, fillBrush, false);
        }

        void fill_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(fillBrush, false);
        }

        #endregion

        #region tilebutton Mouse Event
        void tile_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, tileBrush, false);
        }

        void tile_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, tileBrush, false);
        }

        void tile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(tileBrush, false);
        }

        #endregion

        #region centerbutton Mouse Event
        void center_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, centerBrush, false);
        }

        void center_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, centerBrush, false);
        }

        void center_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(centerBrush, false);
        }

        #endregion

        #region fitbutton Mouse Event
        void fit_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, fitBrush, false);
        }

        void fit_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, fitBrush, false);
        }

        void fit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(fitBrush, false);
        }

        #endregion

        #region stretchutton Mouse Event
        void stretch_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, stretchBrush, false);
        }

        void stretch_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, stretchBrush, false);
        }

        void stretch_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(stretchBrush, false);
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

        void blueradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, blueBrush, 1);
        }

        void blueradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, blueBrush, 1);
        }

        void blueradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(blueBrush, 1);
        }

        #endregion

        #region pink

        void pinkradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, pinkBrush, 2);
        }

        void pinkradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, pinkBrush, 2);
        }

        void pinkradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(pinkBrush, 2);
        }

        #endregion

        #region orange

        void orangeradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, orangeBrush, 3);
        }

        void orangeradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, orangeBrush, 3);
        }

        void orangeradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(orangeBrush, 3);
        }

        #endregion

        #region green

        void greenradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, greenBrush, 4);
        }

        void greenradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, greenBrush, 4);
        }

        void greenradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(greenBrush, 4);
        }

        #endregion

        #region red

        void redradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, redBrush, 5);
        }

        void redradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, redBrush, 5);
        }

        void redradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(redBrush, 5);
        }

        #endregion

        #region teal

        void tealradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, tealBrush, 6);
        }

        void tealradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, tealBrush, 6);
        }

        void tealradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(tealBrush, 6);
        }

        #endregion

        #region aqua

        void aquaradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, aquaBrush, 7);
        }

        void aquaradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, aquaBrush, 7);
        }

        void aquaradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(aquaBrush, 7);
        }

        #endregion

        #region sandy

        void sandyradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, sandyBrush, 8);
        }

        void sandyradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, sandyBrush, 8);
        }

        void sandyradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(sandyBrush, 8);
        }

        #endregion

        #region purple

        void purpleradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, purpleBrush, 9);
        }

        void purpleradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, purpleBrush, 9);
        }

        void purpleradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(purpleBrush, 9);
        }

        #endregion

        #region Cyan

        void cyanradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, cyanBrush, 10);
        }

        void cyanradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, cyanBrush, 10);
        }

        void cyanradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(cyanBrush, 10);
        }

        #endregion

        #region magenta

        void magentaradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, magentaBrush, 11);
        }

        void magentaradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, magentaBrush, 11);
        }

        void magentaradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(magentaBrush, 11);
        }

        #endregion

        #region yellow

        void yellowradio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(165, 23, 23, 23, yellowBrush, 12);
        }

        void yellowradio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(165, 23, 23, 23, yellowBrush, 12);
        }

        void yellowradio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(yellowBrush, 12);
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

        private static void Sandy(object sender, RoutedEventArgs e)
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

using PicView.lib;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.lib.Variables;
using static PicView.lib.Helper;

namespace PicView.Windows
{
    /// <summary>
    /// Interaction logic for QuickSettingsSecondMenu.xaml
    /// </summary>
    public partial class AllSettings : Window
    {
        public AllSettings()
        {
            InitializeComponent();

            ContentRendered += (s, x) =>
            {
                // CloseButton
                CloseButton.MouseEnter += CloseButtonMouseOver;
                CloseButton.MouseLeave += CloseButtonMouseLeave;
                CloseButton.PreviewMouseLeftButtonDown += CloseButtonMouseButtonDown;
                CloseButton.Click += delegate { Close(); };

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

                // VioletRadio
                VioletRadio.PreviewMouseLeftButtonDown += VioletRadio_PreviewMouseLeftButtonDown;
                VioletRadio.MouseEnter += VioletRadio_MouseEnter;
                VioletRadio.MouseLeave += VioletRadio_MouseLeave;
                VioletRadio.Click += Violet;

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
                LoopRadio.Click += SetLooping;

                // PicGalleryRadio
                PicGalleryRadio.PreviewMouseLeftButtonDown += PicGalleryRadio_PreviewMouseLeftButtonDown;
                PicGalleryRadio.MouseEnter += PicGalleryRadio_MouseEnter;
                PicGalleryRadio.MouseLeave += PicGalleryRadio_MouseLeave;
                PicGalleryRadio.IsChecked = Properties.Settings.Default.PicGallery > 0;
                PicGalleryRadio.Checked += SetPicGallery;

                // BorderColorRadio
                BorderRadio.PreviewMouseLeftButtonDown += BorderRadio_PreviewMouseLeftButtonDown;
                BorderRadio.MouseEnter += BorderRadio_MouseEnter;
                BorderRadio.MouseLeave += BorderRadio_MouseLeave;
                BorderRadio.Click += SetBgColorEnabled;
                if (Properties.Settings.Default.WindowBorderColorEnabled)
                    BorderRadio.IsChecked = true;

                //Slidebar
                double value = Properties.Settings.Default.Slidetimeren;
                txtSlide.Text = value.ToString().Replace("000", string.Empty);
                SlideSlider.Value = double.Parse(value.ToString().Replace("000", string.Empty));
                SlideSlider.ValueChanged += SlideSlider_ValueChanged;

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
                        VioletRadio.IsChecked = true;
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

                KeyUp += AllSettings_KeyUp;
            };
        }

        private void AllSettings_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.Q:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Environment.Exit(0);
                    break;
                default:
                    break;
            }
        }


        #region EventHandlers

        // Close Button
        private void CloseButtonMouseOver(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
        }

        private void CloseButtonMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, false);
        }

        private void CloseButtonMouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, CloseButtonBrush, false);
        }

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

        // Violet
        void VioletRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                VioletBrush,
                6
            );
        }

        void VioletRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                VioletBrush,
                6
            );
        }

        void VioletRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.PreviewMouseLeftButtonDownColorEvent(VioletBrush, 6);
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
            AnimationHelper.MouseEnterColorEvent(
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

        // PicGallery
        void PicGalleryRadio_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                PicGalleryBrush,
                false
            );
        }

        void PicGalleryRadio_MouseEnter(object sender, MouseEventArgs e)
        {
            AnimationHelper.MouseEnterColorEvent(
                backgroundBorderColor.A,
                backgroundBorderColor.R,
                backgroundBorderColor.G,
                backgroundBorderColor.B,
                PicGalleryBrush,
                false
            );
        }

        void PicGalleryRadio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, PicGalleryBrush, false);
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
            AnimationHelper.MouseEnterColorEvent(
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

        private static void Violet(object sender, RoutedEventArgs e)
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

        private static void Grey(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorTheme = 12;
        }

        #endregion

        //Slideslider
        private void SlideSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = Properties.Settings.Default.Slidetimeren;
            var slider = sender as Slider;
            value = slider.Value;
            txtSlide.Text = value.ToString("0");
        }

        private void SetSlidetimer()
        {
            switch (Properties.Settings.Default.Slidetimeren.ToString("0"))
            {
                case "1":
                    Properties.Settings.Default.Slidetimeren = 1000;
                    break;

                case "2":
                    Properties.Settings.Default.Slidetimeren = 2000;
                    break;

                case "3":
                    Properties.Settings.Default.Slidetimeren = 3000;
                    break;

                case "4":
                    Properties.Settings.Default.Slidetimeren = 4000;
                    break;

                case "5":
                    Properties.Settings.Default.Slidetimeren = 5000;
                    break;

                case "6":
                    Properties.Settings.Default.Slidetimeren = 6000;
                    break;

                case "7":
                    Properties.Settings.Default.Slidetimeren = 7000;
                    break;

                case "8":
                    Properties.Settings.Default.Slidetimeren = 8000;
                    break;

                case "9":
                    Properties.Settings.Default.Slidetimeren = 9000;
                    break;

                case "10":
                    Properties.Settings.Default.Slidetimeren = 10000;
                    break;

                case "11":
                    Properties.Settings.Default.Slidetimeren = 11000;
                    break;

                case "12":
                    Properties.Settings.Default.Slidetimeren = 12000;
                    break;

                case "13":
                    Properties.Settings.Default.Slidetimeren = 13000;
                    break;

                case "14":
                    Properties.Settings.Default.Slidetimeren = 140000;
                    break;

                case "15":
                    Properties.Settings.Default.Slidetimeren = 15000;
                    break;
            }
        }


        private void SetLooping(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.Looping)
            {
                Properties.Settings.Default.Looping = false;
            }
            else
            {
                Properties.Settings.Default.Looping = true;
            }
        }

        private void SetPicGallery(object sender, RoutedEventArgs e)
        {
            if (PicGalleryRadio.IsChecked.Value)
            {
                Properties.Settings.Default.PicGallery = 0;
            }
            else
            {
                Properties.Settings.Default.PicGallery = 1;
            }
        }

        private void SetBgColorEnabled(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WindowBorderColorEnabled = Properties.Settings.Default.WindowBorderColorEnabled ? false : true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SetSlidetimer();
            SetWindowBorderColor();
            Closing -= Window_Closing;
            e.Cancel = true;
            AnimationHelper.FadeWindow(this, 0, TimeSpan.FromSeconds(.5));
        }

    }
}

using System;
using System.Windows;
using System.Windows.Input;
using static PicView.Fields;
using PicView.HlslEffects;

namespace PicView.Windows
{
    public partial class Effects : Window
    {

        public Effects()
        {
            InitializeComponent();

            ContentRendered += Window_ContentRendered;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            KeyDown += KeysDown;
            KeyUp += KeysUp;
            Scroller.MouseWheel += Info_MouseWheel;

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); mainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            NegativeButton.Click += Negative;
            NegativeButton.Unchecked += Remove_Effects;

            GrayscaleButton.Click += GraySceale;
            GrayscaleButton.Unchecked += Remove_Effects;

            ColorToneButton.Click += ColorToneEffect;
            ColorToneButton.Unchecked += Remove_Effects;

            OldMovieButton.Click += OldMovieEffect;
            OldMovieButton.Unchecked += Remove_Effects;

            BloomButton.Click += Bloom;
            BloomButton.Unchecked += Remove_Effects;

            GloomButton.Click += Gloom;
            GloomButton.Unchecked += Remove_Effects;

            MonochromeButton.Click += Monochrome;
            MonochromeButton.Unchecked += Remove_Effects;

            WavewarperButton.Click += WaveWarperEffect;
            WavewarperButton.Unchecked += Remove_Effects;

            UnderwaterButton.Click += UnderWaterEffect;
            UnderwaterButton.Unchecked += Remove_Effects;

            BandedSwirlButton.Click += BandedSwirlEffect;
            BandedSwirlButton.Unchecked += Remove_Effects;

            RippleButton.Click += RippleEffect1;
            RippleButton.Unchecked += Remove_Effects;

            RippleAltButton.Click += RippleEffect2;
            RippleAltButton.Unchecked += Remove_Effects;

            BlurButton.Click += Poison_blur;
            BlurButton.Unchecked += Remove_Effects;

            DirectionalBlurButton.Click += Dir_blur;
            DirectionalBlurButton.Unchecked += Remove_Effects;

            TelescopicBlurButton.Click += Teleskopisk_blur;
            TelescopicBlurButton.Unchecked += Remove_Effects;

            PixelateButton.Click += PixelateEffect;
            PixelateButton.Unchecked += Remove_Effects;

            EmbossedButton.Click += Embossed;
            EmbossedButton.Unchecked += Remove_Effects;

            SmoothMagnifyButton.Click += MagnifySmoothEffect;
            SmoothMagnifyButton.Unchecked += Remove_Effects;

            PivotButton.Click += PivotEffect;
            PivotButton.Unchecked += Remove_Effects;

            PaperfoldButton.Click += PaperFoldEffect;
            PaperfoldButton.Unchecked += Remove_Effects;

            PencilSketchButton.Click += SketchPencilStrokeEffect;
            PencilSketchButton.Unchecked += Remove_Effects;

            SketchButton.Click += Sketch;
            SketchButton.Unchecked += Remove_Effects;

            TonemappingButton.Click += ToneMapping;
            TonemappingButton.Unchecked += Remove_Effects;

            BandsButton.Click += bands;
            BandsButton.Unchecked += Remove_Effects;

            GlasTileButton.Click += GlasTileEffect;
            GlasTileButton.Unchecked += Remove_Effects;

            FrostyOutlineButton.Click += FrostyOutlineEffect;
            FrostyOutlineButton.Unchecked += Remove_Effects;
        }

        #region Keyboard Shortcuts

        private void KeysDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                case Key.PageDown:
                case Key.S:
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + zoomSpeed);
                    break;
                case Key.Up:
                case Key.PageUp:
                case Key.W:
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - zoomSpeed);
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

        private void Info_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - zoomSpeed);
            }
            else if (e.Delta < 0)
            {
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + zoomSpeed);
            }
        }

        #endregion

        #region HLSL Shader Effects

        private void Remove_Effects(object sender, RoutedEventArgs e)
        {
            mainWindow.img.Effect = null;
        }

        private void Negative(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new InvertColorEffect();
            }
            else NegativeButton.IsChecked = false;
            
        }

        private void GraySceale(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new GrayscaleEffect();
            }
            else GrayscaleButton.IsChecked = false;
        }

        private void ColorToneEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new ColorToneEffect();
            }
            else ColorToneButton.IsChecked = false;

        }

        private void RippleEffect1(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new Transition_RippleEffect();
            }
            else RippleButton.IsChecked = false;
        }

        private void RippleEffect2(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new RippleEffect();
            }
            else RippleAltButton.IsChecked = false;
        }

        private void BandedSwirlEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new BandedSwirlEffect();
            }
            else BandedSwirlButton.IsChecked = false;
        }

        private void Monochrome(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new MonochromeEffect();
            }
            else MonochromeButton.IsChecked = false;

        }

        private void Swirl(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new SwirlEffect();
            }
            else BandedSwirlButton.IsChecked = false;
        }

        private void Bloom(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new BloomEffect();
            }
            else BloomButton.IsChecked = false;
        }

        private void Gloom(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new GloomEffect();
            }
            else GloomButton.IsChecked = false;
        }

        private void ToneMapping(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new ToneMappingEffect();
            }
            else TonemappingButton.IsChecked = false;
        }

        private void Teleskopisk_blur(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new TelescopicBlurPS3Effect();
            }
            else TelescopicBlurButton.IsChecked = false;
        }

        private void Poison_blur(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new GrowablePoissonDiskEffect();
            }
            else BlurButton.IsChecked = false;
        }

        private void Dir_blur(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new DirectionalBlurEffect();
            }
            else DirectionalBlurButton.IsChecked = false;
        }

        private void bands(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new BandsEffect();
            }
            else BandsButton.IsChecked = false;
        }

        private void Embossed(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new EmbossedEffect();
            }
            else EmbossedButton.IsChecked = false;
        }

        private void GlasTileEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new GlassTilesEffect();
            }
            else GlasTileButton.IsChecked = false;
        }

        private void MagnifySmoothEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new MagnifySmoothEffect();
            }
            else SmoothMagnifyButton.IsChecked = false;

        }

        private void PaperFoldEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new PaperFoldEffect();
            }
            else PaperfoldButton.IsChecked = false;
        }

        private void PivotEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new PivotEffect();
            }
            else PivotButton.IsChecked = false;
        }

        private void UnderWaterEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new UnderwaterEffect();
            }
            else UnderwaterButton.IsChecked = false;
        }

        private void WaveWarperEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new WaveWarperEffect();
            }
            else WavewarperButton.IsChecked = false;
        }

        private void FrostyOutlineEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new FrostyOutlineEffect();
            }
            else FrostyOutlineButton.IsChecked = false;
        }

        private void OldMovieEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new OldMovieEffect();
            }
            else OldMovieButton.IsChecked = false;
        }

        private void PixelateEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new PixelateEffect();
            }
            else PixelateButton.IsChecked = false;
        }

        private void Sketch(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new SketchGraniteEffect();
            }
            else SketchButton.IsChecked = false;
        }

        private void SketchPencilStrokeEffect(object sender, RoutedEventArgs e)
        {
            if (mainWindow.img.Effect == null)
            {
                mainWindow.img.Effect = new SketchPencilStrokeEffect();
            }
            else PencilSketchButton.IsChecked = false;
        }

        #endregion


    }
}

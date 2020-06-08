using System;
using System.Windows;
using System.Windows.Input;
using static PicView.Fields;
using static PicView.MouseOverAnimations;
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

            NegativeButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(NegativeBrush);
            NegativeButton.MouseEnter += (s, x) => ButtonMouseOverAnim(NegativeBrush, true);
            NegativeButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(NegativeBrush, false);

            GrayscaleButton.Click += GraySceale;
            GrayscaleButton.Unchecked += Remove_Effects;

            GrayscaleButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(GrayscaleBrush);
            GrayscaleButton.MouseEnter += (s, x) => ButtonMouseOverAnim(GrayscaleBrush, true);
            GrayscaleButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(GrayscaleBrush, false);

            ColorToneButton.Click += ColorToneEffect;
            ColorToneButton.Unchecked += Remove_Effects;

            ColorToneButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ColortoneBrush);
            ColorToneButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ColortoneBrush, true);
            ColorToneButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ColortoneBrush, false);

            OldMovieButton.Click += OldMovieEffect;
            OldMovieButton.Unchecked += Remove_Effects;

            OldMovieButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(OldMovieBrush);
            OldMovieButton.MouseEnter += (s, x) => ButtonMouseOverAnim(OldMovieBrush, true);
            OldMovieButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(OldMovieBrush, false);

            BloomButton.Click += Bloom;
            BloomButton.Unchecked += Remove_Effects;

            BloomButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(BloomBrush);
            BloomButton.MouseEnter += (s, x) => ButtonMouseOverAnim(BloomBrush, true);
            BloomButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(BloomBrush, false);

            GloomButton.Click += Gloom;
            GloomButton.Unchecked += Remove_Effects;

            GloomButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(GloomBrush);
            GloomButton.MouseEnter += (s, x) => ButtonMouseOverAnim(GloomBrush, true);
            GloomButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(GloomBrush, false);

            MonochromeButton.Click += Monochrome;
            MonochromeButton.Unchecked += Remove_Effects;

            MonochromeButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(MonocromeBrush);
            MonochromeButton.MouseEnter += (s, x) => ButtonMouseOverAnim(MonocromeBrush, true);
            MonochromeButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(MonocromeBrush, false);

            WavewarperButton.Click += WaveWarperEffect;
            WavewarperButton.Unchecked += Remove_Effects;

            WavewarperButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(WavewarperBrush);
            WavewarperButton.MouseEnter += (s, x) => ButtonMouseOverAnim(WavewarperBrush, true);
            WavewarperButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(WavewarperBrush, false);

            UnderwaterButton.Click += UnderWaterEffect;
            UnderwaterButton.Unchecked += Remove_Effects;

            UnderwaterButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(UnderwaterBrush);
            UnderwaterButton.MouseEnter += (s, x) => ButtonMouseOverAnim(UnderwaterBrush, true);
            UnderwaterButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(UnderwaterBrush, false);

            BandedSwirlButton.Click += BandedSwirlEffect;
            BandedSwirlButton.Unchecked += Remove_Effects;

            BandedSwirlButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(BandedSwirlBrush);
            BandedSwirlButton.MouseEnter += (s, x) => ButtonMouseOverAnim(BandedSwirlBrush, true);
            BandedSwirlButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(BandedSwirlBrush, false);

            RippleButton.Click += RippleEffect1;
            RippleButton.Unchecked += Remove_Effects;

            RippleButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(RippleBrush);
            RippleButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RippleBrush, true);
            RippleButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(RippleBrush, false);

            RippleAltButton.Click += RippleEffect2;
            RippleAltButton.Unchecked += Remove_Effects;

            RippleAltButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(RippleAltBrush);
            RippleAltButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RippleAltBrush, true);
            RippleAltButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(RippleAltBrush, false);

            BlurButton.Click += Poison_blur;
            BlurButton.Unchecked += Remove_Effects;

            BlurButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(BlurBrush);
            BlurButton.MouseEnter += (s, x) => ButtonMouseOverAnim(BlurBrush, true);
            BlurButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(BlurBrush, false);

            DirectionalBlurButton.Click += Dir_blur;
            DirectionalBlurButton.Unchecked += Remove_Effects;

            DirectionalBlurButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(DirectionalBlurBrush);
            DirectionalBlurButton.MouseEnter += (s, x) => ButtonMouseOverAnim(DirectionalBlurBrush, true);
            DirectionalBlurButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(DirectionalBlurBrush, false);

            TelescopicBlurButton.Click += Teleskopisk_blur;
            TelescopicBlurButton.Unchecked += Remove_Effects;

            TelescopicBlurButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(TelescopicBlurBrush);
            TelescopicBlurButton.MouseEnter += (s, x) => ButtonMouseOverAnim(TelescopicBlurBrush, true);
            TelescopicBlurButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(TelescopicBlurBrush, false);

            PixelateButton.Click += PixelateEffect;
            PixelateButton.Unchecked += Remove_Effects;

            PixelateButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(PixelateBrush);
            PixelateButton.MouseEnter += (s, x) => ButtonMouseOverAnim(PixelateBrush, true);
            PixelateButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(PixelateBrush, false);

            EmbossedButton.Click += Embossed;
            EmbossedButton.Unchecked += Remove_Effects;

            EmbossedButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(EmbossedBrush);
            EmbossedButton.MouseEnter += (s, x) => ButtonMouseOverAnim(EmbossedBrush, true);
            EmbossedButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(EmbossedBrush, false);

            SmoothMagnifyButton.Click += MagnifySmoothEffect;
            SmoothMagnifyButton.Unchecked += Remove_Effects;

            SmoothMagnifyButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SmoothMagnifyBrush);
            SmoothMagnifyButton.MouseEnter += (s, x) => ButtonMouseOverAnim(SmoothMagnifyBrush, true);
            SmoothMagnifyButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SmoothMagnifyBrush, false);

            PivotButton.Click += PivotEffect;
            PivotButton.Unchecked += Remove_Effects;

            PivotButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(PivotBrush);
            PivotButton.MouseEnter += (s, x) => ButtonMouseOverAnim(PivotBrush, true);
            PivotButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(PivotBrush, false);

            PaperfoldButton.Click += PaperFoldEffect;
            PaperfoldButton.Unchecked += Remove_Effects;

            PaperfoldButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(PaperfoldBrush);
            PaperfoldButton.MouseEnter += (s, x) => ButtonMouseOverAnim(PaperfoldBrush, true);
            PaperfoldButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(PaperfoldBrush, false);

            PencilSketchButton.Click += SketchPencilStrokeEffect;
            PencilSketchButton.Unchecked += Remove_Effects;

            PencilSketchButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(PencilSketchBrush);
            PencilSketchButton.MouseEnter += (s, x) => ButtonMouseOverAnim(PencilSketchBrush, true);
            PencilSketchButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(PencilSketchBrush, false);

            SketchButton.Click += Sketch;
            SketchButton.Unchecked += Remove_Effects;

            SketchButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SketchBrush);
            SketchButton.MouseEnter += (s, x) => ButtonMouseOverAnim(SketchBrush, true);
            SketchButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SketchBrush, false);

            TonemappingButton.Click += ToneMapping;
            TonemappingButton.Unchecked += Remove_Effects;

            TonemappingButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(TonemappingBrush);
            TonemappingButton.MouseEnter += (s, x) => ButtonMouseOverAnim(TonemappingBrush, true);
            TonemappingButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(TonemappingBrush, false);

            BandsButton.Click += bands;
            BandsButton.Unchecked += Remove_Effects;

            BandsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(BandsBrush);
            BandsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(BandsBrush, true);
            BandsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(BandsBrush, false);

            GlasTileButton.Click += GlasTileEffect;
            GlasTileButton.Unchecked += Remove_Effects;

            GlasTileButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(GlasTileBrush);
            GlasTileButton.MouseEnter += (s, x) => ButtonMouseOverAnim(GlasTileBrush, true);
            GlasTileButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(GlasTileBrush, false);

            FrostyOutlineButton.Click += FrostyOutlineEffect;
            FrostyOutlineButton.Unchecked += Remove_Effects;

            FrostyOutlineButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(FrostyOutlineBrush);
            FrostyOutlineButton.MouseEnter += (s, x) => ButtonMouseOverAnim(FrostyOutlineBrush, true);
            FrostyOutlineButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(FrostyOutlineBrush, false);
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

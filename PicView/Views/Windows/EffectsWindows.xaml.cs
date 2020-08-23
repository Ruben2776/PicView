using PicView.Editing.HlslEffects;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.UILogic.Animations;
using PicView.UILogic.Loading;
using PicView.UILogic.Sizing;
using PicView.UILogic.TransformImage;
using System;
using System.Windows;
using System.Windows.Input;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.Windows
{
    public partial class EffectsWindow : Window
    {
        public EffectsWindow()
        {
            InitializeComponent();

            ContentRendered += Window_ContentRendered;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // Center vertically
            Top = ((WindowLogic.MonitorInfo.WorkArea.Height * WindowLogic.MonitorInfo.DpiScaling) - ActualHeight) / 2 + WindowLogic.MonitorInfo.WorkArea.Top;

            KeyDown += KeysDown;
            KeyUp += KeysUp;
            Scroller.MouseWheel += Info_MouseWheel;

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); LoadWindows.GetMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            NegativeButton.Checked += Negative;
            NegativeButton.Unchecked += Remove_Effects;

            NegativeButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(NegativeColorsText); };
            NegativeButton.MouseEnter += delegate { ButtonMouseOverAnim(NegativeColorsText); };
            NegativeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(NegativeColorsText); };

            GrayscaleButton.Checked += GraySceale;
            GrayscaleButton.Unchecked += Remove_Effects;

            GrayscaleButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BlackAndWhiteText); };
            GrayscaleButton.MouseEnter += delegate { ButtonMouseOverAnim(BlackAndWhiteText); };
            GrayscaleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BlackAndWhiteText); };

            ColorToneButton.Checked += ColorToneEffect;
            ColorToneButton.Unchecked += Remove_Effects;

            ColorToneButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ColorToneText); };
            ColorToneButton.MouseEnter += delegate { ButtonMouseOverAnim(ColorToneText); };
            ColorToneButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ColorToneText); };

            OldMovieButton.Checked += OldMovieEffect;
            OldMovieButton.Unchecked += Remove_Effects;

            OldMovieButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(OldMovieText); };
            OldMovieButton.MouseEnter += delegate { ButtonMouseOverAnim(OldMovieText); };
            OldMovieButton.MouseLeave += delegate { ButtonMouseLeaveAnim(OldMovieText); };

            BloomButton.Checked += Bloom;
            BloomButton.Unchecked += Remove_Effects;

            BloomButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BloomText); };
            BloomButton.MouseEnter += delegate { ButtonMouseOverAnim(BloomText); };
            BloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BloomText); };

            GloomButton.Checked += Gloom;
            GloomButton.Unchecked += Remove_Effects;

            GloomButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GloomText); };
            GloomButton.MouseEnter += delegate { ButtonMouseOverAnim(GloomText); };
            GloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GloomText); };

            MonochromeButton.Checked += Monochrome;
            MonochromeButton.Unchecked += Remove_Effects;

            MonochromeButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(MonochromeText); };
            MonochromeButton.MouseEnter += delegate { ButtonMouseOverAnim(MonochromeText); };
            MonochromeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(MonochromeText); };

            WavewarperButton.Checked += WaveWarperEffect;
            WavewarperButton.Unchecked += Remove_Effects;

            WavewarperButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(WaveWarperText); };
            WavewarperButton.MouseEnter += delegate { ButtonMouseOverAnim(WaveWarperText); };
            WavewarperButton.MouseLeave += delegate { ButtonMouseLeaveAnim(WaveWarperText); };

            UnderwaterButton.Checked += UnderWaterEffect;
            UnderwaterButton.Unchecked += Remove_Effects;

            UnderwaterButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(UnderwaterText); };
            UnderwaterButton.MouseEnter += delegate { ButtonMouseOverAnim(UnderwaterText); };
            UnderwaterButton.MouseLeave += delegate { ButtonMouseLeaveAnim(UnderwaterText); };

            BandedSwirlButton.Checked += BandedSwirlEffect;
            BandedSwirlButton.Unchecked += Remove_Effects;

            BandedSwirlButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BandedSwirlText); };
            BandedSwirlButton.MouseEnter += delegate { ButtonMouseOverAnim(BandedSwirlText); };
            BandedSwirlButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandedSwirlText); };

            RippleButton.Checked += RippleEffect1;
            RippleButton.Unchecked += Remove_Effects;

            RippleButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RippleText); };
            RippleButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleText); };
            RippleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleText); };

            RippleAltButton.Checked += RippleEffect2;
            RippleAltButton.Unchecked += Remove_Effects;

            RippleAltButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RippleAltText); };
            RippleAltButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleAltText); };
            RippleAltButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleAltText); };

            BlurButton.Checked += Poison_blur;
            BlurButton.Unchecked += Remove_Effects;

            BlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BlurText); };
            BlurButton.MouseEnter += delegate { ButtonMouseOverAnim(BlurText); };
            BlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BlurText); };

            DirectionalBlurButton.Checked += Dir_blur;
            DirectionalBlurButton.Unchecked += Remove_Effects;

            DirectionalBlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(DirectionalBlurText); };
            DirectionalBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(DirectionalBlurText); };
            DirectionalBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(DirectionalBlurText); };

            TelescopicBlurButton.Checked += Teleskopisk_blur;
            TelescopicBlurButton.Unchecked += Remove_Effects;

            TelescopicBlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(TelescopicBlurText); };
            TelescopicBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(TelescopicBlurText); };
            TelescopicBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(TelescopicBlurText); };

            PixelateButton.Checked += PixelateEffect;
            PixelateButton.Unchecked += Remove_Effects;

            PixelateButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PixelateText); };
            PixelateButton.MouseEnter += delegate { ButtonMouseOverAnim(PixelateText); };
            PixelateButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PixelateText); };

            EmbossedButton.Checked += Embossed;
            EmbossedButton.Unchecked += Remove_Effects;

            EmbossedButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(EmbossedText); };
            EmbossedButton.MouseEnter += delegate { ButtonMouseOverAnim(EmbossedText); };
            EmbossedButton.MouseLeave += delegate { ButtonMouseLeaveAnim(EmbossedText); };

            SmoothMagnifyButton.Checked += MagnifySmoothEffect;
            SmoothMagnifyButton.Unchecked += Remove_Effects;

            SmoothMagnifyButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SmoothMagnifyText); };
            SmoothMagnifyButton.MouseEnter += delegate { ButtonMouseOverAnim(SmoothMagnifyText); };
            SmoothMagnifyButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SmoothMagnifyText); };

            PivotButton.Checked += PivotEffect;
            PivotButton.Unchecked += Remove_Effects;

            PivotButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PivotText); };
            PivotButton.MouseEnter += delegate { ButtonMouseOverAnim(PivotText); };
            PivotButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PivotText); };

            PaperfoldButton.Checked += PaperFoldEffect;
            PaperfoldButton.Unchecked += Remove_Effects;

            PaperfoldButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PaperFoldText); };
            PaperfoldButton.MouseEnter += delegate { ButtonMouseOverAnim(PaperFoldText); };
            PaperfoldButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PaperFoldText); };

            PencilSketchButton.Checked += SketchPencilStrokeEffect;
            PencilSketchButton.Unchecked += Remove_Effects;

            PencilSketchButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PencilSketchText); };
            PencilSketchButton.MouseEnter += delegate { ButtonMouseOverAnim(PencilSketchText); };
            PencilSketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PencilSketchText); };

            SketchButton.Checked += Sketch;
            SketchButton.Unchecked += Remove_Effects;

            SketchButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SketchText); };
            SketchButton.MouseEnter += delegate { ButtonMouseOverAnim(SketchText); };
            SketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SketchText); };

            TonemappingButton.Checked += ToneMapping;
            TonemappingButton.Unchecked += Remove_Effects;

            TonemappingButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ToneMappingText); };
            TonemappingButton.MouseEnter += delegate { ButtonMouseOverAnim(ToneMappingText); };
            TonemappingButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ToneMappingText); };

            BandsButton.Checked += bands;
            BandsButton.Unchecked += Remove_Effects;

            BandsButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BandsText); };
            BandsButton.MouseEnter += delegate { ButtonMouseOverAnim(BandsText); };
            BandsButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandsText); };

            GlasTileButton.Checked += GlasTileEffect;
            GlasTileButton.Unchecked += Remove_Effects;

            GlasTileButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GlassTileText); };
            GlasTileButton.MouseEnter += delegate { ButtonMouseOverAnim(GlassTileText); };
            GlasTileButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GlassTileText); };

            FrostyOutlineButton.Checked += FrostyOutlineEffect;
            FrostyOutlineButton.Unchecked += Remove_Effects;

            FrostyOutlineButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FrostyOutlineText); };
            FrostyOutlineButton.MouseEnter += delegate { ButtonMouseOverAnim(FrostyOutlineText); };
            FrostyOutlineButton.MouseLeave += delegate { ButtonMouseLeaveAnim(FrostyOutlineText); };

            // SaveButton
            SaveButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SaveText); };
            SaveButton.MouseEnter += delegate { ButtonMouseOverAnim(SaveText); };
            SaveButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SaveText); };
            SaveButton.Click += (_, _) => Open_Save.SaveFiles();
        }

        #region Keyboard Shortcuts

        private void KeysDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                case Key.PageDown:
                case Key.S:
                    Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 10);
                    break;

                case Key.Up:
                case Key.PageUp:
                case Key.W:
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
                    LoadWindows.GetMainWindow.Focus();
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
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - 10);
            }
            else if (e.Delta < 0)
            {
                Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset + 10);
            }
        }

        #endregion Keyboard Shortcuts

        #region HLSL Shader Effects

        private void Remove_Effects(object sender, RoutedEventArgs e)
        {
            LoadWindows.GetMainWindow.MainImage.Effect = null;
        }

        private void Negative(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new InvertColorEffect();
            }
            else
            {
                NegativeButton.IsChecked = false;
            }
        }

        private void GraySceale(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new GrayscaleEffect();
            }
            else
            {
                GrayscaleButton.IsChecked = false;
            }
        }

        private void ColorToneEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new ColorToneEffect();
            }
            else
            {
                ColorToneButton.IsChecked = false;
            }
        }

        private void RippleEffect1(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new Transition_RippleEffect();
            }
            else
            {
                RippleButton.IsChecked = false;
            }
        }

        private void RippleEffect2(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new RippleEffect();
            }
            else
            {
                RippleAltButton.IsChecked = false;
            }
        }

        private void BandedSwirlEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new BandedSwirlEffect();
            }
            else
            {
                BandedSwirlButton.IsChecked = false;
            }
        }

        private void Monochrome(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new MonochromeEffect();
            }
            else
            {
                MonochromeButton.IsChecked = false;
            }
        }

        private void Swirl(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new SwirlEffect();
            }
            else
            {
                BandedSwirlButton.IsChecked = false;
            }
        }

        private void Bloom(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new BloomEffect();
            }
            else
            {
                BloomButton.IsChecked = false;
            }
        }

        private void Gloom(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new GloomEffect();
            }
            else
            {
                GloomButton.IsChecked = false;
            }
        }

        private void ToneMapping(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new ToneMappingEffect();
            }
            else
            {
                TonemappingButton.IsChecked = false;
            }
        }

        private void Teleskopisk_blur(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new TelescopicBlurPS3Effect();
            }
            else
            {
                TelescopicBlurButton.IsChecked = false;
            }
        }

        private void Poison_blur(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new GrowablePoissonDiskEffect();
            }
            else
            {
                BlurButton.IsChecked = false;
            }
        }

        private void Dir_blur(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new DirectionalBlurEffect();
            }
            else
            {
                DirectionalBlurButton.IsChecked = false;
            }
        }

        private void bands(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new BandsEffect();
            }
            else
            {
                BandsButton.IsChecked = false;
            }
        }

        private void Embossed(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new EmbossedEffect();
            }
            else
            {
                EmbossedButton.IsChecked = false;
            }
        }

        private void GlasTileEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new GlassTilesEffect();
            }
            else
            {
                GlasTileButton.IsChecked = false;
            }
        }

        private void MagnifySmoothEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new MagnifySmoothEffect();
            }
            else
            {
                SmoothMagnifyButton.IsChecked = false;
            }
        }

        private void PaperFoldEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new PaperFoldEffect();
            }
            else
            {
                PaperfoldButton.IsChecked = false;
            }
        }

        private void PivotEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new PivotEffect();
            }
            else
            {
                PivotButton.IsChecked = false;
            }
        }

        private void UnderWaterEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new UnderwaterEffect();
            }
            else
            {
                UnderwaterButton.IsChecked = false;
            }
        }

        private void WaveWarperEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new WaveWarperEffect();
            }
            else
            {
                WavewarperButton.IsChecked = false;
            }
        }

        private void FrostyOutlineEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new FrostyOutlineEffect();
            }
            else
            {
                FrostyOutlineButton.IsChecked = false;
            }
        }

        private void OldMovieEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new OldMovieEffect();
            }
            else
            {
                OldMovieButton.IsChecked = false;
            }
        }

        private void PixelateEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new PixelateEffect();
            }
            else
            {
                PixelateButton.IsChecked = false;
            }
        }

        private void Sketch(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new SketchGraniteEffect();
            }
            else
            {
                SketchButton.IsChecked = false;
            }
        }

        private void SketchPencilStrokeEffect(object sender, RoutedEventArgs e)
        {
            if (LoadWindows.GetMainWindow.MainImage.Effect == null)
            {
                LoadWindows.GetMainWindow.MainImage.Effect = new SketchPencilStrokeEffect();
            }
            else
            {
                PencilSketchButton.IsChecked = false;
            }
        }

        #endregion HLSL Shader Effects
    }
}
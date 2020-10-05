using PicView.Editing.HlslEffects;
using PicView.FileHandling;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.Windows
{
    public partial class EffectsWindow : Window
    {
        public EffectsWindow()
        {
            InitializeComponent();
            Title = Application.Current.Resources["HLSLPictureFX"] + " - PicView";
            MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
            Width *= WindowSizing.MonitorInfo.DpiScaling;

            ContentRendered += Window_ContentRendered;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            KeyDown += (_, e) => Shortcuts.GenericWindowShortcuts.KeysDown(null, e, this);

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); ConfigureWindows.GetMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            NegativeButton.Click += Negative;
            NegativeButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(NegativeColorsText); };
            NegativeButton.MouseEnter += delegate { ButtonMouseOverAnim(NegativeColorsText); };
            NegativeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(NegativeColorsText); };

            GrayscaleButton.Click += GraySceale;
            GrayscaleButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BlackAndWhiteText); };
            GrayscaleButton.MouseEnter += delegate { ButtonMouseOverAnim(BlackAndWhiteText); };
            GrayscaleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BlackAndWhiteText); };

            ColorToneButton.Click += ColorToneEffect;
            ColorToneButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ColorToneText); };
            ColorToneButton.MouseEnter += delegate { ButtonMouseOverAnim(ColorToneText); };
            ColorToneButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ColorToneText); };

            OldMovieButton.Click += OldMovieEffect;
            OldMovieButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(OldMovieText); };
            OldMovieButton.MouseEnter += delegate { ButtonMouseOverAnim(OldMovieText); };
            OldMovieButton.MouseLeave += delegate { ButtonMouseLeaveAnim(OldMovieText); };

            BloomButton.Click += Bloom;
            BloomButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BloomText); };
            BloomButton.MouseEnter += delegate { ButtonMouseOverAnim(BloomText); };
            BloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BloomText); };

            GloomButton.Click += Gloom;
            GloomButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GloomText); };
            GloomButton.MouseEnter += delegate { ButtonMouseOverAnim(GloomText); };
            GloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GloomText); };

            MonochromeButton.Click += Monochrome;
            MonochromeButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(MonochromeText); };
            MonochromeButton.MouseEnter += delegate { ButtonMouseOverAnim(MonochromeText); };
            MonochromeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(MonochromeText); };

            WavewarperButton.Click += WaveWarperEffect;
            WavewarperButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(WaveWarperText); };
            WavewarperButton.MouseEnter += delegate { ButtonMouseOverAnim(WaveWarperText); };
            WavewarperButton.MouseLeave += delegate { ButtonMouseLeaveAnim(WaveWarperText); };

            UnderwaterButton.Click += UnderWaterEffect;
            UnderwaterButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(UnderwaterText); };
            UnderwaterButton.MouseEnter += delegate { ButtonMouseOverAnim(UnderwaterText); };
            UnderwaterButton.MouseLeave += delegate { ButtonMouseLeaveAnim(UnderwaterText); };

            BandedSwirlButton.Click += BandedSwirlEffect;
            BandedSwirlButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BandedSwirlText); };
            BandedSwirlButton.MouseEnter += delegate { ButtonMouseOverAnim(BandedSwirlText); };
            BandedSwirlButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandedSwirlText); };

            SwirlButton.Click += SwirlEffect;
            SwirlButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SwirlText); };
            SwirlButton.MouseEnter += delegate { ButtonMouseOverAnim(SwirlText); };
            SwirlButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SwirlText); };

            RippleButton.Click += RippleEffect1;
            RippleButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RippleText); };
            RippleButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleText); };
            RippleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleText); };

            RippleAltButton.Click += RippleEffect2;
            RippleAltButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RippleAltText); };
            RippleAltButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleAltText); };
            RippleAltButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleAltText); };

            BlurButton.Click += BlurEffect;
            BlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BlurText); };
            BlurButton.MouseEnter += delegate { ButtonMouseOverAnim(BlurText); };
            BlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BlurText); };

            DirectionalBlurButton.Click += Dir_blur;
            DirectionalBlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(DirectionalBlurText); };
            DirectionalBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(DirectionalBlurText); };
            DirectionalBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(DirectionalBlurText); };

            TelescopicBlurButton.Click += Teleskopisk_blur;
            TelescopicBlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(TelescopicBlurText); };
            TelescopicBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(TelescopicBlurText); };
            TelescopicBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(TelescopicBlurText); };

            PixelateButton.Click += PixelateEffect;
            PixelateButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PixelateText); };
            PixelateButton.MouseEnter += delegate { ButtonMouseOverAnim(PixelateText); };
            PixelateButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PixelateText); };

            EmbossedButton.Click += Embossed;
            EmbossedButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(EmbossedText); };
            EmbossedButton.MouseEnter += delegate { ButtonMouseOverAnim(EmbossedText); };
            EmbossedButton.MouseLeave += delegate { ButtonMouseLeaveAnim(EmbossedText); };

            SmoothMagnifyButton.Click += MagnifySmoothEffect;
            SmoothMagnifyButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SmoothMagnifyText); };
            SmoothMagnifyButton.MouseEnter += delegate { ButtonMouseOverAnim(SmoothMagnifyText); };
            SmoothMagnifyButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SmoothMagnifyText); };

            PivotButton.Click += PivotEffect;
            PivotButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PivotText); };
            PivotButton.MouseEnter += delegate { ButtonMouseOverAnim(PivotText); };
            PivotButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PivotText); };

            PaperfoldButton.Click += PaperFoldEffect;
            PaperfoldButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PaperFoldText); };
            PaperfoldButton.MouseEnter += delegate { ButtonMouseOverAnim(PaperFoldText); };
            PaperfoldButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PaperFoldText); };

            PencilSketchButton.Click += SketchPencilStrokeEffect;
            PencilSketchButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PencilSketchText); };
            PencilSketchButton.MouseEnter += delegate { ButtonMouseOverAnim(PencilSketchText); };
            PencilSketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PencilSketchText); };

            SketchButton.Click += Sketch;
            SketchButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SketchText); };
            SketchButton.MouseEnter += delegate { ButtonMouseOverAnim(SketchText); };
            SketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SketchText); };

            TonemappingButton.Click += ToneMapping;
            TonemappingButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ToneMappingText); };
            TonemappingButton.MouseEnter += delegate { ButtonMouseOverAnim(ToneMappingText); };
            TonemappingButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ToneMappingText); };

            BandsButton.Click += Bands;
            BandsButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BandsText); };
            BandsButton.MouseEnter += delegate { ButtonMouseOverAnim(BandsText); };
            BandsButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandsText); };

            GlasTileButton.Click += GlasTileEffect;
            GlasTileButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GlassTileText); };
            GlasTileButton.MouseEnter += delegate { ButtonMouseOverAnim(GlassTileText); };
            GlasTileButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GlassTileText); };

            FrostyOutlineButton.Click += FrostyOutlineEffect;
            FrostyOutlineButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FrostyOutlineText); };
            FrostyOutlineButton.MouseEnter += delegate { ButtonMouseOverAnim(FrostyOutlineText); };
            FrostyOutlineButton.MouseLeave += delegate { ButtonMouseLeaveAnim(FrostyOutlineText); };

            // SaveButton
            SaveButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SaveText); };
            SaveButton.MouseEnter += delegate { ButtonMouseOverAnim(SaveText); };
            SaveButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SaveText); };
            SaveButton.Click += (_, _) => Open_Save.SaveFiles();
        }

        #region HLSL Shader Effects

        private bool Remove_Effects()
        {
            ConfigureWindows.GetMainWindow.MainImage.Effect = null;

            var list = EffectsContainer.Children.OfType<CheckBox>().Where(x => x.IsChecked == true);

            if (list.Any())
            {
                foreach (var item in list)
                {
                    item.IsChecked = false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Negative(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }
            
            ConfigureWindows.GetMainWindow.MainImage.Effect = new InvertColorEffect();
            NegativeButton.IsChecked = true;
        }

        private void GraySceale(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new GrayscaleEffect();
            GrayscaleButton.IsChecked = true;
        }

        private void ColorToneEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new ColorToneEffect();
            ColorToneButton.IsChecked = true;
        }

        private void RippleEffect1(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new Transition_RippleEffect();
            RippleButton.IsChecked = true;
        }

        private void RippleEffect2(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new RippleEffect();
            RippleAltButton.IsChecked = true;
        }

        private void BandedSwirlEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new BandedSwirlEffect();
            BandedSwirlButton.IsChecked = true;
        }

        private void Monochrome(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new MonochromeEffect();
            MonochromeButton.IsChecked = true;
        }

        private void SwirlEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new SwirlEffect();
            SwirlButton.IsChecked = true;
        }

        private void Bloom(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new BloomEffect();
            BloomButton.IsChecked = true;
        }

        private void Gloom(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new GloomEffect();
            GloomButton.IsChecked = true;
        }

        private void ToneMapping(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new ToneMappingEffect();
            TonemappingButton.IsChecked = true;
        }

        private void Teleskopisk_blur(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new TelescopicBlurPS3Effect();
            TelescopicBlurButton.IsChecked = true;
        }

        private void BlurEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new GrowablePoissonDiskEffect();
            BlurButton.IsChecked = true;
        }

        private void Dir_blur(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new DirectionalBlurEffect();
            DirectionalBlurButton.IsChecked = true;
        }

        private void Bands(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new BandsEffect();
            BandsButton.IsChecked = true;
        }

        private void Embossed(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new EmbossedEffect();
            EmbossedButton.IsChecked = true;
        }

        private void GlasTileEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new GlassTilesEffect();
            GlasTileButton.IsChecked = true;
        }

        private void MagnifySmoothEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new MagnifySmoothEffect();
            SmoothMagnifyButton.IsChecked = true;
        }

        private void PaperFoldEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new PaperFoldEffect();
            PaperfoldButton.IsChecked = true;
        }

        private void PivotEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new PivotEffect();
            PivotButton.IsChecked = true;
        }

        private void UnderWaterEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new UnderwaterEffect();
            UnderwaterButton.IsChecked = true;
        }

        private void WaveWarperEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new WaveWarperEffect();
            WavewarperButton.IsChecked = true;
        }

        private void FrostyOutlineEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new FrostyOutlineEffect();
            FrostyOutlineButton.IsChecked = true;
        }

        private void OldMovieEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new OldMovieEffect();
            OldMovieButton.IsChecked = true;
        }

        private void PixelateEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new PixelateEffect();
            PixelateButton.IsChecked = true;
        }

        private void Sketch(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new SketchGraniteEffect();
            SketchButton.IsChecked = true;
        }

        private void SketchPencilStrokeEffect(object sender, RoutedEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
            {
                if (!Remove_Effects())
                {
                    return;
                }
            }

            ConfigureWindows.GetMainWindow.MainImage.Effect = new SketchPencilStrokeEffect();
            PencilSketchButton.IsChecked = true;
        }

        #endregion HLSL Shader Effects
    }
}
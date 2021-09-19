using Microsoft.Windows.Themes;
using PicView.Editing.HlslEffects;
using PicView.FileHandling;
using PicView.UILogic;
using PicView.UILogic.Animations;
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

            IntensitySlider.ValueChanged += (_, _) => IntensitySlider_ValueChanged();

            #region button events

            NegativeButton.Click += Negative;
            NegativeButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(NegativeColorsText); };
            NegativeButton.MouseEnter += delegate { ButtonMouseOverAnim(NegativeColorsText); AnimationHelper.MouseEnterBgTexColor(NegativeButtonBrush); };
            NegativeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(NegativeColorsText); AnimationHelper.MouseLeaveBgTexColor(NegativeButtonBrush); };

            GrayscaleButton.Click += GraySceale;
            GrayscaleButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GrayscaleText); };
            GrayscaleButton.MouseEnter += delegate { ButtonMouseOverAnim(GrayscaleText); AnimationHelper.MouseEnterBgTexColor(GrayscaleButtonBrush); };
            GrayscaleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GrayscaleText); AnimationHelper.MouseLeaveBgTexColor(GrayscaleButtonBrush); };

            ColorToneButton.Click += ColorToneEffect;
            ColorToneButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ColorToneText); };
            ColorToneButton.MouseEnter += delegate { ButtonMouseOverAnim(ColorToneText); AnimationHelper.MouseEnterBgTexColor(ColorToneButtonBrush); };
            ColorToneButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ColorToneText); AnimationHelper.MouseLeaveBgTexColor(ColorToneButtonBrush); };

            OldMovieButton.Click += OldMovieEffect;
            OldMovieButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(OldMovieText); };
            OldMovieButton.MouseEnter += delegate { ButtonMouseOverAnim(OldMovieText); AnimationHelper.MouseEnterBgTexColor(OldMovieButtonBrush); };
            OldMovieButton.MouseLeave += delegate { ButtonMouseLeaveAnim(OldMovieText); AnimationHelper.MouseLeaveBgTexColor(OldMovieButtonBrush); };

            BloomButton.Click += Bloom;
            BloomButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BloomText); };
            BloomButton.MouseEnter += delegate { ButtonMouseOverAnim(BloomText); AnimationHelper.MouseEnterBgTexColor(BloomButtonBrush); };
            BloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BloomText); AnimationHelper.MouseLeaveBgTexColor(BloomButtonBrush); };

            GloomButton.Click += Gloom;
            GloomButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GloomText); };
            GloomButton.MouseEnter += delegate { ButtonMouseOverAnim(GloomText); AnimationHelper.MouseEnterBgTexColor(GloomButtonBrush); };
            GloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GloomText); AnimationHelper.MouseLeaveBgTexColor(GloomButtonBrush); };

            MonochromeButton.Click += Monochrome;
            MonochromeButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(MonochromeText); };
            MonochromeButton.MouseEnter += delegate { ButtonMouseOverAnim(MonochromeText); AnimationHelper.MouseEnterBgTexColor(MonochromeButtonBrush); };
            MonochromeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(MonochromeText); AnimationHelper.MouseLeaveBgTexColor(MonochromeButtonBrush); };

            WavewarperButton.Click += WaveWarperEffect;
            WavewarperButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(WaveWarperText); };
            WavewarperButton.MouseEnter += delegate { ButtonMouseOverAnim(WaveWarperText); AnimationHelper.MouseEnterBgTexColor(WaveWarperButtonBrush); };
            WavewarperButton.MouseLeave += delegate { ButtonMouseLeaveAnim(WaveWarperText); AnimationHelper.MouseLeaveBgTexColor(WaveWarperButtonBrush); };

            UnderwaterButton.Click += UnderWaterEffect;
            UnderwaterButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(UnderwaterText); };
            UnderwaterButton.MouseEnter += delegate { ButtonMouseOverAnim(UnderwaterText); AnimationHelper.MouseEnterBgTexColor(UnderwaterButtonBrush); };
            UnderwaterButton.MouseLeave += delegate { ButtonMouseLeaveAnim(UnderwaterText); AnimationHelper.MouseLeaveBgTexColor(UnderwaterButtonBrush); };

            BandedSwirlButton.Click += BandedSwirlEffect;
            BandedSwirlButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BandedSwirlText); };
            BandedSwirlButton.MouseEnter += delegate { ButtonMouseOverAnim(BandedSwirlText); AnimationHelper.MouseEnterBgTexColor(BandedSwirlButtonBrush); };
            BandedSwirlButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandedSwirlText); AnimationHelper.MouseLeaveBgTexColor(BandedSwirlButtonBrush); };

            SwirlButton.Click += SwirlEffect;
            SwirlButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SwirlText); };
            SwirlButton.MouseEnter += delegate { ButtonMouseOverAnim(SwirlText); AnimationHelper.MouseEnterBgTexColor(SwirlButtonBrush); };
            SwirlButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SwirlText); AnimationHelper.MouseLeaveBgTexColor(SwirlButtonBrush); };

            RippleButton.Click += RippleEffect1;
            RippleButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RippleText); };
            RippleButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleText); AnimationHelper.MouseEnterBgTexColor(RippleButtonBrush); };
            RippleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleText); AnimationHelper.MouseLeaveBgTexColor(RippleButtonBrush); };

            RippleAltButton.Click += RippleEffect2;
            RippleAltButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RippleAltText); };
            RippleAltButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleAltText); AnimationHelper.MouseEnterBgTexColor(RippleAltButtonBrush); };
            RippleAltButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleAltText); AnimationHelper.MouseLeaveBgTexColor(RippleAltButtonBrush); };

            BlurButton.Click += BlurEffect;
            BlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BlurText); };
            BlurButton.MouseEnter += delegate { ButtonMouseOverAnim(BlurText); AnimationHelper.MouseEnterBgTexColor(BlurButtonBrush); };
            BlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BlurText); AnimationHelper.MouseLeaveBgTexColor(BlurButtonBrush); };

            DirectionalBlurButton.Click += Dir_blur;
            DirectionalBlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(DirectionalBlurText); };
            DirectionalBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(DirectionalBlurText); AnimationHelper.MouseEnterBgTexColor(DirectionalBlurButtonBrush); };
            DirectionalBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(DirectionalBlurText); AnimationHelper.MouseLeaveBgTexColor(DirectionalBlurButtonBrush); };

            TelescopicBlurButton.Click += Teleskopisk_blur;
            TelescopicBlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(TelescopicBlurText); };
            TelescopicBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(TelescopicBlurText); AnimationHelper.MouseEnterBgTexColor(TelescopicBlurButtonBrush); };
            TelescopicBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(TelescopicBlurText); AnimationHelper.MouseLeaveBgTexColor(TelescopicBlurButtonBrush); };

            PixelateButton.Click += PixelateEffect;
            PixelateButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PixelateText); };
            PixelateButton.MouseEnter += delegate { ButtonMouseOverAnim(PixelateText); AnimationHelper.MouseEnterBgTexColor(PixelateButtonBrush); };
            PixelateButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PixelateText); AnimationHelper.MouseLeaveBgTexColor(PixelateButtonBrush); };

            EmbossedButton.Click += Embossed;
            EmbossedButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(EmbossedText); };
            EmbossedButton.MouseEnter += delegate { ButtonMouseOverAnim(EmbossedText); AnimationHelper.MouseEnterBgTexColor(EmbossedButtonBrush); };
            EmbossedButton.MouseLeave += delegate { ButtonMouseLeaveAnim(EmbossedText); AnimationHelper.MouseLeaveBgTexColor(EmbossedButtonBrush); };

            SmoothMagnifyButton.Click += MagnifySmoothEffect;
            SmoothMagnifyButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SmoothMagnifyText); };
            SmoothMagnifyButton.MouseEnter += delegate { ButtonMouseOverAnim(SmoothMagnifyText); AnimationHelper.MouseEnterBgTexColor(SmoothMagnifyButtonBrush); };
            SmoothMagnifyButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SmoothMagnifyText); AnimationHelper.MouseLeaveBgTexColor(SmoothMagnifyButtonBrush); };

            PivotButton.Click += PivotEffect;
            PivotButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PivotText); };
            PivotButton.MouseEnter += delegate { ButtonMouseOverAnim(PivotText); AnimationHelper.MouseEnterBgTexColor(PivotButtonBrush); };
            PivotButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PivotText); AnimationHelper.MouseLeaveBgTexColor(PivotButtonBrush); };

            PaperfoldButton.Click += PaperFoldEffect;
            PaperfoldButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PaperFoldText); };
            PaperfoldButton.MouseEnter += delegate { ButtonMouseOverAnim(PaperFoldText); AnimationHelper.MouseEnterBgTexColor(PaperfoldButtonBrush); };
            PaperfoldButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PaperFoldText); AnimationHelper.MouseLeaveBgTexColor(PaperfoldButtonBrush); };

            PencilSketchButton.Click += SketchPencilStrokeEffect;
            PencilSketchButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PencilSketchText); };
            PencilSketchButton.MouseEnter += delegate { ButtonMouseOverAnim(PencilSketchText); AnimationHelper.MouseEnterBgTexColor(PencilSketchButtonBrush); };
            PencilSketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PencilSketchText); AnimationHelper.MouseLeaveBgTexColor(PencilSketchButtonBrush); };

            SketchButton.Click += Sketch;
            SketchButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SketchText); };
            SketchButton.MouseEnter += delegate { ButtonMouseOverAnim(SketchText); AnimationHelper.MouseEnterBgTexColor(SketchButtonBrush); };
            SketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SketchText); AnimationHelper.MouseLeaveBgTexColor(SketchButtonBrush); };

            TonemappingButton.Click += ToneMapping;
            TonemappingButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ToneMappingText); };
            TonemappingButton.MouseEnter += delegate { ButtonMouseOverAnim(ToneMappingText); AnimationHelper.MouseEnterBgTexColor(ToneMappingButtonBrush); };
            TonemappingButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ToneMappingText); AnimationHelper.MouseLeaveBgTexColor(ToneMappingButtonBrush); };

            BandsButton.Click += Bands;
            BandsButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BandsText); };
            BandsButton.MouseEnter += delegate { ButtonMouseOverAnim(BandsText); AnimationHelper.MouseEnterBgTexColor(BandsButtonBrush); };
            BandsButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandsText); AnimationHelper.MouseLeaveBgTexColor(BandsButtonBrush); };

            GlasTileButton.Click += GlasTileEffect;
            GlasTileButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GlassTileText); };
            GlasTileButton.MouseEnter += delegate { ButtonMouseOverAnim(GlassTileText); AnimationHelper.MouseEnterBgTexColor(GlasTileButtonBrush); };
            GlasTileButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GlassTileText); AnimationHelper.MouseLeaveBgTexColor(GlasTileButtonBrush); };

            FrostyOutlineButton.Click += FrostyOutlineEffect;
            FrostyOutlineButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FrostyOutlineText); };
            FrostyOutlineButton.MouseEnter += delegate { ButtonMouseOverAnim(FrostyOutlineText); AnimationHelper.MouseEnterBgTexColor(FrostyOutlineButtonBrush); };
            FrostyOutlineButton.MouseLeave += delegate { ButtonMouseLeaveAnim(FrostyOutlineText); AnimationHelper.MouseLeaveBgTexColor(FrostyOutlineButtonBrush); };

            // SaveButton
            SaveButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SaveText); };
            SaveButton.MouseEnter += delegate { ButtonMouseOverAnim(SaveText); };
            SaveButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SaveText); };
            SaveButton.Click += async (sender, e) => await Open_Save.SaveFilesAsync();

            #endregion
        }

        private void IntensitySlider_ValueChanged()
        {
            if (PixelateButton.IsChecked.Value)
            {
                PixelateEffect(null, null);
            }
            else if (ColorToneButton.IsChecked.Value)
            {
                ColorToneEffect(null, null);
            }
            else if (RippleAltButton.IsChecked.Value)
            {
                RippleEffect2(null, null);
            }
            else if (BandedSwirlButton.IsChecked.Value)
            {
                BandedSwirlEffect(null, null);
            }
            else if (DirectionalBlurButton.IsChecked.Value)
            {
                Dir_blur(null, null);
            }
            else if (BandsButton.IsChecked.Value)
            {
                Bands(null, null);
            }
            else if (EmbossedButton.IsChecked.Value)
            {
                Embossed(null, null);
            }
            else if (GlasTileButton.IsChecked.Value)
            {
                GlasTileEffect(null, null);
            }
            else if (SmoothMagnifyButton.IsChecked.Value)
            {
                MagnifySmoothEffect(null, null);
            }
            else if (PaperfoldButton.IsChecked.Value)
            {
                PaperFoldEffect(null, null);
            }
            else if (PivotButton.IsChecked.Value)
            {
                PivotEffect(null, null);
            }
            else if (UnderwaterButton.IsChecked.Value)
            {
                UnderWaterEffect(null, null);
            }
            else if (WavewarperButton.IsChecked.Value)
            {
                WaveWarperEffect(null, null);
            }
            else if (FrostyOutlineButton.IsChecked.Value)
            {
                FrostyOutlineEffect(null, null);
            }
            else if (OldMovieButton.IsChecked.Value)
            {
                OldMovieEffect(null, null);
            }
            else if (SketchButton.IsChecked.Value)
            {
                Sketch(null, null);
            }
            else if (SwirlButton.IsChecked.Value)
            {
                SwirlEffect(null, null);
            }
            else if (BloomButton.IsChecked.Value)
            {
                Bloom(null, null);
            }
            else if (GloomButton.IsChecked.Value)
            {
                Gloom(null, null);
            }
            else if (TonemappingButton.IsChecked.Value)
            {
                ToneMapping(null, null);
            }
            else if (TelescopicBlurButton.IsChecked.Value)
            {
                Teleskopisk_blur(null, null);
            }
            else if (PencilSketchButton.IsChecked.Value)
            {
                SketchPencilStrokeEffect(null, null);
            }

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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new ColorToneEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new RippleEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new BandedSwirlEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new SwirlEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new BloomEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new GloomEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new ToneMappingEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new TelescopicBlurPS3Effect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new DirectionalBlurEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new BandsEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new EmbossedEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new GlassTilesEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new MagnifySmoothEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new PaperFoldEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new PivotEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new UnderwaterEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new WaveWarperEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new FrostyOutlineEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new OldMovieEffect(IntensitySlider.Value);
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
            
            ConfigureWindows.GetMainWindow.MainImage.Effect = new PixelateEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new SketchGraniteEffect(IntensitySlider.Value);
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

            ConfigureWindows.GetMainWindow.MainImage.Effect = new SketchPencilStrokeEffect(IntensitySlider.Value);
            PencilSketchButton.IsChecked = true;
        }

        #endregion HLSL Shader Effects
    }
}
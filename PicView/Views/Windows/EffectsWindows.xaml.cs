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

        private void Window_ContentRendered(object? sender, EventArgs? e)
        {
            KeyDown += (_, e) => Shortcuts.GenericWindowShortcuts.KeysDown(null, e, this);

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); ConfigureWindows.GetMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            TitleBar.MouseLeftButtonDown += delegate { DragMove(); };

            IntensitySlider.ValueChanged += (_, _) => IntensitySlider_ValueChanged();

            #region button events

            // NegativeButton
            NegativeButton.Click += (_, _) => Negative();
            NegativeButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(NegativeColorsText); };
            NegativeButton.MouseEnter += delegate { ButtonMouseOverAnim(NegativeColorsText); };
            NegativeButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(NegativeButtonBrush); };
            NegativeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(NegativeColorsText); };
            NegativeButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(NegativeButtonBrush); };

            GrayscaleButton.Click += (_, _) => GraySceale();
            GrayscaleButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GrayscaleText); };
            GrayscaleButton.MouseEnter += delegate { ButtonMouseOverAnim(GrayscaleText); AnimationHelper.MouseEnterBgTexColor(GrayscaleButtonBrush); };
            GrayscaleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GrayscaleText); AnimationHelper.MouseLeaveBgTexColor(GrayscaleButtonBrush); };

            ColorToneButton.Click += (_, _) => ColorToneEffect();
            ColorToneButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ColorToneText); };
            ColorToneButton.MouseEnter += delegate { ButtonMouseOverAnim(ColorToneText); AnimationHelper.MouseEnterBgTexColor(ColorToneButtonBrush); };
            ColorToneButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ColorToneText); AnimationHelper.MouseLeaveBgTexColor(ColorToneButtonBrush); };

            OldMovieButton.Click += (_, _) => OldMovieEffect();
            OldMovieButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(OldMovieText); };
            OldMovieButton.MouseEnter += delegate { ButtonMouseOverAnim(OldMovieText); AnimationHelper.MouseEnterBgTexColor(OldMovieButtonBrush); };
            OldMovieButton.MouseLeave += delegate { ButtonMouseLeaveAnim(OldMovieText); AnimationHelper.MouseLeaveBgTexColor(OldMovieButtonBrush); };

            BloomButton.Click += (_, _) => Bloom();
            BloomButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BloomText); };
            BloomButton.MouseEnter += delegate { ButtonMouseOverAnim(BloomText); AnimationHelper.MouseEnterBgTexColor(BloomButtonBrush); };
            BloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BloomText); AnimationHelper.MouseLeaveBgTexColor(BloomButtonBrush); };

            GloomButton.Click += (_, _) => Gloom();
            GloomButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GloomText); };
            GloomButton.MouseEnter += delegate { ButtonMouseOverAnim(GloomText); AnimationHelper.MouseEnterBgTexColor(GloomButtonBrush); };
            GloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GloomText); AnimationHelper.MouseLeaveBgTexColor(GloomButtonBrush); };

            MonochromeButton.Click += (_, _) => Monochrome();
            MonochromeButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(MonochromeText); };
            MonochromeButton.MouseEnter += delegate { ButtonMouseOverAnim(MonochromeText); AnimationHelper.MouseEnterBgTexColor(MonochromeButtonBrush); };
            MonochromeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(MonochromeText); AnimationHelper.MouseLeaveBgTexColor(MonochromeButtonBrush); };

            WavewarperButton.Click += (_, _) => WaveWarperEffect();
            WavewarperButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(WaveWarperText); };
            WavewarperButton.MouseEnter += delegate { ButtonMouseOverAnim(WaveWarperText); AnimationHelper.MouseEnterBgTexColor(WaveWarperButtonBrush); };
            WavewarperButton.MouseLeave += delegate { ButtonMouseLeaveAnim(WaveWarperText); AnimationHelper.MouseLeaveBgTexColor(WaveWarperButtonBrush); };

            UnderwaterButton.Click += (_, _) => UnderWaterEffect();
            UnderwaterButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(UnderwaterText); };
            UnderwaterButton.MouseEnter += delegate { ButtonMouseOverAnim(UnderwaterText); AnimationHelper.MouseEnterBgTexColor(UnderwaterButtonBrush); };
            UnderwaterButton.MouseLeave += delegate { ButtonMouseLeaveAnim(UnderwaterText); AnimationHelper.MouseLeaveBgTexColor(UnderwaterButtonBrush); };

            BandedSwirlButton.Click += (_, _) => BandedSwirlEffect();
            BandedSwirlButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BandedSwirlText); };
            BandedSwirlButton.MouseEnter += delegate { ButtonMouseOverAnim(BandedSwirlText); AnimationHelper.MouseEnterBgTexColor(BandedSwirlButtonBrush); };
            BandedSwirlButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandedSwirlText); AnimationHelper.MouseLeaveBgTexColor(BandedSwirlButtonBrush); };

            SwirlButton.Click += (_, _) => SwirlEffect();
            SwirlButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SwirlText); };
            SwirlButton.MouseEnter += delegate { ButtonMouseOverAnim(SwirlText); AnimationHelper.MouseEnterBgTexColor(SwirlButtonBrush); };
            SwirlButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SwirlText); AnimationHelper.MouseLeaveBgTexColor(SwirlButtonBrush); };

            RippleButton.Click += (_, _) => RippleEffect1();
            RippleButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RippleText); };
            RippleButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleText); AnimationHelper.MouseEnterBgTexColor(RippleButtonBrush); };
            RippleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleText); AnimationHelper.MouseLeaveBgTexColor(RippleButtonBrush); };

            RippleAltButton.Click += (_, _) => RippleEffect2();
            RippleAltButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(RippleAltText); };
            RippleAltButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleAltText); AnimationHelper.MouseEnterBgTexColor(RippleAltButtonBrush); };
            RippleAltButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleAltText); AnimationHelper.MouseLeaveBgTexColor(RippleAltButtonBrush); };

            BlurButton.Click += (_, _) => BlurEffect();
            BlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BlurText); };
            BlurButton.MouseEnter += delegate { ButtonMouseOverAnim(BlurText); AnimationHelper.MouseEnterBgTexColor(BlurButtonBrush); };
            BlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BlurText); AnimationHelper.MouseLeaveBgTexColor(BlurButtonBrush); };

            DirectionalBlurButton.Click += (_, _) => Dir_blur();
            DirectionalBlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(DirectionalBlurText); };
            DirectionalBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(DirectionalBlurText); AnimationHelper.MouseEnterBgTexColor(DirectionalBlurButtonBrush); };
            DirectionalBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(DirectionalBlurText); AnimationHelper.MouseLeaveBgTexColor(DirectionalBlurButtonBrush); };

            TelescopicBlurButton.Click += (_, _) => Teleskopisk_blur();
            TelescopicBlurButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(TelescopicBlurText); };
            TelescopicBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(TelescopicBlurText); AnimationHelper.MouseEnterBgTexColor(TelescopicBlurButtonBrush); };
            TelescopicBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(TelescopicBlurText); AnimationHelper.MouseLeaveBgTexColor(TelescopicBlurButtonBrush); };

            PixelateButton.Click += (_, _) => PixelateEffect();
            PixelateButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PixelateText); };
            PixelateButton.MouseEnter += delegate { ButtonMouseOverAnim(PixelateText); AnimationHelper.MouseEnterBgTexColor(PixelateButtonBrush); };
            PixelateButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PixelateText); AnimationHelper.MouseLeaveBgTexColor(PixelateButtonBrush); };

            EmbossedButton.Click += (_, _) => Embossed();
            EmbossedButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(EmbossedText); };
            EmbossedButton.MouseEnter += delegate { ButtonMouseOverAnim(EmbossedText); AnimationHelper.MouseEnterBgTexColor(EmbossedButtonBrush); };
            EmbossedButton.MouseLeave += delegate { ButtonMouseLeaveAnim(EmbossedText); AnimationHelper.MouseLeaveBgTexColor(EmbossedButtonBrush); };

            SmoothMagnifyButton.Click += (_, _) => MagnifySmoothEffect();
            SmoothMagnifyButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SmoothMagnifyText); };
            SmoothMagnifyButton.MouseEnter += delegate { ButtonMouseOverAnim(SmoothMagnifyText); AnimationHelper.MouseEnterBgTexColor(SmoothMagnifyButtonBrush); };
            SmoothMagnifyButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SmoothMagnifyText); AnimationHelper.MouseLeaveBgTexColor(SmoothMagnifyButtonBrush); };

            PivotButton.Click += (_, _) => PivotEffect();
            PivotButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PivotText); };
            PivotButton.MouseEnter += delegate { ButtonMouseOverAnim(PivotText); AnimationHelper.MouseEnterBgTexColor(PivotButtonBrush); };
            PivotButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PivotText); AnimationHelper.MouseLeaveBgTexColor(PivotButtonBrush); };

            PaperfoldButton.Click += (_, _) => PaperFoldEffect();
            PaperfoldButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PaperFoldText); };
            PaperfoldButton.MouseEnter += delegate { ButtonMouseOverAnim(PaperFoldText); AnimationHelper.MouseEnterBgTexColor(PaperfoldButtonBrush); };
            PaperfoldButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PaperFoldText); AnimationHelper.MouseLeaveBgTexColor(PaperfoldButtonBrush); };

            PencilSketchButton.Click += (_, _) => SketchPencilStrokeEffect();
            PencilSketchButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(PencilSketchText); };
            PencilSketchButton.MouseEnter += delegate { ButtonMouseOverAnim(PencilSketchText); AnimationHelper.MouseEnterBgTexColor(PencilSketchButtonBrush); };
            PencilSketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PencilSketchText); AnimationHelper.MouseLeaveBgTexColor(PencilSketchButtonBrush); };

            SketchButton.Click += (_, _) => Sketch();
            SketchButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SketchText); };
            SketchButton.MouseEnter += delegate { ButtonMouseOverAnim(SketchText); AnimationHelper.MouseEnterBgTexColor(SketchButtonBrush); };
            SketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SketchText); AnimationHelper.MouseLeaveBgTexColor(SketchButtonBrush); };

            TonemappingButton.Click += (_, _) => ToneMapping();
            TonemappingButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ToneMappingText); };
            TonemappingButton.MouseEnter += delegate { ButtonMouseOverAnim(ToneMappingText); AnimationHelper.MouseEnterBgTexColor(ToneMappingButtonBrush); };
            TonemappingButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ToneMappingText); AnimationHelper.MouseLeaveBgTexColor(ToneMappingButtonBrush); };

            BandsButton.Click += (_, _) => Bands();
            BandsButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(BandsText); };
            BandsButton.MouseEnter += delegate { ButtonMouseOverAnim(BandsText); AnimationHelper.MouseEnterBgTexColor(BandsButtonBrush); };
            BandsButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandsText); AnimationHelper.MouseLeaveBgTexColor(BandsButtonBrush); };

            GlasTileButton.Click += (_, _) => GlasTileEffect();
            GlasTileButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(GlassTileText); };
            GlasTileButton.MouseEnter += delegate { ButtonMouseOverAnim(GlassTileText); AnimationHelper.MouseEnterBgTexColor(GlasTileButtonBrush); };
            GlasTileButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GlassTileText); AnimationHelper.MouseLeaveBgTexColor(GlasTileButtonBrush); };

            FrostyOutlineButton.Click += (_, _) => FrostyOutlineEffect();
            FrostyOutlineButton.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(FrostyOutlineText); };
            FrostyOutlineButton.MouseEnter += delegate { ButtonMouseOverAnim(FrostyOutlineText); AnimationHelper.MouseEnterBgTexColor(FrostyOutlineButtonBrush); };
            FrostyOutlineButton.MouseLeave += delegate { ButtonMouseLeaveAnim(FrostyOutlineText); AnimationHelper.MouseLeaveBgTexColor(FrostyOutlineButtonBrush); };

            // SaveButton
            SaveButton.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(SaveText); };
            SaveButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(SaveBrush); };
            SaveButton.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(SaveText); };
            SaveButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(SaveBrush); };
            SaveButton.MouseLeftButtonUp += async (sender, e) => await Open_Save.SaveFilesAsync();

            #endregion
        }

        private void IntensitySlider_ValueChanged()
        {
            if (PixelateButton.IsChecked.HasValue && PixelateButton.IsChecked.Value)
            {
                PixelateEffect();
            }
            else if (ColorToneButton.IsChecked.HasValue && ColorToneButton.IsChecked.Value)
            {
                ColorToneEffect();
            }
            else if (RippleAltButton.IsChecked.HasValue && RippleAltButton.IsChecked.Value)
            {
                RippleEffect2();
            }
            else if (BandedSwirlButton.IsChecked.HasValue && BandedSwirlButton.IsChecked.Value)
            {
                BandedSwirlEffect();
            }
            else if (DirectionalBlurButton.IsChecked.HasValue && DirectionalBlurButton.IsChecked.Value)
            {
                Dir_blur();
            }
            else if (BandsButton.IsChecked.HasValue && BandsButton.IsChecked.Value)
            {
                Bands();
            }
            else if (EmbossedButton.IsChecked.HasValue && EmbossedButton.IsChecked.Value)
            {
                Embossed();
            }
            else if (GlasTileButton.IsChecked.HasValue && GlasTileButton.IsChecked.Value)
            {
                GlasTileEffect();
            }
            else if (SmoothMagnifyButton.IsChecked.HasValue && SmoothMagnifyButton.IsChecked.Value)
            {
                MagnifySmoothEffect();
            }
            else if (PaperfoldButton.IsChecked.HasValue && PaperfoldButton.IsChecked.Value)
            {
                PaperFoldEffect();
            }
            else if (PivotButton.IsChecked.HasValue && PivotButton.IsChecked.Value)
            {
                PivotEffect();
            }
            else if (UnderwaterButton.IsChecked.HasValue && UnderwaterButton.IsChecked.Value)
            {
                UnderWaterEffect();
            }
            else if (WavewarperButton.IsChecked.HasValue && WavewarperButton.IsChecked.Value)
            {
                WaveWarperEffect();
            }
            else if (FrostyOutlineButton.IsChecked.HasValue && FrostyOutlineButton.IsChecked.Value)
            {
                FrostyOutlineEffect();
            }
            else if (OldMovieButton.IsChecked.HasValue && OldMovieButton.IsChecked.Value)
            {
                OldMovieEffect();
            }
            else if (SketchButton.IsChecked.HasValue && SketchButton.IsChecked.Value)
            {
                Sketch();
            }
            else if (SwirlButton.IsChecked.HasValue && SwirlButton.IsChecked.Value)
            {
                SwirlEffect();
            }
            else if (BloomButton.IsChecked.HasValue && BloomButton.IsChecked.Value)
            {
                Bloom();
            }
            else if (GloomButton.IsChecked.HasValue && GloomButton.IsChecked.Value)
            {
                Gloom();
            }
            else if (TonemappingButton.IsChecked.HasValue && TonemappingButton.IsChecked.Value)
            {
                ToneMapping();
            }
            else if (TelescopicBlurButton.IsChecked.HasValue && TelescopicBlurButton.IsChecked.Value)
            {
                Teleskopisk_blur();
            }
            else if (PencilSketchButton.IsChecked.HasValue && PencilSketchButton.IsChecked.Value)
            {
                SketchPencilStrokeEffect();
            }

        }

        #region HLSL Shader Effects



        private bool Remove_Effects()
        {
            ConfigureWindows.GetMainWindow.MainImage.Effect = null;
            var list = EffectsContainer.Children.OfType<Border>();

            if (list.Any())
            {
                int checkboxes = 0;
                foreach (var item in list)
                {
                    var checkbox = item.Child as CheckBox;
                    if (checkbox is not null && checkbox.IsChecked.HasValue && checkbox.IsChecked.Value)
                    {
                        checkboxes++;
                        checkbox.IsChecked = false;
                    }
                }
                if (checkboxes > 0)
                {
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        private void Negative()
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

        private void GraySceale()
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

        private void ColorToneEffect()
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

        private void RippleEffect1()
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

        private void RippleEffect2()
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

        private void BandedSwirlEffect()
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

        private void Monochrome()
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

        private void SwirlEffect()
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

        private void Bloom()
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

        private void Gloom()
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

        private void ToneMapping()
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

        private void Teleskopisk_blur()
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

        private void BlurEffect()
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

        private void Dir_blur()
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

        private void Bands()
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

        private void Embossed()
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

        private void GlasTileEffect()
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

        private void MagnifySmoothEffect()
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

        private void PaperFoldEffect()
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

        private void PivotEffect()
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

        private void UnderWaterEffect()
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

        private void WaveWarperEffect()
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

        private void FrostyOutlineEffect()
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

        private void OldMovieEffect()
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

        private void PixelateEffect()
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

        private void Sketch()
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

        private void SketchPencilStrokeEffect()
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
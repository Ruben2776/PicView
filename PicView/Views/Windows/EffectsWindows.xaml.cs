using PicView.Animations;
using PicView.Editing.HlslEffects;
using PicView.FileHandling;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.Windows
{
    public partial class EffectsWindow : Window
    {
        public EffectsWindow()
        {
            InitializeComponent();
            Title = Application.Current.Resources["Effects"] + " - PicView";
            MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
            Width *= WindowSizing.MonitorInfo.DpiScaling;
            if (double.IsNaN(Width)) // Fixes if user opens window when loading from startup
            {
                WindowSizing.MonitorInfo = MonitorSize.GetMonitorSize();
                MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
                Width *= WindowSizing.MonitorInfo.DpiScaling;
            }

            ContentRendered += Window_ContentRendered;
        }

        private void Window_ContentRendered(object? sender, EventArgs? e)
        {
            KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(null, e, this);

            // CloseButton
            CloseButton.TheButton.Click += delegate { Hide(); ConfigureWindows.GetMainWindow.Focus(); };

            // MinButton
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            IntensitySlider.ValueChanged += (_, _) => IntensitySlider_ValueChanged();

            MouseLeftButtonDown += (_, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            #region button events

            // NegativeButton
            NegativeButton.Click += (_, _) => Negative();
            NegativeButton.MouseEnter += delegate { ButtonMouseOverAnim(NegativeColorsText); };
            NegativeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(NegativeColorsText); };

            GrayscaleButton.Click += (_, _) => GraySceale();
            GrayscaleButton.MouseEnter += delegate { ButtonMouseOverAnim(GrayscaleText); };
            GrayscaleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GrayscaleText); };

            ColorToneButton.Click += (_, _) => ColorToneEffect();
            ColorToneButton.MouseEnter += delegate { ButtonMouseOverAnim(ColorToneText); };
            ColorToneButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ColorToneText); };

            OldMovieButton.Click += (_, _) => OldMovieEffect();
            OldMovieButton.MouseEnter += delegate { ButtonMouseOverAnim(OldMovieText); };
            OldMovieButton.MouseLeave += delegate { ButtonMouseLeaveAnim(OldMovieText); };

            BloomButton.Click += (_, _) => Bloom();
            BloomButton.MouseEnter += delegate { ButtonMouseOverAnim(BloomText); };
            BloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BloomText); };

            GloomButton.Click += (_, _) => Gloom();
            GloomButton.MouseEnter += delegate { ButtonMouseOverAnim(GloomText); };
            GloomButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GloomText); };

            MonochromeButton.Click += (_, _) => Monochrome();
            MonochromeButton.MouseEnter += delegate { ButtonMouseOverAnim(MonochromeText); };
            MonochromeButton.MouseLeave += delegate { ButtonMouseLeaveAnim(MonochromeText); };

            WavewarperButton.Click += (_, _) => WaveWarperEffect();
            WavewarperButton.MouseEnter += delegate { ButtonMouseOverAnim(WaveWarperText); };
            WavewarperButton.MouseLeave += delegate { ButtonMouseLeaveAnim(WaveWarperText); };

            UnderwaterButton.Click += (_, _) => UnderWaterEffect();
            UnderwaterButton.MouseEnter += delegate { ButtonMouseOverAnim(UnderwaterText); };
            UnderwaterButton.MouseLeave += delegate { ButtonMouseLeaveAnim(UnderwaterText); };

            BandedSwirlButton.Click += (_, _) => BandedSwirlEffect();
            BandedSwirlButton.MouseEnter += delegate { ButtonMouseOverAnim(BandedSwirlText); };
            BandedSwirlButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandedSwirlText); };

            SwirlButton.Click += (_, _) => SwirlEffect();
            SwirlButton.MouseEnter += delegate { ButtonMouseOverAnim(SwirlText); };
            SwirlButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SwirlText); };

            RippleButton.Click += (_, _) => RippleEffect1();
            RippleButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleText); };
            RippleButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleText); };

            RippleAltButton.Click += (_, _) => RippleEffect2();
            RippleAltButton.MouseEnter += delegate { ButtonMouseOverAnim(RippleAltText); };
            RippleAltButton.MouseLeave += delegate { ButtonMouseLeaveAnim(RippleAltText); };

            BlurButton.Click += (_, _) => BlurEffect();
            BlurButton.MouseEnter += delegate { ButtonMouseOverAnim(BlurText); };
            BlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BlurText); };

            DirectionalBlurButton.Click += (_, _) => Dir_blur();
            DirectionalBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(DirectionalBlurText); };
            DirectionalBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(DirectionalBlurText); };

            TelescopicBlurButton.Click += (_, _) => Teleskopisk_blur();
            TelescopicBlurButton.MouseEnter += delegate { ButtonMouseOverAnim(TelescopicBlurText); };
            TelescopicBlurButton.MouseLeave += delegate { ButtonMouseLeaveAnim(TelescopicBlurText); };

            PixelateButton.Click += (_, _) => PixelateEffect();
            PixelateButton.MouseEnter += delegate { ButtonMouseOverAnim(PixelateText); };
            PixelateButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PixelateText); };

            EmbossedButton.Click += (_, _) => Embossed();
            EmbossedButton.MouseEnter += delegate { ButtonMouseOverAnim(EmbossedText); };
            EmbossedButton.MouseLeave += delegate { ButtonMouseLeaveAnim(EmbossedText); };

            SmoothMagnifyButton.Click += (_, _) => MagnifySmoothEffect();
            SmoothMagnifyButton.MouseEnter += delegate { ButtonMouseOverAnim(SmoothMagnifyText); };
            SmoothMagnifyButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SmoothMagnifyText); };

            PivotButton.Click += (_, _) => PivotEffect();
            PivotButton.MouseEnter += delegate { ButtonMouseOverAnim(PivotText); };
            PivotButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PivotText); };

            PaperfoldButton.Click += (_, _) => PaperFoldEffect();
            PaperfoldButton.MouseEnter += delegate { ButtonMouseOverAnim(PaperFoldText); };
            PaperfoldButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PaperFoldText); };

            PencilSketchButton.Click += (_, _) => SketchPencilStrokeEffect();
            PencilSketchButton.MouseEnter += delegate { ButtonMouseOverAnim(PencilSketchText); };
            PencilSketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(PencilSketchText); };

            SketchButton.Click += (_, _) => Sketch();
            SketchButton.MouseEnter += delegate { ButtonMouseOverAnim(SketchText); };
            SketchButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SketchText); };

            TonemappingButton.Click += (_, _) => ToneMapping();
            TonemappingButton.MouseEnter += delegate { ButtonMouseOverAnim(ToneMappingText); };
            TonemappingButton.MouseLeave += delegate { ButtonMouseLeaveAnim(ToneMappingText); };

            BandsButton.Click += (_, _) => Bands();
            BandsButton.MouseEnter += delegate { ButtonMouseOverAnim(BandsText); };
            BandsButton.MouseLeave += delegate { ButtonMouseLeaveAnim(BandsText); };

            GlasTileButton.Click += (_, _) => GlasTileEffect();
            GlasTileButton.MouseEnter += delegate { ButtonMouseOverAnim(GlassTileText); };
            GlasTileButton.MouseLeave += delegate { ButtonMouseLeaveAnim(GlassTileText); };

            FrostyOutlineButton.Click += (_, _) => FrostyOutlineEffect();
            FrostyOutlineButton.MouseEnter += delegate { ButtonMouseOverAnim(FrostyOutlineText); };
            FrostyOutlineButton.MouseLeave += delegate { ButtonMouseLeaveAnim(FrostyOutlineText); };

            // SaveButton
            SaveButton.MouseEnter += delegate { ButtonMouseOverAnim(SaveText); };
            SaveButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(SaveBrush); };
            SaveButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SaveText); };
            SaveButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(SaveBrush); };
            SaveButton.Click += async (_, _) => await Open_Save.SaveFilesAsync();

            // SetAsButton
            SetAsButton.MouseEnter += delegate { ButtonMouseOverAnim(SetAsText); };
            SetAsButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(SetAsBrush); };
            SetAsButton.MouseLeave += delegate { ButtonMouseLeaveAnim(SetAsText); };
            SetAsButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(SetAsBrush); };
            SetAsButton.Click += async (_, _) => await Wallpaper.SetWallpaperAsync(Wallpaper.WallpaperStyle.Fit).ConfigureAwait(false);

            // CopyButton
            CopyButton.MouseEnter += delegate { ButtonMouseOverAnim(CopyText); };
            CopyButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(CopyBrush); };
            CopyButton.MouseLeave += delegate { ButtonMouseLeaveAnim(CopyText); };
            CopyButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(CopyBrush); };
            CopyButton.Click += (_, _) => Copy_Paste.CopyBitmap();

            #endregion button events
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

            var list = EffectsContainer.Children.OfType<CheckBox>().Where(x => x.IsChecked == true);

            if (list.Any())
            {
                foreach (var item in list)
                {
                    item.IsChecked = false;
                }
                return true;
            }

            return false;
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
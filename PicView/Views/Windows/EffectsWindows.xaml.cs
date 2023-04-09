using PicView.Animations;
using PicView.Editing.HlslEffects;
using PicView.FileHandling;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinRT;
using static PicView.Animations.MouseOverAnimations;
using static PicView.SystemIntegration.Wallpaper;

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
            WindowBlur.EnableBlur(this);
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

            // Button, Brush and Text for Negative effect
            NegativeButton.Click += (_, _) => Negative();
            SetIconButterMouseOverAnimations(NegativeButton, NegativeButtonBrush, NegativeColorsText);

            // Button, Brush and Text for Grayscale effect
            GrayscaleButton.Click += (_, _) => GraySceale();
            SetIconButterMouseOverAnimations(GrayscaleButton, GrayscaleButtonBrush, GrayscaleText);

            // Button, Brush and Text for Color Tone effect
            ColorToneButton.Click += (_, _) => ColorToneEffect();
            SetIconButterMouseOverAnimations(ColorToneButton, ColorToneButtonBrush, ColorToneText);

            // Button, Brush and Text for Old Movie effect
            OldMovieButton.Click += (_, _) => OldMovieEffect();
            SetIconButterMouseOverAnimations(OldMovieButton, OldMovieButtonBrush, OldMovieText);

            // Button, Brush and Text for Bloom effect
            BloomButton.Click += (_, _) => Bloom();
            SetIconButterMouseOverAnimations(BloomButton, BloomButtonBrush, BloomText);

            // Button, Brush and Text for Gloom effect
            GloomButton.Click += (_, _) => Gloom();
            SetIconButterMouseOverAnimations(GloomButton, GloomButtonBrush, GloomText);

            // Button, Brush and Text for Monochrome effect
            MonochromeButton.Click += (_, _) => Monochrome();
            SetIconButterMouseOverAnimations(MonochromeButton, MonochromeButtonBrush, MonochromeText);

            // Button, Brush and Text for Wave Warper effect
            WavewarperButton.Click += (_, _) => WaveWarperEffect();
            SetIconButterMouseOverAnimations(WavewarperButton, WaveWarperButtonBrush, WaveWarperText);

            // Button, Brush and Text for Underwater effect
            UnderwaterButton.Click += (_, _) => UnderWaterEffect();
            SetIconButterMouseOverAnimations(UnderwaterButton, UnderwaterButtonBrush, UnderwaterText);

            // Button, Brush and Text for Banded Swirl effect
            BandedSwirlButton.Click += (_, _) => BandedSwirlEffect();
            SetIconButterMouseOverAnimations(BandedSwirlButton, BandedSwirlButtonBrush, BandedSwirlText);

            // Button, Brush and Text for Swirl effect
            SwirlButton.Click += (_, _) => SwirlEffect();
            SetIconButterMouseOverAnimations(SwirlButton, SwirlButtonBrush, SwirlText);

            // Button, Brush and Text for Ripple effect
            RippleButton.Click += (_, _) => RippleEffect1();
            SetIconButterMouseOverAnimations(RippleButton, RippleButtonBrush, RippleText);

            // Button, Brush and Text for Ripple Alternate effect
            RippleAltButton.Click += (_, _) => RippleEffect2();
            SetIconButterMouseOverAnimations(RippleAltButton, RippleAltButtonBrush, RippleAltText);

            // Button, Brush and Text for Blur effect
            BlurButton.Click += (_, _) => BlurEffect();
            SetIconButterMouseOverAnimations(BlurButton, BlurButtonBrush, BlurText);

            // Button, Brush and Text for Directional Blur effect
            DirectionalBlurButton.Click += (_, _) => Dir_blur();
            SetIconButterMouseOverAnimations(DirectionalBlurButton, DirectionalBlurButtonBrush, DirectionalBlurText);

            // Button, Brush and Text for Telescopic Blur effect
            TelescopicBlurButton.Click += (_, _) => Teleskopisk_blur();
            SetIconButterMouseOverAnimations(TelescopicBlurButton, TelescopicBlurButtonBrush, TelescopicBlurText);

            // Button, Brush and Text for Pixelate effect
            PixelateButton.Click += (_, _) => PixelateEffect();
            SetIconButterMouseOverAnimations(PixelateButton, PixelateButtonBrush, PixelateText);

            // Button, Brush and Text for Embossed effect
            EmbossedButton.Click += (_, _) => Embossed();
            SetIconButterMouseOverAnimations(EmbossedButton, EmbossedButtonBrush, EmbossedText);

            // Button, Brush and Text for Smooth Magnify effect
            SmoothMagnifyButton.Click += (_, _) => MagnifySmoothEffect();
            SetIconButterMouseOverAnimations(SmoothMagnifyButton, SmoothMagnifyButtonBrush, SmoothMagnifyText);

            // Button, Brush and Text for Pivot effect
            PivotButton.Click += (_, _) => PivotEffect();
            SetIconButterMouseOverAnimations(PivotButton, PivotButtonBrush, PivotText);

            // Button, Brush and Text for Paper Fold effect
            PaperfoldButton.Click += (_, _) => PaperFoldEffect();
            SetIconButterMouseOverAnimations(PaperfoldButton, PaperfoldButtonBrush, PaperFoldText);

            // Button, Brush and Text for Pencil Sketch Stroke effect
            PencilSketchButton.Click += (_, _) => SketchPencilStrokeEffect();
            SetIconButterMouseOverAnimations(PencilSketchButton, PencilSketchButtonBrush, PencilSketchText);

            // Button, Brush and Text for Sketch effect
            SketchButton.Click += (_, _) => Sketch();
            SetIconButterMouseOverAnimations(SketchButton, SketchButtonBrush, SketchText);

            // Button, Brush and Text for Tone Mapping effect
            ToneMappingButton.Click += (_, _) => ToneMapping();
            SetIconButterMouseOverAnimations(ToneMappingButton, ToneMappingButtonBrush, ToneMappingText);

            // Button, Brush and Text for Bands effect
            BandsButton.Click += (_, _) => Bands();
            SetIconButterMouseOverAnimations(BandsButton, BandsButtonBrush, BandsText);

            // Button, Brush and Text for Glass Tile effect
            GlasTileButton.Click += (_, _) => GlasTileEffect();
            SetIconButterMouseOverAnimations(GlasTileButton, GlasTileButtonBrush, GlassTileText);

            // Button, Brush and Text for Frosty Outline effect
            FrostyOutlineButton.Click += (_, _) => FrostyOutlineEffect();
            SetIconButterMouseOverAnimations(FrostyOutlineButton, FrostyOutlineButtonBrush, FrostyOutlineText);

            // Button, Brush and Text for SetAsWallpaper function
            SetIconButterMouseOverAnimations(SetAsWallpaperButton, SetAsWallpaperButtonBrush, SetAsWallpaperText);
            SetAsWallpaperButton.Click += async delegate
            {
                var x = WallpaperStyle.Fill;
                if (Fit.IsSelected) { x = WallpaperStyle.Fit; }
                if (Center.IsSelected) { x = WallpaperStyle.Center; }
                if (Tile.IsSelected) { x = WallpaperStyle.Tile; }
                if (Fit.IsSelected) { x = WallpaperStyle.Fit; }

                await SetWallpaperAsync(x).ConfigureAwait(false);
            };

            // Button, Brush and Text for SetAsLockscreen function
            SetIconButterMouseOverAnimations(SetAsLockScreenButton, SetAsLockscreenButtonBrush, SetAsLockscreenText);
            SetAsLockScreenButton.Click += async (_, _) => await LockScreenHelper.SetLockScreenImageAsync().ConfigureAwait(false);

            // Button, Brush and Text for Copy function
            SetIconButterMouseOverAnimations(CopyButton, CopyBrush, CopyText);
            CopyButton.Click += (_, _) => Copy_Paste.CopyBitmap();

            // Button, Brush and Text for Save function
            SetIconButterMouseOverAnimations(SaveButton, SaveBrush, SaveText);
            SaveButton.Click += async (_, _) => await Open_Save.SaveFilesAsync();

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
            else if (ToneMappingButton.IsChecked.HasValue && ToneMappingButton.IsChecked.Value)
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
            bool value = false;
            ConfigureWindows.GetMainWindow.MainImage.Effect = null;
            IntensitySlider.IsEnabled = false;

            var list = EffectsContainer.Children.OfType<CheckBox>().Where(x => x.IsChecked == true);
            var borders = EffectsContainer.Children.OfType<Border>();
            foreach (var item in borders)
            {
                var checkbox = item.Child.As<CheckBox>();
                if (checkbox is not null and { IsChecked : true})
                {
                    checkbox.IsChecked = false;
                    value = true;
                }
            }

            return value;
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
            IntensitySlider.IsEnabled = false;
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
            IntensitySlider.IsEnabled = false;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = false;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = false;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            ToneMappingButton.IsChecked = true;
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = false;
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
            IntensitySlider.IsEnabled = false;
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
            IntensitySlider.IsEnabled = false;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
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
            IntensitySlider.IsEnabled = true;
        }

        #endregion HLSL Shader Effects
    }
}
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.ImageEffects;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class EffectsView : UserControl
{
    private DisposableBag _disposables;
    private readonly Subject<Unit> _changes = new();

    private readonly ImageEffectConfig _default = new();
    private readonly TimeSpan _debounceTime = TimeSpan.FromMilliseconds(60);

    private bool _reloading;

    private readonly List<EffectPreset> _presets = [];

    public EffectsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DetachedFromLogicalTree += OnDetachedFromLogicalTree;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not CoreViewModel core)
        {
            return;
        }
        
        core.Effects ??= new EffectsViewModel();
        core.Effects.EffectConfig.Value ??= new ImageEffectConfig();
        
        // TODO handle initialize when no image present, or the image is not a file
        
        InitButtons();
        InitControlHooks();
        InitPresets();
        InitPipeline(core.Effects);
        
        // apply existing config to UI if any
        ApplyConfigToUi(core.Effects.EffectConfig.Value);
        if (!IsDefault(core.Effects.EffectConfig.Value))
        {
            RequestUpdate();
        }
        else
        {
            HideResetBtn();
        }

        // Reset all UI upon file change
        // TODO: dynamically update Image effects upon image change
        core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.FileInfo
            .Subscribe(_ =>
            {
                ResetAllUi();
            }, DebugHelper.LogError(nameof(EffectsView), nameof(ResetAllUi)))
            .AddTo(ref _disposables);

        core.Effects.IsLoading.Value = false;
    }
    
    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }

    private void InitButtons()
    {
        PointerPressed += OnPointerPressed;

        // Footer buttons
        ResetButton.Click += async (_, _) => await RemoveEffects();
        CancelButton.Click += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.Close();
            }
        };

        // Per-tab reset
        ResetColorTabBtn.Click += (_, _) => { ResetColor(); HideCancelBtn(); RequestUpdate(); };
        ResetLightingTabBtn.Click += (_, _) => { ResetLighting(); HideCancelBtn(); RequestUpdate(); };
        ResetEffectsTabBtn.Click += (_, _) => { ResetEffects(); HideCancelBtn(); RequestUpdate(); };

        ResetAllBtn.Click += (_, _) =>
        {
            ResetAllUiAndConfig();
            HideResetBtn();
            RequestUpdate();
        };

        // Toggle buttons
        BlackAndWhiteToggleButton.Click += (_, _) => { HideCancelBtn(); RequestUpdate(); };
        NegativeToggleButton.Click += (_, _) => { HideCancelBtn(); RequestUpdate(); };
        OldMovieToggleButton.Click += (_, _) => { HideCancelBtn(); RequestUpdate(); };

        // Individual reset icons
        ResetHueBtn.Click += (_, _) => HueSlider.Value = 0;
        ResetSaturationBtn.Click += (_, _) => SaturationSlider.Value = 0;
        ResetTemperatureBtn.Click += (_, _) => TemperatureSlider.Value = 0;
        ResetTintBtn.Click += (_, _) => TintSlider.Value = 0;
        ResetVibranceBtn.Click += (_, _) => VibranceSlider.Value = 0;

        ResetExposureBtn.Click += (_, _) => ExposureSlider.Value = 0;
        ResetBrightnessBtn.Click += (_, _) => BrightnessSlider.Value = 0;
        ResetContrastBtn.Click += (_, _) => ContrastSlider.Value = 0;
        ResetGammaBtn.Click += (_, _) => GammaSlider.Value = 1;

        ResetHighlightsBtn.Click += (_, _) => HighlightsSlider.Value = 0;
        ResetShadowsBtn.Click += (_, _) => ShadowsSlider.Value = 0;
        ResetBlacksBtn.Click += (_, _) => BlacksSlider.Value = 0;
        ResetWhitesBtn.Click += (_, _) => WhitesSlider.Value = 0;

        ResetDehazeBtn.Click += (_, _) => DehazeSlider.Value = 0;
        ResetClarityBtn.Click += (_, _) => ClaritySlider.Value = 0;
        ResetGrainBtn.Click += (_, _) => GrainSlider.Value = 0;
        ResetSharpenBtn.Click += (_, _) => SharpenSlider.Value = 0;
        ResetVignetteBtn.Click += (_, _) => VignetteSlider.Value = 0;

        ResetBlurBtn.Click += (_, _) => BlurSlider.Value = 0;
        ResetPencilSketchBtn.Click += (_, _) => PencilSketchSlider.Value = 0;
        ResetPosterizeBtn.Click += (_, _) => PosterizeSlider.Value = 0;
        ResetSolarizeBtn.Click += (_, _) => SolarizeSlider.Value = 0;
    }

    private void InitControlHooks()
    {
        // Sliders (ValueChanged -> debounce update)
        Hook(HueSlider);
        Hook(SaturationSlider);
        Hook(TemperatureSlider);
        Hook(TintSlider);
        Hook(VibranceSlider);

        Hook(ExposureSlider);
        Hook(BrightnessSlider);
        Hook(ContrastSlider);
        Hook(GammaSlider);

        Hook(HighlightsSlider);
        Hook(ShadowsSlider);
        Hook(BlacksSlider);
        Hook(WhitesSlider);

        Hook(DehazeSlider);
        Hook(ClaritySlider);
        Hook(GrainSlider);
        Hook(SharpenSlider);
        Hook(VignetteSlider);

        Hook(BlurSlider);
        Hook(PencilSketchSlider);
        Hook(PosterizeSlider);
        Hook(SolarizeSlider);

        Hook(CBShadowsCR); Hook(CBShadowsMG); Hook(CBShadowsYB);
        Hook(CBMidCR);    Hook(CBMidMG);     Hook(CBMidYB);
        Hook(CBHighCR);   Hook(CBHighMG);    Hook(CBHighYB);

        // Toggle buttons (Click -> update)
        BlackAndWhiteToggleButton.Click += (_, _) =>
        {
            if (_reloading)
            {
                return;
            }
            HideCancelBtn();
            RequestUpdate();
        };

        NegativeToggleButton.Click += (_, _) =>
        {
            if (_reloading) return;
            HideCancelBtn();
            RequestUpdate();
        };

        OldMovieToggleButton.Click += (_, _) =>
        {
            if (_reloading) return;
            HideCancelBtn();
            RequestUpdate();
        };

        PresetCombo.SelectionChanged += (_, _) =>
        {
            if (_reloading)
            {
                return;
            }
            if (PresetCombo.SelectedItem is not EffectPreset p)
            {
                return;
            }

            _reloading = true;
            try
            {
                p.Apply();
            }
            finally
            {
                _reloading = false;
            }

            HideCancelBtn();
            RequestUpdate();
        };
    }

    private void Hook(RangeBase slider)
    {
        Observable.FromEventHandler<RangeBaseValueChangedEventArgs>(h => slider.ValueChanged += h, h => slider.ValueChanged -= h)
            .Subscribe(_ =>
            {
                if (_reloading) return;
                HideCancelBtn();
                RequestUpdate();
            })
            .AddTo(ref _disposables);
    }

    private void InitPipeline(EffectsViewModel effectsViewModel)
    {
        _changes
            .Debounce(_debounceTime)
            .Select(_ =>
            {
                if (_reloading)
                {
                    return (Config: null, Vm: effectsViewModel);
                }
        
                var config = effectsViewModel.EffectConfig.Value ??= new ImageEffectConfig();
                ReadUiIntoConfig(config);
                return (Config: config, Vm: effectsViewModel);
            })
            .SelectAwait(async (state, ct) =>
            {
                if (state.Config is null)
                    return (Magick: null, state.Vm);
        
                state.Vm.IsLoading.Value = true;

                if (DataContext is not CoreViewModel core)
                {
                    return (Magick: null, state.Vm);
                }
        
                var magick = await ImageEffectsHelper.ApplyEffects(
                    core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo,
                    state.Config,
                    ct);
        
                return (Magick: magick, state.Vm);
            })
            .Subscribe(result =>
            {
                var (magick, viewModel) = result;
                if (DataContext is not CoreViewModel core)
                {
                    return;
                }
        
                if (magick is not null)
                {
                    core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.Value.Image.Value = magick.ToWriteableBitmap();
                    viewModel.ProcessedImage = magick;
                }
        
                viewModel.IsLoading.Value = false;
            }, DebugHelper.LogError(nameof(EffectsView), nameof(InitPipeline)))
            .AddTo(ref _disposables);
    }

    private void RequestUpdate() => _changes.OnNext(Unit.Default);

    private void ReadUiIntoConfig(ImageEffectConfig c)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            // Color
            c.HueDegrees = HueSlider.Value;
            c.Saturation = new Percentage(SaturationSlider.Value);
            c.Temperature = (int)TemperatureSlider.Value;
            c.Tint = (int)TintSlider.Value;
            c.Vibrance = (int)VibranceSlider.Value;

            // Lighting
            c.ExposureStops = ExposureSlider.Value;
            c.Brightness = new Percentage(BrightnessSlider.Value);
            c.Contrast = new Percentage(ContrastSlider.Value);
            c.Gamma = GammaSlider.Value;

            c.Highlights = (int)HighlightsSlider.Value;
            c.Shadows = (int)ShadowsSlider.Value;
            c.Blacks = (int)BlacksSlider.Value;
            c.Whites = (int)WhitesSlider.Value;

            // Effects
            c.Dehaze = (int)DehazeSlider.Value;
            c.Clarity = (int)ClaritySlider.Value;
            c.Grain = (int)GrainSlider.Value;
            c.Sharpen = SharpenSlider.Value;
            c.Vignette = VignetteSlider.Value;

            // Existing
            c.BlurLevel = BlurSlider.Value;
            c.SketchStrokeWidth = PencilSketchSlider.Value;
            c.PosterizeLevel = (int)PosterizeSlider.Value == 1 ? 2 : (int)PosterizeSlider.Value;
            c.Solarize = new Percentage(SolarizeSlider.Value);

            c.BlackAndWhite = BlackAndWhiteToggleButton.IsChecked ?? false;
            c.Negative = NegativeToggleButton.IsChecked ?? false;
            c.OldMovie = OldMovieToggleButton.IsChecked ?? false;

            // Color Balance (Shadows/Mids/Highs)
            c.CBShadows = new ColorBalanceTriplet((int)CBShadowsCR.Value, (int)CBShadowsMG.Value, (int)CBShadowsYB.Value);
            c.CBMidtones = new ColorBalanceTriplet((int)CBMidCR.Value, (int)CBMidMG.Value, (int)CBMidYB.Value);
            c.CBHighlights = new ColorBalanceTriplet((int)CBHighCR.Value, (int)CBHighMG.Value, (int)CBHighYB.Value);
        });
    }

    private void ApplyConfigToUi(ImageEffectConfig c)
    {
        _reloading = true;
        try
        {
            HueSlider.Value = c.HueDegrees;
            SaturationSlider.Value = c.Saturation.ToInt32();
            TemperatureSlider.Value = c.Temperature;
            TintSlider.Value = c.Tint;
            VibranceSlider.Value = c.Vibrance;

            ExposureSlider.Value = c.ExposureStops;
            BrightnessSlider.Value = c.Brightness.ToInt32();
            ContrastSlider.Value = c.Contrast.ToInt32();
            GammaSlider.Value = c.Gamma <= 0 ? 1 : c.Gamma;

            HighlightsSlider.Value = c.Highlights;
            ShadowsSlider.Value = c.Shadows;
            BlacksSlider.Value = c.Blacks;
            WhitesSlider.Value = c.Whites;

            DehazeSlider.Value = c.Dehaze;
            ClaritySlider.Value = c.Clarity;
            GrainSlider.Value = c.Grain;
            SharpenSlider.Value = c.Sharpen;
            VignetteSlider.Value = c.Vignette;

            BlurSlider.Value = c.BlurLevel;
            PencilSketchSlider.Value = c.SketchStrokeWidth;
            PosterizeSlider.Value = c.PosterizeLevel;
            SolarizeSlider.Value = c.Solarize.ToInt32();

            BlackAndWhiteToggleButton.IsChecked = c.BlackAndWhite;
            NegativeToggleButton.IsChecked = c.Negative;
            OldMovieToggleButton.IsChecked = c.OldMovie;

            CBShadowsCR.Value = c.CBShadows.CyanRed;
            CBShadowsMG.Value = c.CBShadows.MagentaGreen;
            CBShadowsYB.Value = c.CBShadows.YellowBlue;

            CBMidCR.Value = c.CBMidtones.CyanRed;
            CBMidMG.Value = c.CBMidtones.MagentaGreen;
            CBMidYB.Value = c.CBMidtones.YellowBlue;

            CBHighCR.Value = c.CBHighlights.CyanRed;
            CBHighMG.Value = c.CBHighlights.MagentaGreen;
            CBHighYB.Value = c.CBHighlights.YellowBlue;
        }
        finally
        {
            _reloading = false;
        }
    }

    private void InitPresets()
    {
        _presets.Clear();

        // Smart presets: avoid aggressive vignette, keep safe, fast and pleasant
        _presets.AddRange([
            new EffectPreset(TranslationManager.Translation.Normal, ResetAllUi),
            new EffectPreset(TranslationManager.Translation.VividPop, () =>
            {
                ResetAllUi();
                VibranceSlider.Value = 25;
                SaturationSlider.Value = 10;
                ContrastSlider.Value = 10;
                ClaritySlider.Value = 12;
                DehazeSlider.Value = 8;
            }),
            new EffectPreset(TranslationManager.Translation.SoftMatte, () =>
            {
                ResetAllUi();
                ContrastSlider.Value = -12;
                GammaSlider.Value = 1.12;
                BlacksSlider.Value = 8;
                HighlightsSlider.Value = -10;
                VignetteSlider.Value = 10;
                GrainSlider.Value = 10;
            }),
            new EffectPreset(TranslationManager.Translation.HighContrastBW, () =>
            {
                ResetAllUi();
                BlackAndWhiteToggleButton.IsChecked = true;
                ContrastSlider.Value = 28;
                ExposureSlider.Value = 0.25;
                HighlightsSlider.Value = -15;
                ShadowsSlider.Value = 15;
                ClaritySlider.Value = 15;
                VignetteSlider.Value = 12;
            }),
            new EffectPreset(TranslationManager.Translation.OldMovie, () =>
            {
                ResetAllUi();
                OldMovieToggleButton.IsChecked = true;
                ContrastSlider.Value = 8;
                GrainSlider.Value = 25;
                VignetteSlider.Value = 15;
            }),
            new EffectPreset(TranslationManager.Translation.PortraitSoft, () =>
            {
                ResetAllUi();
                ExposureSlider.Value = 0.15;
                HighlightsSlider.Value = -10;
                ShadowsSlider.Value = 12;
                ClaritySlider.Value = 6;
                SharpenSlider.Value = 6;
                VibranceSlider.Value = 8;
            }),
            new EffectPreset(TranslationManager.Translation.LandscapeCrisp, () =>
            {
                ResetAllUi();
                DehazeSlider.Value = 12;
                ClaritySlider.Value = 18;
                SharpenSlider.Value = 14;
                VibranceSlider.Value = 18;
                HighlightsSlider.Value = -10;
                ShadowsSlider.Value = 10;
            })
        ]);

        PresetCombo.ItemsSource = _presets;
        PresetCombo.SelectedIndex = 0;
    }

    private void ResetAllUiAndConfig()
    {
        _reloading = true;
        try
        {
            ResetAllUi();
            if (DataContext is not CoreViewModel core)
            {
                return;
            }
            core.Effects?.EffectConfig.Value = new ImageEffectConfig();
        }
        finally
        {
            _reloading = false;
        }
    }

    private void ResetAllUi()
    {
        ResetColor();
        ResetLighting();
        ResetEffects();
        
        Dispatcher.UIThread.Invoke(() =>
        {
            // classic
            BlurSlider.Value = 0;
            PencilSketchSlider.Value = 0;
            PosterizeSlider.Value = 0;
            SolarizeSlider.Value = 0;

            BlackAndWhiteToggleButton.IsChecked = false;
            NegativeToggleButton.IsChecked = false;
            OldMovieToggleButton.IsChecked = false;
        });
    }

    private void ResetColor()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            HueSlider.Value = 0;
            SaturationSlider.Value = 0;
            TemperatureSlider.Value = 0;
            TintSlider.Value = 0;
            VibranceSlider.Value = 0;

            CBShadowsCR.Value = 0; CBShadowsMG.Value = 0; CBShadowsYB.Value = 0;
            CBMidCR.Value = 0; CBMidMG.Value = 0; CBMidYB.Value = 0;
            CBHighCR.Value = 0; CBHighMG.Value = 0; CBHighYB.Value = 0;
        });
    }

    private void ResetLighting()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            ExposureSlider.Value = 0;
            BrightnessSlider.Value = 0;
            ContrastSlider.Value = 0;
            GammaSlider.Value = 1;

            HighlightsSlider.Value = 0;
            ShadowsSlider.Value = 0;
            BlacksSlider.Value = 0;
            WhitesSlider.Value = 0;
        });
    }

    private void ResetEffects()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            DehazeSlider.Value = 0;
            ClaritySlider.Value = 0;
            GrainSlider.Value = 0;
            SharpenSlider.Value = 0;
            VignetteSlider.Value = 0;
        });
    }

    public async Task RemoveEffects()
    {
        _reloading = true;
        try
        {
            if (DataContext is not CoreViewModel core)
            {
                return;
            }
            await core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ReloadAsync().ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ResetAllUiAndConfig();
                HideResetBtn();
            });
        }
        finally
        {
            await Task.Delay(_debounceTime);
            _reloading = false;
        }
    }

    private void HideResetBtn()
    {
        ResetButton.IsVisible = false;
        CancelButton.IsVisible = true;
    }

    private void HideCancelBtn()
    {
        if (_reloading) return;
        ResetButton.IsVisible = true;
        CancelButton.IsVisible = false;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            ContextMenu.Open();
    }

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        _disposables.Dispose();
    }

    private bool IsDefault(ImageEffectConfig c)
    {
        return c.Brightness == _default.Brightness &&
               c.Contrast == _default.Contrast &&
               c.Solarize == _default.Solarize &&
               c.SketchStrokeWidth == _default.SketchStrokeWidth &&
               c.PosterizeLevel == _default.PosterizeLevel &&
               c.BlurLevel == _default.BlurLevel &&
               c.BlackAndWhite == _default.BlackAndWhite &&
               c.Negative == _default.Negative &&
               c.OldMovie == _default.OldMovie &&

               c.HueDegrees == _default.HueDegrees &&
               c.Saturation == _default.Saturation &&
               c.Temperature == _default.Temperature &&
               c.Tint == _default.Tint &&
               c.Vibrance == _default.Vibrance &&

               c.ExposureStops == _default.ExposureStops &&
               c.Gamma == _default.Gamma &&

               c.Highlights == _default.Highlights &&
               c.Shadows == _default.Shadows &&
               c.Blacks == _default.Blacks &&
               c.Whites == _default.Whites &&

               c.Dehaze == _default.Dehaze &&
               c.Clarity == _default.Clarity &&
               c.Grain == _default.Grain &&
               c.Sharpen == _default.Sharpen &&
               c.Vignette == _default.Vignette &&

               c.CBShadows.Equals(_default.CBShadows) &&
               c.CBMidtones.Equals(_default.CBMidtones) &&
               c.CBHighlights.Equals(_default.CBHighlights);
    }

    private sealed class EffectPreset
    {
        public EffectPreset(string name, Action apply) { Name = name; Apply = apply; }
        public string Name { get; }
        public Action Apply { get; }
        public override string ToString() => Name;
    }
}


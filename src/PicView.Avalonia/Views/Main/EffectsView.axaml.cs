using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Extensions;
using PicView.Avalonia.History;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ImageEffects;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class EffectsView : UserControl
{
    private readonly CompositeDisposable _disposables = new();
    private readonly Subject<Unit> _changes = new();

    private readonly ImageEffectConfig _default = new();
    private readonly TimeSpan _debounceTime = TimeSpan.FromMilliseconds(60);

    private bool _reloading;

    private readonly List<EffectPreset> _presets = new();

    public EffectsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DetachedFromLogicalTree += OnDetachedFromLogicalTree;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        vm.PicViewer.EffectConfig.Value ??= new ImageEffectConfig();

        // reset on file change
        Observable.EveryValueChanged(vm.PicViewer.FileInfo, x => x.CurrentValue, UIHelper.GetFrameProvider)
            .Subscribe(_ => ResetAllUiAndConfig(vm))
            .AddTo(_disposables);

        InitButtons(vm);
        InitControlHooks();
        InitPresets();
        InitPipeline(vm);

        // apply existing config to UI if any
        ApplyConfigToUi(vm.PicViewer.EffectConfig.CurrentValue);
        if (!IsDefault(vm.PicViewer.EffectConfig.CurrentValue))
            RequestUpdate();
        else
            HideResetBtn();
    }

    private void InitButtons(MainViewModel vm)
    {
        if (!Settings.Theme.Dark)
        {
            if (TryGetResource("CancelBrush", Application.Current.RequestedThemeVariant, out var cBrush) && cBrush is SolidColorBrush brush)
            {
                UIHelper.SetButtonHover(CancelButton, brush);
                UIHelper.SetButtonHover(ResetButton, brush);
            }

            UIHelper.SwitchAccentHoverClass(ResetButton);
            UIHelper.SwitchAccentHoverClass(CancelButton);
            LowerPanel.Background = new SolidColorBrush(Color.Parse("#5DA2A2A2"));
        }

        PointerPressed += OnPointerPressed;

        // Footer buttons
        ResetButton.Click += async (_, _) => await RemoveEffects();
        CancelButton.Click += (_, _) => (VisualRoot as Window)?.Close();
        ApplyButton.Click += async (_, _) => await CommitToHistoryAndClose();

        // Per-tab reset
        ResetColorTabBtn.Click += (_, _) => { ResetColor(); HideCancelBtn(); RequestUpdate(); };
        ResetLightingTabBtn.Click += (_, _) => { ResetLighting(); HideCancelBtn(); RequestUpdate(); };
        ResetEffectsTabBtn.Click += (_, _) => { ResetEffects(); HideCancelBtn(); RequestUpdate(); };

        ResetAllBtn.Click += (_, _) =>
        {
            ResetAllUiAndConfig(vm);
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
            if (_reloading) return;
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
            if (_reloading) return;
            if (PresetCombo.SelectedItem is not EffectPreset p) return;

            _reloading = true;
            try { p.Apply(); }
            finally { _reloading = false; }

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
            .AddTo(_disposables);
    }

    private void InitPipeline(MainViewModel vm)
    {
        _changes
            .Debounce(_debounceTime)
            .ObserveOn(UIHelper.GetFrameProvider)
            .Select(_ =>
            {
                if (_reloading)
                    return (Config: (ImageEffectConfig?)null, Vm: vm);

                var config = vm.PicViewer.EffectConfig.Value ??= new ImageEffectConfig();
                ReadUiIntoConfig(config);
                return (Config: config, Vm: vm);
            })
            .SelectAwait(async (state, ct) =>
            {
                if (state.Config is null)
                    return (Magick: (MagickImage?)null, state.Vm);

                state.Vm.MainWindow.IsLoadingIndicatorShown.Value = true;

                var magick = await ImageEffectsHelper.ApplyEffects(
                    state.Vm.PicViewer.FileInfo.CurrentValue,
                    state.Config,
                    ct);

                return (Magick: magick, state.Vm);
            })
            .Subscribe(result =>
            {
                var (magick, viewModel) = result;

                if (magick is not null)
                    viewModel.PicViewer.ImageSource.Value = magick.ToWriteableBitmap();

                viewModel.MainWindow.IsLoadingIndicatorShown.Value = false;
            })
            .AddTo(_disposables);
    }

    private void RequestUpdate() => _changes.OnNext(Unit.Default);

    private void ReadUiIntoConfig(ImageEffectConfig c)
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
        _presets.AddRange(new[]
        {
            new EffectPreset("Neutral", () => { ResetAllUi(); }),
            new EffectPreset("Vivid Pop", () =>
            {
                ResetAllUi();
                VibranceSlider.Value = 25;
                SaturationSlider.Value = 10;
                ContrastSlider.Value = 10;
                ClaritySlider.Value = 12;
                DehazeSlider.Value = 8;
            }),
            new EffectPreset("Soft Matte", () =>
            {
                ResetAllUi();
                ContrastSlider.Value = -12;
                GammaSlider.Value = 1.12;
                BlacksSlider.Value = 8;
                HighlightsSlider.Value = -10;
                VignetteSlider.Value = 10;
                GrainSlider.Value = 10;
            }),
            new EffectPreset("High Contrast B&W", () =>
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
            new EffectPreset("Old Movie", () =>
            {
                ResetAllUi();
                OldMovieToggleButton.IsChecked = true;
                ContrastSlider.Value = 8;
                GrainSlider.Value = 25;
                VignetteSlider.Value = 15;
            }),
            new EffectPreset("Portrait Soft", () =>
            {
                ResetAllUi();
                ExposureSlider.Value = 0.15;
                HighlightsSlider.Value = -10;
                ShadowsSlider.Value = 12;
                ClaritySlider.Value = 6;
                SharpenSlider.Value = 6;
                VibranceSlider.Value = 8;
            }),
            new EffectPreset("Landscape Crisp", () =>
            {
                ResetAllUi();
                DehazeSlider.Value = 12;
                ClaritySlider.Value = 18;
                SharpenSlider.Value = 14;
                VibranceSlider.Value = 18;
                HighlightsSlider.Value = -10;
                ShadowsSlider.Value = 10;
            })
        });

        PresetCombo.ItemsSource = _presets;
        PresetCombo.SelectedIndex = 0;

        ApplyPresetBtn.Click += (_, _) =>
        {
            if (PresetCombo.SelectedItem is not EffectPreset p) return;

            _reloading = true;
            try { p.Apply(); }
            finally { _reloading = false; }

            HideCancelBtn();
            RequestUpdate();
        };
    }

    private void ResetAllUiAndConfig(MainViewModel vm)
    {
        _reloading = true;
        try
        {
            ResetAllUi();
            vm.PicViewer.EffectConfig.Value = new ImageEffectConfig();
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

        // classic
        BlurSlider.Value = 0;
        PencilSketchSlider.Value = 0;
        PosterizeSlider.Value = 0;
        SolarizeSlider.Value = 0;

        BlackAndWhiteToggleButton.IsChecked = false;
        NegativeToggleButton.IsChecked = false;
        OldMovieToggleButton.IsChecked = false;
    }

    private void ResetColor()
    {
        HueSlider.Value = 0;
        SaturationSlider.Value = 0;
        TemperatureSlider.Value = 0;
        TintSlider.Value = 0;
        VibranceSlider.Value = 0;

        CBShadowsCR.Value = 0; CBShadowsMG.Value = 0; CBShadowsYB.Value = 0;
        CBMidCR.Value = 0; CBMidMG.Value = 0; CBMidYB.Value = 0;
        CBHighCR.Value = 0; CBHighMG.Value = 0; CBHighYB.Value = 0;
    }

    private void ResetLighting()
    {
        ExposureSlider.Value = 0;
        BrightnessSlider.Value = 0;
        ContrastSlider.Value = 0;
        GammaSlider.Value = 1;

        HighlightsSlider.Value = 0;
        ShadowsSlider.Value = 0;
        BlacksSlider.Value = 0;
        WhitesSlider.Value = 0;
    }

    private void ResetEffects()
    {
        DehazeSlider.Value = 0;
        ClaritySlider.Value = 0;
        GrainSlider.Value = 0;
        SharpenSlider.Value = 0;
        VignetteSlider.Value = 0;
    }

    public async Task RemoveEffects()
    {
        _reloading = true;
        try
        {
            await NavigationManager.QuickReload().ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (DataContext is MainViewModel vm)
                    ResetAllUiAndConfig(vm);

                HideResetBtn();
            });
        }
        finally
        {
            await Task.Delay(_debounceTime);
            _reloading = false;
        }
    }

    private async Task CommitToHistoryAndClose()
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
            return;

        if (vm.PicViewer.ImageSource.Value is not Bitmap bmp)
            return;

        if (vm.PicViewer.EffectConfig.Value is null)
            return;

        await using (DebouncedLoadingScope.Start(vm.MainWindow.IsLoadingIndicatorShown, 50))
        {
            var config = vm.PicViewer.EffectConfig.CurrentValue;
            var desc = BuildEffectDescription(config);

            await vm.HistoryManager.AddSnapshot(EditKind.Effect, desc, bmp).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => (VisualRoot as Window)?.Close());
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

        if (DataContext is MainViewModel vm)
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
    }

    private static string BuildEffectDescription(ImageEffectConfig c)
    {
        var parts = new List<string>();

        if (c.ExposureStops != 0) parts.Add($"Exposure {c.ExposureStops:+0.##;-0.##;0}");
        if (c.Brightness.ToInt32() != 0) parts.Add($"Brightness {c.Brightness.ToInt32()}");
        if (c.Contrast.ToInt32() != 0) parts.Add($"Contrast {c.Contrast.ToInt32()}");
        if (c.Gamma is > 0 and not 1) parts.Add($"Gamma {c.Gamma:0.##}");

        if (c.Highlights != 0) parts.Add($"Highlights {c.Highlights:+0;-0;0}");
        if (c.Shadows != 0) parts.Add($"Shadows {c.Shadows:+0;-0;0}");
        if (c.Blacks != 0) parts.Add($"Blacks {c.Blacks:+0;-0;0}");
        if (c.Whites != 0) parts.Add($"Whites {c.Whites:+0;-0;0}");

        if (c.HueDegrees != 0) parts.Add($"Hue {c.HueDegrees:+0;-0;0}");
        if (c.Saturation.ToInt32() != 0) parts.Add($"Saturation {c.Saturation.ToInt32()}");
        if (c.Temperature != 0) parts.Add($"Temp {c.Temperature:+0;-0;0}");
        if (c.Tint != 0) parts.Add($"Tint {c.Tint:+0;-0;0}");
        if (c.Vibrance != 0) parts.Add($"Vibrance {c.Vibrance:+0;-0;0}");

        if (c.Dehaze != 0) parts.Add($"Dehaze {c.Dehaze}");
        if (c.Clarity != 0) parts.Add($"Clarity {c.Clarity}");
        if (c.Grain != 0) parts.Add($"Grain {c.Grain}");
        if (c.Sharpen > 0) parts.Add($"Sharpen {c.Sharpen:0}");
        if (c.Vignette > 0) parts.Add($"Vignette {c.Vignette:0}");

        if (c.BlurLevel > 0) parts.Add($"Blur {c.BlurLevel:0}");
        if (c.PosterizeLevel > 0) parts.Add($"Posterize {c.PosterizeLevel}");
        if (c.SketchStrokeWidth > 0) parts.Add($"Sketch {c.SketchStrokeWidth:0.##}");
        if (c.Solarize.ToInt32() > 0) parts.Add($"Solarize {c.Solarize.ToInt32()}");

        if (c.BlackAndWhite) parts.Add("B&W");
        if (c.Negative) parts.Add("Negative");
        if (c.OldMovie) parts.Add("Old Movie");

        return parts.Count == 0 ? "Effect: none" : "Effect: " + string.Join(", ", parts);
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

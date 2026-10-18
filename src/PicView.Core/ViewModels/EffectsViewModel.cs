using ImageMagick;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.ImageEffects;
using PicView.Core.Localization;
using R3;

namespace PicView.Core.ViewModels;

public class EffectsViewModel : IDisposable
{
    private DisposableBag _disposables;
    private readonly TimeSpan _debounceTime = TimeSpan.FromMilliseconds(60);
    private readonly Func<ImageEffectConfig, CancellationToken, Task<MagickImage?>> _render;
    private readonly Subject<MagickImage> _rendered = new();
    private int _discards;
    private bool _isDiscarding;
    public BindableReactiveProperty<ImageEffectConfig?> EffectConfig { get; } = new();
    public EffectPreset[] Presets { get; }
    public BindableReactiveProperty<EffectPreset?> SelectedPreset { get; } = new();
    public BindableReactiveProperty<bool> IsResetVisible { get; } = new(false);
    /// <summary>
    /// The shown file's image, with effects applied once there are any. <see langword="null"/> when the shown image
    /// is not a file, such as a clipboard image.
    /// </summary>
    public MagickImage? ProcessedImage { get; set; }
    public BindableReactiveProperty<bool> IsLoading { get; } = new(false);

    /// <summary>
    /// Emits each image rendered with the current effects, on a background thread.
    /// </summary>
    public Observable<MagickImage> Rendered => _rendered;
    public EffectsWindowConfig? WindowConfig { get; set; }

    #region Properties

    // Color
    public BindableReactiveProperty<double> HueDegrees { get; } = new(0);
    public BindableReactiveProperty<double> Saturation { get; } = new(0);
    public BindableReactiveProperty<int> Temperature { get; } = new(0);
    public BindableReactiveProperty<int> Tint { get; } = new(0);
    public BindableReactiveProperty<int> Vibrance { get; } = new(0);

    // Lighting
    public BindableReactiveProperty<double> ExposureStops { get; } = new(0);
    public BindableReactiveProperty<double> Brightness { get; } = new(0);
    public BindableReactiveProperty<double> Contrast { get; } = new(0);
    public BindableReactiveProperty<double> Gamma { get; } = new(1);

    // Tonal
    public BindableReactiveProperty<int> Highlights { get; } = new(0);
    public BindableReactiveProperty<int> Shadows { get; } = new(0);
    public BindableReactiveProperty<int> Blacks { get; } = new(0);
    public BindableReactiveProperty<int> Whites { get; } = new(0);

    // Effects
    public BindableReactiveProperty<int> Dehaze { get; } = new(0);
    public BindableReactiveProperty<int> Clarity { get; } = new(0);
    public BindableReactiveProperty<int> Grain { get; } = new(0);
    public BindableReactiveProperty<double> Sharpen { get; } = new(0);
    public BindableReactiveProperty<double> Vignette { get; } = new(0);

    // Existing/Classic
    public BindableReactiveProperty<double> BlurLevel { get; } = new(0);
    public BindableReactiveProperty<double> SketchStrokeWidth { get; } = new(0);
    public BindableReactiveProperty<int> PosterizeLevel { get; } = new(0);
    public BindableReactiveProperty<double> Solarize { get; } = new(0);

    public BindableReactiveProperty<bool> BlackAndWhite { get; } = new(false);
    public BindableReactiveProperty<bool> Negative { get; } = new(false);
    public BindableReactiveProperty<bool> OldMovie { get; } = new(false);

    // Color Balance
    public BindableReactiveProperty<int> CBShadowsCR { get; } = new(0);
    public BindableReactiveProperty<int> CBShadowsMG { get; } = new(0);
    public BindableReactiveProperty<int> CBShadowsYB { get; } = new(0);

    public BindableReactiveProperty<int> CBMidCR { get; } = new(0);
    public BindableReactiveProperty<int> CBMidMG { get; } = new(0);
    public BindableReactiveProperty<int> CBMidYB { get; } = new(0);

    public BindableReactiveProperty<int> CBHighCR { get; } = new(0);
    public BindableReactiveProperty<int> CBHighMG { get; } = new(0);
    public BindableReactiveProperty<int> CBHighYB { get; } = new(0);

    #endregion

    public EffectsViewModel(Func<ImageEffectConfig, CancellationToken, Task<MagickImage?>> render)
    {
        _render = render;
        Presets = CreatePresets();
        SelectedPreset.Value = Presets[0];
        SelectedPreset
            .Skip(1)
            .WhereNotNull()
            .Subscribe(preset => preset.Apply())
            .AddTo(ref _disposables);
        InitPipeline();
    }

    private Observable<Unit> ValueChanges() =>
        Observable.Merge(
            HueDegrees.Skip(1).AsUnitObservable(),
            Saturation.Skip(1).AsUnitObservable(),
            Temperature.Skip(1).AsUnitObservable(),
            Tint.Skip(1).AsUnitObservable(),
            Vibrance.Skip(1).AsUnitObservable(),
            ExposureStops.Skip(1).AsUnitObservable(),
            Brightness.Skip(1).AsUnitObservable(),
            Contrast.Skip(1).AsUnitObservable(),
            Gamma.Skip(1).AsUnitObservable(),
            Highlights.Skip(1).AsUnitObservable(),
            Shadows.Skip(1).AsUnitObservable(),
            Blacks.Skip(1).AsUnitObservable(),
            Whites.Skip(1).AsUnitObservable(),
            Dehaze.Skip(1).AsUnitObservable(),
            Clarity.Skip(1).AsUnitObservable(),
            Grain.Skip(1).AsUnitObservable(),
            Sharpen.Skip(1).AsUnitObservable(),
            Vignette.Skip(1).AsUnitObservable(),
            BlurLevel.Skip(1).AsUnitObservable(),
            SketchStrokeWidth.Skip(1).AsUnitObservable(),
            PosterizeLevel.Skip(1).AsUnitObservable(),
            Solarize.Skip(1).AsUnitObservable(),
            BlackAndWhite.Skip(1).AsUnitObservable(),
            Negative.Skip(1).AsUnitObservable(),
            OldMovie.Skip(1).AsUnitObservable(),
            CBShadowsCR.Skip(1).AsUnitObservable(),
            CBShadowsMG.Skip(1).AsUnitObservable(),
            CBShadowsYB.Skip(1).AsUnitObservable(),
            CBMidCR.Skip(1).AsUnitObservable(),
            CBMidMG.Skip(1).AsUnitObservable(),
            CBMidYB.Skip(1).AsUnitObservable(),
            CBHighCR.Skip(1).AsUnitObservable(),
            CBHighMG.Skip(1).AsUnitObservable(),
            CBHighYB.Skip(1).AsUnitObservable());

    private void InitPipeline()
    {
        ValueChanges()
            .Subscribe(_ => IsResetVisible.Value = !IsDefault())
            .AddTo(ref _disposables);

        ValueChanges()
            .Where(_ => !_isDiscarding)
            .Select(_ => Volatile.Read(ref _discards))
            .Debounce(_debounceTime)
            .Where(discards => discards == Volatile.Read(ref _discards))
            .SelectAwait(async (discards, ct) =>
            {
                IsLoading.Value = true;
                var config = ToConfig();
                EffectConfig.Value = config;
                var magick = await _render(config, ct).ConfigureAwait(false);
                return (Magick: magick, Discards: discards);
            }, AwaitOperation.Switch)
            .Subscribe(result =>
            {
                if (result.Discards != Volatile.Read(ref _discards))
                {
                    result.Magick?.Dispose();
                }
                else if (result.Magick is not null)
                {
                    ProcessedImage = result.Magick;
                    _rendered.OnNext(result.Magick);
                }
                IsLoading.Value = false;
            }, DebugHelper.LogError(nameof(EffectsViewModel), nameof(InitPipeline)))
            .AddTo(ref _disposables);
    }

    public bool IsDefault()
    {
        return Brightness.Value == 0 &&
               Contrast.Value == 0 &&
               Solarize.Value == 0 &&
               SketchStrokeWidth.Value == 0 &&
               PosterizeLevel.Value == 0 &&
               BlurLevel.Value == 0 &&
               !BlackAndWhite.Value &&
               !Negative.Value &&
               !OldMovie.Value &&

               HueDegrees.Value == 0 &&
               Saturation.Value == 0 &&
               Temperature.Value == 0 &&
               Tint.Value == 0 &&
               Vibrance.Value == 0 &&

               ExposureStops.Value == 0 &&
               Gamma.Value == 1 &&

               Highlights.Value == 0 &&
               Shadows.Value == 0 &&
               Blacks.Value == 0 &&
               Whites.Value == 0 &&

               Dehaze.Value == 0 &&
               Clarity.Value == 0 &&
               Grain.Value == 0 &&
               Sharpen.Value == 0 &&
               Vignette.Value == 0 &&

               CBShadowsCR.Value == 0 && CBShadowsMG.Value == 0 && CBShadowsYB.Value == 0 &&
               CBMidCR.Value == 0 && CBMidMG.Value == 0 && CBMidYB.Value == 0 &&
               CBHighCR.Value == 0 && CBHighMG.Value == 0 && CBHighYB.Value == 0;
    }

    private ImageEffectConfig ToConfig()
    {
        var c = new ImageEffectConfig();
        c.HueDegrees = HueDegrees.Value;
        c.Saturation = new Percentage(Saturation.Value);
        c.Temperature = Temperature.Value;
        c.Tint = Tint.Value;
        c.Vibrance = Vibrance.Value;

        c.ExposureStops = ExposureStops.Value;
        c.Brightness = new Percentage(Brightness.Value);
        c.Contrast = new Percentage(Contrast.Value);
        c.Gamma = Gamma.Value;

        c.Highlights = Highlights.Value;
        c.Shadows = Shadows.Value;
        c.Blacks = Blacks.Value;
        c.Whites = Whites.Value;

        c.Dehaze = Dehaze.Value;
        c.Clarity = Clarity.Value;
        c.Grain = Grain.Value;
        c.Sharpen = Sharpen.Value;
        c.Vignette = Vignette.Value;

        c.BlurLevel = BlurLevel.Value;
        c.SketchStrokeWidth = SketchStrokeWidth.Value;
        c.PosterizeLevel = PosterizeLevel.Value == 1 ? 2 : PosterizeLevel.Value;
        c.Solarize = new Percentage(Solarize.Value);

        c.BlackAndWhite = BlackAndWhite.Value;
        c.Negative = Negative.Value;
        c.OldMovie = OldMovie.Value;

        c.CBShadows = new ColorBalanceTriplet(CBShadowsCR.Value, CBShadowsMG.Value, CBShadowsYB.Value);
        c.CBMidtones = new ColorBalanceTriplet(CBMidCR.Value, CBMidMG.Value, CBMidYB.Value);
        c.CBHighlights = new ColorBalanceTriplet(CBHighCR.Value, CBHighMG.Value, CBHighYB.Value);
        return c;
    }

    private EffectPreset[] CreatePresets()
    {
        var t = TranslationManager.Translation;
        return
        [
            new EffectPreset(t.Normal, ResetAll),
            new EffectPreset(t.VividPop, () =>
            {
                ResetAll();
                Vibrance.Value = 25;
                Saturation.Value = 10;
                Contrast.Value = 10;
                Clarity.Value = 12;
                Dehaze.Value = 8;
            }),
            new EffectPreset(t.SoftMatte, () =>
            {
                ResetAll();
                Contrast.Value = -12;
                Gamma.Value = 1.12;
                Blacks.Value = 8;
                Highlights.Value = -10;
                Vignette.Value = 10;
                Grain.Value = 10;
            }),
            new EffectPreset(t.HighContrastBW, () =>
            {
                ResetAll();
                BlackAndWhite.Value = true;
                Contrast.Value = 28;
                ExposureStops.Value = 0.25;
                Highlights.Value = -15;
                Shadows.Value = 15;
                Clarity.Value = 15;
                Vignette.Value = 12;
            }),
            new EffectPreset(t.OldMovie, () =>
            {
                ResetAll();
                OldMovie.Value = true;
                Contrast.Value = 8;
                Grain.Value = 25;
                Vignette.Value = 15;
            }),
            new EffectPreset(t.PortraitSoft, () =>
            {
                ResetAll();
                ExposureStops.Value = 0.15;
                Highlights.Value = -10;
                Shadows.Value = 12;
                Clarity.Value = 6;
                Sharpen.Value = 6;
                Vibrance.Value = 8;
            }),
            new EffectPreset(t.LandscapeCrisp, () =>
            {
                ResetAll();
                Dehaze.Value = 12;
                Clarity.Value = 18;
                Sharpen.Value = 14;
                Vibrance.Value = 18;
                Highlights.Value = -10;
                Shadows.Value = 10;
            }),
        ];
    }

    /// <summary>
    /// Resets every value without rendering, and drops any render still pending, such as when the shown image
    /// changes or is reloaded without effects.
    /// </summary>
    public void DiscardEffects()
    {
        Interlocked.Increment(ref _discards);
        _isDiscarding = true;
        try
        {
            ResetAll();
        }
        finally
        {
            _isDiscarding = false;
        }
        EffectConfig.Value = new ImageEffectConfig();
    }

    public void ResetAll()
    {
        ResetColor();
        ResetLighting();
        ResetEffects();

        BlurLevel.Value = 0;
        SketchStrokeWidth.Value = 0;
        PosterizeLevel.Value = 0;
        Solarize.Value = 0;

        BlackAndWhite.Value = false;
        Negative.Value = false;
        OldMovie.Value = false;
        SelectedPreset.Value = Presets[0];
    }

    public void ResetColor()
    {
        HueDegrees.Value = 0;
        Saturation.Value = 0;
        Temperature.Value = 0;
        Tint.Value = 0;
        Vibrance.Value = 0;

        ResetCBShadows();
        ResetCBMidtones();
        ResetCBHighlights();
    }

    public void ResetLighting()
    {
        ExposureStops.Value = 0;
        Brightness.Value = 0;
        Contrast.Value = 0;
        Gamma.Value = 1;

        Highlights.Value = 0;
        Shadows.Value = 0;
        Blacks.Value = 0;
        Whites.Value = 0;
    }

    public void ResetEffects()
    {
        Dehaze.Value = 0;
        Clarity.Value = 0;
        Grain.Value = 0;
        Sharpen.Value = 0;
        Vignette.Value = 0;
    }

    // Granular Resets
    public void ResetHue() => HueDegrees.Value = 0;
    public void ResetSaturation() => Saturation.Value = 0;
    public void ResetTemperature() => Temperature.Value = 0;
    public void ResetTint() => Tint.Value = 0;
    public void ResetVibrance() => Vibrance.Value = 0;

    public void ResetExposure() => ExposureStops.Value = 0;
    public void ResetBrightness() => Brightness.Value = 0;
    public void ResetContrast() => Contrast.Value = 0;
    public void ResetGamma() => Gamma.Value = 1;

    public void ResetHighlights() => Highlights.Value = 0;
    public void ResetShadows() => Shadows.Value = 0;
    public void ResetBlacks() => Blacks.Value = 0;
    public void ResetWhites() => Whites.Value = 0;

    public void ResetDehaze() => Dehaze.Value = 0;
    public void ResetClarity() => Clarity.Value = 0;
    public void ResetGrain() => Grain.Value = 0;
    public void ResetSharpen() => Sharpen.Value = 0;
    public void ResetVignette() => Vignette.Value = 0;

    public void ResetBlur() => BlurLevel.Value = 0;
    public void ResetPencilSketch() => SketchStrokeWidth.Value = 0;
    public void ResetPosterize() => PosterizeLevel.Value = 0;
    public void ResetSolarize() => Solarize.Value = 0;
    
    public void ResetCBShadowsCR() => CBShadowsCR.Value = 0;
    public void ResetCBShadowsMG() => CBShadowsMG.Value = 0;
    public void ResetCBShadowsYB() => CBShadowsYB.Value = 0;
    
    public void ResetCBHighCR() => CBHighCR.Value = 0;
    public void ResetCBHighMG() => CBHighMG.Value = 0;
    public void ResetCBHighYB() => CBHighYB.Value = 0;
    
    public void ResetCBMidMG() => CBMidMG.Value = 0;
    public void ResetCBMidYB() => CBMidYB.Value = 0;
    public void ResetCBMidCR() => CBMidCR.Value = 0;

    public void ResetCBShadows()
    {
        CBShadowsCR.Value = 0;
        CBShadowsMG.Value = 0;
        CBShadowsYB.Value = 0;
    }

    public void ResetCBMidtones()
    {
        CBMidCR.Value = 0;
        CBMidMG.Value = 0;
        CBMidYB.Value = 0;
    }

    public void ResetCBHighlights()
    {
        CBHighCR.Value = 0;
        CBHighMG.Value = 0;
        CBHighYB.Value = 0;
    }

    public void Dispose()
    {
        _disposables.Dispose();

        EffectConfig.Dispose();

        HueDegrees.Dispose();
        Saturation.Dispose();
        Temperature.Dispose();
        Tint.Dispose();
        Vibrance.Dispose();

        ExposureStops.Dispose();
        Brightness.Dispose();
        Contrast.Dispose();
        Gamma.Dispose();

        Highlights.Dispose();
        Shadows.Dispose();
        Blacks.Dispose();
        Whites.Dispose();

        Dehaze.Dispose();
        Clarity.Dispose();
        Grain.Dispose();
        Sharpen.Dispose();
        Vignette.Dispose();

        BlurLevel.Dispose();
        SketchStrokeWidth.Dispose();
        PosterizeLevel.Dispose();
        Solarize.Dispose();

        BlackAndWhite.Dispose();
        Negative.Dispose();
        OldMovie.Dispose();

        CBShadowsCR.Dispose();
        CBShadowsMG.Dispose();
        CBShadowsYB.Dispose();

        CBMidCR.Dispose();
        CBMidMG.Dispose();
        CBMidYB.Dispose();

        CBHighCR.Dispose();
        CBHighMG.Dispose();
        CBHighYB.Dispose();

        _rendered.Dispose();
        SelectedPreset.Dispose();
        IsResetVisible.Dispose();
        ProcessedImage?.Dispose();
    }
}

public sealed class EffectPreset(string name, Action apply)
{
    public string Name { get; } = name;
    public Action Apply { get; } = apply;
    public override string ToString() => Name;
}

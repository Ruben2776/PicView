using System.Collections.ObjectModel;
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
    public BindableReactiveProperty<ImageEffectConfig?> EffectConfig { get; } = new();
    public ObservableCollection<EffectPreset> Presets { get; } = [];
    public BindableReactiveProperty<EffectPreset?> SelectedPreset { get; } = new();
    public BindableReactiveProperty<bool> IsResetVisible { get; } = new(false);
    public MagickImage ProcessedImage { get; set; } = new();
    public BindableReactiveProperty<bool> IsLoading { get; } = new(true);
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

    public void Initialize(FileInfo file)
    {
        EffectConfig.Value = new ImageEffectConfig();
        ProcessedImage = new MagickImage(file);
        InitPresets();
        InitPipeline();
    }

    private void InitPipeline()
    {
        Observable.Merge(
            HueDegrees.Select(_ => Unit.Default),
            Saturation.Select(_ => Unit.Default),
            Temperature.Select(_ => Unit.Default),
            Tint.Select(_ => Unit.Default),
            Vibrance.Select(_ => Unit.Default),
            ExposureStops.Select(_ => Unit.Default),
            Brightness.Select(_ => Unit.Default),
            Contrast.Select(_ => Unit.Default),
            Gamma.Select(_ => Unit.Default),
            Highlights.Select(_ => Unit.Default),
            Shadows.Select(_ => Unit.Default),
            Blacks.Select(_ => Unit.Default),
            Whites.Select(_ => Unit.Default),
            Dehaze.Select(_ => Unit.Default),
            Clarity.Select(_ => Unit.Default),
            Grain.Select(_ => Unit.Default),
            Sharpen.Select(_ => Unit.Default),
            Vignette.Select(_ => Unit.Default),
            BlurLevel.Select(_ => Unit.Default),
            SketchStrokeWidth.Select(_ => Unit.Default),
            PosterizeLevel.Select(_ => Unit.Default),
            Solarize.Select(_ => Unit.Default),
            BlackAndWhite.Select(_ => Unit.Default),
            Negative.Select(_ => Unit.Default),
            OldMovie.Select(_ => Unit.Default),
            CBShadowsCR.Select(_ => Unit.Default),
            CBShadowsMG.Select(_ => Unit.Default),
            CBShadowsYB.Select(_ => Unit.Default),
            CBMidCR.Select(_ => Unit.Default),
            CBMidMG.Select(_ => Unit.Default),
            CBMidYB.Select(_ => Unit.Default),
            CBHighCR.Select(_ => Unit.Default),
            CBHighMG.Select(_ => Unit.Default),
            CBHighYB.Select(_ => Unit.Default)
        )
        .Skip(1)
        .ObserveOnThreadPool()
        .Debounce(_debounceTime)
        .Subscribe(ApplyEffects, DebugHelper.LogError(nameof(EffectsViewModel), nameof(InitPipeline)))
        .AddTo(ref _disposables);
        IsLoading.Value = false;
    }

    private void ApplyEffects(Unit unit)
    {
        IsLoading.Value = true;
        UpdateConfigFromProperties();
        
        var magick = ImageEffectsHelper.ApplyImageEffects(
            ProcessedImage,
            EffectConfig.CurrentValue,
            CancellationToken.None);
        if (magick is not null)
        {
            ProcessedImage = magick;
        }
        IsLoading.Value = false;
        IsResetVisible.Value = !IsDefault();
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

    private void UpdateConfigFromProperties()
    {
        EffectConfig.Value.HueDegrees = HueDegrees.Value;
        EffectConfig.Value.Saturation = new Percentage(Saturation.Value);
        EffectConfig.Value.Temperature = Temperature.Value;
        EffectConfig.Value.Tint = Tint.Value;
        EffectConfig.Value.Vibrance = Vibrance.Value;

        EffectConfig.Value.ExposureStops = ExposureStops.Value;
        EffectConfig.Value.Brightness = new Percentage(Brightness.Value);
        EffectConfig.Value.Contrast = new Percentage(Contrast.Value);
        EffectConfig.Value.Gamma = Gamma.Value;

        EffectConfig.Value.Highlights = Highlights.Value;
        EffectConfig.Value.Shadows = Shadows.Value;
        EffectConfig.Value.Blacks = Blacks.Value;
        EffectConfig.Value.Whites = Whites.Value;

        EffectConfig.Value.Dehaze = Dehaze.Value;
        EffectConfig.Value.Clarity = Clarity.Value;
        EffectConfig.Value.Grain = Grain.Value;
        EffectConfig.Value.Sharpen = Sharpen.Value;
        EffectConfig.Value.Vignette = Vignette.Value;

        EffectConfig.Value.BlurLevel = BlurLevel.Value;
        EffectConfig.Value.SketchStrokeWidth = SketchStrokeWidth.Value;
        EffectConfig.Value.PosterizeLevel = PosterizeLevel.Value == 1 ? 2 : PosterizeLevel.Value;
        EffectConfig.Value.Solarize = new Percentage(Solarize.Value);

        EffectConfig.Value.BlackAndWhite = BlackAndWhite.Value;
        EffectConfig.Value.Negative = Negative.Value;
        EffectConfig.Value.OldMovie = OldMovie.Value;

        EffectConfig.Value.CBShadows = new ColorBalanceTriplet(CBShadowsCR.Value, CBShadowsMG.Value, CBShadowsYB.Value);
        EffectConfig.Value.CBMidtones = new ColorBalanceTriplet(CBMidCR.Value, CBMidMG.Value, CBMidYB.Value);
        EffectConfig.Value.CBHighlights = new ColorBalanceTriplet(CBHighCR.Value, CBHighMG.Value, CBHighYB.Value);
    }

    public void ApplyConfig(ImageEffectConfig c)
    {
        IsLoading.Value = true;
        try
        {
            HueDegrees.Value = c.HueDegrees;
            Saturation.Value = c.Saturation.ToDouble();
            Temperature.Value = c.Temperature;
            Tint.Value = c.Tint;
            Vibrance.Value = c.Vibrance;

            ExposureStops.Value = c.ExposureStops;
            Brightness.Value = c.Brightness.ToDouble();
            Contrast.Value = c.Contrast.ToDouble();
            Gamma.Value = c.Gamma <= 0 ? 1 : c.Gamma;

            Highlights.Value = c.Highlights;
            Shadows.Value = c.Shadows;
            Blacks.Value = c.Blacks;
            Whites.Value = c.Whites;

            Dehaze.Value = c.Dehaze;
            Clarity.Value = c.Clarity;
            Grain.Value = c.Grain;
            Sharpen.Value = c.Sharpen;
            Vignette.Value = c.Vignette;

            BlurLevel.Value = c.BlurLevel;
            SketchStrokeWidth.Value = c.SketchStrokeWidth;
            PosterizeLevel.Value = c.PosterizeLevel;
            Solarize.Value = c.Solarize.ToDouble();

            BlackAndWhite.Value = c.BlackAndWhite;
            Negative.Value = c.Negative;
            OldMovie.Value = c.OldMovie;

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
            IsLoading.Value = false;
        }
    }

    private void InitPresets()
    {
        Presets.Clear();
        var t = TranslationManager.Translation;
        Presets.Add(new EffectPreset(t.Normal, ResetAll));
        Presets.Add(new EffectPreset(t.VividPop, () =>
        {
            ResetAll();
            Vibrance.Value = 25;
            Saturation.Value = 10;
            Contrast.Value = 10;
            Clarity.Value = 12;
            Dehaze.Value = 8;
        }));
        Presets.Add(new EffectPreset(t.SoftMatte, () =>
        {
            ResetAll();
            Contrast.Value = -12;
            Gamma.Value = 1.12;
            Blacks.Value = 8;
            Highlights.Value = -10;
            Vignette.Value = 10;
            Grain.Value = 10;
        }));
        Presets.Add(new EffectPreset(t.HighContrastBW, () =>
        {
            ResetAll();
            BlackAndWhite.Value = true;
            Contrast.Value = 28;
            ExposureStops.Value = 0.25;
            Highlights.Value = -15;
            Shadows.Value = 15;
            Clarity.Value = 15;
            Vignette.Value = 12;
        }));
        Presets.Add(new EffectPreset(t.OldMovie, () =>
        {
            ResetAll();
            OldMovie.Value = true;
            Contrast.Value = 8;
            Grain.Value = 25;
            Vignette.Value = 15;
        }));
        Presets.Add(new EffectPreset(t.PortraitSoft, () =>
        {
            ResetAll();
            ExposureStops.Value = 0.15;
            Highlights.Value = -10;
            Shadows.Value = 12;
            Clarity.Value = 6;
            Sharpen.Value = 6;
            Vibrance.Value = 8;
        }));
        Presets.Add(new EffectPreset(t.LandscapeCrisp, () =>
        {
            ResetAll();
            Dehaze.Value = 12;
            Clarity.Value = 18;
            Sharpen.Value = 14;
            Vibrance.Value = 18;
            Highlights.Value = -10;
            Shadows.Value = 10;
        }));
    }

    public void ResetAll()
    {
        IsLoading.Value = true;
        try
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
        }
        finally
        {
            IsLoading.Value = false;
        }
    }

    public void ResetColor()
    {
        HueDegrees.Value = 0;
        Saturation.Value = 0;
        Temperature.Value = 0;
        Tint.Value = 0;
        Vibrance.Value = 0;

        CBShadowsCR.Value = 0; CBShadowsMG.Value = 0; CBShadowsYB.Value = 0;
        CBMidCR.Value = 0; CBMidMG.Value = 0; CBMidYB.Value = 0;
        CBHighCR.Value = 0; CBHighMG.Value = 0; CBHighYB.Value = 0;
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

        SelectedPreset.Dispose();
        IsResetVisible.Dispose();
        ProcessedImage.Dispose();
    }
}

public sealed class EffectPreset(string name, Action apply)
{
    public string Name { get; } = name;
    public Action Apply { get; } = apply;
    public override string ToString() => Name;
}

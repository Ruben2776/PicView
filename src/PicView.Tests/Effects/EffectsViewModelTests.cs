using ImageMagick;
using PicView.Core.ImageEffects;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Effects;

public class EffectsViewModelTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly List<ImageEffectConfig> _renders = [];
    private TaskCompletionSource<ImageEffectConfig> _nextRender = NewRender();
    private readonly EffectsViewModel _effects;

    public EffectsViewModelTests()
    {
        _effects = new EffectsViewModel(Render);
    }

    private static TaskCompletionSource<ImageEffectConfig> NewRender() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Task<MagickImage?> Render(ImageEffectConfig config, CancellationToken ct)
    {
        lock (_renders)
        {
            _renders.Add(config);
        }
        _nextRender.TrySetResult(config);
        return Task.FromResult<MagickImage?>(null);
    }

    [Fact]
    public async Task ChangingAValue_RendersOnceWithMatchingConfig()
    {
        _effects.Contrast.Value = 20;
        _effects.Temperature.Value = -15;
        _effects.BlackAndWhite.Value = true;

        var config = await _nextRender.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.Single(_renders);
        Assert.Equal(new Percentage(20), config.Contrast);
        Assert.Equal(-15, config.Temperature);
        Assert.True(config.BlackAndWhite);
        Assert.Equal(1, config.Gamma);
    }

    [Fact]
    public async Task Rendering_PublishesTheImageAsProcessed()
    {
        using var image = new MagickImage(MagickColors.Red, 4, 3);
        using var effects = new EffectsViewModel((_, _) => Task.FromResult<MagickImage?>(image));
        var published = new TaskCompletionSource<MagickImage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = effects.Rendered.Subscribe(published.SetResult);

        effects.Sharpen.Value = 5;

        Assert.Same(image, await published.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Assert.Same(image, effects.ProcessedImage);
    }

    [Fact]
    public void ChangingAValue_ShowsReset()
    {
        Assert.False(_effects.IsResetVisible.Value);

        _effects.Vignette.Value = 10;

        Assert.True(_effects.IsResetVisible.Value);
    }

    [Fact]
    public void ReturningToDefaults_HidesReset()
    {
        _effects.Gamma.Value = 1.5;

        _effects.Gamma.Value = 1;

        Assert.False(_effects.IsResetVisible.Value);
    }

    [Fact]
    public void SelectingVividPop_SetsItsValuesAndClearsOthers()
    {
        _effects.Grain.Value = 40;
        var vividPop = _effects.Presets[1];

        _effects.SelectedPreset.Value = vividPop;

        Assert.Equal(25, _effects.Vibrance.Value);
        Assert.Equal(10, _effects.Saturation.Value);
        Assert.Equal(10, _effects.Contrast.Value);
        Assert.Equal(12, _effects.Clarity.Value);
        Assert.Equal(8, _effects.Dehaze.Value);
        Assert.Equal(0, _effects.Grain.Value);
    }

    [Fact]
    public void ResetLighting_RestoresLightingOnly()
    {
        _effects.Gamma.Value = 1.4;
        _effects.Shadows.Value = 20;
        _effects.HueDegrees.Value = 90;

        _effects.ResetLighting();

        Assert.Equal(1, _effects.Gamma.Value);
        Assert.Equal(0, _effects.Shadows.Value);
        Assert.Equal(90, _effects.HueDegrees.Value);
    }

    [Fact]
    public void ResetCBShadows_RestoresShadowsBalanceOnly()
    {
        _effects.CBShadowsCR.Value = 30;
        _effects.CBShadowsMG.Value = -20;
        _effects.CBShadowsYB.Value = 10;
        _effects.CBMidCR.Value = 15;

        _effects.ResetCBShadows();

        Assert.Equal(0, _effects.CBShadowsCR.Value);
        Assert.Equal(0, _effects.CBShadowsMG.Value);
        Assert.Equal(0, _effects.CBShadowsYB.Value);
        Assert.Equal(15, _effects.CBMidCR.Value);
    }

    [Fact]
    public void ResetCBMidtones_RestoresMidtonesBalanceOnly()
    {
        _effects.CBMidCR.Value = 30;
        _effects.CBMidMG.Value = -20;
        _effects.CBMidYB.Value = 10;
        _effects.CBHighYB.Value = 15;

        _effects.ResetCBMidtones();

        Assert.Equal(0, _effects.CBMidCR.Value);
        Assert.Equal(0, _effects.CBMidMG.Value);
        Assert.Equal(0, _effects.CBMidYB.Value);
        Assert.Equal(15, _effects.CBHighYB.Value);
    }

    [Fact]
    public void ResetCBHighlights_RestoresHighlightsBalanceOnly()
    {
        _effects.CBHighCR.Value = 30;
        _effects.CBHighMG.Value = -20;
        _effects.CBHighYB.Value = 10;
        _effects.CBShadowsMG.Value = 15;

        _effects.ResetCBHighlights();

        Assert.Equal(0, _effects.CBHighCR.Value);
        Assert.Equal(0, _effects.CBHighMG.Value);
        Assert.Equal(0, _effects.CBHighYB.Value);
        Assert.Equal(15, _effects.CBShadowsMG.Value);
    }

    [Fact]
    public async Task DiscardEffects_WithPendingChange_ResetsWithoutRendering()
    {
        _effects.Contrast.Value = 20;

        _effects.DiscardEffects();
        await Task.Delay(300, TestContext.Current.CancellationToken);

        Assert.Empty(_renders);
        Assert.Equal(0, _effects.Contrast.Value);
        Assert.False(_effects.IsResetVisible.Value);
    }

    public void Dispose() => _effects.Dispose();
}

using PicView.Core.Config;
using PicView.Core.Config.ConfigFileManagement;

namespace PicView.Tests.Effects;

public class EffectsWindowConfigTests
{
    [Fact]
    public async Task EffectsWindowConfig_SaveAsync_WithNaNAndInfinity_DoesNotThrowAndSanitizesValues()
    {
        var config = new EffectsWindowConfig();
        await config.LoadAsync();

        Assert.NotNull(config.WindowProperties);
        config.WindowProperties.Width = double.NaN;
        config.WindowProperties.Height = double.PositiveInfinity;
        config.WindowProperties.Top = 100;
        config.WindowProperties.Left = 200;

        // Must not throw .NET number values such as positive and negative infinity cannot be written as valid JSON
        var exception = await Record.ExceptionAsync(async () => await config.SaveAsync());
        Assert.Null(exception);

        // Properties should be sanitized to null
        Assert.Null(config.WindowProperties.Width);
        Assert.Null(config.WindowProperties.Height);
        Assert.Equal(100, config.WindowProperties.Top);
        Assert.Equal(200, config.WindowProperties.Left);
    }

    [Fact]
    public async Task ConfigFileManager_SanitizesNonFiniteWindowProperties()
    {
        var config = new EffectsWindowConfig();
        var props = new EffectsWindowConfig.EffectsWindowProperties
        {
            Width = double.NegativeInfinity,
            Height = double.NaN,
            Top = 50,
            Left = 60
        };

        var path = await ConfigFileManager.SaveConfigFileAndReturnPathAsync(
            config,
            null,
            props,
            typeof(EffectsWindowConfig.EffectsWindowProperties),
            EffectsWindowGenerationContext.Default);

        Assert.NotNull(path);
        Assert.Null(props.Width);
        Assert.Null(props.Height);
    }
}

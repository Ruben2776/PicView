using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Tests.Navigation;

public class ScreensaverHoldTests
{
    public ScreensaverHoldTests() => SetDefaults();

    [Fact]
    public void Take_First_DisablesOnce()
    {
        var platform = new CountingPlatform();
        var hold = new ScreensaverHold(platform);

        hold.Take();
        hold.Take();

        Assert.Equal(1, platform.Disabled);
        Assert.True(hold.IsHeld);
    }

    [Fact]
    public void Release_Last_EnablesOnce()
    {
        var platform = new CountingPlatform();
        var hold = new ScreensaverHold(platform);
        hold.Take();
        hold.Take();

        hold.Release();
        Assert.Equal(0, platform.Enabled);
        Assert.True(hold.IsHeld);

        hold.Release();
        Assert.Equal(1, platform.Enabled);
        Assert.False(hold.IsHeld);
    }

    [Fact]
    public void Release_NothingTaken_DoesNothing()
    {
        var platform = new CountingPlatform();
        var hold = new ScreensaverHold(platform);

        hold.Release();

        Assert.Equal(0, platform.Enabled);
        Assert.False(hold.IsHeld);
    }

    [Fact]
    public void Take_NullPlatform_TracksWithoutThrowing()
    {
        var hold = new ScreensaverHold(null);

        hold.Take();

        Assert.True(hold.IsHeld);
    }

    [Fact]
    public void CoreViewModel_NoClock_UsesSystem()
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));

        Assert.Same(TimeProvider.System, core.Time);
        Assert.False(core.Screensaver.IsHeld);
    }
}

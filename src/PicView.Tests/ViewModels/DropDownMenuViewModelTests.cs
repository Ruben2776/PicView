using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.ViewModels;

[Collection("Sequential")]
public class DropDownMenuViewModelTests
{
    public DropDownMenuViewModelTests()
    {
        SetDefaults();
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
    }

    private static GlobalSettingsViewModel NewSettings() => new(static () => ValueTask.FromResult(true));

    [Fact]
    public void IsFileHistoryVisible_SettingOff_False()
    {
        var settings = NewSettings();
        settings.IsFileHistoryEnabled.Value = false;
        var menu = new DropDownMenuViewModel(settings);
        Assert.False(menu.IsFileHistoryVisible.Value);
    }

    [Fact]
    public void IsFileHistoryVisible_SettingOnAndOptionsClosed_True()
    {
        var settings = NewSettings();
        settings.IsFileHistoryEnabled.Value = true;
        var menu = new DropDownMenuViewModel(settings);
        Assert.True(menu.IsFileHistoryVisible.Value);
    }

    [Fact]
    public void IsFileHistoryVisible_WhileOptionsAreOpen_False()
    {
        var settings = NewSettings();
        settings.IsFileHistoryEnabled.Value = true;
        var menu = new DropDownMenuViewModel(settings);
        menu.OpenGalleryOptions();
        Assert.False(menu.IsFileHistoryVisible.Value);
        menu.CloseToDefault();
        Assert.True(menu.IsFileHistoryVisible.Value);
    }

    [Fact]
    public void IsFileHistoryVisible_SettingToggledWhileOpen_Follows()
    {
        var settings = NewSettings();
        settings.IsFileHistoryEnabled.Value = true;
        var menu = new DropDownMenuViewModel(settings);
        menu.IsDropDownMenuVisible.Value = true;
        settings.IsFileHistoryEnabled.Value = false;
        Assert.False(menu.IsFileHistoryVisible.Value);
        settings.IsFileHistoryEnabled.Value = true;
        Assert.True(menu.IsFileHistoryVisible.Value);
    }

    [Fact]
    public void Dispose_ThenSettingChanged_IsFileHistoryVisibleStops()
    {
        var settings = NewSettings();
        settings.IsFileHistoryEnabled.Value = true;
        var menu = new DropDownMenuViewModel(settings);
        Assert.True(menu.IsFileHistoryVisible.Value);

        menu.Dispose();
        settings.IsFileHistoryEnabled.Value = false;

        Assert.True(menu.IsFileHistoryVisible.Value);
    }

    [Fact]
    public void IsFileHistoryVisible_NoSettings_False()
    {
        var menu = new DropDownMenuViewModel();
        Assert.False(menu.IsFileHistoryVisible.Value);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}

using PicView.Core.Config;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class GlobalSettingsConfigLocationTests
{
    public GlobalSettingsConfigLocationTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
    }

    private static ValueTask<bool> NoSave() => ValueTask.FromResult(true);

    [Fact]
    public void SyncConfigLocation_Roaming_OnlyRoamingIsChecked()
    {
        var vm = new GlobalSettingsViewModel(NoSave);

        vm.SyncConfigLocation(ConfigLocation.Roaming);

        Assert.False(vm.IsConfigAuto.Value);
        Assert.False(vm.IsConfigPortable.Value);
        Assert.True(vm.IsConfigRoaming.Value);
    }

    [Fact]
    public void Constructor_ReadsModeFromSettings()
    {
        Settings.StartUp.ConfigLocation = ConfigLocation.Portable;

        var vm = new GlobalSettingsViewModel(NoSave);

        Assert.True(vm.IsConfigPortable.Value);
        Assert.False(vm.IsConfigAuto.Value);
        Assert.False(vm.IsConfigRoaming.Value);
    }

    [Fact]
    public void SyncConfigLocation_SameMode_StillNotifies()
    {
        var vm = new GlobalSettingsViewModel(NoSave);
        var count = 0;
        using var sub = vm.IsConfigRoaming.Subscribe(_ => count++);

        vm.SyncConfigLocation(ConfigLocation.Roaming);
        var afterFirst = count;
        vm.SyncConfigLocation(ConfigLocation.Roaming);

        Assert.True(count > afterFirst);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}

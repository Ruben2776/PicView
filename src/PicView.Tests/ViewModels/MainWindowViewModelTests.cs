using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.ViewModels;

[Collection("Sequential")]
public class MainWindowViewModelTests
{
    private readonly CoreViewModel _core;

    public MainWindowViewModelTests()
    {
        SetDefaults();
        TranslationManager.Init();
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        _core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()),
            new GlobalSettingsViewModel(static () => ValueTask.FromResult(true)));
    }

    private MainWindowViewModel NewWindow() =>
        new(_core, _core.Translation, _core.GlobalSettings!, _core.GallerySettings);

    private static WindowModeState Fullscreen =>
        WindowModeState.Start(new RememberedWindowMode(true, false, false), isAppStart: true);

    [Fact]
    public void Interface_AtConstruction_FollowsTheSettingsInNormalMode()
    {
        var vm = NewWindow();
        Assert.Equal(InterfaceVisibility.For(true, true, true, WindowMode.Normal), vm.Interface.Value);
        Assert.False(vm.IsHoverBarShown.Value);
    }

    [Fact]
    public void Interface_StartsFullscreen_HidesEveryBar()
    {
        var vm = NewWindow();
        vm.WindowModeState.Value = Fullscreen;
        Assert.False(vm.Interface.Value.IsInterfaceShown);
        Assert.False(vm.Interface.Value.IsBottomBarShown);
        Assert.Equal(0, vm.Interface.Value.TitlebarHeight);
        Assert.Equal(0, vm.Interface.Value.BottombarHeight);
        Assert.True(vm.IsHoverBarShown.Value);
    }

    [Fact]
    public void Interface_LeavingFullscreen_GivesBackWhatTheSettingsSay()
    {
        var vm = NewWindow();
        vm.WindowModeState.Value = Fullscreen;
        vm.WindowModeState.Value = vm.WindowModeState.Value.ToggleFullscreen();
        Assert.Equal(InterfaceVisibility.For(true, true, true, WindowMode.Normal), vm.Interface.Value);
    }

    [Fact]
    public void Interface_SettingChanged_FollowsIt()
    {
        var vm = NewWindow();
        _core.GlobalSettings!.ShowBottomBar.Value = false;
        Assert.False(vm.Interface.Value.IsBottomBarShown);
        Assert.True(vm.IsHoverBarShown.Value);
        _core.GlobalSettings.ShowInterface.Value = false;
        Assert.False(vm.Interface.Value.IsInterfaceShown);
    }

    [Fact]
    public void Interface_BottomBarToggledWhileFullscreen_NotifiesNothingAndShowsOnLeaving()
    {
        var vm = NewWindow();
        vm.WindowModeState.Value = Fullscreen;
        var notified = 0;
        using var watching = vm.Interface.AsObservable().Skip(1).Subscribe(_ => notified++);

        _core.GlobalSettings!.ShowBottomBar.Value = false;
        _core.GlobalSettings.ShowBottomBar.Value = true;
        Assert.Equal(0, notified);

        vm.WindowModeState.Value = vm.WindowModeState.Value.ToggleFullscreen();
        Assert.True(vm.Interface.Value.IsBottomBarShown);
    }

    [Fact]
    public void Interface_MinimizedFromFullscreen_NotifiesNothing()
    {
        var vm = NewWindow();
        vm.WindowModeState.Value = Fullscreen;
        var notified = 0;
        using var watching = vm.Interface.AsObservable().Skip(1).Subscribe(_ => notified++);

        vm.WindowModeState.Value = vm.WindowModeState.Value.OnNativeState(WindowMode.Minimized);
        vm.WindowModeState.Value = vm.WindowModeState.Value.OnNativeState(WindowMode.Fullscreen);

        Assert.Equal(0, notified);
        Assert.False(vm.Interface.Value.IsInterfaceShown);
    }

    [Fact]
    public void Interface_NoSettingsGiven_DerivesFromTheRecordAndFollowsTheMode()
    {
        Settings.UIProperties.ShowBottomNavBar = false;
        var vm = new MainWindowViewModel(null!, null!, null!, null!);
        Assert.True(vm.Interface.Value.IsInterfaceShown);
        Assert.False(vm.Interface.Value.IsBottomBarShown);
        vm.WindowModeState.Value = Fullscreen;
        Assert.False(vm.Interface.Value.IsInterfaceShown);
    }

    [Fact]
    public void Bars_AreProjectionsOfInterface()
    {
        var vm = NewWindow();
        _core.GlobalSettings!.ShowInterface.Value = false;
        Assert.False(vm.IsUIShown.Value);
        Assert.False(vm.IsTopToolbarShown.Value);
        Assert.False(vm.IsBottomToolbarShown.Value);
        Assert.Equal(0, vm.TitlebarHeight.Value);
        Assert.Equal(0, vm.BottombarHeight.Value);
        _core.GlobalSettings.ShowInterface.Value = true;
        Assert.Equal(SizeDefaults.MainTitlebarHeight, vm.TitlebarHeight.Value);
        Assert.Equal(SizeDefaults.BottombarHeight, vm.BottombarHeight.Value);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}

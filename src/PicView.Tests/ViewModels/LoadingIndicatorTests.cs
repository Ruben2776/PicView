using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.ViewModels;

/// <summary>
/// A Main Window derives its loading indicator: it shows while its active tab is Busy, or while the tab says Loading or
/// Downloading and the titlebar is hidden (ADR-0025).
/// </summary>
[Collection("Sequential")]
public class LoadingIndicatorTests
{
    private readonly CoreViewModel _core;

    public LoadingIndicatorTests()
    {
        SetDefaults();
        TranslationManager.Init();
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        _core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()),
            new GlobalSettingsViewModel(static () => ValueTask.FromResult(true)));
    }

    private MainWindowViewModel NewWindow(TabViewModel? firstTab = null) =>
        new(_core, _core.Translation, _core.GlobalSettings!, _core.GallerySettings, firstTab);

    private void ShowTitlebar(bool shown) => _core.GlobalSettings!.ShowInterface.Value = shown;

    [Fact]
    public void IdleTab_ShowsNoIndicator()
    {
        var vm = NewWindow();
        Assert.False(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void BusyTab_TitlebarShown_ShowsTheIndicator()
    {
        var vm = NewWindow();
        ShowTitlebar(true);
        using var busy = vm.WindowTabs.ActiveTab.Value.ClaimBusy();
        Assert.True(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void BusyTab_TitlebarHidden_ShowsTheIndicator()
    {
        var vm = NewWindow();
        ShowTitlebar(false);
        using var busy = vm.WindowTabs.ActiveTab.Value.ClaimBusy();
        Assert.True(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void LoadingTab_TitlebarShown_DoesNotShowTheIndicator()
    {
        var vm = NewWindow();
        ShowTitlebar(true);
        vm.WindowTabs.ActiveTab.Value.SetLoading();
        Assert.False(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void LoadingTab_TitlebarHidden_ShowsTheIndicator()
    {
        var vm = NewWindow();
        ShowTitlebar(false);
        vm.WindowTabs.ActiveTab.Value.SetLoading();
        Assert.True(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void ClaimEnds_WhileLoadingWithTitlebarHidden_KeepsTheIndicator()
    {
        var vm = NewWindow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        ShowTitlebar(false);
        tab.SetLoading();

        tab.ClaimBusy().Dispose();

        Assert.True(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void LoadingEnds_WhileBusy_KeepsTheIndicator()
    {
        var vm = NewWindow();
        var tab = vm.WindowTabs.ActiveTab.Value;
        ShowTitlebar(false);
        var busy = tab.ClaimBusy();
        var ticket = tab.SetLoading();

        tab.EndLoading(ticket);
        var shownAfterLoadingEnded = vm.IsLoadingIndicatorShown.Value;
        busy.Dispose();

        Assert.True(shownAfterLoadingEnded);
        Assert.False(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void BusyTab_TogglingTheTitlebar_KeepsTheIndicator()
    {
        var vm = NewWindow();
        ShowTitlebar(true);
        using var busy = vm.WindowTabs.ActiveTab.Value.ClaimBusy();

        ShowTitlebar(false);
        var shownWhileHidden = vm.IsLoadingIndicatorShown.Value;
        ShowTitlebar(true);

        Assert.True(shownWhileHidden);
        Assert.True(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void SwitchingToAnIdleTab_HidesTheIndicator_SwitchingBackShowsIt()
    {
        var vm = NewWindow();
        var busyTab = vm.WindowTabs.ActiveTab.Value;
        using var busy = busyTab.ClaimBusy();
        var idleTab = vm.WindowTabs.CreateTab();

        vm.WindowTabs.ActiveTab.Value = idleTab;
        var shownOnIdleTab = vm.IsLoadingIndicatorShown.Value;
        vm.WindowTabs.ActiveTab.Value = busyTab;

        Assert.False(shownOnIdleTab);
        Assert.True(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void BusyBackgroundTab_DoesNotShowTheIndicator()
    {
        var vm = NewWindow();
        var backgroundTab = vm.WindowTabs.ActiveTab.Value;
        vm.WindowTabs.ActiveTab.Value = vm.WindowTabs.CreateTab();

        using var busy = backgroundTab.ClaimBusy();

        Assert.False(vm.IsLoadingIndicatorShown.Value);
    }

    [Fact]
    public void TabBusyBeforeWindowBuilt_ShowsTheIndicator()
    {
        var tab = NewWindow().WindowTabs.ActiveTab.Value;
        using var busy = tab.ClaimBusy();

        var vm = NewWindow(tab);

        Assert.True(vm.IsLoadingIndicatorShown.Value);
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}

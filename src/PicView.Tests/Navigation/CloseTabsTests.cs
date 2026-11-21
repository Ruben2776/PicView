using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.Navigation;

/// <summary>
/// Pins the tab menu's Close other tabs, Close tabs to the left and Close tabs to the right.
/// </summary>
[Collection("Sequential")]
public class CloseTabsTests
{
    private readonly CoreViewModel _core;

    public CloseTabsTests()
    {
        SetDefaults();
        TranslationManager.Init();
        _core = new CoreViewModel(null, file => new ImageModel { FileInfo = file });
    }

    // A window with four tabs: the first it opened with, then three more; the last is active
    private (TabOverviewViewModel Tabs, TabViewModel[] All) CreateWindowWithFourTabs()
    {
        var vm = new MainWindowViewModel(_core, _core.Translation, _core.GlobalSettings!, _core.GallerySettings);
        var tabs = vm.WindowTabs;
        var first = tabs.ActiveTab.Value;
        return (tabs, [first, tabs.CreateTab(), tabs.CreateTab(), tabs.CreateTab()]);
    }

    [Fact]
    public void CloseOtherTabs_LeavesOnlyThatTab_Active()
    {
        var (tabs, all) = CreateWindowWithFourTabs();

        all[1].CloseOtherTabs();

        Assert.Equal([all[1]], tabs.Tabs.Value);
        Assert.Same(all[1], tabs.ActiveTab.Value);
        Assert.All([all[0], all[2], all[3]], tab => Assert.True(tab.IsClosing));
    }

    [Fact]
    public void CloseTabsToTheLeft_ClosesOnlyTheTabsBeforeIt()
    {
        var (tabs, all) = CreateWindowWithFourTabs();

        all[2].CloseTabsToTheLeft();

        Assert.Equal([all[2], all[3]], tabs.Tabs.Value);
        // The active tab stayed open, so it stays active
        Assert.Same(all[3], tabs.ActiveTab.Value);
    }

    [Fact]
    public void CloseTabsToTheRight_ClosingTheActiveTab_SelectsThatTab()
    {
        var (tabs, all) = CreateWindowWithFourTabs();

        all[1].CloseTabsToTheRight();

        Assert.Equal([all[0], all[1]], tabs.Tabs.Value);
        Assert.Same(all[1], tabs.ActiveTab.Value);
    }

    [Fact]
    public void CloseTabsToTheLeft_FirstTab_ClosesNothing()
    {
        var (tabs, all) = CreateWindowWithFourTabs();

        all[0].CloseTabsToTheLeft();

        Assert.Equal(all, tabs.Tabs.Value);
        Assert.Same(all[3], tabs.ActiveTab.Value);
    }

    [Fact]
    public void CloseTabsToTheRight_LastTab_ClosesNothing()
    {
        var (tabs, all) = CreateWindowWithFourTabs();

        all[3].CloseTabsToTheRight();

        Assert.Equal(all, tabs.Tabs.Value);
    }
}

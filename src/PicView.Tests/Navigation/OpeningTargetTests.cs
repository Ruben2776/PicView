using PicView.Core.Config;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Tests.Navigation;

public class OpeningTargetTests
{
    public OpeningTargetTests() => SetDefaults();

    [Fact]
    public void PrepareNewTab_NewTab_NonFreshActive_AppendsAndSelects()
    {
        var tabs = new TabOverviewViewModel(null!);
        var browsing = tabs.ActiveTab.Value;
        browsing.IsInitialized = true;
        tabs.SelectTab(tabs.InsertTab(0));
        var active = tabs.ActiveTab.Value;
        active.IsInitialized = true;

        var created = OpeningTarget.PrepareNewTab(tabs, OpeningMode.NewTab);

        Assert.NotNull(created);
        Assert.Same(created, tabs.Tabs.Value[^1]);
        Assert.Same(created, tabs.ActiveTab.Value);
        Assert.Equal(3, tabs.Tabs.Value.Count);
    }

    [Fact]
    public void PrepareNewTab_NewTab_FreshActive_ReturnsNull()
    {
        var tabs = new TabOverviewViewModel(null!);
        var active = tabs.ActiveTab.Value;

        var created = OpeningTarget.PrepareNewTab(tabs, OpeningMode.NewTab);

        Assert.Null(created);
        Assert.Same(active, Assert.Single(tabs.Tabs.Value));
        Assert.Same(active, tabs.ActiveTab.Value);
    }

    [Theory]
    [InlineData(OpeningMode.SameWindow)]
    [InlineData(OpeningMode.NewWindow)]
    public void PrepareNewTab_SameWindowOrNewWindow_ReturnsNull(OpeningMode mode)
    {
        var tabs = new TabOverviewViewModel(null!);
        var active = tabs.ActiveTab.Value;
        active.IsInitialized = true;

        var created = OpeningTarget.PrepareNewTab(tabs, mode);

        Assert.Null(created);
        Assert.Same(active, Assert.Single(tabs.Tabs.Value));
        Assert.Same(active, tabs.ActiveTab.Value);
    }

    [Theory]
    [InlineData(OpeningMode.NewWindow, true)]
    [InlineData(OpeningMode.SameWindow, false)]
    [InlineData(OpeningMode.NewTab, false)]
    public void OpensOthersInWindows_OnlyNewWindow(OpeningMode mode, bool expected) =>
        Assert.Equal(expected, OpeningTarget.OpensOthersInWindows(mode));
}

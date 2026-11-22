using PicView.Core.Config;
using PicView.Core.FileHistory;
using PicView.Core.Navigation;

namespace PicView.Tests.StartUp;

public class StartUpPlanTests
{
    private static readonly Func<string, bool> Everything = _ => true;

    private static LastSession Session(int active, params string[] sources) =>
        new() { Sources = [..sources], ActiveIndex = active };

    [Theory]
    [InlineData(StartUpBehavior.None)]
    [InlineData(StartUpBehavior.OpenLastFile)]
    [InlineData(StartUpBehavior.ContinueSession)]
    public void HistoryOff_ShowsMenu_ForEveryBehavior(StartUpBehavior behavior)
    {
        var plan = StartUpPlan.Decide(behavior, false, "a.jpg", Session(0, "a.jpg"), Everything);

        Assert.True(plan.ShowsMenu);
        Assert.False(plan.HasOtherTabs);
    }

    [Fact]
    public void Menu_ShowsMenu_WithNoSources()
    {
        Assert.True(StartUpPlan.Menu.ShowsMenu);
        Assert.Empty(StartUpPlan.Menu.Sources);
        Assert.False(StartUpPlan.Menu.HasOtherTabs);
    }

    [Fact]
    public void None_ShowsMenu()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.None, true, "a.jpg", Session(0, "a.jpg"), Everything);

        Assert.True(plan.ShowsMenu);
    }

    [Fact]
    public void OpenLastFile_WithLastFile_OpensIt()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.OpenLastFile, true, "missing.jpg", null, _ => false);

        Assert.False(plan.ShowsMenu);
        Assert.Equal("missing.jpg", plan.ActiveSource);
        Assert.Equal(["missing.jpg"], plan.Sources);
        Assert.Equal(0, plan.ActiveIndex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void OpenLastFile_Blank_ShowsMenu(string? lastFile)
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.OpenLastFile, true, lastFile, null, Everything);

        Assert.True(plan.ShowsMenu);
    }

    [Fact]
    public void ContinueSession_KeepsOrderAndActive()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.ContinueSession, true, null, Session(2, "a", "b", "c"),
            Everything);

        Assert.Equal(["a", "b", "c"], plan.Sources);
        Assert.Equal(2, plan.ActiveIndex);
        Assert.Equal("c", plan.ActiveSource);
        Assert.True(plan.HasOtherTabs);
    }

    [Fact]
    public void ContinueSession_ActiveFileGone_NextRemainingBecomesActive()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.ContinueSession, true, null, Session(1, "a", "b", "c"),
            path => path != "b");

        Assert.Equal(["a", "c"], plan.Sources);
        Assert.Equal(1, plan.ActiveIndex);
        Assert.Equal("c", plan.ActiveSource);
    }

    [Fact]
    public void ContinueSession_LastActiveFileGone_PreviousRemainingBecomesActive()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.ContinueSession, true, null, Session(2, "a", "b", "c"),
            path => path != "c");

        Assert.Equal(["a", "b"], plan.Sources);
        Assert.Equal(1, plan.ActiveIndex);
        Assert.Equal("b", plan.ActiveSource);
    }

    [Fact]
    public void ContinueSession_EarlierFileGone_IsLeftForTheRestore()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.ContinueSession, true, null, Session(2, "a", "b", "c"),
            path => path != "a");

        Assert.Equal(["a", "b", "c"], plan.Sources);
        Assert.Equal(2, plan.ActiveIndex);
        Assert.Equal("c", plan.ActiveSource);
    }

    [Fact]
    public void ContinueSession_ActiveAndNextGone_WalksOnToTheFirstThatExists()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.ContinueSession, true, null, Session(1, "a", "b", "c", "d"),
            path => path is "a" or "d");

        Assert.Equal(["a", "d"], plan.Sources);
        Assert.Equal(1, plan.ActiveIndex);
        Assert.Equal("d", plan.ActiveSource);
    }

    [Fact]
    public void Decide_DoesNotCheckNonActiveSources()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.ContinueSession, true, null, Session(1, "a", "b", "c"),
            path => path == "b" ? true : throw new InvalidOperationException($"{path} was checked"));

        Assert.Equal(["a", "b", "c"], plan.Sources);
        Assert.Equal(1, plan.ActiveIndex);
        Assert.Equal("b", plan.ActiveSource);
    }

    [Fact]
    public void ContinueSession_AllGone_ShowsMenu()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.ContinueSession, true, null, Session(0, "a", "b"), _ => false);

        Assert.True(plan.ShowsMenu);
        Assert.Empty(plan.Sources);
    }

    [Fact]
    public void ContinueSession_NoSession_ShowsMenu()
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.ContinueSession, true, "a.jpg", null, Everything);

        Assert.True(plan.ShowsMenu);
    }

    [Theory]
    [InlineData("https://example.com/a.png")]
    [InlineData("HTTP://example.com/a.png")]
    public void ContinueSession_UrlKeptWithoutExistenceCheck(string url)
    {
        var plan = StartUpPlan.Decide(StartUpBehavior.ContinueSession, true, null, Session(0, "a", url),
            _ => false);

        Assert.Equal([url], plan.Sources);
        Assert.Equal(url, plan.ActiveSource);
        Assert.Equal(0, plan.ActiveIndex);
    }
}

using PicView.Core.Gallery;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins the rules of a tab's Gallery Mode (ADR-0011): one switcher per tab, crop and Single Image hide without changing
/// the mode, and the tab's window only remembers what the user chose by hand. Nothing is written to the settings
/// record; the window stores its settings when it closes.
/// </summary>
[Collection("Sequential")]
public class GalleryModeSwitcherTests : IDisposable
{
    private readonly List<GalleryModeChange> _changes = [];
    private readonly GalleryModeSwitcher _sut;
    private readonly IDisposable _subscription;

    // The gallery settings of the Main Window the switcher's tab is in
    private readonly WindowGallerySettings _window;

    public GalleryModeSwitcherTests()
    {
        SetDefaults();
        _window = WindowGallerySettings.FromRecord(Settings.Gallery);
        _sut = new GalleryModeSwitcher();
        _sut.RemembersIn(() => _window);
        _subscription = _sut.Changes.Subscribe(_changes.Add);
    }

    [Fact]
    public void Start_Docked_DocksWithoutAnimationOrSave()
    {
        _sut.Start(true);

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal([new GalleryModeChange(GalleryMode.Closed, GalleryMode.Docked, false)], _changes);
        Assert.False(_window.StartsDocked.Value);
        Assert.False(Settings.Gallery.IsGalleryDocked);
    }

    [Fact]
    public void Start_TakesWhatItsWindowSaysANewTabStartsWith()
    {
        _window.StartsDocked.Value = true;

        _sut.Start();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal([new GalleryModeChange(GalleryMode.Closed, GalleryMode.Docked, false)], _changes);
    }

    [Fact]
    public void ToggleExpanded_FromClosed_ExpandsWithAnimation()
    {
        _sut.ToggleExpanded();

        Assert.Equal([new GalleryModeChange(GalleryMode.Closed, GalleryMode.Expanded, true)], _changes);
        Assert.False(_window.StartsDocked.Value);
    }

    [Fact]
    public void ToggleExpanded_FromExpandedAfterDocked_GivesBackDocked()
    {
        _sut.Start(true);
        _sut.ToggleExpanded();
        _sut.ToggleExpanded();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.False(_window.StartsDocked.Value);
    }

    [Fact]
    public async Task Contract_FromExpandedAfterClosed_Closes()
    {
        // Preset to what a stray write would not leave: giving the Expanded Gallery back remembers nothing
        _window.StartsDocked.Value = true;
        _sut.ToggleExpanded();

        await _sut.Contract();

        Assert.Equal(GalleryMode.Closed, _sut.Mode.CurrentValue);
        Assert.True(_window.StartsDocked.Value);
    }

    [Fact]
    public async Task Contract_FromExpandedAfterDocked_GivesBackDocked()
    {
        // Preset to what a stray write would not leave: giving the Expanded Gallery back remembers nothing
        _window.StartsDocked.Value = false;
        _sut.Start(true);
        _sut.ToggleExpanded();

        await _sut.Contract();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.False(_window.StartsDocked.Value);
    }

    [Fact]
    public async Task Contract_FromDocked_ClosesAndRemembersUndocked()
    {
        _window.StartsDocked.Value = true;
        Settings.Gallery.IsGalleryDocked = true;
        _sut.Start(true);

        await _sut.Contract();

        Assert.Equal(GalleryMode.Closed, _sut.Mode.CurrentValue);
        Assert.False(_window.StartsDocked.Value);
        Assert.True(Settings.Gallery.IsGalleryDocked);
    }

    [Fact]
    public async Task ToggleDocked_FromClosed_DocksAndRemembersDocked()
    {
        await _sut.ToggleDocked();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal(new GalleryModeChange(GalleryMode.Closed, GalleryMode.Docked, true), _changes[^1]);
        Assert.True(_window.StartsDocked.Value);
        Assert.False(Settings.Gallery.IsGalleryDocked);
    }

    [Fact]
    public async Task ToggleDocked_FromExpanded_StaysExpandedAndFlipsWhatItGivesBack()
    {
        _sut.ToggleExpanded();

        await _sut.ToggleDocked();

        Assert.Equal(GalleryMode.Expanded, _sut.Mode.CurrentValue);
        Assert.True(_sut.IsDocked.CurrentValue);
        Assert.True(_window.StartsDocked.Value);

        await _sut.Contract();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
    }

    [Fact]
    public async Task Close_FromExpandedAfterDocked_ClosesAndRemembersUndocked()
    {
        _window.StartsDocked.Value = true;
        _sut.Start(true);
        _sut.ToggleExpanded();

        await _sut.Close();

        Assert.Equal(GalleryMode.Closed, _sut.Mode.CurrentValue);
        Assert.False(_sut.IsDocked.CurrentValue);
        Assert.False(_window.StartsDocked.Value);
    }

    [Fact]
    public async Task DockAt_FromClosed_DocksAtPosition()
    {
        await _sut.DockAt(GalleryDockPosition.Left);

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal(GalleryDockPosition.Left, _window.DockPosition.Value);
        Assert.True(_window.StartsDocked.Value);
        Assert.Equal(GalleryDockPosition.Bottom, Settings.Gallery.DockPosition);
        Assert.False(Settings.Gallery.IsGalleryDocked);
    }

    [Fact]
    public void SetHidden_WhileDocked_ShowsClosedWithoutAnimationAndKeepsMode()
    {
        _sut.Start(true);
        _changes.Clear();

        _sut.SetHidden(true);

        Assert.Equal(GalleryMode.Closed, _sut.ShownMode.CurrentValue);
        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal([new GalleryModeChange(GalleryMode.Docked, GalleryMode.Closed, false)], _changes);
    }

    [Fact]
    public void SetHidden_False_GivesBackShownModeWithoutAnimation()
    {
        _sut.Start(true);
        _sut.SetHidden(true);
        _changes.Clear();

        _sut.SetHidden(false);

        Assert.Equal([new GalleryModeChange(GalleryMode.Closed, GalleryMode.Docked, false)], _changes);
    }

    [Fact]
    public async Task Commands_WhileHidden_AreRefused()
    {
        _window.StartsDocked.Value = true;
        _sut.Start(true);
        _sut.SetHidden(true);

        _sut.ToggleExpanded();
        await _sut.ToggleDocked();
        await _sut.Contract();
        await _sut.Close();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.True(_window.StartsDocked.Value);
    }

    [Fact]
    public async Task DockAt_WhileHidden_SetsOnlyThePosition()
    {
        _sut.SetHidden(true);

        await _sut.DockAt(GalleryDockPosition.Top);

        Assert.Equal(GalleryDockPosition.Top, _window.DockPosition.Value);
        Assert.Equal(GalleryMode.Closed, _sut.Mode.CurrentValue);
        Assert.False(_window.StartsDocked.Value);
    }

    [Fact]
    public void SetOverLimit_WhileDocked_ShowsClosedWithoutAnimationAndKeepsMode()
    {
        _sut.Start(true);
        _changes.Clear();

        _sut.SetOverLimit(true);

        Assert.Equal(GalleryMode.Closed, _sut.ShownMode.CurrentValue);
        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.True(_sut.IsHidden.CurrentValue);
        Assert.Equal([new GalleryModeChange(GalleryMode.Docked, GalleryMode.Closed, false)], _changes);
    }

    [Fact]
    public void SetOverLimit_False_GivesBackShownModeWithoutAnimation()
    {
        _sut.Start(true);
        _sut.SetOverLimit(true);
        _changes.Clear();

        _sut.SetOverLimit(false);

        Assert.False(_sut.IsHidden.CurrentValue);
        Assert.Equal([new GalleryModeChange(GalleryMode.Closed, GalleryMode.Docked, false)], _changes);
    }

    [Fact]
    public void SetOverLimit_False_WhileCropHides_StaysHidden()
    {
        _sut.Start(true);
        _sut.SetHidden(true);
        _sut.SetOverLimit(true);
        _changes.Clear();

        _sut.SetOverLimit(false);

        Assert.True(_sut.IsHidden.CurrentValue);
        Assert.Equal(GalleryMode.Closed, _sut.ShownMode.CurrentValue);
        Assert.Empty(_changes);
    }

    [Fact]
    public async Task Commands_WhileOverLimit_AreRefusedWithoutRemembering()
    {
        _window.StartsDocked.Value = true;
        _sut.Start(true);
        _sut.SetOverLimit(true);

        _sut.ToggleExpanded();
        await _sut.ToggleDocked();
        await _sut.Contract();
        await _sut.Close();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal(GalleryMode.Closed, _sut.ShownMode.CurrentValue);
        Assert.True(_window.StartsDocked.Value);
    }

    [Fact]
    public async Task TwoSwitchers_DoNotAffectEachOther()
    {
        using var other = new GalleryModeSwitcher();

        await _sut.ToggleDocked();
        _sut.SetHidden(true);

        Assert.Equal(GalleryMode.Closed, other.Mode.CurrentValue);
        Assert.False(other.IsHidden.CurrentValue);
    }

    [Fact]
    public async Task Switcher_WithoutAWindow_RemembersNothing_AndThrowsNothing()
    {
        Settings.Gallery.IsGalleryDocked = true;
        Settings.Gallery.DockPosition = GalleryDockPosition.Right;
        using var switcher = new GalleryModeSwitcher();

        var thrown = await Record.ExceptionAsync(async () =>
        {
            switcher.Start();
            await switcher.Contract();
            await switcher.DockAt(GalleryDockPosition.Left);
            await switcher.ToggleDocked();
            await switcher.ToggleDocked();
            switcher.ToggleExpanded();
            await switcher.Close();
        });

        Assert.Null(thrown);
        Assert.Equal(GalleryMode.Closed, switcher.Mode.CurrentValue);
        Assert.True(Settings.Gallery.IsGalleryDocked);
        Assert.Equal(GalleryDockPosition.Right, Settings.Gallery.DockPosition);
    }

    [Fact]
    public void Start_WithoutAWindow_StartsAsTheRecordSays()
    {
        Settings.Gallery.IsGalleryDocked = true;
        using var switcher = new GalleryModeSwitcher();

        switcher.Start();

        Assert.Equal(GalleryMode.Docked, switcher.Mode.CurrentValue);
    }

    [Fact]
    public async Task Switcher_ItsWindowClosed_RemembersNothing_AndThrowsNothing()
    {
        _window.Dispose();

        var thrown = await Record.ExceptionAsync(async () =>
        {
            _sut.Start();
            await _sut.ToggleDocked();
            await _sut.DockAt(GalleryDockPosition.Left);
            await _sut.Close();
        });

        Assert.Null(thrown);
        Assert.Equal(GalleryMode.Closed, _sut.Mode.CurrentValue);
    }

    public void Dispose()
    {
        _subscription.Dispose();
        _sut.Dispose();
        _window.Dispose();
    }
}

using PicView.Core.Gallery;
using R3;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins the rules of a tab's Gallery Mode (ADR-0011): one switcher per tab, crop and Single Image hide without changing
/// the mode, and the setting only remembers what the user chose by hand.
/// </summary>
[Collection("Sequential")]
public class GalleryModeSwitcherTests : IDisposable
{
    private readonly CountingSave _save = new();
    private readonly List<GalleryModeChange> _changes = [];
    private readonly GalleryModeSwitcher _sut;
    private readonly IDisposable _subscription;

    public GalleryModeSwitcherTests()
    {
        SetDefaults();
        _sut = new GalleryModeSwitcher(_save.SaveAsync);
        _subscription = _sut.Changes.Subscribe(_changes.Add);
    }

    [Fact]
    public void Start_Docked_DocksWithoutAnimationOrSave()
    {
        _sut.Start(true);

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal([new GalleryModeChange(GalleryMode.Closed, GalleryMode.Docked, false)], _changes);
        Assert.Equal(0, _save.Count);
        Assert.False(Settings.Gallery.IsGalleryDocked);
    }

    [Fact]
    public void ToggleExpanded_FromClosed_ExpandsWithAnimation()
    {
        _sut.ToggleExpanded();

        Assert.Equal([new GalleryModeChange(GalleryMode.Closed, GalleryMode.Expanded, true)], _changes);
        Assert.Equal(0, _save.Count);
    }

    [Fact]
    public void ToggleExpanded_FromExpandedAfterDocked_GivesBackDocked()
    {
        _sut.Start(true);
        _sut.ToggleExpanded();
        _sut.ToggleExpanded();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal(0, _save.Count);
    }

    [Fact]
    public async Task Contract_FromExpandedAfterClosed_Closes()
    {
        _sut.ToggleExpanded();

        await _sut.Contract();

        Assert.Equal(GalleryMode.Closed, _sut.Mode.CurrentValue);
        Assert.Equal(0, _save.Count);
    }

    [Fact]
    public async Task Contract_FromExpandedAfterDocked_GivesBackDocked()
    {
        Settings.Gallery.IsGalleryDocked = true;
        _sut.Start(true);
        _sut.ToggleExpanded();

        await _sut.Contract();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal(0, _save.Count);
        Assert.True(Settings.Gallery.IsGalleryDocked);
    }

    [Fact]
    public async Task Contract_FromDocked_ClosesAndRemembersUndocked()
    {
        Settings.Gallery.IsGalleryDocked = true;
        _sut.Start(true);

        await _sut.Contract();

        Assert.Equal(GalleryMode.Closed, _sut.Mode.CurrentValue);
        Assert.False(Settings.Gallery.IsGalleryDocked);
        Assert.Equal(1, _save.Count);
    }

    [Fact]
    public async Task ToggleDocked_FromClosed_DocksAndRemembersDocked()
    {
        await _sut.ToggleDocked();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal(new GalleryModeChange(GalleryMode.Closed, GalleryMode.Docked, true), _changes[^1]);
        Assert.True(Settings.Gallery.IsGalleryDocked);
        Assert.Equal(1, _save.Count);
    }

    [Fact]
    public async Task ToggleDocked_FromExpanded_StaysExpandedAndFlipsWhatItGivesBack()
    {
        _sut.ToggleExpanded();

        await _sut.ToggleDocked();

        Assert.Equal(GalleryMode.Expanded, _sut.Mode.CurrentValue);
        Assert.True(_sut.IsDocked.CurrentValue);
        Assert.True(Settings.Gallery.IsGalleryDocked);

        await _sut.Contract();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
    }

    [Fact]
    public async Task Close_FromExpandedAfterDocked_ClosesAndRemembersUndocked()
    {
        Settings.Gallery.IsGalleryDocked = true;
        _sut.Start(true);
        _sut.ToggleExpanded();

        await _sut.Close();

        Assert.Equal(GalleryMode.Closed, _sut.Mode.CurrentValue);
        Assert.False(_sut.IsDocked.CurrentValue);
        Assert.False(Settings.Gallery.IsGalleryDocked);
        Assert.Equal(1, _save.Count);
    }

    [Fact]
    public async Task DockAt_FromClosed_DocksAtPosition()
    {
        await _sut.DockAt(GalleryDockPosition.Left);

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal(GalleryDockPosition.Left, Settings.Gallery.DockPosition);
        Assert.True(Settings.Gallery.IsGalleryDocked);
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
        _sut.Start(true);
        _sut.SetHidden(true);

        _sut.ToggleExpanded();
        await _sut.ToggleDocked();
        await _sut.Contract();
        await _sut.Close();

        Assert.Equal(GalleryMode.Docked, _sut.Mode.CurrentValue);
        Assert.Equal(0, _save.Count);
    }

    [Fact]
    public async Task DockAt_WhileHidden_SetsOnlyThePosition()
    {
        _sut.SetHidden(true);

        await _sut.DockAt(GalleryDockPosition.Top);

        Assert.Equal(GalleryDockPosition.Top, Settings.Gallery.DockPosition);
        Assert.Equal(GalleryMode.Closed, _sut.Mode.CurrentValue);
    }

    [Fact]
    public async Task TwoSwitchers_DoNotAffectEachOther()
    {
        using var other = new GalleryModeSwitcher(_save.SaveAsync);

        await _sut.ToggleDocked();
        _sut.SetHidden(true);

        Assert.Equal(GalleryMode.Closed, other.Mode.CurrentValue);
        Assert.False(other.IsHidden.CurrentValue);
    }

    public void Dispose()
    {
        _subscription.Dispose();
        _sut.Dispose();
    }

    private sealed class CountingSave
    {
        public int Count { get; private set; }

        public ValueTask<bool> SaveAsync()
        {
            Count++;
            return ValueTask.FromResult(true);
        }
    }
}

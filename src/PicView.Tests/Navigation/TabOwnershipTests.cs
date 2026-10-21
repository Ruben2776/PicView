using ImageMagick;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Tests.Navigation;

/// <summary>
/// Pins which Main Window a tab belongs to, observed through each window's <see cref="TabOverviewViewModel"/>.
/// </summary>
[Collection("Sequential")]
public class TabOwnershipTests
{
    private readonly CoreViewModel _core;

    public TabOwnershipTests()
    {
        SetDefaults();
        TranslationManager.Init();
        _core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
    }

    [Fact]
    public void MoveTab_ToAnotherWindow_IsOwnedAndSelectedThereAndTheNeighbourHere()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var remaining = source.WindowTabs.ActiveTab.Value;
        var tab = source.WindowTabs.CreateTab();

        var moved = source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.True(moved);
        Assert.Equal([remaining], source.WindowTabs.Tabs.Value);
        Assert.Same(remaining, source.WindowTabs.ActiveTab.Value);
        Assert.Contains(tab, target.WindowTabs.Tabs.Value);
        Assert.Same(tab, target.WindowTabs.ActiveTab.Value);
        Assert.Same(target, tab.ParentWindowContext);
    }

    [Fact]
    public void MoveTab_ToAnIndex_InsertsItThere()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var first = target.WindowTabs.ActiveTab.Value;
        var second = target.WindowTabs.CreateTab();
        var tab = source.WindowTabs.CreateTab();

        source.WindowTabs.MoveTab(tab, target.WindowTabs, index: 1);

        Assert.Equal([first, tab, second], target.WindowTabs.Tabs.Value);
        Assert.Same(tab, target.WindowTabs.ActiveTab.Value);
    }

    [Fact]
    public void MoveOut_TabOfAnotherWindow_LeavesItThere()
    {
        var source = CreateWindow();
        var other = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        other.WindowTabs.CreateTab();

        var movedOut = other.WindowTabs.MoveOut(tab);

        Assert.False(movedOut);
        Assert.Contains(tab, source.WindowTabs.Tabs.Value);
        Assert.Equal(2, other.WindowTabs.Tabs.Value.Count);
    }

    [Fact]
    public void Reorder_TabToAnotherPlace_MovesItAndKeepsTheActiveTab()
    {
        var window = CreateWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();
        var third = window.WindowTabs.CreateTab();
        window.WindowTabs.SelectTab(second);

        window.WindowTabs.Reorder(first, 2);

        Assert.Equal([second, third, first], window.WindowTabs.Tabs.Value);
        Assert.Same(second, window.WindowTabs.ActiveTab.Value);
    }

    [Fact]
    public void Reorder_PastTheLastTab_MovesItToTheEnd()
    {
        var window = CreateWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();

        window.WindowTabs.Reorder(first, 5);

        Assert.Equal([second, first], window.WindowTabs.Tabs.Value);
    }

    [Fact]
    public void MoveTab_LastTab_StaysWhereItIs()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var tab = source.WindowTabs.ActiveTab.Value;

        var moved = source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.False(moved);
        Assert.Equal([tab], source.WindowTabs.Tabs.Value);
        Assert.Same(source, tab.ParentWindowContext);
    }

    [Fact]
    public void MoveTab_OpenedTab_IsAttachedThereAndDetachedHere()
    {
        var sourceHookups = new RecordingHookups();
        var targetHookups = new RecordingHookups();
        var source = CreateWindow(sourceHookups);
        var target = CreateWindow(targetHookups);
        var tab = source.WindowTabs.CreateTab();
        source.WindowTabs.HookUp(tab);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.Equal([tab], sourceHookups.Detached);
        Assert.Equal([tab], targetHookups.Attached);
    }

    [Fact]
    public void MoveTab_FreshTab_StaysUnattached()
    {
        var targetHookups = new RecordingHookups();
        var source = CreateWindow(new RecordingHookups());
        var target = CreateWindow(targetHookups);
        var tab = source.WindowTabs.CreateTab();

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.Empty(targetHookups.Attached);
    }

    [Fact]
    public void NewWindow_OpenedWithAMovedTab_HasOnlyThatTabAndAttachesIt()
    {
        var source = CreateWindow(new RecordingHookups());
        var tab = source.WindowTabs.CreateTab();
        source.WindowTabs.HookUp(tab);
        var hookups = new RecordingHookups();

        source.WindowTabs.MoveOut(tab);
        var window = CreateWindow(hookups, firstTab: tab);

        Assert.Equal([tab], window.WindowTabs.Tabs.Value);
        Assert.Same(tab, window.WindowTabs.ActiveTab.Value);
        Assert.Same(window, tab.ParentWindowContext);
        Assert.Equal([tab], hookups.Attached);
    }

    [Fact]
    public void HookUp_CalledTwice_AttachesTheTabOnce()
    {
        var hookups = new RecordingHookups();
        var window = CreateWindow(hookups);
        var tab = window.WindowTabs.ActiveTab.Value;

        window.WindowTabs.HookUp(tab);
        window.WindowTabs.HookUp(tab);

        Assert.True(tab.IsInitialized);
        Assert.Equal([tab], hookups.Attached);
    }

    [Fact]
    public void CloseTab_AfterMovingToAnotherWindow_ClosesItInThatWindow()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var sourceTab = source.WindowTabs.ActiveTab.Value;
        var targetTab = target.WindowTabs.ActiveTab.Value;
        var tab = source.WindowTabs.CreateTab();

        source.WindowTabs.MoveTab(tab, target.WindowTabs);
        tab.CloseTab();

        Assert.Equal([targetTab], target.WindowTabs.Tabs.Value);
        Assert.Same(targetTab, target.WindowTabs.ActiveTab.Value);
        Assert.Equal([sourceTab], source.WindowTabs.Tabs.Value);
    }

    [Fact]
    public void CloseTab_OpenedTab_DetachesIt()
    {
        var hookups = new RecordingHookups();
        var window = CreateWindow(hookups);
        var tab = window.WindowTabs.CreateTab();
        window.WindowTabs.HookUp(tab);

        window.WindowTabs.CloseTab(tab);

        Assert.Equal([tab], hookups.Detached);
    }

    [Fact]
    public void CloseTab_TabOfAnotherWindow_LeavesItOpen()
    {
        var source = CreateWindow();
        var other = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        other.WindowTabs.CreateTab();

        other.WindowTabs.CloseTab(tab);

        Assert.Contains(tab, source.WindowTabs.Tabs.Value);
        Assert.Equal(2, other.WindowTabs.Tabs.Value.Count);
    }

    [Fact]
    public void CloseTab_LastTab_StaysOpen()
    {
        var window = CreateWindow();
        var tab = window.WindowTabs.ActiveTab.Value;

        window.WindowTabs.CloseTab(tab);

        Assert.Equal([tab], window.WindowTabs.Tabs.Value);
    }

    [Fact]
    public void TearDown_CalledTwice_ReleasesEachTabOnce()
    {
        var watcher = new RecordingFileWatcher();
        _core.InitializeNavigation(null!, new MockThumbnailLoader(), watcher);
        var tabs = CreateWindow().WindowTabs;
        tabs.InitializeActiveTab([]);
        var first = tabs.ActiveTab.Value;
        var second = tabs.CreateTab();
        _core.SharedThumbnailCache.Add(first.Id, "first.jpg", new object());
        _core.SharedThumbnailCache.Add(second.Id, "second.jpg", new object());

        tabs.TearDown();
        tabs.TearDown();

        Assert.Equal([first, second], watcher.Unwatched);
        Assert.True(_core.SharedThumbnailCache.IsEmpty);
    }

    [Fact]
    public void SharedServices_TwoMainWindows_AreTheSameInstances()
    {
        var watcher = new RecordingFileWatcher();
        _core.InitializeNavigation(null!, new MockThumbnailLoader(), watcher);

        var first = CreateWindow().WindowTabs;
        var second = CreateWindow().WindowTabs;

        Assert.Same(watcher, first.SharedFileWatcher);
        Assert.Same(first.SharedFileWatcher, second.SharedFileWatcher);
        Assert.Same(first.SharedCache, second.SharedCache);
        Assert.Same(first.SharedThumbnailLoader, second.SharedThumbnailLoader);
    }

    private MainWindowViewModel CreateWindow(ITabHookups? hookups = null, TabViewModel? firstTab = null)
    {
        var vm = new MainWindowViewModel(_core, _core.Translation, null!, _core.GlobalSettings!, _core.GallerySettings,
            firstTab);
        if (hookups is not null)
        {
            vm.WindowTabs.SetHookups(hookups);
        }
        return vm;
    }

    private sealed class RecordingHookups : ITabHookups
    {
        public List<TabViewModel> Attached { get; } = [];
        public List<TabViewModel> Detached { get; } = [];

        public IDisposable Attach(TabViewModel tab)
        {
            Attached.Add(tab);
            return new Detaching(() => Detached.Add(tab));
        }

        private sealed class Detaching(Action detach) : IDisposable
        {
            public void Dispose() => detach();
        }
    }

    private sealed class RecordingFileWatcher : IFileWatcherService
    {
        public List<TabViewModel> Unwatched { get; } = [];
        public void Watch(TabViewModel tab, string? directory = null) { }
        public void Unwatch(TabViewModel tab) => Unwatched.Add(tab);
        public void RestartWatches() { }
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }
}

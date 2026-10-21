using ImageMagick;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Tests.Navigation;

/// <summary>
/// Pins which Main Window a tab belongs to, observed through each window's <see cref="TabOverviewViewModel"/>.
/// </summary>
[Collection("Sequential")]
public class TabOwnershipTests
{
    public TabOwnershipTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [Fact]
    public void CloseTab_AfterMovingToAnotherWindow_ClosesItInThatWindow()
    {
        var core = CreateCore();
        var source = CreateWindowViewModel(core);
        var target = CreateWindowViewModel(core);
        var sourceTab = source.WindowTabs.ActiveTab.Value;
        var targetTab = target.WindowTabs.ActiveTab.Value;
        var tab = source.WindowTabs.CreateTab();

        source.WindowTabs.RemoveTab(tab);
        target.WindowTabs.Adopt(tab);
        tab.CloseTab();

        Assert.Equal([targetTab], target.WindowTabs.Tabs.Value);
        Assert.Same(targetTab, target.WindowTabs.ActiveTab.Value);
        Assert.Equal([sourceTab], source.WindowTabs.Tabs.Value);
    }

    [Fact]
    public void Adopt_TabFromAnotherWindow_IsOwnedAndSelectedThere()
    {
        var core = CreateCore();
        var source = CreateWindowViewModel(core);
        var target = CreateWindowViewModel(core);
        var tab = source.WindowTabs.CreateTab();

        source.WindowTabs.RemoveTab(tab);
        target.WindowTabs.Adopt(tab);

        Assert.Same(target, tab.ParentWindowContext);
        Assert.Same(tab, target.WindowTabs.ActiveTab.Value);
    }

    [Fact]
    public void CloseTab_TabOfAnotherWindow_LeavesItOpen()
    {
        var core = CreateCore();
        var source = CreateWindowViewModel(core);
        var other = CreateWindowViewModel(core);
        var tab = source.WindowTabs.CreateTab();
        other.WindowTabs.CreateTab();

        other.WindowTabs.CloseTab(tab);

        Assert.Contains(tab, source.WindowTabs.Tabs.Value);
        Assert.Equal(2, other.WindowTabs.Tabs.Value.Count);
    }

    [Fact]
    public void CloseTab_LastTab_StaysOpen()
    {
        var window = CreateWindowViewModel(CreateCore());
        var tab = window.WindowTabs.ActiveTab.Value;

        window.WindowTabs.CloseTab(tab);

        Assert.Equal([tab], window.WindowTabs.Tabs.Value);
    }

    [Fact]
    public void TearDown_CalledTwice_ReleasesEachTabOnce()
    {
        var watcher = new RecordingFileWatcher();
        var thumbnails = new RecordingThumbnailCache();
        var tabs = new TabOverviewViewModel(null!);
        tabs.LoadAndInitialize(null!, new SharedImageCache(LoadNothing), thumbnails, new MockThumbnailLoader(), watcher);
        var first = tabs.ActiveTab.Value;
        var second = tabs.CreateTab();

        tabs.TearDown();
        tabs.TearDown();

        Assert.Equal([first, second], watcher.Unwatched);
        Assert.Equal([first.Id, second.Id], thumbnails.RemovedOwners);
    }

    private static CoreViewModel CreateCore() =>
        new(null, _ => ValueTask.FromResult(new ImageModel()));

    private static MainWindowViewModel CreateWindowViewModel(CoreViewModel core) =>
        new(core, core.Translation, null!, core.GlobalSettings!, core.GallerySettings);

    private static ValueTask<ImageModel> LoadNothing(FileInfo file) =>
        ValueTask.FromResult(new ImageModel { FileInfo = file });

    private sealed class RecordingFileWatcher : IFileWatcherService
    {
        public List<TabViewModel> Unwatched { get; } = [];
        public void Watch(TabViewModel tab, string? directory = null) { }
        public void Unwatch(TabViewModel tab) => Unwatched.Add(tab);
    }

    private sealed class RecordingThumbnailCache : IThumbnailCache
    {
        public List<uint> RemovedOwners { get; } = [];
        public bool IsEmpty => true;
        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail)
        {
            thumbnail = null;
            return false;
        }
        public void Remove(string path) { }
        public void RemoveOwner(uint ownerId) => RemovedOwners.Add(ownerId);
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }
}

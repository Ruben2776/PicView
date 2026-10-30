using System.Collections.Specialized;
using ImageMagick;
using ObservableCollections;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins how a tab's Gallery Items follow its file list (ADR-0010). Each test changes only what the gallery hears: the
/// tab's file list, through its iterator, and whether the gallery is shown.
/// </summary>
[Collection("Sequential")]
public class GalleryFollowsFilesTests : IDisposable
{
    private readonly SharedImageCache _cache = new(static f => ValueTask.FromResult(new ImageModel { FileInfo = f }));
    private readonly RecordingThumbnailCache _thumbnails = new();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "PicViewGalleryTests_" + Guid.NewGuid());

    public GalleryFollowsFilesTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task FilesListed_GalleryShown_LoadsAnItemForEachFileInOrder()
    {
        var tab = CreateTab();
        Show(tab);

        tab.ImageIterator.Initialize(await FilesAsync("b.jpg", "a.jpg", "c.jpg"));

        await WaitUntilLoadedAsync(tab, "b.jpg", "a.jpg", "c.jpg");
    }

    [Fact]
    public async Task FilesListed_GalleryNeverShown_LoadsNothing()
    {
        var tab = CreateTab();

        tab.ImageIterator.Initialize(await FilesAsync("a.jpg", "b.jpg"));
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Empty(tab.Gallery.GalleryItems);
        Assert.Equal(GalleryLoadingState.NotLoaded, tab.Gallery.LoadingState);
    }

    [Fact]
    public async Task Shown_AfterTheFilesWereListed_LoadsThem()
    {
        var tab = CreateTab();
        tab.ImageIterator.Initialize(await FilesAsync("a.jpg", "b.jpg"));

        Show(tab);

        await WaitUntilLoadedAsync(tab, "a.jpg", "b.jpg");
    }

    [Fact]
    public async Task Expanded_AfterTheFilesWereListed_LoadsThem()
    {
        var tab = CreateTab();
        tab.ImageIterator.Initialize(await FilesAsync("a.jpg", "b.jpg"));

        tab.Gallery.Switcher.ToggleExpanded();

        await WaitUntilLoadedAsync(tab, "a.jpg", "b.jpg");
    }

    [Fact]
    public async Task OtherFolderListed_WhileClosed_DropsTheItemsAndLoadsTheNewFolderWhenShown()
    {
        var tab = CreateTab();
        Show(tab);
        tab.ImageIterator.Initialize(await FilesAsync("a.jpg", "b.jpg"));
        await WaitUntilLoadedAsync(tab, "a.jpg", "b.jpg");
        Hide(tab);

        tab.ImageIterator.Initialize(await FilesAsync(Path.Combine("other", "c.jpg")));
        await IdleAsync(tab);

        Assert.Empty(tab.Gallery.GalleryItems);
        Assert.Contains(tab.Id, _thumbnails.RemovedOwners);
        Show(tab);
        await WaitUntilLoadedAsync(tab, "c.jpg");
    }

    [Fact]
    public async Task SameFilesInAnotherOrder_Loaded_SortsTheItemsItHas()
    {
        var tab = CreateTab();
        Show(tab);
        var files = await FilesAsync("a.jpg", "b.jpg", "c.jpg");
        tab.ImageIterator.Initialize(files);
        await WaitUntilLoadedAsync(tab, "a.jpg", "b.jpg", "c.jpg");
        var items = tab.Gallery.GalleryItems.ToArray();

        tab.ImageIterator.Initialize([files[2], files[0], files[1]]);
        await IdleAsync(tab);

        Assert.Equal([items[2], items[0], items[1]], tab.Gallery.GalleryItems);
    }

    [Fact]
    public async Task SameFilesInAnotherOrder_WhileLoading_LoadsThemAgainInTheNewOrder()
    {
        var tab = CreateTab();
        Show(tab);
        var files = await FilesAsync(Enumerable.Range(0, 300).Select(i => $"{i:D3}.jpg").ToArray());

        tab.ImageIterator.Initialize(files);
        files.Reverse();
        tab.ImageIterator.Initialize(files);

        await WaitUntilLoadedAsync(tab, files.Select(file => file.Name).ToArray());
    }

    [Fact]
    public async Task SameFilesListedAgain_KeepsTheItems()
    {
        var tab = CreateTab();
        Show(tab);
        var files = await FilesAsync("a.jpg", "b.jpg");
        tab.ImageIterator.Initialize(files);
        await WaitUntilLoadedAsync(tab, "a.jpg", "b.jpg");
        var items = tab.Gallery.GalleryItems.ToArray();

        tab.ImageIterator.Initialize(files.ToList(), 1);
        await IdleAsync(tab);

        Assert.Equal(items, tab.Gallery.GalleryItems);
        Assert.Equal(GalleryLoadingState.Loaded, tab.Gallery.LoadingState);
    }

    [Fact]
    public async Task FileInserted_Loaded_AddsItsItemInItsPlace()
    {
        var tab = await LoadedTabAsync("a.jpg", "c.jpg");
        var inserted = (await FilesAsync("b.jpg"))[0];

        tab.ImageIterator.Insert(inserted, string.CompareOrdinal);
        await IdleAsync(tab);

        AssertItems(tab, "a.jpg", "b.jpg", "c.jpg");
    }

    [Fact]
    public async Task FileInserted_GalleryNeverShown_AddsNoItems()
    {
        var tab = CreateTab();
        tab.ImageIterator.Initialize(await FilesAsync("a.jpg"));

        tab.ImageIterator.Insert((await FilesAsync("b.jpg"))[0], string.CompareOrdinal);
        await IdleAsync(tab);

        Assert.Empty(tab.Gallery.GalleryItems);
    }

    [Fact]
    public async Task FileInserted_WhileLoading_LoadsAgainWithTheFile()
    {
        var tab = CreateTab();
        Show(tab);
        var files = await FilesAsync(Enumerable.Range(0, 300).Select(i => $"{i:D3}.jpg").ToArray());
        var inserted = (await FilesAsync("150a.jpg"))[0];

        tab.ImageIterator.Initialize(files);
        tab.ImageIterator.Insert(inserted, string.CompareOrdinal);

        await WaitUntilLoadedAsync(tab, tab.ImageIterator.Files.Select(file => file.Name).ToArray());
        Assert.Equal(301, tab.Gallery.GalleryItems.Count);
    }

    [Fact]
    public async Task FileRemoved_Loaded_RemovesItsItem()
    {
        var tab = await LoadedTabAsync("a.jpg", "b.jpg", "c.jpg");

        tab.ImageIterator.Remove(Path.Combine(_directory, "b.jpg"));
        await IdleAsync(tab);

        AssertItems(tab, "a.jpg", "c.jpg");
    }

    [Fact]
    public async Task LastFileRemoved_Loaded_EmptiesTheItems()
    {
        var tab = await LoadedTabAsync("a.jpg");

        tab.ImageIterator.Remove(Path.Combine(_directory, "a.jpg"));
        await IdleAsync(tab);

        Assert.Empty(tab.Gallery.GalleryItems);
    }

    [Fact]
    public async Task FileReplaced_Loaded_RebuildsItsItem()
    {
        var tab = await LoadedTabAsync("a.jpg", "b.jpg");
        var before = tab.Gallery.GalleryItems[0];

        tab.ImageIterator.Replace(new FileInfo(Path.Combine(_directory, "a.jpg")));
        await IdleAsync(tab);

        Assert.NotSame(before, tab.Gallery.GalleryItems[0]);
        Assert.Null(tab.Gallery.GalleryItems[0].Image.Value);
        AssertItems(tab, "a.jpg", "b.jpg");
    }

    [Fact]
    public async Task FileRenamed_Loaded_MovesItsItemToItsNewPlace()
    {
        var tab = await LoadedTabAsync("a.jpg", "b.jpg");
        var renamed = (await FilesAsync("c.jpg"))[0];

        tab.ImageIterator.Move(Path.Combine(_directory, "a.jpg"), renamed, string.CompareOrdinal);
        await IdleAsync(tab);

        AssertItems(tab, "b.jpg", "c.jpg");
    }

    [Fact]
    public async Task FileInserted_Loaded_ReadsItsDetailsFromTheChange()
    {
        var tab = await LoadedTabAsync("a.jpg");
        var path = Path.Combine(_directory, "b.png");
        using (var image = new MagickImage(MagickColors.Red, 3, 2))
        {
            await image.WriteAsync(path, TestContext.Current.CancellationToken);
        }
        var file = new FileInfo(path);

        tab.ImageIterator.Insert(file, string.CompareOrdinal, new FileDetailsSource(file, waitUntilReady: false));

        await WaitUntilAsync(() => tab.Gallery.GalleryItems.Count == 2 &&
                                   tab.Gallery.GalleryItems[1].Details?.PixelWidth == 3u);
        Assert.Equal(2u, tab.Gallery.GalleryItems[1].Details!.PixelHeight);
    }

    [Fact]
    public async Task FolderRenamed_Loaded_ItemsFollowItAndKeepTheirThumbnails()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "sub"));
        var tab = await LoadedTabAsync("a.jpg", Path.Combine("sub", "b.jpg"));
        var thumbnail = new object();
        tab.Gallery.GalleryItems[1].Image.Value = thumbnail;
        var renamed = Path.Combine(_directory, "renamed");
        Directory.Move(Path.Combine(_directory, "sub"), renamed);

        tab.ImageIterator.RenameFolder(Path.Combine(_directory, "sub"), renamed);
        await IdleAsync(tab);

        Assert.Equal(renamed, tab.Gallery.GalleryItems[1].FileInfo!.DirectoryName);
        Assert.Same(thumbnail, tab.Gallery.GalleryItems[1].Image.Value);
    }

    [Fact]
    public async Task SingleImageShown_ThenTheSameFiles_KeepsTheItems()
    {
        var tab = await LoadedTabAsync("a.jpg", "b.jpg");
        var items = tab.Gallery.GalleryItems.ToArray();
        var files = tab.ImageIterator.Files.ToList();

        tab.DisposeImageIterator();
        BrowseAgain(tab, files);
        await IdleAsync(tab);

        Assert.Equal(items, tab.Gallery.GalleryItems);
        Assert.Equal(GalleryLoadingState.Loaded, tab.Gallery.LoadingState);
    }

    [Fact]
    public async Task SingleImageShown_ThenOtherFiles_LoadsThem()
    {
        var tab = await LoadedTabAsync("a.jpg", "b.jpg");

        tab.DisposeImageIterator();
        BrowseAgain(tab, await FilesAsync(Path.Combine("other", "c.jpg")));

        await WaitUntilLoadedAsync(tab, "c.jpg");
    }

    [Fact]
    public async Task SingleImageShown_KeepsTheItemsWhileItShows()
    {
        var tab = await LoadedTabAsync("a.jpg", "b.jpg");

        tab.DisposeImageIterator();
        Hide(tab);
        Show(tab);
        await IdleAsync(tab);

        AssertItems(tab, "a.jpg", "b.jpg");
    }

    [Fact]
    public async Task FilesListed_TheChangeCarriesTheCurrentIndex()
    {
        var changes = new List<FileListChange>();
        using var iterator = new ImageIterator(_cache, _thumbnails, new MockThumbnailLoader(), CreateTab(), changes.Add);

        iterator.Initialize(await FilesAsync("a.jpg", "b.jpg", "c.jpg"), 2);

        Assert.Equal(2, Assert.Single(changes).Index);
    }

    [Fact]
    public async Task FilesListed_CurrentImageFarAlong_AddsEveryItemAtOnceAndReadsOutwardFromIt()
    {
        var tab = CreateTab();
        var reader = UseRecordingReader(tab);
        var changes = RecordChanges(tab);
        Show(tab);

        tab.ImageIterator.Initialize(await FilesAsync(Names(0, 200)), 150);
        await WaitUntilLoadedAsync(tab, Names(0, 200));

        Assert.Equal((NotifyCollectionChangedAction.Add, 200), changes[0]);
        var reads = reader.Reads;
        Assert.Equal(200, reads.Distinct().Count());
        Assert.Equal(Names(130, 41).ToHashSet(), reads[..41].ToHashSet());
        Assert.Equal(Names(120, 10).Concat(Names(171, 10)).ToHashSet(), reads[41..61].ToHashSet());
        Assert.Equal(Names(99, 11).Concat(Names(191, 9)).ToHashSet(), reads[81..101].ToHashSet());
    }

    [Fact]
    public async Task CurrentImageChanged_WhileSizesAreRead_ReadsAroundTheNewOne()
    {
        var tab = CreateTab();
        var reader = UseRecordingReader(tab);
        reader.Pause(afterReads: 61);
        Show(tab);

        tab.ImageIterator.Initialize(await FilesAsync(Names(0, 200)), 150);
        // A held read means the batch after the first 61 reads was already chosen around image 150
        await WaitUntilAsync(() => reader.Started > 61);
        tab.NavigationIndex.Value = 10;
        reader.Release();

        await WaitUntilLoadedAsync(tab, Names(0, 200));
        // Reads 62-81 were chosen before the change; the batch after them is around image 10
        Assert.Equal(Names(1, 20).ToHashSet(), reader.Reads[81..101].ToHashSet());
    }

    [Fact]
    public async Task SizesRead_WhileLoading_AreWrittenOntoTheSameItemsOneSignalPerBatch()
    {
        var tab = CreateTab();
        UseRecordingReader(tab);
        var changes = RecordChanges(tab);
        var signals = 0;
        using var subscription = tab.Gallery.SizesChanged.Subscribe(_ => Interlocked.Increment(ref signals));
        Show(tab);

        tab.ImageIterator.Initialize(await FilesAsync(Names(0, 200)), 150);
        await WaitUntilAsync(() => tab.Gallery.GalleryItems.Count == 200);
        var items = tab.Gallery.GalleryItems.ToArray();
        await WaitUntilLoadedAsync(tab, Names(0, 200));

        Assert.Equal(items, tab.Gallery.GalleryItems);
        Assert.All(items, item => Assert.Equal(200u, item.Details!.PixelWidth));
        Assert.DoesNotContain(changes, change => change.Action is NotifyCollectionChangedAction.Replace);
        Assert.Equal(8, signals); // 159 items after the neighborhood, in batches of 20
    }

    [Fact]
    public async Task FileWithoutReadableDetails_IsReadOnceAndStaysSquare()
    {
        var tab = CreateTab();
        var reader = UseRecordingReader(tab);
        reader.DetailsOf = name => name == "045.jpg" ? null : new GalleryFileDetails(200, 100, null);
        Show(tab);

        tab.ImageIterator.Initialize(await FilesAsync(Names(0, 50)));
        await WaitUntilLoadedAsync(tab, Names(0, 50));

        Assert.Equal(1, reader.Reads.Count(name => name == "045.jpg"));
        Assert.Same(GalleryFileDetails.Unreadable, tab.Gallery.GalleryItems[45].Details);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(500, 29)]
    public async Task FilesListed_CurrentIndexOutOfRange_ReadsAroundTheNearestEnd(int currentIndex, int firstRead)
    {
        var tab = CreateTab();
        var reader = UseRecordingReader(tab);
        Show(tab);

        tab.ImageIterator.Initialize(await FilesAsync(Names(0, 50)), currentIndex);
        await WaitUntilLoadedAsync(tab, Names(0, 50));

        Assert.Equal(Names(firstRead, 21).ToHashSet(), reader.Reads[..21].ToHashSet());
    }

    [Fact]
    public async Task OtherFolderListed_WhileSizesAreRead_DropsTheBatchBeingRead()
    {
        var tab = CreateTab();
        var reader = UseRecordingReader(tab);
        reader.Pause(afterReads: 61);
        Show(tab);
        tab.ImageIterator.Initialize(await FilesAsync(Names(0, 200)), 150);
        await WaitUntilAsync(() => reader.Started > 61);
        var oldItems = tab.Gallery.GalleryItems.ToArray();

        tab.ImageIterator.Initialize(await FilesAsync(Path.Combine("other", "c.jpg")));
        await IdleAsync(tab);
        reader.Release();

        await WaitUntilLoadedAsync(tab, "c.jpg");
        await Task.Delay(100, TestContext.Current.CancellationToken);
        // Only the neighborhood and the first batch were written before the folder changed
        Assert.Equal(61, oldItems.Count(item => item.Details is not null));
    }

    [Fact]
    public async Task LastFileRemoved_BeforeTheItemsAreAdded_FinishesWithNoItems()
    {
        var tab = CreateTab();
        var reader = UseRecordingReader(tab);
        reader.Pause(afterReads: 0);
        Show(tab);
        tab.ImageIterator.Initialize(await FilesAsync("a.jpg"));
        await WaitUntilAsync(() => tab.Gallery.LoadingState is GalleryLoadingState.Starting);

        tab.ImageIterator.Remove(Path.Combine(_directory, "a.jpg"));
        await IdleAsync(tab);
        reader.Release();

        await WaitUntilLoadedAsync(tab);
    }

    [Fact]
    public async Task ReadingFails_WhileLoading_KeepsTheItemsAndStaysLoading()
    {
        var tab = CreateTab();
        var reader = UseRecordingReader(tab);
        reader.DetailsOf = name => name == "100.jpg"
            ? throw new IOException("Cannot read")
            : new GalleryFileDetails(200, 100, null);
        Show(tab);

        tab.ImageIterator.Initialize(await FilesAsync(Names(0, 200)), 150);
        await WaitUntilAsync(() => reader.Reads.Contains("100.jpg"));
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Equal(GalleryLoadingState.Loading, tab.Gallery.LoadingState);
        Assert.Equal(200, tab.Gallery.GalleryItems.Count);
        // The neighborhood and two batches were read; the batch that failed was not written
        Assert.Equal(81, tab.Gallery.GalleryItems.Count(item => item.Details is not null));
    }

    private TabViewModel CreateTab()
    {
        var tab = new TabViewModel(null!)
        {
            Model = new ImageModel { FileInfo = new FileInfo(Path.Combine(_directory, "shown.jpg")) }
        };
        tab.Initialize(_cache, _thumbnails, new MockThumbnailLoader(), thumbnailCache: _thumbnails);
        return tab;
    }

    private async Task<TabViewModel> LoadedTabAsync(params string[] names)
    {
        var tab = CreateTab();
        Show(tab);
        tab.ImageIterator.Initialize(await FilesAsync(names));
        await WaitUntilLoadedAsync(tab, names.Select(Path.GetFileName).ToArray()!);
        return tab;
    }

    /// <summary>
    /// What browsing files again after a Single Image does: a new iterator listing the files.
    /// </summary>
    private void BrowseAgain(TabViewModel tab, List<FileInfo> files)
    {
        tab.Model = new ImageModel { FileInfo = files[0] };
        tab.InitializeImageIterator(files, _cache, _thumbnails, new MockThumbnailLoader(), thumbnailCache: _thumbnails);
    }

    private static Task IdleAsync(TabViewModel tab) => tab.GalleryCoordinator!.Idle;

    private static void Show(TabViewModel tab) => tab.Gallery.Switcher.Start(true);

    private static void Hide(TabViewModel tab) => tab.Gallery.Switcher.Start(false);

    private static string[] Names(int start, int count) =>
        Enumerable.Range(start, count).Select(i => $"{i:D3}.jpg").ToArray();

    private static RecordingDetailsReader UseRecordingReader(TabViewModel tab)
    {
        var reader = new RecordingDetailsReader();
        tab.GalleryCoordinator!.ReadDetails = reader.Read;
        return reader;
    }

    /// <summary>Each change to the tab's Gallery Items: its action and how many items it added.</summary>
    private static List<(NotifyCollectionChangedAction Action, int Added)> RecordChanges(TabViewModel tab)
    {
        var changes = new List<(NotifyCollectionChangedAction, int)>();
        tab.Gallery.GalleryItems.CollectionChanged += (in NotifyCollectionChangedEventArgs<GalleryItemViewModel> e) =>
        {
            var added = e.Action is NotifyCollectionChangedAction.Add ? (e.IsSingleItem ? 1 : e.NewItems.Length) : 0;
            lock (changes)
            {
                changes.Add((e.Action, added));
            }
        };
        return changes;
    }

    /// <summary>
    /// Files that exist, as a gallery load reads each file's size and date.
    /// </summary>
    private async Task<List<FileInfo>> FilesAsync(params string[] names)
    {
        var files = new List<FileInfo>(names.Length);
        foreach (var name in names)
        {
            var path = Path.Combine(_directory, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "x", TestContext.Current.CancellationToken);
            files.Add(new FileInfo(path));
        }
        return files;
    }

    private static void AssertItems(TabViewModel tab, params string[] names) =>
        Assert.Equal(names, tab.Gallery.GalleryItems.Select(item => item.FileInfo!.Name));

    private static Task WaitUntilLoadedAsync(TabViewModel tab, params string[] names) =>
        WaitUntilAsync(() => tab.Gallery.LoadingState is GalleryLoadingState.Loaded &&
                             tab.Gallery.GalleryItems.Select(item => item.FileInfo!.Name).SequenceEqual(names),
            () => $"state {tab.Gallery.LoadingState}, items {string.Join(", ", tab.Gallery.GalleryItems.Take(5).Select(item => item.FileInfo?.Name))}");

    private static async Task WaitUntilAsync(Func<bool> condition, Func<string>? describe = null)
    {
        var deadline = Environment.TickCount64 + 5000;
        while (!condition())
        {
            Assert.True(Environment.TickCount64 < deadline, "The gallery did not settle in time. " + describe?.Invoke());
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private sealed class RecordingThumbnailCache : IThumbnailCache
    {
        public List<uint> RemovedOwners { get; } = [];

        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail) { thumbnail = null; return false; }
        public void Remove(string path) { }
        public void RemoveOwner(uint ownerId) => RemovedOwners.Add(ownerId);
        public bool IsEmpty => true;
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    /// <summary>
    /// Stands in for reading files. It records each read in order and gives every file 200 × 100 unless told otherwise.
    /// Once paused, it holds every read after the first <c>afterReads</c> until released.
    /// </summary>
    private sealed class RecordingDetailsReader
    {
        private readonly Lock _lock = new();
        private readonly List<string> _reads = [];
        private readonly ManualResetEventSlim _released = new(true);
        private int _started;
        private int _readsBeforePause = int.MaxValue;

        public Func<string, GalleryFileDetails?> DetailsOf { get; set; } = _ => new GalleryFileDetails(200, 100, null);

        /// <summary>How many reads have begun, held ones included.</summary>
        public int Started => Volatile.Read(ref _started);

        public string[] Reads
        {
            get
            {
                lock (_lock)
                {
                    return [.. _reads];
                }
            }
        }

        public void Pause(int afterReads)
        {
            _readsBeforePause = afterReads;
            _released.Reset();
        }

        public void Release() => _released.Set();

        public GalleryFileDetails? Read(FileInfo file)
        {
            if (Interlocked.Increment(ref _started) > _readsBeforePause)
            {
                _released.Wait();
            }
            lock (_lock)
            {
                _reads.Add(file.Name);
            }
            return DetailsOf(file.Name);
        }
    }
}

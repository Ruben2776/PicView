using ImageMagick;
using PicView.Core.Gallery;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// Pins how tabs follow the files in the folders they browse. Folder events are raised by hand through a fake
/// <see cref="IFolderEvents"/>, except in the one test that checks the watcher on disk.
/// </summary>
[Collection("Sequential")]
public class FileWatcherServiceTests : IDisposable
{
    private readonly FileWatcherService _service;
    private readonly FakeFolderEvents _folderEvents = new();
    private readonly SharedImageCache _cache;
    private readonly MockThumbnailCache _mockThumbnailCache;
    private readonly string _testDirectory;

    public FileWatcherServiceTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();

        // Ensure Settings are initialized for FileWatcherService usage
        SetDefaults();
        TranslationManager.LoadLanguage("en");

        _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        _cache = new SharedImageCache(static f => ValueTask.FromResult(new ImageModel { FileInfo = f }));
        _mockThumbnailCache = new MockThumbnailCache();
        // Use default ordinal comparison for tests
        _service = new FileWatcherService((s1, s2) => string.CompareOrdinal(s1, s2), _cache, _mockThumbnailCache,
            _folderEvents);
    }

    [Fact]
    public void Watch_TwoTabsInOneFolder_StartsOneWatch()
    {
        Settings.Sorting.IncludeSubDirectories = true;
        var first = CreateTab(_testDirectory);
        var second = CreateTab(_testDirectory);

        _service.Watch(first);
        _service.Watch(second);

        var watch = Assert.Single(_folderEvents.Watches);
        Assert.Equal(_testDirectory, watch.Directory);
        Assert.True(watch.IncludesSubdirectories);
    }

    [Fact]
    public void Unwatch_LastTabOfAFolder_StopsTheWatch()
    {
        var first = CreateTab(_testDirectory);
        var second = CreateTab(_testDirectory);
        _service.Watch(first);
        _service.Watch(second);
        var watch = Assert.Single(_folderEvents.Watches);

        _service.Unwatch(first);
        var afterFirst = watch.IsDisposed;
        _service.Unwatch(second);

        Assert.False(afterFirst, "Another tab still watches the folder");
        Assert.True(watch.IsDisposed);
    }

    [Fact]
    public void RestartWatches_AfterIncludeSubdirectoriesChanged_WatchesWithTheNewSetting()
    {
        Settings.Sorting.IncludeSubDirectories = false;
        var watch = Watch(CreateTab(_testDirectory));

        Settings.Sorting.IncludeSubDirectories = true;
        _service.RestartWatches();

        Assert.True(watch.IsDisposed);
        var restarted = _folderEvents.Watches[^1];
        Assert.NotSame(watch, restarted);
        Assert.Equal(_testDirectory, restarted.Directory);
        Assert.True(restarted.IncludesSubdirectories);
    }

    [Fact]
    public async Task RestartWatches_TabsStillFollowTheirFolders()
    {
        // Arrange
        var files = Files("a.jpg", "b.jpg");
        var first = CreateTab(_testDirectory, files, files[0]);
        var second = CreateTab(_testDirectory, files, files[1]);
        Watch(first);
        _service.Watch(second);

        // Act
        _service.RestartWatches();
        _folderEvents.Watches[^1].RaiseDeleted("a.jpg");

        // Assert
        await WaitUntilAsync(() => first.ImageIterator.Files.Count == 1 && second.ImageIterator.Files.Count == 1);
    }

    [Fact]
    public async Task OnFileCreated_UpdatesTabFiles()
    {
        // Arrange
        var existingFile = new FileInfo(Path.Combine(_testDirectory, "existing.jpg"));
        var tab = CreateTab(_testDirectory, [existingFile], existingFile);

        // Act
        Watch(tab).RaiseCreated("test.jpg");

        // Assert
        await WaitUntilAsync(() => tab.ImageIterator.Files.Count == 2);
        Assert.Contains(tab.ImageIterator.Files, f => f.Name == "test.jpg");
    }

    [Fact]
    public async Task OnFileCreated_WhenFileAlreadyInList_DoesNotDuplicate()
    {
        // Arrange: Tab already has this file in files list
        var existingFile = new FileInfo(Path.Combine(_testDirectory, "overwrite.jpg"));
        var tab = CreateTab(_testDirectory, [existingFile], existingFile);
        var watch = Watch(tab);

        // Act: Events of one kind are handled in order, so the second shows the first was handled
        watch.RaiseCreated("overwrite.jpg");
        watch.RaiseCreated("later.jpg");

        // Assert: No duplicates should be inserted
        await WaitUntilAsync(() => tab.ImageIterator.Files.Any(f => f.Name == "later.jpg"));
        Assert.Single(tab.ImageIterator.Files, f => f.FullName == existingFile.FullName);
    }

    [Fact]
    public async Task OnFileChanged_ForgetsTheStaleImage()
    {
        // Arrange: the file is on disk, so waiting for it to be ready ends at once
        var filePath = Path.Combine(_testDirectory, "change.jpg");
        await File.WriteAllTextAsync(filePath, "dummy content", TestContext.Current.CancellationToken);
        var file = new FileInfo(filePath);
        var tab = CreateTab(_testDirectory, [file], file);
        _cache.Put(tab.Id, file, new ImageModel { FileInfo = file });

        // Act
        Watch(tab).RaiseChanged("change.jpg");

        // Assert
        await WaitUntilAsync(() => _mockThumbnailCache.RemovedPaths.Contains(filePath));
        Assert.Single(tab.ImageIterator.Files);
        Assert.False(_cache.TryGetReady(file, out _), "The stale image should no longer be served");
    }

    [Fact]
    public async Task OnFileDeleted_UpdatesTabFiles_AndForgetsTheImage()
    {
        // Arrange
        var files = Files("test1.jpg", "test2.jpg", "test3.jpg");
        var tab = CreateTab(_testDirectory, files, files[0]);
        _cache.Put(tab.Id, files[0], new ImageModel { FileInfo = files[0] });

        // Act
        Watch(tab).RaiseDeleted("test1.jpg");

        // Assert
        await WaitUntilAsync(() => tab.ImageIterator.Files.Count == 2);
        Assert.DoesNotContain(tab.ImageIterator.Files, f => f.FullName == files[0].FullName);
        Assert.False(_cache.TryGetReady(files[0], out _), "The stale image should no longer be served");
        Assert.Contains(files[0].FullName, _mockThumbnailCache.RemovedPaths);
    }

    [Fact]
    public async Task OnFileDeleted_TwoTabsInTheFolder_UpdatesBoth()
    {
        // Arrange
        var files = Files("test1.jpg", "test2.jpg", "test3.jpg");
        var first = CreateTab(_testDirectory, files, files[0]);
        var second = CreateTab(_testDirectory, files, files[2]);
        var watch = Watch(first);
        _service.Watch(second);

        // Act: Delete a file both tabs browse
        watch.RaiseDeleted("test2.jpg");

        // Assert
        await WaitUntilAsync(() => first.ImageIterator.Files.Count == 2 && second.ImageIterator.Files.Count == 2);
        Assert.DoesNotContain(first.ImageIterator.Files, f => f.Name == "test2.jpg");
        Assert.DoesNotContain(second.ImageIterator.Files, f => f.Name == "test2.jpg");
    }

    [Fact]
    public async Task OnFileDeleted_AfterTheFirstTabStopsWatching_UpdatesTheOtherTab()
    {
        // Arrange
        var files = Files("test1.jpg", "test2.jpg");
        var first = CreateTab(_testDirectory);
        var watch = Watch(first);
        var second = CreateTab(_testDirectory, files, files[0]);
        _service.Watch(second);

        // Act: The first tab moves on, then a file of the second tab is deleted
        _service.Unwatch(first);
        watch.RaiseDeleted("test2.jpg");

        // Assert
        await WaitUntilAsync(() => second.ImageIterator.Files.Count == 1);
        Assert.DoesNotContain(second.ImageIterator.Files, f => f.Name == "test2.jpg");
    }

    [Fact]
    public async Task OnFileRenamed_UpdatesTabFiles_AndModel()
    {
        // Arrange
        var files = Files("old.jpg");
        var newPath = Path.Combine(_testDirectory, "new.jpg");
        var tab = CreateTab(_testDirectory, files, files[0]);
        _cache.Put(tab.Id, files[0], new ImageModel { FileInfo = files[0] });

        // Act
        Watch(tab).RaiseRenamed("old.jpg", "new.jpg");

        // Assert
        await WaitUntilAsync(() => tab.Model.FileInfo?.FullName == newPath);
        Assert.Equal(newPath, Assert.Single(tab.ImageIterator.Files).FullName);
        Assert.False(_cache.TryGetReady(files[0], out _), "The stale image should no longer be served");
        Assert.Contains(files[0].FullName, _mockThumbnailCache.RemovedPaths);
    }

    [Fact]
    public async Task OnFileCreated_LoadedGallery_AddsTheFileInItsPlace()
    {
        // Arrange: a gallery that is not docked, but has loaded its items
        var files = await FilesOnDiskAsync("a.jpg", "c.jpg");
        var tab = CreateTab(_testDirectory, files, files[0]);
        LoadGallery(tab);
        await WriteFileAsync("b.jpg");

        // Act
        Watch(tab).RaiseCreated("b.jpg");

        // Assert
        await WaitUntilAsync(() => tab.Gallery.GalleryItems.Count == 3);
        Assert.Equal(["a.jpg", "b.jpg", "c.jpg"], tab.Gallery.GalleryItems.Select(item => item.FileInfo!.Name));
    }

    [Fact]
    public async Task OnFileCreated_GalleryNeverUsed_AddsNoItems()
    {
        // Arrange
        var files = Files("a.jpg");
        var tab = CreateTab(_testDirectory, files, files[0]);

        // Act
        Watch(tab).RaiseCreated("b.jpg");

        // Assert
        await WaitUntilAsync(() => tab.ImageIterator.Files.Count == 2);
        Assert.Empty(tab.Gallery.GalleryItems);
    }

    [Fact]
    public async Task OnFileCreated_WhileTheGalleryLoads_LoadsItAgainWithTheFile()
    {
        // Arrange: a load in progress reads the file list as it goes
        var files = await FilesOnDiskAsync("a.jpg", "c.jpg");
        var tab = CreateTab(_testDirectory, files, files[0]);
        tab.Gallery.LoadingState = GalleryLoadingState.Loading;
        await WriteFileAsync("b.jpg");

        // Act
        Watch(tab).RaiseCreated("b.jpg");

        // Assert
        await WaitUntilAsync(() => tab.Gallery.LoadingState is GalleryLoadingState.Loaded &&
                                   tab.Gallery.GalleryItems.Count == 3);
        Assert.Equal(["a.jpg", "b.jpg", "c.jpg"], tab.Gallery.GalleryItems.Select(item => item.FileInfo!.Name));
    }

    [Fact]
    public async Task OnFileChanged_LoadedGallery_RebuildsTheFilesItem()
    {
        // Arrange
        var files = await FilesOnDiskAsync("a.jpg", "b.jpg");
        var filePath = files[0].FullName;
        var tab = CreateTab(_testDirectory, files, files[1]);
        LoadGallery(tab);
        var before = tab.Gallery.GalleryItems[0];

        // Act
        Watch(tab).RaiseChanged("a.jpg");

        // Assert: a new item, whose thumbnail loads again when it is shown
        await WaitUntilAsync(() => !ReferenceEquals(before, tab.Gallery.GalleryItems[0]));
        Assert.Equal(filePath, tab.Gallery.GalleryItems[0].FileInfo!.FullName);
        Assert.Null(tab.Gallery.GalleryItems[0].Image.Value);
    }

    [Fact]
    public async Task OnFileRenamed_LoadedGallery_MovesTheFilesItem()
    {
        // Arrange
        var files = await FilesOnDiskAsync("a.jpg", "b.jpg");
        var tab = CreateTab(_testDirectory, files, files[1]);
        LoadGallery(tab);
        await WriteFileAsync("c.jpg");

        // Act
        Watch(tab).RaiseRenamed("a.jpg", "c.jpg");

        // Assert
        await WaitUntilAsync(() => tab.Gallery.GalleryItems.LastOrDefault()?.FileInfo?.Name == "c.jpg");
        Assert.Equal(["b.jpg", "c.jpg"], tab.Gallery.GalleryItems.Select(item => item.FileInfo!.Name));
    }

    [Fact]
    public async Task OnFileDeleted_LoadedGallery_RemovesTheFilesItem()
    {
        // Arrange
        var files = await FilesOnDiskAsync("a.jpg", "b.jpg", "c.jpg");
        var tab = CreateTab(_testDirectory, files, files[0]);
        LoadGallery(tab);

        // Act
        Watch(tab).RaiseDeleted("b.jpg");

        // Assert
        await WaitUntilAsync(() => tab.Gallery.GalleryItems.Count == 2);
        Assert.Equal(["a.jpg", "c.jpg"], tab.Gallery.GalleryItems.Select(item => item.FileInfo!.Name));
    }

    [Fact]
    public void FileDetailsSource_AskedByTwoTabs_ReadsTheFileOnce()
    {
        var details = new FileDetailsSource(new FileInfo(Path.Combine(_testDirectory, "a.jpg")),
            waitUntilReady: false);

        var first = details.GetAsync(TestContext.Current.CancellationToken);
        var second = details.GetAsync(TestContext.Current.CancellationToken);

        Assert.Same(first, second);
    }

    [Fact]
    public async Task OnFileCreated_OnDisk_ReachesTheTab()
    {
        // Arrange: the watcher PicView uses, on a real folder
        using var service = new FileWatcherService((s1, s2) => string.CompareOrdinal(s1, s2), _cache,
            _mockThumbnailCache);
        var existingFile = new FileInfo(Path.Combine(_testDirectory, "existing.jpg"));
        var tab = new TabViewModel(null!, service);
        tab.Model = new ImageModel { FileInfo = existingFile };
        tab.Initialize(_cache, _mockThumbnailCache, new MockThumbnailLoader(), service, _mockThumbnailCache);
        tab.InitializeImageIterator([existingFile], _cache, _mockThumbnailCache, new MockThumbnailLoader(), null,
            _mockThumbnailCache);

        // Act
        var filePath = Path.Combine(_testDirectory, "test.jpg");
        await File.WriteAllTextAsync(filePath, "dummy content", TestContext.Current.CancellationToken);

        // Assert
        await WaitUntilAsync(() => tab.ImageIterator.Files.Any(f => f.FullName == filePath));
    }

    [Fact]
    public async Task OnFileDeleted_LastFile_MakesTheTabFresh()
    {
        // Arrange
        var actionRunner = new Input.RecordingActionRunner();
        var files = await FilesOnDiskAsync("a.jpg", "b.jpg");
        var tab = CreateTab(_testDirectory, files, files[0], WindowWith(actionRunner));
        LoadGallery(tab);
        var watch = Watch(tab);

        // Act
        watch.RaiseDeleted("a.jpg");
        watch.RaiseDeleted("b.jpg");

        // Assert: it no longer browses the folder or shows its file, as a tab that never browsed files
        await WaitUntilAsync(() => actionRunner.StartUpMenuTabs.Count == 1);
        Assert.Same(tab, actionRunner.StartUpMenuTabs[0]);
        Assert.True(tab.IsFresh);
        Assert.Null(tab.ImageIterator);
        Assert.Null(tab.Model.FileInfo);
        Assert.Null(tab.FileInfo.Value);
        Assert.Empty(tab.Gallery.GalleryItems);
        Assert.True(watch.IsDisposed, "A fresh tab follows no folder");
    }

    [Fact]
    public async Task OnFileDeleted_LastFile_ShowsTheStartUpMenuInEveryTabOfTheFolderOnly()
    {
        // Arrange: two tabs in the emptied folder, and the window's active tab in another folder
        var actionRunner = new Input.RecordingActionRunner();
        var window = WindowWith(actionRunner);
        var files = Files("a.jpg");
        var first = CreateTab(_testDirectory, files, files[0], window);
        var second = CreateTab(_testDirectory, files, files[0], window);
        var otherDirectory = Path.Combine(_testDirectory, "other");
        var otherFiles = new List<FileInfo> { new(Path.Combine(otherDirectory, "x.jpg")) };
        var other = CreateTab(otherDirectory, otherFiles, otherFiles[0], window);
        var watch = Watch(first);
        _service.Watch(second);
        _service.Watch(other);

        // Act
        watch.RaiseDeleted("a.jpg");

        // Assert
        await WaitUntilAsync(() => actionRunner.StartUpMenuTabs.Count == 2);
        Assert.Equal([first, second], actionRunner.StartUpMenuTabs);
        Assert.True(first.IsFresh);
        Assert.True(second.IsFresh);
        Assert.Single(other.ImageIterator!.Files);
    }

    [Fact]
    public async Task OnSubfolderDeleted_RemovesItsFiles()
    {
        // Arrange: browsing subdirectories, showing a file in the subfolder
        Settings.Sorting.IncludeSubDirectories = true;
        var files = Files("a.jpg", Path.Combine("sub", "b.jpg"), Path.Combine("sub", "c.jpg"), "d.jpg");
        var tab = CreateTab(_testDirectory, files, files[1]);

        // Act
        Watch(tab).RaiseDeleted("sub");

        // Assert
        await WaitUntilAsync(() => tab.ImageIterator!.Files.Count == 2);
        Assert.Equal(["a.jpg", "d.jpg"], tab.ImageIterator!.Files.Select(f => f.Name));
    }

    [Fact]
    public async Task OnSubfolderDeleted_OnDisk_SubfolderSentToTheRecycleBin_RemovesItsFiles()
    {
        // Arrange
        Settings.Sorting.IncludeSubDirectories = true;
        var subfolder = Path.Combine(_testDirectory, "sub");
        Directory.CreateDirectory(subfolder);
        var files = await FilesOnDiskAsync("a.jpg", Path.Combine("sub", "b.jpg"));
        using var service = new FileWatcherService((s1, s2) => string.CompareOrdinal(s1, s2), _cache,
            _mockThumbnailCache);
        var tab = new TabViewModel(null!, service);
        tab.Model = new ImageModel { FileInfo = files[0] };
        tab.Initialize(_cache, _mockThumbnailCache, new MockThumbnailLoader(), service, _mockThumbnailCache);
        tab.InitializeImageIterator(files, _cache, _mockThumbnailCache, new MockThumbnailLoader(), null,
            _mockThumbnailCache);

        // Act: moved out of the watched folder, as the Recycle Bin does, so no event comes for its files
        var outside = Path.Combine(Path.GetTempPath(), "PicViewTests_Moved_" + Guid.NewGuid());
        Directory.Move(subfolder, outside);

        try
        {
            // Assert
            await WaitUntilAsync(() => tab.ImageIterator!.Files.Count == 1);
            Assert.Equal("a.jpg", tab.ImageIterator!.Files[0].Name);
        }
        finally
        {
            Directory.Delete(outside, true);
        }
    }

    [Fact]
    public async Task OnSubfolderRenamed_ItsFilesFollowIt()
    {
        // Arrange: browsing subdirectories, showing a file in the subfolder
        Settings.Sorting.IncludeSubDirectories = true;
        var files = Files("a.jpg", Path.Combine("sub", "b.jpg"), Path.Combine("sub", "c.jpg"));
        var tab = CreateTab(_testDirectory, files, files[1]);
        var renamed = Path.Combine(_testDirectory, "renamed");

        // Act
        Watch(tab).RaiseRenamed("sub", "renamed");

        // Assert: in the same order, still showing the same file
        await WaitUntilAsync(() => tab.ImageIterator!.Files[1].DirectoryName == renamed);
        Assert.Equal(
            [files[0].FullName, Path.Combine(renamed, "b.jpg"), Path.Combine(renamed, "c.jpg")],
            tab.ImageIterator!.Files.Select(f => f.FullName));
        Assert.Equal(Path.Combine(renamed, "b.jpg"), tab.Model.FileInfo?.FullName);
        Assert.Equal(Path.Combine(renamed, "b.jpg"), tab.FileInfo.Value?.FullName);
    }

    [Fact]
    public async Task OnSubfolderRenamed_LoadedGallery_ItsItemsFollowIt()
    {
        // Arrange
        Settings.Sorting.IncludeSubDirectories = true;
        Directory.CreateDirectory(Path.Combine(_testDirectory, "sub"));
        var files = await FilesOnDiskAsync("a.jpg", Path.Combine("sub", "b.jpg"));
        var tab = CreateTab(_testDirectory, files, files[0]);
        LoadGallery(tab);
        var renamed = Path.Combine(_testDirectory, "renamed");
        var watch = Watch(tab);

        // Act: the item reads its file's size and date there
        Directory.Move(Path.Combine(_testDirectory, "sub"), renamed);
        watch.RaiseRenamed("sub", "renamed");

        // Assert
        await WaitUntilAsync(() => tab.Gallery.GalleryItems[1].FileInfo?.DirectoryName == renamed);
        Assert.Equal(["a.jpg", "b.jpg"], tab.Gallery.GalleryItems.Select(item => item.FileInfo!.Name));
    }

    [Fact]
    public async Task OnFolderRenamed_EveryTabOfTheFolderFollowsIt()
    {
        // Arrange
        var files = Files("a.jpg", "b.jpg");
        var first = CreateTab(_testDirectory, files, files[1]);
        var second = CreateTab(_testDirectory, files, files[0]);
        var watch = Watch(first);
        _service.Watch(second);
        var renamed = _testDirectory + "_renamed";

        // Act
        watch.RaiseMoved(renamed);

        // Assert: both tabs browse the folder under its new name, showing the same files
        await WaitUntilAsync(() => first.ImageIterator!.Files[0].DirectoryName == renamed &&
                                   second.ImageIterator!.Files[0].DirectoryName == renamed);
        Assert.Equal(Path.Combine(renamed, "b.jpg"), first.Model.FileInfo?.FullName);
        Assert.Equal(Path.Combine(renamed, "a.jpg"), second.Model.FileInfo?.FullName);
    }

    [Fact]
    public async Task OnFolderRenamed_WatchesTheFolderUnderItsNewName()
    {
        // Arrange
        var files = Files("a.jpg", "b.jpg");
        var first = CreateTab(_testDirectory, files, files[0]);
        var second = CreateTab(_testDirectory, files, files[0]);
        var watch = Watch(first);
        _service.Watch(second);
        var renamed = _testDirectory + "_renamed";

        // Act
        watch.RaiseMoved(renamed);
        await WaitUntilAsync(() => _folderEvents.Watches.Count == 2);
        var restarted = _folderEvents.Watches[^1];
        restarted.RaiseDeleted("b.jpg");

        // Assert: one watch under the new name, which still reaches both tabs
        Assert.True(watch.IsDisposed);
        Assert.Equal(renamed, restarted.Directory);
        await WaitUntilAsync(() => first.ImageIterator!.Files.Count == 1 && second.ImageIterator!.Files.Count == 1);
    }

    [Fact]
    public async Task OnFolderRenamed_OnDisk_TabFollowsIt()
    {
        // Arrange
        var folder = Path.Combine(_testDirectory, "watched");
        Directory.CreateDirectory(folder);
        var file = new FileInfo(Path.Combine(folder, "a.jpg"));
        await File.WriteAllTextAsync(file.FullName, "dummy content", TestContext.Current.CancellationToken);
        using var service = new FileWatcherService((s1, s2) => string.CompareOrdinal(s1, s2), _cache,
            _mockThumbnailCache);
        var tab = new TabViewModel(null!, service);
        tab.Model = new ImageModel { FileInfo = file };
        tab.Initialize(_cache, _mockThumbnailCache, new MockThumbnailLoader(), service, _mockThumbnailCache);
        tab.InitializeImageIterator([file], _cache, _mockThumbnailCache, new MockThumbnailLoader(), null,
            _mockThumbnailCache);
        var renamed = Path.Combine(_testDirectory, "renamed");

        // Act
        Directory.Move(folder, renamed);
        await WaitUntilAsync(() => tab.Model.FileInfo?.DirectoryName == renamed);
        var created = Path.Combine(renamed, "b.jpg");
        await File.WriteAllTextAsync(created, "dummy content", TestContext.Current.CancellationToken);

        // Assert: the folder is watched under its new name
        await WaitUntilAsync(() => tab.ImageIterator!.Files.Any(f => f.FullName == created));
    }

    [Fact]
    public async Task OnFolderGone_MakesEveryTabOfTheFolderFresh()
    {
        // Arrange
        var actionRunner = new Input.RecordingActionRunner();
        var window = WindowWith(actionRunner);
        var files = Files("a.jpg", "b.jpg");
        var first = CreateTab(_testDirectory, files, files[0], window);
        var second = CreateTab(_testDirectory, files, files[1], window);
        var watch = Watch(first);
        _service.Watch(second);

        // Act
        watch.RaiseGone();

        // Assert
        await WaitUntilAsync(() => actionRunner.StartUpMenuTabs.Count == 2);
        Assert.Equal([first, second], actionRunner.StartUpMenuTabs);
        Assert.Null(first.ImageIterator);
        Assert.Null(second.ImageIterator);
        Assert.True(watch.IsDisposed);
    }

    [Fact]
    public async Task OnFolderGone_OnDisk_FolderSentToTheRecycleBin_MakesTheTabFresh()
    {
        // Arrange: moving the folder away, as the Recycle Bin does, raises no event for its files
        var folder = Path.Combine(_testDirectory, "watched");
        Directory.CreateDirectory(folder);
        var file = new FileInfo(Path.Combine(folder, "a.jpg"));
        await File.WriteAllTextAsync(file.FullName, "dummy content", TestContext.Current.CancellationToken);
        using var service = new FileWatcherService((s1, s2) => string.CompareOrdinal(s1, s2), _cache,
            _mockThumbnailCache);
        var actionRunner = new Input.RecordingActionRunner();
        var tab = new TabViewModel(WindowWith(actionRunner), service);
        tab.Model = new ImageModel { FileInfo = file };
        tab.Initialize(_cache, _mockThumbnailCache, new MockThumbnailLoader(), service, _mockThumbnailCache);
        tab.InitializeImageIterator([file], _cache, _mockThumbnailCache, new MockThumbnailLoader(), null,
            _mockThumbnailCache);

        // Act: into another folder, as the Recycle Bin does
        var otherFolder = Path.Combine(_testDirectory, "other");
        Directory.CreateDirectory(otherFolder);
        Directory.Move(folder, Path.Combine(otherFolder, "watched"));

        // Assert
        await WaitUntilAsync(() => actionRunner.StartUpMenuTabs.Count == 1);
        Assert.Null(tab.ImageIterator);
    }

    [Fact]
    public async Task OnFileDeleted_OnDisk_AllFilesAtOnce_MakesTheTabFresh()
    {
        // Arrange: the watcher PicView uses, and a loader that fails for a file that is gone, as PicView's does
        var cache = new SharedImageCache(static f =>
            ValueTask.FromResult(File.Exists(f.FullName)
                ? new ImageModel { FileInfo = f, Image = new object() }
                : new ImageModel { FileInfo = f, ImageType = ImageType.Invalid }));
        using var service = new FileWatcherService((s1, s2) => string.CompareOrdinal(s1, s2), cache,
            _mockThumbnailCache);
        var actionRunner = new Input.RecordingActionRunner();
        var files = await FilesOnDiskAsync(Enumerable.Range(1, 30).Select(i => $"{i:D2}.jpg").ToArray());
        var tab = new TabViewModel(WindowWith(actionRunner), service);
        tab.Model = new ImageModel { FileInfo = files[0] };
        tab.Initialize(cache, _mockThumbnailCache, new MockThumbnailLoader(), service, _mockThumbnailCache);
        tab.InitializeImageIterator(files, cache, _mockThumbnailCache, new MockThumbnailLoader(), null,
            _mockThumbnailCache);

        // Act
        foreach (var file in files)
        {
            File.Delete(file.FullName);
        }

        // Assert
        await WaitUntilAsync(() => actionRunner.StartUpMenuTabs.Count == 1);
        Assert.True(tab.IsFresh);
        Assert.Null(tab.ImageIterator);
    }

    private static MainWindowViewModel WindowWith(Input.RecordingActionRunner actionRunner) =>
        new(null!, null!, null!, null!, null!) { ActionRunner = actionRunner };

    /// <summary>
    /// Files that exist, as a gallery load reads each file's size and date.
    /// </summary>
    private async Task<List<FileInfo>> FilesOnDiskAsync(params string[] names)
    {
        foreach (var name in names)
        {
            await WriteFileAsync(name);
        }
        return Files(names);
    }

    private static void LoadGallery(TabViewModel tab) =>
        tab.GalleryCoordinator!.LoadGallery(tab.ImageIterator.Files, null!);

    /// <summary>
    /// Writes a file the folder event is about, so waiting for it to be ready ends at once.
    /// </summary>
    private async Task<string> WriteFileAsync(string name)
    {
        var path = Path.Combine(_testDirectory, name);
        await File.WriteAllTextAsync(path, "dummy content", TestContext.Current.CancellationToken);
        return path;
    }

    private FakeFolderWatch Watch(TabViewModel tab)
    {
        _service.Watch(tab);
        return _folderEvents.Watches.Single(w => w.Directory == _testDirectory);
    }

    private List<FileInfo> Files(params string[] names) =>
        names.Select(name => new FileInfo(Path.Combine(_testDirectory, name))).ToList();

    private TabViewModel CreateTab(string directory, List<FileInfo>? files = null, FileInfo? shown = null,
        MainWindowViewModel? window = null)
    {
        var tab = new TabViewModel(window!, _service);
        // Watch() finds the directory from the model, so give it a file in that directory
        var dummyFile = new FileInfo(Path.Combine(directory, "placeholder.png"));
        tab.Model = new ImageModel { FileInfo = dummyFile };

        tab.Initialize(_cache, _mockThumbnailCache, new MockThumbnailLoader(), _service, _mockThumbnailCache);
        if (files is not null)
        {
            // Without the watcher, so a test decides which tab watches first
            tab.InitializeImageIterator(files, _cache, _mockThumbnailCache, new MockThumbnailLoader(), null,
                _mockThumbnailCache);
            tab.Model = new ImageModel { FileInfo = shown ?? files[0] };
        }
        return tab;
    }

    /// <summary>
    /// Waits for the events raised to be handled, which can finish on another thread.
    /// </summary>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = Environment.TickCount64 + 5000;
        while (!condition())
        {
            Assert.True(Environment.TickCount64 < deadline, "The folder event was not handled in time");
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch { /* Best effort cleanup */ }
        }
    }

    // Mocks
    private sealed class FakeFolderEvents : IFolderEvents
    {
        public List<FakeFolderWatch> Watches { get; } = [];

        public IFolderWatch Watch(string directory, bool includeSubdirectories)
        {
            var watch = new FakeFolderWatch(directory, includeSubdirectories);
            Watches.Add(watch);
            return watch;
        }
    }

    private sealed class FakeFolderWatch(string directory, bool includeSubdirectories) : IFolderWatch
    {
        private readonly Subject<FileSystemEventArgs> _created = new();
        private readonly Subject<FileSystemEventArgs> _changed = new();
        private readonly Subject<RenamedEventArgs> _renamed = new();
        private readonly Subject<FileSystemEventArgs> _deleted = new();
        private readonly Subject<Unit> _gone = new();
        private readonly Subject<string> _moved = new();

        public string Directory { get; } = directory;
        public bool IncludesSubdirectories { get; } = includeSubdirectories;
        public bool IsDisposed { get; private set; }

        public Observable<FileSystemEventArgs> Created => _created;
        public Observable<FileSystemEventArgs> Changed => _changed;
        public Observable<RenamedEventArgs> Renamed => _renamed;
        public Observable<FileSystemEventArgs> Deleted => _deleted;
        public Observable<Unit> Gone => _gone;
        public Observable<string> Moved => _moved;

        public void RaiseCreated(string name) =>
            _created.OnNext(new FileSystemEventArgs(WatcherChangeTypes.Created, Directory, name));

        public void RaiseChanged(string name) =>
            _changed.OnNext(new FileSystemEventArgs(WatcherChangeTypes.Changed, Directory, name));

        public void RaiseRenamed(string oldName, string newName) =>
            _renamed.OnNext(new RenamedEventArgs(WatcherChangeTypes.Renamed, Directory, newName, oldName));

        public void RaiseDeleted(string name) =>
            _deleted.OnNext(new FileSystemEventArgs(WatcherChangeTypes.Deleted, Directory, name));

        public void RaiseGone() => _gone.OnNext(Unit.Default);

        public void RaiseMoved(string newDirectory) => _moved.OnNext(newDirectory);

        public void Dispose()
        {
            IsDisposed = true;
            _created.Dispose();
            _changed.Dispose();
            _renamed.Dispose();
            _deleted.Dispose();
            _gone.Dispose();
            _moved.Dispose();
        }
    }

    private class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private class MockThumbnailCache : IThumbnailCache
    {
        public List<string> RemovedPaths { get; } = new();

        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail) { thumbnail = null; return false; }
        public void Remove(string path) => RemovedPaths.Add(path);
        public void RemoveOwner(uint ownerId) { }
        public bool IsEmpty => true;
    }

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}

using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileSorting;
using PicView.Core.Http;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;
using static PicView.Core.Navigation.FileTypeResolver;

namespace PicView.Core.Navigation;

/// <summary>
/// The one owner of every Load of a Source into its tab (ADR-0019): it takes the load version and the Archive
/// Openings mark, makes the tab's iterator when it has none, says Loading, shows the image, lists the files around it,
/// watches their folder, primes the cache, reports which opening is shown, and publishes whether the tab is loading.
/// </summary>
public sealed class TabLoad
{
    /// <summary>
    /// How long to wait for the folder watcher to add a new file before rebuilding the file list instead.
    /// </summary>
    private static readonly TimeSpan FolderWatcherWait = TimeSpan.FromSeconds(2);

    private readonly TabViewModel _tab;
    private readonly TabServices _services;
    private readonly Func<string, string, int> _compareNames;
    private readonly BindableReactiveProperty<bool> _isLoading = new();

    // What IsLoading should say, and a stamp of each change to it, so a change published late cannot overwrite a newer
    // one. Only the newest load's end clears it: an overtaken load still waiting leaves it to the newer one
    private readonly Lock _loadingLock = new();
    private bool _loading;
    private int _loadingStamp;

    internal TabLoad(TabViewModel tab, TabServices services)
    {
        _tab = tab;
        _services = services;
        // The platform is read when files are sorted, not here, so a tab whose services lack one still has a Load
        _compareNames = CompareNames;
    }

    /// <summary>
    /// Whether a Load of this tab is running: true from its start until the newest Load shows, is superseded or fails.
    /// An overtaken Load still waiting does not keep it true.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<bool> IsLoading => _isLoading;

    /// <summary>
    /// Loads a Source into the tab: a file, a folder, an archive or a URL. Completes once the image shows, a newer Load
    /// supersedes this one, or it fails, since a step Back or Forward ends when its load completes (ADR-0012).
    /// Callable from any thread; never waits for the UI thread; shows the image before it lists files.
    /// </summary>
    public ValueTask<LoadResult> LoadAsync(string source, LoadOptions options = default)
    {
        // A source that resolves to nothing, such as a deleted file, begins no load, so it changes nothing
        if (string.IsNullOrWhiteSpace(source) || CheckIfLoadableString(source) is not { } resolved)
        {
            return ValueTask.FromResult(LoadResult.Failed());
        }

        // Base64 has no path to list or show from. A relist lists around a file the tab shows
        if (resolved.Type is LoadAbleFileType.Base64 || (options.Relist && resolved.Type is not LoadAbleFileType.File))
        {
            return ValueTask.FromResult(LoadResult.Failed());
        }

        return RunAsync(resolved, options);
    }

    // The protocol: begin a version, mark, say Loading, load the kind of source, report the shown opening, end
    private async ValueTask<LoadResult> RunAsync(FileTypeStruct resolved, LoadOptions options)
    {
        var run = new Run(_tab.BeginLoad());
        // A tab that closed has no load to run
        if (!IsCurrent(run))
        {
            return LoadResult.Superseded;
        }

        Started();
        run.Ticket = _tab.SetLoading();
        // The archives the tab opened so far, which it leaves once the image shows. One that opens after survives this
        // load. A relist shows nothing new and leaves the archive it shows open
        var leavesArchives = !options.Relist && resolved.Type is not LoadAbleFileType.Web;
        var mark = leavesArchives ? _tab.Archives.Mark() : default;
        try
        {
            run.Cancellation = _tab.GetTabCancellation();
            run.Token = run.Cancellation.Token;
            return resolved.Type switch
            {
                LoadAbleFileType.File when options.Relist =>
                    await RelistAsync(new FileInfo(resolved.Data), options.RootDirectory, run).ConfigureAwait(false),
                LoadAbleFileType.File => await LoadFileAsync(new FileInfo(resolved.Data), options, run).ConfigureAwait(false),
                LoadAbleFileType.Directory => await LoadFolderAsync(resolved.Data, run).ConfigureAwait(false),
                LoadAbleFileType.Zip => await LoadArchiveAsync(resolved.Data, options.ArchiveEntry, run).ConfigureAwait(false),
                LoadAbleFileType.Web => await LoadUrlAsync(resolved.Data, run).ConfigureAwait(false),
                _ => LoadResult.Failed()
            };
        }
        catch (OperationCanceledException)
        {
            return IsCurrent(run) ? LoadResult.Failed() : LoadResult.Superseded;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(TabLoad), nameof(LoadAsync), e);
            if (!IsCurrent(run))
            {
                return LoadResult.Superseded;
            }

            return run.IsShown ? LoadResult.Loaded : LoadResult.Failed();
        }
        finally
        {
            if (run.IsShown)
            {
                // Only a load whose image shows leaves the archives, as a load that a newer one overtakes leaves them
                // to it
                if (leavesArchives)
                {
                    _tab.Archives.Shown(mark);
                }
            }
            else
            {
                // This load said Loading and showed nothing, so the titles say what the tab shows again, unless a newer
                // load said Loading since
                _tab.EndLoading(run.Ticket);
            }

            Ended(run);
        }
    }

    private async ValueTask<LoadResult> LoadFileAsync(FileInfo file, LoadOptions options, Run run)
    {
        // A file in the tab's list is stepped to, unless the caller gave its model or its own listing
        if (options.Model is not null || options.RootDirectory is not null || options.Files is not null ||
            _tab.ImageIterator is not { Files.Count: > 0 } iterator)
        {
            return await Task.Run(() => ShowThenListAsync(file, options.Files, options.Model, options.SecondaryModel,
                    options.RootDirectory, run).AsTask(),
                run.Token).ConfigureAwait(false);
        }

        var index = SideBySidePair.IndexOf(iterator.Files, file);
        if (index is null && IsInShownFolder(file))
        {
            // A file just written next to the shown one, such as a duplicate, is added by the folder watcher,
            // which also adds it to the gallery, so wait for it rather than rebuilding the list and the gallery
            if (await iterator.WaitForFileAsync(file.FullName, FolderWatcherWait, run.Token).ConfigureAwait(false))
            {
                index = SideBySidePair.IndexOf(iterator.Files, file);
            }

            if (!IsCurrent(run))
            {
                return LoadResult.Superseded;
            }

            if (run.Token.IsCancellationRequested)
            {
                return LoadResult.Failed();
            }
        }

        if (index is not { } found)
        {
            return await Task.Run(() => ShowThenListAsync(file, options.Files, options.Model, options.SecondaryModel,
                    options.RootDirectory, run).AsTask(),
                run.Token).ConfigureAwait(false);
        }

        await iterator.IterateToIndexAsync(found, run.Cancellation).ConfigureAwait(false);
        if (!IsCurrent(run))
        {
            return LoadResult.Superseded;
        }

        // The step shows nothing when the file left the list meanwhile, or its image could not be read, so
        // this load showed its file only when the tab shows it now, beside the image before it when it is the
        // last of a pair (ADR-0024); otherwise its end ends its Loading
        run.IsShown = IsModelOf(_tab.Model, file) || IsModelOf(_tab.SecondaryModel, file);
        return run.IsShown ? LoadResult.Loaded : LoadResult.Failed();

        // TODO: Figure out way to share file list, if another tab is already in the same directory
        // AsTask, because Task.Run waits for a Task but returns a ValueTask without awaiting it
    }

    private async ValueTask<LoadResult> LoadFolderAsync(string folder, Run run)
    {
        var files = await Task.Run(() => FileListRetriever.RetrieveFiles(new FileInfo(folder), _compareNames), run.Token)
            .ConfigureAwait(false);
        if (!IsCurrent(run))
        {
            return LoadResult.Superseded;
        }

        if (files.Count is 0)
        {
            return LoadResult.Failed();
        }

        return await ShowThenListAsync(files[0], files, null, null, null, run).ConfigureAwait(false);
    }

    private async ValueTask<LoadResult> LoadArchiveAsync(string archive, string? startEntry, Run run)
    {
        ArchiveOpening? opening = null;
        try
        {
            opening = await _tab.Archives.OpenAsync(archive, startEntry, _services.Platform.ExtractWithLocalSoftwareAsync,
                _compareNames, run.Token).ConfigureAwait(false);
            // A newer load of a file overtakes this one too, which the opening cannot see
            if (!IsCurrent(run))
            {
                return LoadResult.Superseded;
            }

            if (opening is null)
            {
                return LoadResult.Failed();
            }

            var result = await ShowThenListAsync(opening.StartFile, opening.Files, null, null, null, run)
                .ConfigureAwait(false);
            if (run.IsShown)
            {
                // The other pages come in the background while the start page shows
                opening.ExtractRest();
            }

            return result;
        }
        finally
        {
            if (!run.IsShown)
            {
                // Unless a page of it shows, the opening goes again, and the tab's archive stays the one it shows
                opening?.Abandon();
            }
        }
    }

    private async ValueTask<LoadResult> LoadUrlAsync(string url, Run run)
    {
        // Drops the iterator, as for a clipboard image, so browsing files again makes a new one and SourceOpener docks
        // the gallery again
        _tab.DisposeImageIterator();

        var platform = _services.Platform;
        platform.StopTaskbarProgress();
        var safeFileName = HttpManager.GetSafeFileName(url);
        var destPath = TempFileManager.GetNewTempFilePath(safeFileName);

        using var client = new HttpClientDownloadWithProgress(url, destPath);
        client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
        {
            // A newer load owns the titles and the taskbar, so an overtaken download reports nothing
            if (!IsCurrent(run))
            {
                return;
            }

            var displayProgress = HttpManager.GetProgressDisplay(totalFileSize, totalBytesDownloaded, progressPercentage);
            // Each report enters Downloading anew, and its ticket is the one this load ends
            run.Ticket = _tab.SetDownloading(
                $"{safeFileName} {TranslationManager.Translation?.Downloading} {displayProgress}");

            if (Settings.UIProperties.IsTaskbarProgressEnabled && totalBytesDownloaded.HasValue && totalFileSize.HasValue)
            {
                platform.SetTaskbarProgress((ulong)totalBytesDownloaded.Value, (ulong)totalFileSize.Value);
            }
        };

        try
        {
            await client.StartDownloadAsync(run.Token).ConfigureAwait(false);

            platform.StopTaskbarProgress();

            // Nothing shows, so the progress text must not stay in the titles: the end of the load ends its ticket
            if (run.Token.IsCancellationRequested)
            {
                return LoadResult.Failed();
            }

            if (!IsCurrent(run))
            {
                return LoadResult.Superseded;
            }

            var model = await _services.ImageLoader.GetImageModelAsync(new FileInfo(destPath), run.Token)
                .ConfigureAwait(false);
            if (!IsCurrent(run))
            {
                return LoadResult.Superseded;
            }

            // Set before the image is shown, since the tab records the URL then
            _tab.SourceURL = url;
            _tab.DisplaySingleImage(model, SingleImageType.Url, url);
            run.IsShown = true;

            _tab.Archives.Cleanup();
            return LoadResult.Loaded;
        }
        catch (OperationCanceledException)
        {
            platform.StopTaskbarProgress();
            return IsCurrent(run) ? LoadResult.Failed() : LoadResult.Superseded;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(TabLoad), nameof(LoadUrlAsync), e);
            platform.StopTaskbarProgress();
            if (!IsCurrent(run))
            {
                return LoadResult.Superseded;
            }

            // Held until the next load, so the user sees that this one failed
            _tab.SetError(TranslationManager.Translation?.ErrorLoadingImage ?? "Error");
            return LoadResult.Failed(e.Message);
        }
    }

    /// <summary>
    /// Shows <paramref name="file"/>, from <paramref name="given"/> or the cache, then lists the files around it:
    /// <paramref name="files"/>, or those of <paramref name="rootDirectory"/> when the file is inside it, or those of
    /// its own folder. Side by side, the pair shows once the files are listed, with <paramref name="givenSecondary"/>
    /// beside it when that is the partner's model, else the partner from the cache. Then it watches the folder and
    /// primes the cache. Once a newer load begins, it stops before changing the tab's image, files or gallery, so a slow
    /// load that finishes last cannot overwrite a newer one.
    /// </summary>
    private async ValueTask<LoadResult> ShowThenListAsync(FileInfo file, IReadOnlyList<FileInfo>? files,
        ImageModel? given, ImageModel? givenSecondary, string? rootDirectory, Run run)
    {
        // A newer load that began meanwhile watches its own folder, which this one must not stop
        if (!IsCurrent(run))
        {
            return LoadResult.Superseded;
        }

        var cache = _services.Cache;
        var watcher = _services.Watcher;
        // Made before the unwatch, which also ends any watch that making it started
        EnsureIterator();
        watcher.Unwatch(_tab);

        // Show image quickly to make it feel fast
        var model = given ?? await cache.GetAsync(_tab.Id, file, run.Token).ConfigureAwait(false);
        if (!IsCurrent(run))
        {
            return LoadResult.Superseded;
        }

        if (model is null)
        {
            return LoadResult.Failed();
        }

        var (isSideBySide, looping) = _tab.PairSettings();
        if (!isSideBySide)
        {
            _tab.Display(model);
            run.IsShown = true;
        }

        await Task.Yield();
        var retrieved = files ?? ListAround(file, rootDirectory);
        if (!IsCurrent(run))
        {
            return LoadResult.Superseded;
        }

        var index = SideBySidePair.IndexOf(retrieved, file) ?? 0;
        // The opened file shows in the pair that holds it, so the last file shows beside the one before it (ADR-0024)
        var (primary, partner) = SideBySidePair.PairAt(index, retrieved.Count, isSideBySide, looping);
        var iterator = EnsureIterator();
        iterator.Initialize(retrieved, primary);

        // The image of the pair that is not the opened file, and where it is
        ImageModel? otherModel = null;
        var otherIndex = -1;
        if (isSideBySide)
        {
            if (partner is { } partnerIndex)
            {
                otherIndex = primary == index ? partnerIndex : primary;
                var otherFile = iterator.Files[otherIndex];
                // The startup path decodes the other image of the pair beside the opened file, so it is not decoded again
                otherModel = IsModelOf(givenSecondary, otherFile)
                    ? givenSecondary
                    : await cache.GetAsync(_tab.Id, otherFile, run.Token).ConfigureAwait(false);
                if (!IsCurrent(run))
                {
                    return LoadResult.Superseded;
                }

                if (otherModel is null)
                {
                    return LoadResult.Failed();
                }
            }

            if (otherModel is null)
            {
                _tab.Display(model);
            }
            else if (primary == index)
            {
                _tab.Display(model, otherModel);
            }
            else
            {
                _tab.Display(otherModel, model);
            }

            run.IsShown = true;
        }

        // A newer load that begins after this sets its own files after these
        if (!IsCurrent(run))
        {
            return LoadResult.Superseded;
        }

        watcher.Watch(_tab, file.DirectoryName);
        cache.ReleaseImages(_tab.Id);
        if (iterator.Files.Count > 0)
        {
            cache.Put(_tab.Id, iterator.Files[index], model);
            if (otherModel is not null)
            {
                cache.Put(_tab.Id, iterator.Files[otherIndex], otherModel);
            }

            var currentFile = iterator.Files[primary];

            cache.Preload(_tab.Id, currentFile, false, _tab.GetTabCancellation().Token);
        }

        return LoadResult.Loaded;
    }

    /// <summary>
    /// Lists the shown <paramref name="file"/>'s folder again, or <paramref name="rootDirectory"/> when the file is
    /// inside it, without showing anything, and keeps the file the tab shows once the listing is done at its place. A
    /// shown file the listing no longer has changes nothing.
    /// </summary>
    private async ValueTask<LoadResult> RelistAsync(FileInfo file, string? rootDirectory, Run run)
    {
        var files = await Task.Run(() => ListAround(file, rootDirectory), run.Token).ConfigureAwait(false);
        if (!IsCurrent(run))
        {
            return LoadResult.Superseded;
        }

        // A step while the listing ran shows another file, which keeps its place. A tab that shows nothing keeps the
        // file the relist was asked for
        var shown = _tab.Model?.FileInfo ?? file;
        if (SideBySidePair.IndexOf(files, shown) is not { } index)
        {
            return LoadResult.Failed();
        }

        var cache = _services.Cache;
        var watcher = _services.Watcher;
        var iterator = EnsureIterator();
        watcher.Unwatch(_tab);
        iterator.Initialize(files, index);
        watcher.Watch(_tab, shown.DirectoryName);
        cache.ReleaseImages(_tab.Id);
        PutShown(files, _tab.Model);
        PutShown(files, _tab.SecondaryModel);
        cache.Preload(_tab.Id, files[index], false, _tab.GetTabCancellation().Token);
        return LoadResult.Loaded;
    }

    // Keeps a model the tab shows in the cache under its file, when the listing has it
    private void PutShown(IReadOnlyList<FileInfo> files, ImageModel? model)
    {
        if (model?.FileInfo is { } shown && SideBySidePair.IndexOf(files, shown) is { } index)
        {
            _services.Cache.Put(_tab.Id, files[index], model);
        }
    }

    /// <summary>
    /// The files around <paramref name="file"/>: those of <paramref name="rootDirectory"/> when the file is inside it
    /// and the listing has it, else those of the file's own folder.
    /// </summary>
    private List<FileInfo> ListAround(FileInfo file, string? rootDirectory)
    {
        if (rootDirectory is null || !IsInside(file, rootDirectory))
        {
            return FileListRetriever.RetrieveFiles(file, _compareNames);
        }

        var fromRoot = FileListRetriever.RetrieveFiles(new FileInfo(rootDirectory), _compareNames);
        // Without subdirectories, a file deeper in the root is not listed from it
        return SideBySidePair.IndexOf(fromRoot, file) is not null ? fromRoot 
            : FileListRetriever.RetrieveFiles(file, _compareNames);
    }

    private static bool IsModelOf(ImageModel? model, FileInfo file) =>
        string.Equals(model?.FileInfo?.FullName, file.FullName, StringComparison.OrdinalIgnoreCase);

    private static bool IsInside(FileInfo file, string directory)
    {
        var prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        return file.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether <paramref name="file"/> is an image in the folder of the file the tab shows, which the folder
    /// watcher reports when it is created.
    /// </summary>
    private bool IsInShownFolder(FileInfo file) =>
        file.FullName.IsSupported() &&
        string.Equals(file.DirectoryName, _tab.Model?.FileInfo?.DirectoryName, StringComparison.OrdinalIgnoreCase);

    // The tab's iterator, made from the services when it has none: a tab that never browsed files, or whose Single
    // Image disposed it
    private IImageIterator EnsureIterator()
    {
        if (_tab.ImageIterator is { } iterator)
        {
            return iterator;
        }

        _tab.InitializeImageIterator([]);
        return _tab.ImageIterator!;
    }

    private int CompareNames(string x, string y) => _services.Platform.CompareStrings(x, y);

    // Whether no newer load began and the tab is still open, so this load may still change the tab
    private bool IsCurrent(Run run) => !_tab.IsDisposed && _tab.IsLatestLoad(run.Version);

    private void Started()
    {
        int stamp;
        lock (_loadingLock)
        {
            _loading = true;
            stamp = ++_loadingStamp;
        }

        PublishLoading(true, stamp);
    }

    // Only the newest load's end says the tab is no longer loading. An older load ending leaves it to the newer one,
    // which began after it and ends after it
    private void Ended(Run run)
    {
        int stamp;
        lock (_loadingLock)
        {
            if (!_tab.IsLatestLoad(run.Version))
            {
                return;
            }

            _loading = false;
            stamp = ++_loadingStamp;
        }

        PublishLoading(false, stamp);
    }

    // Published outside the lock, so no subscriber runs under it. A change made while this one was published is
    // published again after it, so the last value set is always the newest
    private void PublishLoading(bool loading, int stamp)
    {
        while (true)
        {
            _isLoading.Value = loading;
            lock (_loadingLock)
            {
                if (_loadingStamp == stamp)
                {
                    return;
                }

                loading = _loading;
                stamp = _loadingStamp;
            }
        }
    }

    // One load: its version, the tab's cancellation it reads, the Title Phase ticket it ends, and whether it showed
    private sealed class Run(int version)
    {
        public int Version { get; } = version;

        public CancellationTokenSource Cancellation { get; set; } = null!;

        public CancellationToken Token { get; set; }

        // A download's progress reports replace it from the download's thread
        public int Ticket
        {
            get => Volatile.Read(ref field);
            set => Volatile.Write(ref field, value);
        }

        public bool IsShown { get; set; }
    }
}

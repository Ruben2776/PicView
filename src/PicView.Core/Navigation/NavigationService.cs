using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileSorting;
using PicView.Core.Http;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

public class NavigationService(
    IImageModelLoader imageModelLoader,
    SharedImageCache cache,
    IFileWatcherService fileWatcherService,
    IPlatformSpecificService platformService,
    Func<string, string, int> stringComparer)
    : INavigationService
{
    
    private FileAndDirectoryNavigator? _navigator;

    public async ValueTask RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct, List<FileInfo>? files = null) =>
        await RepopulateIterator(fileInfo, tab, ct, files, tab.BeginLoad()).ConfigureAwait(false);

    /// <param name="load">The version of the load this is part of. Once a newer load begins, this one stops before
    /// changing the tab's image, files or gallery, so a slow load that finishes last cannot overwrite a newer one.</param>
    /// <returns>Whether the tab showed the file.</returns>
    private async ValueTask<bool> RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct,
        IReadOnlyList<FileInfo>? files, int load)
    {
        var isShown = false;
        try
        {
            fileWatcherService.Unwatch(tab);

            // Show image quickly to make it feel fast
            ImageModel? secondaryModel = null;
            var secondaryIndex = 0;
            var model = await cache.GetAsync(tab.Id, fileInfo, ct.Token).ConfigureAwait(false);
            if (model is null || !tab.IsLatestLoad(load))
            {
                return false;
            }

            if (!Settings.ImageScaling.ShowImageSideBySide)
            {
                tab.Display(model);
                isShown = true;
            }

            await Task.Yield();
            var retrieved = files ?? FileListRetriever.RetrieveFiles(fileInfo, stringComparer);
            if (!tab.IsLatestLoad(load))
            {
                return isShown;
            }
            var index = SideBySidePair.IndexOf(retrieved, fileInfo) ?? 0;
            tab.ImageIterator.Initialize(retrieved, index);

            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                if (SideBySidePair.PartnerOf(index, tab.ImageIterator.Files.Count) is { } nextIteration)
                {
                    var secondaryFileInfo = tab.ImageIterator.Files[nextIteration];
                    secondaryModel = await cache.GetAsync(tab.Id, secondaryFileInfo, ct.Token).ConfigureAwait(false);
                    if (secondaryModel is null || !tab.IsLatestLoad(load))
                    {
                        return false;
                    }
                    secondaryIndex = nextIteration;
                }
                tab.Display(model, secondaryModel);
                isShown = true;
            }

            // A newer load that begins after this sets its own files after these
            if (!tab.IsLatestLoad(load))
            {
                return true;
            }
            tab.UpdateTabTitle();
            fileWatcherService.Watch(tab, fileInfo.DirectoryName);
            cache.ReleaseImages(tab.Id);
            if (tab.ImageIterator.Files.Count > 0)
            {
                var currentFile = tab.ImageIterator.Files[index];
                cache.Put(tab.Id, currentFile, model);
                if (secondaryModel is not null)
                {
                    cache.Put(tab.Id, tab.ImageIterator.Files[secondaryIndex], secondaryModel);
                }
                cache.Preload(tab.Id, currentFile, false, tab.GetTabCancellation().Token);
            }

            return true;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(RepopulateIterator), e);
            return isShown;
        }
    }

    /// <inheritdoc cref="LoadFromFileAsync(FileInfo, TabViewModel, CancellationTokenSource)"/>
    public async ValueTask LoadFromFileAsync(string source, TabViewModel tab, CancellationTokenSource ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        await LoadFromFileAsync(new FileInfo(source), tab, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a file into <paramref name="tab"/>, and completes only once the file shows, a newer load supersedes this one,
    /// or the load fails, since a step Back or Forward ends when its load completes (ADR-0012).
    /// </summary>
    public async ValueTask LoadFromFileAsync(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct)
    {
        if (!fileInfo.Exists)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromFileAsync), $"Attempted to load a file that does not exist: {fileInfo}");
            return;
        }

        if (fileInfo.FullName.IsArchive())
        {
            await LoadFromArchiveAsync(fileInfo.FullName, tab, ct).ConfigureAwait(false);
            return;
        }
        var load = tab.BeginLoad();
        // The archives the tab opened so far, which it leaves once the file shows. One that opens after survives this load
        var opened = tab.ArchiveExtractionService?.OpenedArchives ?? 0;
        var iterator = tab.ImageIterator;

        if (iterator?.Files is null || iterator.Files.Count is 0)
        {
            // TODO: Figure out way to share file list, if another tab is already in the same directory
            await Repopulate().ConfigureAwait(false);
            return;
        }

        var index = SideBySidePair.IndexOf(tab.ImageIterator.Files, fileInfo);
        if (index is null && IsInShownFolder(fileInfo, tab))
        {
            // A file just written next to the shown one, such as a duplicate, is added by the folder watcher,
            // which also adds it to the gallery, so wait for it rather than rebuilding the list and the gallery
            if (await iterator.WaitForFileAsync(fileInfo.FullName, FolderWatcherWait, ct.Token).ConfigureAwait(false))
            {
                index = SideBySidePair.IndexOf(tab.ImageIterator.Files, fileInfo);
            }
            if (ct.IsCancellationRequested || !tab.IsLatestLoad(load))
            {
                return;
            }
        }

        if (index is { } found)
        {
            await tab.ImageIterator.IterateToIndexAsync(found, ct).ConfigureAwait(false);
            tab.ArchiveExtractionService?.CleanupOpened(opened);
        }
        else
        {
            await Repopulate().ConfigureAwait(false);
        }

        return;

        // Only a file that shows leaves the archives, as a load that a newer one overtakes leaves them to it
        async Task Repopulate()
        {
            // AsTask, because Task.Run waits for a Task but returns a ValueTask without awaiting it
            if (await Task.Run(() => RepopulateIterator(fileInfo, tab, ct, null, load).AsTask(), ct.Token)
                    .ConfigureAwait(false))
            {
                tab.ArchiveExtractionService?.CleanupOpened(opened);
            }
        }
    }

    /// <summary>
    /// How long to wait for the folder watcher to add a new file before rebuilding the file list instead.
    /// </summary>
    private static readonly TimeSpan FolderWatcherWait = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Whether <paramref name="fileInfo"/> is an image in the folder of the file the tab shows, which the folder
    /// watcher reports when it is created.
    /// </summary>
    private static bool IsInShownFolder(FileInfo fileInfo, TabViewModel tab) =>
        fileInfo.FullName.IsSupported() &&
        string.Equals(fileInfo.DirectoryName, tab.Model?.FileInfo?.DirectoryName, StringComparison.OrdinalIgnoreCase);

    public async Task<bool> LoadFromDirectoryAsync(FileInfo source, TabViewModel tab, CancellationTokenSource ct)
    {
        var load = tab.BeginLoad();
        // The archives the tab opened so far, which it leaves once the file shows. One that opens after survives this load
        var opened = tab.ArchiveExtractionService?.OpenedArchives ?? 0;
        var files = await Task.Run(() =>
        {
            var files = FileListRetriever.RetrieveFiles(source, stringComparer);
            return files.Count is 0 ? null : files;
        }, ct.Token).ConfigureAwait(false);
        if (files is null)
        {
            return false;
        }
        if (await RepopulateIterator(files[0], tab, ct, files, load).ConfigureAwait(false))
        {
            tab.ArchiveExtractionService?.CleanupOpened(opened);
        }
        return true;
    }

    public async ValueTask<LoadResult> LoadFromStringAsync(string source, TabViewModel tab, CancellationTokenSource ct,
        string? rootDirectory = null, string? archiveEntry = null)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return LoadResult.Failed();
        }

        var check = FileTypeResolver.CheckIfLoadableString(source);
        if (check is null)
        {
            return LoadResult.Failed();
        }

        switch (check.Value.Type)
        {
            case FileTypeResolver.LoadAbleFileType.File:
                if (rootDirectory is null)
                {
                    await LoadFromFileAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
                }
                else
                {
                    var load = tab.BeginLoad();
                    // The archives the tab opened so far, which it leaves once the file shows. One that opens after
                    // survives this load
                    var opened = tab.ArchiveExtractionService?.OpenedArchives ?? 0;
                    var files = await Task.Run(() => FileListRetriever.RetrieveFiles(new FileInfo(rootDirectory), stringComparer), ct.Token)
                        .ConfigureAwait(false);
                    if (await RepopulateIterator(new FileInfo(check.Value.Data), tab, ct, files.Count > 0 ? files : null,
                            load).ConfigureAwait(false))
                    {
                        tab.ArchiveExtractionService?.CleanupOpened(opened);
                    }
                }
                return LoadResult.Loaded;
            case FileTypeResolver.LoadAbleFileType.Directory:
                return await LoadFromDirectoryAsync(new FileInfo(check.Value.Data), tab, ct).ConfigureAwait(false)
                    ? LoadResult.Loaded
                    : LoadResult.Failed();
            case FileTypeResolver.LoadAbleFileType.Web:
                return await LoadFromUrlAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
            case FileTypeResolver.LoadAbleFileType.Zip:
                return await LoadFromArchiveAsync(check.Value.Data, tab, ct, archiveEntry).ConfigureAwait(false)
                    ? LoadResult.Loaded
                    : LoadResult.Failed();
            default:
                return LoadResult.Failed();
        }
    }

    public async ValueTask<bool> LoadFromArchiveAsync(string archivePath, TabViewModel tab, CancellationTokenSource ct,
        string? startEntry = null)
    {
        if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath) ||
            tab.ArchiveExtractionService is not { } extraction)
        {
            return false;
        }

        // Taken first, so a load that a newer one overtakes while its archive opens shows nothing
        var load = tab.BeginLoad();
        tab.SetLoading();
        // Show progress spinner while extracting archive
        if (tab.ParentWindowContext is not null)
        {
            tab.ParentWindowContext.IsLoadingIndicatorShown.Value = true;
        }

        // The archives the tab opened so far, which it leaves once a page of this one shows: the one it reads, and those
        // older loads opened, overtaken or not. This load's own opens after, as does a newer load's
        var opened = extraction.OpenedArchives;
        // The folder this load's archive opens to. Unless a page from it shows, it goes again, and the tab's archive stays
        // the one it shows
        string? folder = null;
        var isShown = false;

        try
        {
            var preparation = await extraction.PrepareArchiveAsync(
                archivePath,
                platformService.ExtractWithLocalSoftwareAsync,
                stringComparer).ConfigureAwait(false);

            if (preparation is not { } prep)
            {
                return false;
            }

            folder = prep.TempDirectory;
            if (IsOvertaken())
            {
                return false;
            }

            // The archive the tab leaves stops extracting, so this one's pages come sooner
            extraction.StopExtraction();

            if (prep.IsFullyExtracted)
            {
                // Local-software extractor already wrote every file to disk; build the file list
                // from the already-extracted paths so we don't depend on FileListRetriever's
                // recursion settings.
                var allFiles = prep.EntryKeys.Select(p => new FileInfo(p)).ToList();
                if (allFiles.Count is 0)
                {
                    return false;
                }

                var startFile = startEntry is null
                    ? null
                    : allFiles.Find(f => string.Equals(extraction.EntryOf(f.FullName), startEntry,
                        StringComparison.Ordinal));
                isShown = await RepopulateIterator(startFile ?? allFiles[0], tab, ct, allFiles, load).ConfigureAwait(false);
                if (!isShown)
                {
                    return false;
                }

                extraction.CleanupOpened(opened);
                return true;
            }

            // The entry to open the archive at, else its first
            var keys = prep.EntryKeys;
            var start = Math.Max(Array.FindIndex(keys, key => string.Equals(key, startEntry, StringComparison.Ordinal)), 0);

            if (!Settings.Navigation.AlwaysUncompressEntireArchive)
            {
                // Staged extraction: extract the start entry (and its partner if side-by-side), navigate to it, then
                // extract the rest in the background one by one while FileWatcherService inserts each new file into the
                // iterator.
                var startPath = await extraction.ExtractEntryAsync(archivePath, keys[start], ct.Token, folder).ConfigureAwait(false);

                if (string.IsNullOrEmpty(startPath) || IsOvertaken())
                {
                    return false;
                }

                var startFile = new FileInfo(startPath);
                List<FileInfo> seedFiles = [startFile];
                var partner = SideBySidePair.PartnerOf(start, keys.Length);
                // A partner that fails to extract, such as a corrupt entry, is left out, so the start page shows alone
                if (partner is { } partnerIndex &&
                    await extraction.ExtractEntryAsync(archivePath, keys[partnerIndex], ct.Token, folder)
                        .ConfigureAwait(false) is { } partnerPath)
                {
                    // In the entries' order, which FileWatcherService inserts the others by. The partner of the last
                    // entry is the first, when looping
                    seedFiles.Insert(partnerIndex < start ? 0 : 1, new FileInfo(partnerPath));
                }

                if (IsOvertaken())
                {
                    return false;
                }

                isShown = await RepopulateIterator(startFile, tab, ct, seedFiles, load).ConfigureAwait(false);
                if (!isShown)
                {
                    return false;
                }

                // Extract the others in the background, one by one, with no batches or progress indicator. A partner
                // that failed is not tried again, as one bad entry would end the extraction of the rest
                var (afterKeys, beforeKeys) = RemainingKeys(keys, start, start + 1, partner);
                if (afterKeys.Length + beforeKeys.Length > 0)
                {
                    var backgroundToken = extraction.ResetExtractionCts();
                    tab.IsArchiveExtracting.Value = true;
                    tab.ArchiveExtractionProgressText.Value = null;
                    tab.ImageIterator?.UpdateNavigationProperties();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await extraction.ExtractRemainingAsync(archivePath, afterKeys, beforeKeys,
                                ct: backgroundToken, folder: folder).ConfigureAwait(false);
                        }
                        finally
                        {
                            EndExtraction(tab, backgroundToken, folder, stringComparer);
                        }
                    }, backgroundToken);
                }
                else
                {
                    tab.IsArchiveExtracting.Value = false;
                    tab.ArchiveExtractionProgressText.Value = null;
                }

                extraction.CleanupOpened(opened);
                return true;
            }

            // Staged extraction (AlwaysUncompressEntireArchive = true): extract the start entry and up to 9 after it
            // immediately and navigate to it, then extract the rest in batches of 10 in the background while reporting
            // progress with the bottom-right indicator.
            const int initialExtractCount = 10;
            var initialKeys = keys.Skip(start).Take(initialExtractCount).ToArray();
            var extractedPaths = await extraction.ExtractEntriesAsync(archivePath, initialKeys, ct.Token, folder).ConfigureAwait(false);

            if (extractedPaths.Count == 0 || IsOvertaken())
            {
                return false;
            }

            // Seed iterator with initial extracted pages for immediate viewing and preloading
            var seedFilesList = extractedPaths.Select(p => new FileInfo(p)).ToList();
            isShown = await RepopulateIterator(seedFilesList[0], tab, ct, seedFilesList, load).ConfigureAwait(false);
            if (!isShown)
            {
                return false;
            }

            // Extract the rest in the background, in batches of 10. FileWatcherService picks them up.
            if (keys.Length > initialKeys.Length)
            {
                var totalCount = keys.Length;
                var initialExtractedCount = extractedPaths.Count;
                var (afterKeys, beforeKeys) = RemainingKeys(keys, start, start + initialKeys.Length);
                var backgroundToken = extraction.ResetExtractionCts();

                tab.IsArchiveExtracting.Value = true;
                tab.ArchiveExtractionProgressText.Value = $"{initialExtractedCount}/{totalCount}";

                var progress = new Progress<int>(current =>
                {
                    tab.ArchiveExtractionProgressText.Value = $"{current}/{totalCount}";
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await extraction.ExtractRemainingAsync(
                            archivePath,
                            afterKeys,
                            beforeKeys,
                            initialCount: initialExtractedCount,
                            totalCount: totalCount,
                            progress: progress,
                            batchSize: 10,
                            ct: backgroundToken,
                            folder: folder).ConfigureAwait(false);
                    }
                    finally
                    {
                        EndExtraction(tab, backgroundToken, folder, stringComparer);
                    }
                }, backgroundToken);
            }
            else
            {
                tab.IsArchiveExtracting.Value = false;
                tab.ArchiveExtractionProgressText.Value = null;
            }

            extraction.CleanupOpened(opened);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromArchiveAsync), ex);
            return false;
        }
        finally
        {
            if (!isShown)
            {
                extraction.Cleanup(folder);
            }

            // Hide progress spinner when initial extraction and display complete
            if (tab.ParentWindowContext is not null)
            {
                tab.ParentWindowContext.IsLoadingIndicatorShown.Value = false;
            }
        }

        // A newer load began, which shows what the tab shows next, or the load was cancelled
        bool IsOvertaken() => ct.IsCancellationRequested || !tab.IsLatestLoad(load);
    }

    /// <summary>
    /// Ends the background extraction with <paramref name="extraction"/>, extracted or not: the tab no longer counts as
    /// extracting, and a Next waiting for a page looks at the list again. Unless a newer archive's extraction has started
    /// since, as when Back opens an archive again: Next waits for that one's pages, and would step without them.
    /// </summary>
    /// <param name="folder">
    /// The folder the extraction wrote to. Once it finished, the pages it wrote there that the tab does not list yet are
    /// listed first: the watcher handles a folder's events one at a time and can be behind, and a waiting Next would
    /// count the list without them as final. The watcher then finds them listed. Null to list none.
    /// </param>
    /// <param name="compareNames">How the tab's files are sorted, which the watcher inserts by too.</param>
    internal static void EndExtraction(TabViewModel tab, CancellationToken extraction, string? folder = null,
        Func<string, string, int>? compareNames = null)
    {
        if (tab.ArchiveExtractionService?.IsLatestExtraction(extraction) ?? true)
        {
            if (!extraction.IsCancellationRequested && folder is not null && compareNames is not null
                && tab.ArchiveExtractionService?.ExtractedFilesOf(folder) is { Length: > 0 } extracted
                && tab.ImageIterator?.InsertMissing(extracted, folder, compareNames) > 0)
            {
                tab.UpdateTabTitle();
            }

            tab.IsArchiveExtracting.Value = false;
            tab.ArchiveExtractionProgressText.Value = null;
        }

        tab.ImageIterator?.NotifyFileAdded();
        tab.ImageIterator?.UpdateNavigationProperties();
    }

    /// <summary>
    /// The entries of an archive left to extract once those from <paramref name="start"/> up to <paramref name="end"/>
    /// are shown: those after them in order, and those before the start, nearest first. The extraction takes turns
    /// between the two, so that paging either way from the start soon finds its pages. <paramref name="alsoShown"/> is
    /// another entry shown with them, as the first one is beside the last when looping.
    /// </summary>
    private static (string[] After, string[] Before) RemainingKeys(string[] keys, int start, int end,
        int? alsoShown = null)
    {
        var after = new List<string>(keys.Length - end);
        for (var i = end; i < keys.Length; i++)
        {
            if (i != alsoShown)
            {
                after.Add(keys[i]);
            }
        }

        var before = new List<string>(start);
        for (var i = start - 1; i >= 0; i--)
        {
            if (i != alsoShown)
            {
                before.Add(keys[i]);
            }
        }

        return (after.ToArray(), before.ToArray());
    }

    public async ValueTask<LoadResult> LoadFromUrlAsync(string url, TabViewModel tab, CancellationTokenSource ct)
    {
        // Showing the downloaded image supersedes loading files still running.
        // Drops the iterator, as for a clipboard image, so SourceOpener docks the gallery again when browsing files
        tab.BeginLoad();
        tab.DisposeImageIterator();

        platformService.StopTaskbarProgress();
        var safeFileName = HttpManager.GetSafeFileName(url);
        var destPath = TempFileManager.GetNewTempFilePath(safeFileName);
        
        using var client = new HttpClientDownloadWithProgress(url, destPath);
        client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
        {
            var displayProgress = HttpManager.GetProgressDisplay(totalFileSize, totalBytesDownloaded, progressPercentage);
            var title = $"{safeFileName} {TranslationManager.Translation?.Downloading} {displayProgress}";

            // Update UI properties
            if (!string.Equals(tab.TabTitle.Value, title, StringComparison.OrdinalIgnoreCase))
            {
                tab.TabTitle.Value = title;
            }
            if (!string.Equals(tab.Title.Value, title, StringComparison.OrdinalIgnoreCase))
            {
                tab.Title.Value = title;
            }
            if (!string.Equals(tab.WindowTitle.Value, title, StringComparison.OrdinalIgnoreCase))
            {
                tab.WindowTitle.Value = title;
            }
            if (!string.Equals(tab.TitleTooltip.Value, title, StringComparison.OrdinalIgnoreCase))
            {
                tab.TitleTooltip.Value = title;
            }

            if (Settings.UIProperties.IsTaskbarProgressEnabled && totalBytesDownloaded.HasValue && totalFileSize.HasValue)
            {
                platformService.SetTaskbarProgress((ulong)totalBytesDownloaded.Value, (ulong)totalFileSize.Value);
            }
        };

        try
        {
            await client.StartDownloadAsync(ct.Token).ConfigureAwait(false);

            platformService.StopTaskbarProgress();

            if (ct.IsCancellationRequested)
            {
                return LoadResult.Failed();
            }

            var model = await imageModelLoader.GetImageModelAsync(new FileInfo(destPath), ct.Token)
                .ConfigureAwait(false);

            // Set before the image is shown, since the tab records the URL then
            tab.SourceURL = url;
            tab.DisplaySingleImage(model, SingleImageType.Url, url);

            // Set titles to filename after successful load
            tab.UpdateTabTitle();

            tab.CanNavigateBackwards.Value = false;
            tab.CanNavigateForwards.Value = false;

            tab.ArchiveExtractionService.Cleanup();
            return LoadResult.Loaded;
        }
        catch (OperationCanceledException)
        {
            platformService.StopTaskbarProgress();
            return LoadResult.Failed();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromUrlAsync), e);
            platformService.StopTaskbarProgress();
            tab.TabTitle.Value = TranslationManager.Translation?.ErrorLoadingImage ?? "Error";
            return LoadResult.Failed(e.Message);
        }
        finally
        {
            tab.UpdateTabTitle();
        }
    }

    public async ValueTask NavigateAsync(TabViewModel tab, NavigateTo to, CancellationTokenSource ct) =>
        await tab.ImageIterator.NavigateAsync(to, SkipAmount.One, ct).ConfigureAwait(false);

    public async ValueTask NavigateByIncrementsAsync(TabViewModel tab, SkipAmount skipAmount, bool forwards, CancellationTokenSource ct)
    {
        var iterator = tab.ImageIterator;
        if (iterator is null)
        {
            return;
        }
        await iterator.NavigateByIncrementsAsync(skipAmount,forwards, ct).ConfigureAwait(false);
    }
    
    public async ValueTask NavigateToNextFolderAsync(TabViewModel tab, CancellationTokenSource ct)
    {
        var currentDir = tab.Model?.FileInfo?.DirectoryName;
        if (currentDir == null)
        {
            return;
        }

        _navigator ??= new FileAndDirectoryNavigator(stringComparer);
        var nextDir = await Task.Run(() => _navigator.FindNextValidDirectory(currentDir), ct.Token).ConfigureAwait(false);
        if (nextDir != null)
        {
            await LoadFromDirectoryAsync(new FileInfo(nextDir), tab, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask NavigateToPreviousFolderAsync(TabViewModel tab, CancellationTokenSource ct)
    {
        var currentDir = tab.Model?.FileInfo?.DirectoryName;
        if (currentDir == null)
        {
            return;
        }
        _navigator ??= new FileAndDirectoryNavigator(stringComparer);
        var prevDir = await Task.Run(() => _navigator.FindPreviousValidDirectory(currentDir), ct.Token).ConfigureAwait(false);
        if (prevDir != null)
        {
            await LoadFromDirectoryAsync(new FileInfo(prevDir), tab, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask NavigateToNextArchiveAsync(TabViewModel tab, CancellationTokenSource ct) 
        => await NavigateArchiveCoreAsync(tab, true, ct).ConfigureAwait(false);

    public async ValueTask NavigateToPreviousArchiveAsync(TabViewModel tab, CancellationTokenSource ct)
        => await NavigateArchiveCoreAsync(tab, false, ct).ConfigureAwait(false);

    private async ValueTask NavigateArchiveCoreAsync(TabViewModel tab, bool next, CancellationTokenSource ct)
    {
        var currentFile = tab.ArchiveExtractionService.IsArchived ?
            new FileInfo(tab.ArchiveExtractionService.LastOpenedArchive) : tab.Model.FileInfo;
           
        var currentDir = currentFile?.DirectoryName;
        if (currentDir == null)
        {
            return;
        }
        _navigator ??= new FileAndDirectoryNavigator(stringComparer);
        var nextArchive = await Task.Run(() =>
            _navigator.FindNextArchive(currentDir, next, currentFile!.FullName), ct.Token).ConfigureAwait(false);
        if (nextArchive != null)
        {
            await LoadFromArchiveAsync(nextArchive, tab, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask SortAsync(TabViewModel tab, SortFilesBy sortOrder, CancellationTokenSource ct)
    {
        Settings.Sorting.SortPreference = (int)sortOrder;
        await ApplySortAsync(tab, ct).ConfigureAwait(false);
    }

    public async ValueTask SortAsync(TabViewModel tab, bool ascending, CancellationTokenSource ct)
    {
        Settings.Sorting.Ascending = ascending;
        await ApplySortAsync(tab, ct).ConfigureAwait(false);
    }

    private async ValueTask ApplySortAsync(TabViewModel tab, CancellationTokenSource ct)
    {
        if (tab.ImageIterator.Files.Count <= 0)
        {
            return;
        }

        try
        {
            // Get current file to maintain position
            var currentFile = tab.Model?.FileInfo;
            if (currentFile is null)
            {
                return;
            }

            // Retrieve and sort files based on new settings
            var newFiles = await Task.Run(() => FileListRetriever.RetrieveFiles(currentFile, stringComparer), ct.Token).ConfigureAwait(false);

            if (newFiles.Count is 0)
            {
                return;
            }

            // Update files in iterator, keeping the position on the current file
            tab.ImageIterator.Initialize(newFiles, SideBySidePair.IndexOf(newFiles, currentFile) ?? -1);
            
            // Update title; the gallery follows the sorted list by itself
            tab.UpdateTabTitle();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(ApplySortAsync), e);
        }
    }
}
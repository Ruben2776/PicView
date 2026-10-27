using System.Collections.ObjectModel;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.FileSearch;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.Http;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Navigation;

public class NavigationService(
    IImageModelLoader imageModelLoader,
    SharedImageCache cache,
    IFileWatcherService fileWatcherService,
    IPlatformSpecificService platformService,
    Func<string, string, int> stringComparer)
    : INavigationService
{
    
    public BindableReactiveProperty<ObservableCollection<FileSearchResult>?>? FilteredFileInfos { get; set; }
    public ReactiveCommand<string>? LoadFromStringCommand { get; set; }
    private FileAndDirectoryNavigator? _navigator;

    public ValueTask RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct, List<FileInfo>? files = null) =>
        RepopulateIterator(fileInfo, tab, ct, files, tab.BeginLoad());

    /// <param name="load">The version of the load this is part of. Once a newer load begins, this one stops before
    /// changing the tab's image, files or gallery, so a slow load that finishes last cannot overwrite a newer one.</param>
    private async ValueTask RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct,
        IReadOnlyList<FileInfo>? files, int load)
    {
        try
        {
            fileWatcherService.Unwatch(tab);

            // Show image quickly to make it feel fast
            ImageModel? secondaryModel = null;
            var secondaryIndex = 0;
            var model = await cache.GetAsync(tab.Id, fileInfo, ct.Token).ConfigureAwait(false);
            if (model is null || !tab.IsLatestLoad(load))
            {
                return;
            }

            if (!Settings.ImageScaling.ShowImageSideBySide)
            {
                tab.Display(model);
            }

            await Task.Yield();
            IReadOnlyList<FileInfo> retrieved = files ?? FileListRetriever.RetrieveFiles(fileInfo, stringComparer);
            if (!tab.IsLatestLoad(load))
            {
                return;
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
                        return;
                    }
                    secondaryIndex = nextIteration;
                }
                tab.Display(model, secondaryModel);
            }

            // A newer load that begins after this sets its own files and gallery after these
            if (!tab.IsLatestLoad(load))
            {
                return;
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
            FileHistoryManager.Add(fileInfo.FullName);

            if ((tab.Gallery.IsDockedGalleryVisible.CurrentValue || tab.Gallery.IsGalleryExpanded.CurrentValue) && tab.ThumbnailCache is not null)
            {
                tab.ThumbnailCache.RemoveOwner(tab.Id);
                if (tab.Gallery.LoadingState is GalleryLoadingState.Loading or GalleryLoadingState.Loaded)
                {
#pragma warning disable MA0042
                    // ReSharper disable once MethodHasAsyncOverload
                    ct.Cancel();
#pragma warning restore MA0042
                    tab.ResetNavigationCts();
                    tab.GalleryCoordinator?.ReloadGallery(tab.ImageIterator.Files, tab.ParentWindowContext.Core);
                    return;
                }
                tab.Gallery.LoadingState = GalleryLoadingState.NotLoaded;
                await Task.Run(() => tab.GalleryCoordinator.LoadGallery(tab.ImageIterator.Files, tab.ParentWindowContext.Core), ct.Token).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(RepopulateIterator), e);
        }
    }

    public async ValueTask LoadFromFileAsync(string source, TabViewModel tab, CancellationTokenSource ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        await LoadFromFileAsync(new FileInfo(source), tab, ct).ConfigureAwait(false);
    }

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
        }
        else
        {
            await Repopulate().ConfigureAwait(false);
        }
        
        tab.ArchiveExtractionService.Cleanup();

        return;

        async Task Repopulate()
        {
            await Task.Run(() => RepopulateIterator(fileInfo, tab, ct, null, load).ConfigureAwait(false), ct.Token).ConfigureAwait(false);
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
        var files = await Task.Run(() =>
        {
            var files = FileListRetriever.RetrieveFiles(source, stringComparer);
            return files.Count is 0 ? null : files;
        }, ct.Token).ConfigureAwait(false);
        if (files is null)
        {
            return false;
        }
        await RepopulateIterator(files[0], tab, ct, files, load).ConfigureAwait(false);
        tab.ArchiveExtractionService.Cleanup();
        return true;
    }

    public async ValueTask<LoadResult> LoadFromStringAsync(string source, TabViewModel tab, CancellationTokenSource ct,
        string? rootDirectory = null)
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
                    var files = await Task.Run(() => FileListRetriever.RetrieveFiles(new FileInfo(rootDirectory), stringComparer), ct.Token)
                        .ConfigureAwait(false);
                    await RepopulateIterator(new FileInfo(check.Value.Data), tab, ct, files.Count > 0 ? files : null, load).ConfigureAwait(false);
                    tab.ArchiveExtractionService.Cleanup();
                }
                return LoadResult.Loaded;
            case FileTypeResolver.LoadAbleFileType.Directory:
                return await LoadFromDirectoryAsync(new FileInfo(check.Value.Data), tab, ct).ConfigureAwait(false)
                    ? LoadResult.Loaded
                    : LoadResult.Failed();
            case FileTypeResolver.LoadAbleFileType.Web:
                return await LoadFromUrlAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
            case FileTypeResolver.LoadAbleFileType.Zip:
                return await LoadFromArchiveAsync(check.Value.Data, tab, ct).ConfigureAwait(false)
                    ? LoadResult.Loaded
                    : LoadResult.Failed();
            default:
                return LoadResult.Failed();
        }
    }

    public async ValueTask<bool> LoadFromArchiveAsync(string archivePath, TabViewModel tab, CancellationTokenSource ct)
    {
        if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath))
        {
            return false;
        }

        tab.SetLoading();
        // Show progress spinner while extracting archive
        if (tab.ParentWindowContext is not null)
        {
            tab.ParentWindowContext.IsLoadingIndicatorShown.Value = true;
        }
        
        // Retrieve the temporary directory for the possible previous archive extraction
        var tempZipDir = tab.ArchiveExtractionService.TempZipDirectory;

        try
        {
            var preparation = await tab.ArchiveExtractionService.PrepareArchiveAsync(
                archivePath,
                platformService.ExtractWithLocalSoftwareAsync,
                stringComparer).ConfigureAwait(false);

            if (preparation is null || string.IsNullOrEmpty(tab.ArchiveExtractionService.TempZipDirectory))
            {
                return false;
            }

            if (ct.IsCancellationRequested)
            {
                return false;
            }

            var prep = preparation.Value;

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

                await RepopulateIterator(allFiles[0], tab, ct, allFiles).ConfigureAwait(false);

                FileHistoryManager.Add(archivePath);
                tab.ArchiveExtractionService.Cleanup(tempZipDir);
                return true;
            }

            if (!Settings.Navigation.AlwaysUncompressEntireArchive)
            {
                // Staged extraction: extract the first entry (or 2 if side-by-side), navigate to it, then extract the rest in
                // the background one by one while FileWatcherService inserts each new file into the iterator.
                var firstKey = prep.EntryKeys[0];
                var firstPath = await tab.ArchiveExtractionService.ExtractEntryAsync(archivePath, firstKey, ct.Token).ConfigureAwait(false);

                if (string.IsNullOrEmpty(firstPath) || ct.IsCancellationRequested)
                {
                    return false;
                }

                if (SideBySidePair.ShownCount(prep.EntryKeys.Length) > 1)
                {
                    var secondKey = prep.EntryKeys[1];
                    var secondPath = await tab.ArchiveExtractionService.ExtractEntryAsync(archivePath, secondKey, ct.Token).ConfigureAwait(false);
                    var seedFiles = new List<FileInfo>
                    {
                        new(firstPath),
                        new(secondPath)
                    };
                    await RepopulateIterator(seedFiles[0], tab, ct, seedFiles).ConfigureAwait(false);
                }
                else
                {
                    var seedFiles = new List<FileInfo> { new(firstPath) };
                    await RepopulateIterator(seedFiles[0], tab, ct, seedFiles).ConfigureAwait(false);
                }

                FileHistoryManager.Add(archivePath);

                // Kick off background extraction of remaining entries one by one without batching or progress indicator.
                var initialCount = SideBySidePair.ShownCount(prep.EntryKeys.Length);
                if (prep.EntryKeys.Length > initialCount)
                {
                    var remainingKeys = prep.EntryKeys.Skip(initialCount).ToArray();
                    var backgroundToken = tab.ArchiveExtractionService.ResetExtractionCts();
                    tab.IsArchiveExtracting.Value = true;
                    tab.ArchiveExtractionProgressText.Value = null;
                    tab.ImageIterator?.UpdateNavigationProperties();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await tab.ArchiveExtractionService.ExtractRemainingAsync(archivePath, remainingKeys, ct: backgroundToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            tab.IsArchiveExtracting.Value = false;
                            tab.ArchiveExtractionProgressText.Value = null;
                            tab.ImageIterator?.NotifyFileAdded();
                            tab.ImageIterator?.UpdateNavigationProperties();
                        }
                    }, backgroundToken);
                }
                else
                {
                    tab.IsArchiveExtracting.Value = false;
                    tab.ArchiveExtractionProgressText.Value = null;
                }

                tab.ArchiveExtractionService.Cleanup(tempZipDir);
                return true;
            }

            // Staged extraction (AlwaysUncompressEntireArchive = true): extract up to the first 10 entries immediately and navigate to them,
            // then extract the rest in batches of 10 in the background while reporting progress with the bottom-right indicator.
            const int initialExtractCount = 10;
            var initialKeys = prep.EntryKeys.Take(initialExtractCount).ToArray();
            var extractedPaths = await tab.ArchiveExtractionService.ExtractEntriesAsync(archivePath, initialKeys, ct.Token).ConfigureAwait(false);

            if (extractedPaths.Count == 0 || ct.IsCancellationRequested)
            {
                return false;
            }

            // Seed iterator with initial extracted pages for immediate viewing and preloading
            var seedFilesList = extractedPaths.Select(p => new FileInfo(p)).ToList();
            await RepopulateIterator(seedFilesList[0], tab, ct, seedFilesList).ConfigureAwait(false);

            FileHistoryManager.Add(archivePath);

            // Kick off background extraction of remaining entries in batches of 10. FileWatcherService picks them up.
            if (prep.EntryKeys.Length > initialKeys.Length)
            {
                var totalCount = prep.EntryKeys.Length;
                var initialExtractedCount = extractedPaths.Count;
                var remainingKeys = prep.EntryKeys.Skip(initialKeys.Length).ToArray();
                var backgroundToken = tab.ArchiveExtractionService.ResetExtractionCts();

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
                        await tab.ArchiveExtractionService.ExtractRemainingAsync(
                            archivePath,
                            remainingKeys,
                            initialCount: initialExtractedCount,
                            totalCount: totalCount,
                            progress: progress,
                            batchSize: 10,
                            ct: backgroundToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        tab.IsArchiveExtracting.Value = false;
                        tab.ArchiveExtractionProgressText.Value = null;
                        tab.ImageIterator?.NotifyFileAdded();
                        tab.ImageIterator?.UpdateNavigationProperties();
                    }
                }, backgroundToken);
            }
            else
            {
                tab.IsArchiveExtracting.Value = false;
                tab.ArchiveExtractionProgressText.Value = null;
            }

            tab.ArchiveExtractionService.Cleanup(tempZipDir);
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
            // Hide progress spinner when initial extraction and display complete
            if (tab.ParentWindowContext is not null)
            {
                tab.ParentWindowContext.IsLoadingIndicatorShown.Value = false;
            }
        }
    }

    public async ValueTask<LoadResult> LoadFromUrlAsync(string url, TabViewModel tab, CancellationTokenSource ct)
    {
        // Showing the downloaded image supersedes loading files still running
        tab.BeginLoad();
        tab.ImageIterator?.Dispose();

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
            tab.Display(model);

            // Set titles to filename after successful load
            tab.SourceURL = url;
            tab.SingleImageType = SingleImageType.Url;
            tab.Gallery.ActiveGalleryMode.Value = GalleryMode.Closed;
            tab.UpdateTabTitle();

            tab.CanNavigateBackwards.Value = false;
            tab.CanNavigateForwards.Value = false;

            FileHistoryManager.Add(url);
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
    
    public async ValueTask<bool> LoadLastFileAsync(TabViewModel tab, CancellationTokenSource ct)
    {
        var lastFile = Settings.StartUp.LastFile;
        var lastEntry = FileHistoryManager.GetLastEntry();

        // determine which file source to use (prioritize LastFile, fallback to History)
        var fileToLoad = !string.IsNullOrWhiteSpace(lastFile) ? lastFile : lastEntry;
        if (string.IsNullOrWhiteSpace(fileToLoad))
        {
            return false;
        }

        await LoadFromStringAsync(fileToLoad, tab, ct).ConfigureAwait(false);
        return true;
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
            
            // Update title
            tab.UpdateTabTitle();
            
            tab.GalleryCoordinator?.SortLoadedGallery(newFiles);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(ApplySortAsync), e);
        }
    }
}
using System.Collections.ObjectModel;
using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.FileSearch;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.Http;
using PicView.Core.ImageDecoding;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Navigation;

public class NavigationService(
    IImageModelLoader imageModelLoader,
    IImageCache cache,
    IFileWatcherService fileWatcherService,
    IPlatformSpecificService platformService,
    IThumbnailLoader thumbnailLoader,
    Func<string, string, int> stringComparer)
    : INavigationService
{
    
    public BindableReactiveProperty<ObservableCollection<FileSearchResult>?>? FilteredFileInfos { get; set; }
    public ReactiveCommand<string>? LoadFromStringCommand { get; set; }
    private FileAndDirectoryNavigator? _navigator;

    public async ValueTask RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct, List<FileInfo>? files = null)
    {
        try
        {
            fileWatcherService.Unwatch(tab);

            // Show image quickly to make it feel fast
            ImageModel model;
            ImageModel? secondaryModel = null;
            int index;
            var secondaryIndex = 0;
            if (cache.TryGet(fileInfo, out var preLoadValue))
            {
                model = preLoadValue.ImageModel;
            }
            else
            {
                model = await imageModelLoader.GetImageModelAsync(fileInfo, ct.Token).ConfigureAwait(false);
            }
            tab.ImageIterator.Files = files ?? FileListRetriever.RetrieveFiles(fileInfo, stringComparer);
            index = FindIndex(fileInfo, tab);
            if (index < 0 && tab.ImageIterator.Files.Count > 0)
            {
                index = 0;
            }
            tab.ImageIterator.Initialize(tab.ImageIterator.Files, index);

            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                if (tab.ImageIterator.Files.Count > 0 && index >= 0)
                {
                    var (_, nextIteration, _) = IterationHelper.GetIterations(index, tab.ImageIterator.Files.Count, NavigateTo.Next, SkipAmount.None);
                    if (nextIteration >= 0 && nextIteration < tab.ImageIterator.Files.Count)
                    {
                        var secondaryFileInfo = tab.ImageIterator.Files[nextIteration];
                        if (cache.TryGet(secondaryFileInfo, out var secondaryPreLoadValue))
                        {
                            secondaryModel = secondaryPreLoadValue.ImageModel;
                        }
                        else
                        {
                            secondaryModel = await imageModelLoader.GetImageModelAsync(secondaryFileInfo, ct.Token).ConfigureAwait(false);
                        }
                        tab.SecondaryModel = secondaryModel;
                        tab.SecondaryImage.Value = secondaryModel.Image;
                        tab.SecondaryImageType.Value = secondaryModel.ImageType;
                        tab.SecondaryFileInfo.Value = secondaryFileInfo;
                        secondaryIndex = nextIteration;
                    }
                    else
                    {
                        tab.SecondaryModel = null;
                        tab.SecondaryImage.Value = null;
                        tab.SecondaryImageType.Value = null;
                        tab.SecondaryFileInfo.Value = null;
                    }
                }
                ShowModel(model);
            }
            else
            {
                tab.SecondaryModel = null;
                tab.SecondaryImage.Value = null;
                tab.SecondaryImageType.Value = null;
                tab.SecondaryFileInfo.Value = null;
                ShowModel(model);
            }
            
            tab.UpdateTabTitle();
            fileWatcherService.Watch(tab, fileInfo.DirectoryName);
            cache.Clear(tab.Id);
            cache.Add(tab.Id, index, new PreLoadValue(model), tab.ImageIterator.Files.Count, false);
            if (secondaryModel is not null)
            {
                cache.Add(tab.Id, secondaryIndex, new PreLoadValue(secondaryModel), tab.ImageIterator.Files.Count, false);
            }
            cache.Preload(tab.Id, index, false, tab.ImageIterator.Files, tab.GetTabCancellation().Token);
            FileHistoryManager.Add(fileInfo.FullName);

            if ((tab.Gallery.IsDockedGalleryVisible.CurrentValue || tab.Gallery.IsGalleryExpanded.CurrentValue) && tab.ThumbnailCache != null)
            {
                if (tab.Gallery.LoadingState is GalleryLoadingState.Loading or GalleryLoadingState.Loaded)
                {
                    await ct.CancelAsync().ConfigureAwait(false);
                    tab.ResetNavigationCts();
                    await GalleryLoader.ReloadGallery(tab, tab.ImageIterator.Files, thumbnailLoader, tab.ThumbnailCache, tab.GetTabCancellation().Token).ConfigureAwait(false);
                    return;
                }
                tab.Gallery.LoadingState = GalleryLoadingState.NotLoaded;
                await GalleryLoader.LoadGalleryAsync(tab, tab.ImageIterator.Files, thumbnailLoader, tab.ThumbnailCache, ct.Token).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(RepopulateIterator), e);
        }
        
        return;

        void ShowModel(ImageModel model)
        {
            tab.Model = model; // Image updated via reactive subscription
            tab.FileInfo.Value = model.FileInfo;
            tab.Image.Value = model.Image;
            tab.ImageType.Value = model.ImageType;
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
        var iterator = tab.ImageIterator;

        if (iterator.Files is null || iterator.Files.Count is 0)
        {
            // TODO: Figure out way to share file list, if another tab is already in the same directory
            await Repopulate().ConfigureAwait(false);
            return;
        }

        var index = FindIndex(fileInfo, tab);
        if (index is not -1)
        {
            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                var (_, nextIteration, _) = IterationHelper.GetIterations(index, tab.ImageIterator.Files.Count, NavigateTo.Next, SkipAmount.None);
                await tab.ImageIterator.IterateToIndicesAsync(index, nextIteration, ct).ConfigureAwait(false);
            }
            else
            {
                await tab.ImageIterator.IterateToIndexAsync(index, ct).ConfigureAwait(false);
            }
        }
        else
        {
            await Repopulate().ConfigureAwait(false);
        }
        
        tab.ArchiveExtractionService.Cleanup();

        return;

        async ValueTask Repopulate()
        {
            await RepopulateIterator(fileInfo, tab, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask LoadFromDirectoryAsync(FileInfo source, TabViewModel tab, CancellationTokenSource ct)
    {
        var files = await Task.Run(() => FileListRetriever.RetrieveFiles(source, stringComparer), ct.Token).ConfigureAwait(false);
        if (files.Count is 0)
        {
            return;
        }

        var first = files[0];
        await RepopulateIterator(first, tab, ct, files).ConfigureAwait(false);
        tab.ArchiveExtractionService.Cleanup();
    }

    public async ValueTask<bool> LoadFromStringAsync(string source, TabViewModel tab, CancellationTokenSource ct)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        var check = FileTypeResolver.CheckIfLoadableString(source);
        if (check is null)
        {
            return false;
        }

        switch (check.Value.Type)
        {
            case FileTypeResolver.LoadAbleFileType.File:
                await LoadFromFileAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
                return true;
            case FileTypeResolver.LoadAbleFileType.Directory:
            {
                await LoadFromDirectoryAsync(new FileInfo(check.Value.Data), tab, ct).ConfigureAwait(false);
                return true;
            }
            case FileTypeResolver.LoadAbleFileType.Web:
                await LoadFromUrlAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
                return true;
            case FileTypeResolver.LoadAbleFileType.Zip:
                return await LoadFromArchiveAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
            default:
                return false;
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

            // Staged extraction: extract up to the first 10 entries immediately and navigate to them,
            // then extract the rest in the background while FileWatcherService inserts each new file into the iterator.
            const int initialExtractCount = 10;
            var initialKeys = prep.EntryKeys.Take(initialExtractCount).ToArray();
            var extractedPaths = await tab.ArchiveExtractionService.ExtractEntriesAsync(archivePath, initialKeys, ct.Token).ConfigureAwait(false);

            if (extractedPaths.Count == 0 || ct.IsCancellationRequested)
            {
                return false;
            }

            // Seed iterator with initial extracted pages for immediate viewing and preloading
            var seedFiles = extractedPaths.Select(p => new FileInfo(p)).ToList();
            await RepopulateIterator(seedFiles[0], tab, ct, seedFiles).ConfigureAwait(false);

            FileHistoryManager.Add(archivePath);

            // Kick off background extraction of remaining entries. FileWatcherService picks them up.
            if (prep.EntryKeys.Length > initialKeys.Length)
            {
                var remainingKeys = prep.EntryKeys.Skip(initialKeys.Length).ToArray();
                var backgroundToken = tab.GetTabCancellation().Token;
                _ = Task.Run(() => tab.ArchiveExtractionService.ExtractRemainingAsync(archivePath, remainingKeys, backgroundToken), backgroundToken);
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

    public async ValueTask LoadFromUrlAsync(string url, TabViewModel tab, CancellationTokenSource ct)
    {
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

            if (totalBytesDownloaded.HasValue && totalFileSize.HasValue)
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
                return;
            }
            
            var model = await imageModelLoader.GetImageModelAsync(new FileInfo(destPath), ct.Token).ConfigureAwait(false);
            tab.Model = model;
            tab.SecondaryModel = null;
            
            // Set titles to filename after successful load
            tab.SourceURL = url;
            tab.SingleImageType = SingleImageType.Url;
            tab.UpdateTabTitle();
            
            tab.CanNavigateBackwards.Value = false;
            tab.CanNavigateForwards.Value = false;

            FileHistoryManager.Add(url);
            tab.ArchiveExtractionService.Cleanup();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromUrlAsync), e);
            platformService.StopTaskbarProgress();
            // Revert or show error state if needed
            tab.TabTitle.Value = TranslationManager.Translation?.ErrorLoadingImage ?? "Error";
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
        var fileToLoad = !string.IsNullOrEmpty(lastFile) ? lastFile : lastEntry;
        if (string.IsNullOrEmpty(lastEntry))
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

            // Update files in iterator
            tab.ImageIterator.Files = newFiles;

            // Find new index of current file
            var newIndex = FindIndex(currentFile, tab);
            tab.ImageIterator.SetCurrentIndex(newIndex);
            
            // Update cache mapping
            cache.Resynchronize(tab.Id, newFiles);
            
            // Update title
            tab.UpdateTabTitle();
            
            GalleryLoader.SortLoadedGallery(tab, newFiles);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(ApplySortAsync), e);
        }
    }

    private static int FindIndex(FileInfo fileInfo, TabViewModel tab) =>
        tab.ImageIterator.Files.FindIndex(x =>
            x.FullName.AsSpan().Equals(fileInfo.FullName.AsSpan(), StringComparison.OrdinalIgnoreCase));
}
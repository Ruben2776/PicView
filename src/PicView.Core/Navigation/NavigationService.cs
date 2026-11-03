using PicView.Core.DebugTools;
using PicView.Core.FileSorting;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

// Loading is the tab's Load's, with the tab's services, so the navigation needs only how to sort names
public class NavigationService(Func<string, string, int> stringComparer) : INavigationService
{

    private FileAndDirectoryNavigator? _navigator;

    // The six load entries forward to the tab's Load (ADR-0019), which reads the tab's own cancellation. Their
    // CancellationTokenSource parameters stay until their callers move to the Load

    // Given files are the shown file's folder, which the Load lists anyway
    public async ValueTask RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct,
        List<FileInfo>? files = null) =>
        await tab.Load.LoadAsync(fileInfo.FullName, new LoadOptions(Relist: files is null)).ConfigureAwait(false);

    /// <inheritdoc cref="LoadFromFileAsync(FileInfo, TabViewModel, CancellationTokenSource)"/>
    public async ValueTask LoadFromFileAsync(string source, TabViewModel tab, CancellationTokenSource ct) =>
        await tab.Load.LoadAsync(source).ConfigureAwait(false);

    /// <summary>
    /// Loads a file into <paramref name="tab"/>, and completes only once the file shows, a newer load supersedes this one,
    /// or the load fails, since a step Back or Forward ends when its load completes (ADR-0012).
    /// </summary>
    public async ValueTask LoadFromFileAsync(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct) =>
        await tab.Load.LoadAsync(fileInfo.FullName).ConfigureAwait(false);

    // An overtaken load reports success, as the old copy did, so the caller leaves the newer load's view alone. Task 5
    // deletes this forward and gives its caller the real outcome
    public async Task<bool> LoadFromDirectoryAsync(FileInfo source, TabViewModel tab, CancellationTokenSource ct) =>
        (await tab.Load.LoadAsync(source.FullName).ConfigureAwait(false)).Outcome is not LoadOutcome.Failed;

    // An overtaken load reports success, as the old copy did, so SourceOpener leaves the newer load's view alone. Task
    // 5 deletes this forward and gives its caller the real outcome
    public async ValueTask<LoadResult> LoadFromStringAsync(string source, TabViewModel tab, CancellationTokenSource ct,
        string? rootDirectory = null, string? archiveEntry = null)
    {
        var result = await tab.Load
            .LoadAsync(source, new LoadOptions(RootDirectory: rootDirectory, ArchiveEntry: archiveEntry))
            .ConfigureAwait(false);
        return result.Outcome is LoadOutcome.Superseded ? LoadResult.Loaded : result;
    }

    public async ValueTask<bool> LoadFromArchiveAsync(string archivePath, TabViewModel tab, CancellationTokenSource ct,
        string? startEntry = null) =>
        (await tab.Load.LoadAsync(archivePath, new LoadOptions(ArchiveEntry: startEntry)).ConfigureAwait(false)).IsLoaded;

    public ValueTask<LoadResult> LoadFromUrlAsync(string url, TabViewModel tab, CancellationTokenSource ct) =>
        tab.Load.LoadAsync(url);

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
            await tab.Load.LoadAsync(nextDir).ConfigureAwait(false);
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
            await tab.Load.LoadAsync(prevDir).ConfigureAwait(false);
        }
    }

    public async ValueTask NavigateToNextArchiveAsync(TabViewModel tab, CancellationTokenSource ct)
        => await NavigateArchiveCoreAsync(tab, true, ct).ConfigureAwait(false);

    public async ValueTask NavigateToPreviousArchiveAsync(TabViewModel tab, CancellationTokenSource ct)
        => await NavigateArchiveCoreAsync(tab, false, ct).ConfigureAwait(false);

    private async ValueTask NavigateArchiveCoreAsync(TabViewModel tab, bool next, CancellationTokenSource ct)
    {
        var currentFile = tab.Archives.OpenArchive is { } openArchive ?
            new FileInfo(openArchive) : tab.Model.FileInfo;

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
            await tab.Load.LoadAsync(nextArchive).ConfigureAwait(false);
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
            // The titles and the gallery follow the sorted list by themselves
            tab.ImageIterator.Initialize(newFiles, SideBySidePair.IndexOf(newFiles, currentFile) ?? -1);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(ApplySortAsync), e);
        }
    }
}

using PicView.Core.FileSorting;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

// Loading is the tab's Load's, with the tab's services, so the navigation needs only how to sort names
public class NavigationService(Func<string, string, int> stringComparer) : INavigationService
{
    private FileAndDirectoryNavigator? _navigator;

    public async ValueTask NavigateAsync(TabViewModel tab, NavigateTo to, CancellationTokenSource ct)
    {
        // A tab that never browsed files, such as a New Tab, has nothing to step through
        var iterator = tab.ImageIterator;
        if (iterator is null)
        {
            return;
        }

        await iterator.NavigateAsync(to, SkipAmount.One, ct).ConfigureAwait(false);
    }

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
}

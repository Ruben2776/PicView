using PicView.Core.FileSorting;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

// Loading is the tab's Load's, with the tab's services; stepping through folders and archives takes the tab's ordering
public class NavigationService : INavigationService
{
    private FileAndDirectoryNavigator? _navigator;

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

        _navigator ??= new FileAndDirectoryNavigator();
        var ordering = tab.CurrentOrdering();
        var nextDir = await Task.Run(() => _navigator.FindNextValidDirectory(currentDir, ordering), ct.Token).ConfigureAwait(false);
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
        _navigator ??= new FileAndDirectoryNavigator();
        var ordering = tab.CurrentOrdering();
        var prevDir = await Task.Run(() => _navigator.FindPreviousValidDirectory(currentDir, ordering), ct.Token).ConfigureAwait(false);
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
        _navigator ??= new FileAndDirectoryNavigator();
        var ordering = tab.CurrentOrdering();
        var nextArchive = await Task.Run(() =>
            _navigator.FindNextArchive(currentDir, next, currentFile!.FullName, ordering), ct.Token).ConfigureAwait(false);
        if (nextArchive != null)
        {
            await tab.Load.LoadAsync(nextArchive).ConfigureAwait(false);
        }
    }
}

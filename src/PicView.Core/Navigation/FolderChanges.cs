using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

/// <summary>
/// What a file event in its folder means for one tab: its file list, the file it shows, its title and its gallery.
/// The watcher has already removed the file's stale image and thumbnail from the shared caches.
/// </summary>
internal sealed class FolderChanges(TabViewModel tab)
{
    public ValueTask HandleAsync(FolderChange change, CancellationToken ct) => change.Kind switch
    {
        FolderChangeKind.Created => CreatedAsync(change, ct),
        FolderChangeKind.Changed => ChangedAsync(change, ct),
        FolderChangeKind.Renamed => RenamedAsync(change, ct),
        FolderChangeKind.Deleted => DeletedAsync(change),
        _ => ValueTask.CompletedTask
    };

    private async ValueTask CreatedAsync(FolderChange change, CancellationToken ct)
    {
        if (tab.ImageIterator is not { } iterator)
        {
            return;
        }

        var file = change.File;

        // A file created over one in the list, as when overwriting it
        var existingIndex = iterator.Replace(file);
        if (existingIndex >= 0)
        {
            if (IsShown(file.FullName))
            {
                tab.Model.FileInfo = file;
                tab.FileInfo.Value = file;
                tab.UpdateTabTitle();
            }

            await Gallery(g => g.FileReplacedAsync(existingIndex, file, change.Details, ct)).ConfigureAwait(false);
            return;
        }

        var insertionIndex = iterator.Insert(file, change.CompareNames);
        iterator.NotifyFileAdded();
        tab.UpdateTabTitle();

        await Gallery(g => g.FileInsertedAsync(insertionIndex, file, change.Details, ct)).ConfigureAwait(false);
    }

    private async ValueTask ChangedAsync(FolderChange change, CancellationToken ct)
    {
        if (tab.ImageIterator is not { } iterator)
        {
            return;
        }

        var file = change.File;
        var index = iterator.Replace(file);

        // Update the shown file's details to reflect its new size
        if (IsShown(file.FullName) && await change.Details.GetAsync(ct).ConfigureAwait(false) is { } details)
        {
            tab.Model.PixelWidth = details.PixelWidth;
            tab.Model.PixelHeight = details.PixelHeight;
            tab.Model.FileInfo = file;
            tab.FileInfo.Value = file;
            tab.UpdateTabTitle();
        }

        await Gallery(g => g.FileReplacedAsync(index, file, change.Details, ct)).ConfigureAwait(false);
    }

    private async ValueTask RenamedAsync(FolderChange change, CancellationToken ct)
    {
        var file = change.File;
        var wasShownFileRenamed = tab.Model?.FileInfo?.FullName.AsSpan()
            .Equals(change.OldFullPath.AsSpan(), StringComparison.OrdinalIgnoreCase) ?? false;

        if (tab.ImageIterator is not { } iterator || change.OldFullPath is null)
        {
            return;
        }

        var (removeIndex, insertionIndex) = iterator.Move(change.OldFullPath, file, change.CompareNames);
        if (removeIndex >= 0)
        {
            Gallery(g => g.FileRemoved(removeIndex));
        }
        if (insertionIndex >= 0)
        {
            await Gallery(g => g.FileInsertedAsync(insertionIndex, file, change.Details, ct)).ConfigureAwait(false);
        }

        if (wasShownFileRenamed && tab.Model is { } currentModel)
        {
            var newModel = new ImageModel
            {
                FileInfo = file,
                Image = currentModel.Image,
                ImageType = currentModel.ImageType
            };
            tab.Display(newModel, tab.SecondaryModel);
        }

        tab.UpdateTabTitle();
    }

    private async ValueTask DeletedAsync(FolderChange change)
    {
        if (tab.ImageIterator is not { } iterator)
        {
            return;
        }
        var oldIndex = iterator.CurrentIndex;
        var currentFile = tab.Model.FileInfo;
        if (currentFile is null)
        {
            return;
        }
        var wasShownFileDeleted = currentFile.FullName.AsSpan()
            .Equals(change.File.FullName.AsSpan(), StringComparison.OrdinalIgnoreCase);

        var removeIndex = iterator.Remove(change.File.FullName);
        if (removeIndex is -1)
        {
            return;
        }

        if (iterator.Files.Count is 0)
        {
            if (tab.ParentWindowContext is not null)
            {
                await tab.ParentWindowContext.Mapper.ShowStartUpMenu().ConfigureAwait(false);
            }
            return;
        }

        if (wasShownFileDeleted)
        {
            var isNavigatingBackwards = Settings.Navigation.IsNavigatingBackwardsWhenDeleting;
            var targetIndex = isNavigatingBackwards ? oldIndex - 1 : oldIndex;
            targetIndex = Math.Clamp(targetIndex, 0, iterator.Files.Count - 1);
            if (tab.IsFileWatcherNavigationEnabled)
            {
                await iterator.IterateToIndexAsync(targetIndex, tab.GetTabCancellation())
                    .ConfigureAwait(false);
            }
            else
            {
                iterator.SetCurrentIndex(targetIndex);
            }
        }

        tab.UpdateTabTitle();
        Gallery(g => g.FileRemoved(removeIndex));
    }

    private bool IsShown(string path) =>
        string.Equals(path, tab.FileInfo?.CurrentValue?.FullName, StringComparison.OrdinalIgnoreCase);

    private ValueTask Gallery(Func<Gallery.GalleryCoordinator, ValueTask> change) =>
        tab.GalleryCoordinator is { } coordinator ? change(coordinator) : ValueTask.CompletedTask;

    private void Gallery(Action<Gallery.GalleryCoordinator> change)
    {
        if (tab.GalleryCoordinator is { } coordinator)
        {
            change(coordinator);
        }
    }
}

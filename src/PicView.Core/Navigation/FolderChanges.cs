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
        FolderChangeKind.SubfolderDeleted => SubfolderDeletedAsync(change),
        FolderChangeKind.FolderRenamed => FolderRenamed(change),
        _ => ValueTask.CompletedTask
    };

    /// <summary>
    /// Follows the files the tab browses in a renamed folder to their new paths, showing the same file.
    /// </summary>
    private ValueTask FolderRenamed(FolderChange change)
    {
        if (tab.ImageIterator is not { } iterator || change.OldFullPath is not { } oldFolder)
        {
            return ValueTask.CompletedTask;
        }

        var newFolder = change.File.FullName;
        var moved = iterator.RenameFolder(oldFolder, newFolder);
        if (moved.Length is 0)
        {
            return ValueTask.CompletedTask;
        }

        foreach (var index in moved)
        {
            var file = iterator.Files[index];
            Gallery(g => g.FileMoved(index, file));
        }

        // The shown images are the same, only their paths changed
        if (tab.Model.FileInfo is { } shown &&
            FolderChange.PathInRenamedFolder(shown.FullName, oldFolder, newFolder) is { } shownPath)
        {
            tab.Model.FileInfo = new FileInfo(shownPath);
            tab.FileInfo.Value = tab.Model.FileInfo;
        }
        if (tab.SecondaryModel?.FileInfo is { } secondary &&
            FolderChange.PathInRenamedFolder(secondary.FullName, oldFolder, newFolder) is { } secondaryPath)
        {
            tab.SecondaryModel.FileInfo = new FileInfo(secondaryPath);
            tab.SecondaryFileInfo.Value = tab.SecondaryModel.FileInfo;
        }

        tab.UpdateTabTitle();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Follows each file the tab browses in the deleted subfolder as if it were deleted on its own.
    /// </summary>
    private async ValueTask SubfolderDeletedAsync(FolderChange change)
    {
        if (tab.ImageIterator is not { } iterator)
        {
            return;
        }

        var subfolder = Path.TrimEndingDirectorySeparator(change.File.FullName) + Path.DirectorySeparatorChar;
        var deleted = iterator.Files
            .Where(file => file.FullName.StartsWith(subfolder, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        foreach (var file in deleted)
        {
            await DeletedAsync(change with { Kind = FolderChangeKind.Deleted, File = file }).ConfigureAwait(false);
        }
    }

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

        // A temp file renamed over a file in the list, as when Windows Explorer saves an edited property: the
        // original was renamed aside first, which is ignored as it is not an image, so it is still in the list
        if (!Contains(iterator, change.OldFullPath) && Contains(iterator, file.FullName))
        {
            await ChangedAsync(change, ct).ConfigureAwait(false);
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
            await tab.BecomeFreshAsync().ConfigureAwait(false);
            return;
        }

        if (wasShownFileDeleted)
        {
            var isNavigatingBackwards = Settings.Navigation.IsNavigatingBackwardsWhenDeleting;
            var targetIndex = isNavigatingBackwards ? oldIndex - 1 : oldIndex;
            targetIndex = Math.Clamp(targetIndex, 0, iterator.Files.Count - 1);
            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                var (iteration,  secondary, _) = SideBySidePair.Navigate(targetIndex,
                    tab.ImageIterator.Files.Count, NavigateTo.Next, SkipAmount.None);
                if (secondary.HasValue)
                {
                    await iterator.IterateToIndicesAsync(iteration, secondary.Value, tab.GetTabCancellation()).ConfigureAwait(false);
                }
            }
            else
            {
                await iterator.IterateToIndexAsync(targetIndex, tab.GetTabCancellation())
                    .ConfigureAwait(false);
            }
        }

        tab.UpdateTabTitle();
        Gallery(g => g.FileRemoved(removeIndex));
    }

    private static bool Contains(Interfaces.IImageIterator iterator, string path)
    {
        foreach (var file in iterator.Files)
        {
            if (string.Equals(file.FullName, path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
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

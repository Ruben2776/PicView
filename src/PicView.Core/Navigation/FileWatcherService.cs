using System.Collections.Concurrent;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Navigation;

public class FileWatcherService(
    Func<string, string, int> stringComparer,
    IImageCache cache,
    IThumbnailCache? thumbnailCache = null,
    IThumbnailLoader? thumbnailLoader = null)
    : IFileWatcherService, IDisposable
{
    private readonly IImageCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly Lock _lock = new();
    private readonly Func<string, string, int> _stringComparer = stringComparer ?? throw new ArgumentNullException(nameof(stringComparer));

    // Maps Directory Path -> (Watcher, Subscribers)
    private readonly
        ConcurrentDictionary<string, (FileSystemWatcher Watcher, IDisposable Subscription,
            List<WeakReference<TabViewModel>> Subscribers)> _watchers = new(StringComparer.OrdinalIgnoreCase);

    public void Watch(TabViewModel tab, string? directory = null)
    {
        if (tab?.ImageIterator is null)
        {
            return;
        }

        if (tab.Model is null)
        {
            return;
        }
        
        if (string.IsNullOrEmpty(directory))
        {
            var fileInfo = tab.Model?.FileInfo;
            if (fileInfo is null || string.IsNullOrEmpty(fileInfo.DirectoryName))
            {
                return;
            }

            directory = fileInfo.DirectoryName;
        }

        lock (_lock)
        {
            // If we are already watching this directory, just add the subscriber
            if (_watchers.TryGetValue(directory, out var entry))
            {
                // Remove dead references first
                entry.Subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));

                // Add if not exists
                if (!entry.Subscribers.Any(wr => wr.TryGetTarget(out var t) && ReferenceEquals(t, tab)))
                {
                    entry.Subscribers.Add(new WeakReference<TabViewModel>(tab));
                }

                return;
            }

            var watcher = new FileSystemWatcher(directory)
            {
                EnableRaisingEvents = true,
                Filter = "*.*",
                IncludeSubdirectories = Settings.Sorting.IncludeSubDirectories,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            // We use Observable.FromEvent to bridge standard .NET events to R3

            var created = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                h => (s, e) => h(e),
                h => watcher.Created += h,
                h => watcher.Created -= h
            );

            var deleted = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                h => (s, e) => h(e),
                h => watcher.Deleted += h,
                h => watcher.Deleted -= h
            );

            var renamed = Observable.FromEvent<RenamedEventHandler, RenamedEventArgs>(
                h => (s, e) => h(e),
                h => watcher.Renamed += h,
                h => watcher.Renamed -= h
            );
            
            var changed = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
                h => (s, e) => h(e),
                h => watcher.Changed += h,
                h => watcher.Changed -= h
            );

            // AwaitOperation.Sequential ensures we don't process two file events for the same folder at the exact same time, 
            // which protects the Integrity of the 'files' list and the CurrentIndex.

            var fileCreatedSub = created.SubscribeAwait(async (e, ct) =>
                await OnFileCreatedAsync(tab, e).ConfigureAwait(false), 
                DebugHelper.LogError(nameof(FileWatcherService), nameof(OnFileCreatedAsync)));

            var fileDeletedSub = deleted.SubscribeAwait(async (e, ct) =>
                await OnFileDeletedAsync(tab, e).ConfigureAwait(false), 
                DebugHelper.LogError(nameof(FileWatcherService), nameof(OnFileDeletedAsync)));

            var fileRenamedSub = renamed.SubscribeAwait(async (e, ct) =>
                await OnFileRenamedAsync(tab, e).ConfigureAwait(false), 
                DebugHelper.LogError(nameof(FileWatcherService), nameof(OnFileRenamedAsync)));
            
            var fileChangedSub = changed.SubscribeAwait(async (e, ct) =>
                await OnFileChangedAsync(tab, e).ConfigureAwait(false), 
                DebugHelper.LogError(nameof(FileWatcherService), nameof(OnFileChangedAsync)));

            // Combine disposables
            var subscription = Disposable.Combine(fileCreatedSub, fileDeletedSub, fileRenamedSub, fileChangedSub);

            _watchers[directory] = (watcher, subscription, [new WeakReference<TabViewModel>(tab)]);
        }
    }

    public void Unwatch(TabViewModel tab)
    {
        lock (_lock)
        {
            // iterate all watchers to find the tab
            var keysToRemove = new List<string>();

            foreach (var kvp in _watchers)
            {
                var (watcher, subscription, subscribers) = kvp.Value;

                // Remove the tab
                subscribers.RemoveAll(wr => !wr.TryGetTarget(out var t) || ReferenceEquals(t, tab));

                if (subscribers.Count != 0)
                {
                    continue;
                }

                // Dispose R3 subscription AND Watcher
                subscription.Dispose();
                watcher.Dispose();
                keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
            {
                _watchers.TryRemove(key, out _);
            }
        }
    }

    private async ValueTask OnFileCreatedAsync(TabViewModel tab, FileSystemEventArgs e)
    {
        if (!e.FullPath.IsSupported())
        {
            return;
        }

        var newFile = new FileInfo(e.FullPath);

        if (tab.ImageIterator.Files is not List<FileInfo> files)
        {
            return;
        }
        var insertionIndex = FileSortOrder.InsertSorted(files, newFile, _stringComparer);
        
        tab.ImageIterator.UpdateNavigationProperties();
        
        if (tab.Model.FileInfo is null)
        {
            return;
        }
        var newIndex = files.FindIndex(x =>
            x.FullName.AsSpan().Equals(tab.Model.FileInfo.FullName.AsSpan(), StringComparison.OrdinalIgnoreCase));
        if (newIndex >= 0)
        {
            tab.ImageIterator.SetCurrentIndex(newIndex);
        }

        _cache.Resynchronize(tab.Id, files);
        tab.UpdateTabTitle();

        if (insertionIndex >= 0 && tab.Gallery.IsGalleryDocked.CurrentValue)
        {
            using var magick = new MagickImage();
            await magick.PingAsync(newFile).ConfigureAwait(false);
            var item = new GalleryItemViewModel
            {
                FileInfo = newFile
            };

            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(newFile, magick.Width, magick.Height);
            item.FileName.Value = thumbData.FileName;
            item.FileSize.Value = thumbData.FileSize;
            item.FileDate.Value = thumbData.FileDate;
            item.FileLocation.Value = thumbData.FileLocation;
            item.ImageSize.Value = thumbData.ImageSize;

            tab.Gallery.GalleryItems.Insert(insertionIndex, item);

            if (thumbnailLoader != null)
            {
                var maxHeight = Math.Max(Settings.Gallery.DockedGalleryItemSize, Settings.Gallery.ExpandedGalleryItemSize);
                if (maxHeight <= 0)
                {
                    maxHeight = GalleryDefaults.DefaultDockedGalleryHeight;
                }

                var thumb = await thumbnailLoader.GetThumbnailAsync(newFile, (uint)maxHeight).ConfigureAwait(false);
                if (thumb != null)
                {
                    thumbnailCache?.Add(tab.Id, newFile.FullName, thumb);
                }
                item.Image.Value = thumb;
            }
        }
    }

    private async ValueTask OnFileDeletedAsync(TabViewModel tab, FileSystemEventArgs e)
    {
        if (!e.FullPath.IsSupported())
        {
            return;
        }
        var fullPath = e.FullPath;

        thumbnailCache?.Remove(fullPath);
        
        var oldIndex = tab.ImageIterator.CurrentIndex;
        var currentFile = tab.Model.FileInfo;
        if (currentFile is null)
        {
            return;
        }
        var wasCurrentFileDeleted = currentFile.FullName.AsSpan().Equals(e.FullPath.AsSpan(), StringComparison.OrdinalIgnoreCase);

        if (tab.ImageIterator.Files is not List<FileInfo> files)
        {
            return;
        }
        var removeIndex = files.FindIndex(x => x.FullName.AsSpan().Equals(fullPath.AsSpan(), StringComparison.OrdinalIgnoreCase));
        if (removeIndex is -1)
        {
            return;
        }
        files.RemoveAt(removeIndex);
        
        if (files.Count is 0)
        {
            if (tab.ParentWindowContext is not null)
            {
                await tab.ParentWindowContext.Mapper.ShowStartUpMenu().ConfigureAwait(false);
            }
            return;
        }

        if (wasCurrentFileDeleted)
        {
            var isNavigatingBackwards = Settings.Navigation.IsNavigatingBackwardsWhenDeleting;
            var targetIndex = isNavigatingBackwards ? oldIndex - 1 : oldIndex;
            targetIndex = Math.Clamp(targetIndex, 0, files.Count - 1);
            if (tab.IsFileWatcherNavigationEnabled)
            {
                await tab.ImageIterator.IterateToIndexAsync(targetIndex, tab.GetTabCancellation())
                    .ConfigureAwait(false);
            }
            else
            {
                tab.ImageIterator.SetCurrentIndex(targetIndex);
            }
        }
        else
        {
            var newIndex = files.FindIndex(x =>
                x.FullName.AsSpan().Equals(currentFile.FullName.AsSpan(), StringComparison.OrdinalIgnoreCase));
            if (newIndex >= 0)
            {
                tab.ImageIterator.SetCurrentIndex(newIndex);
            }
        }

        _cache.Resynchronize(tab.Id, files);
        tab.UpdateTabTitle();
        
        if (tab.Gallery.GalleryItems.Count > removeIndex)
        {
            tab.Gallery.GalleryItems.RemoveAt(removeIndex);
        }
    }

    private async ValueTask OnFileRenamedAsync(TabViewModel tab, RenamedEventArgs e)
    {
        if (!e.FullPath.IsSupported())
        {
            return;
        }

        thumbnailCache?.Remove(e.OldFullPath);

        var newFileInfo = new FileInfo(e.FullPath);

        var currentFile = tab.Model?.FileInfo;
        var wasCurrentFileRenamed = currentFile?.FullName.AsSpan()
            .Equals(e.OldFullPath.AsSpan(), StringComparison.OrdinalIgnoreCase) ?? false;

        if (tab.ImageIterator.Files is not List<FileInfo> files)
        {
            return;
        }

        var removeIndex = files.FindIndex(x => x.FullName.AsSpan().Equals(e.OldFullPath.AsSpan(), StringComparison.OrdinalIgnoreCase));
        if (removeIndex >= 0)
        {
            files.RemoveAt(removeIndex);
            if (tab.Gallery.GalleryItems.Count > removeIndex)
            {
                tab.Gallery.GalleryItems.RemoveAt(removeIndex);
            }
        }
        var insertionIndex = FileSortOrder.InsertSorted(files, newFileInfo, _stringComparer);

        if (insertionIndex >= 0 && insertionIndex > tab.Gallery.GalleryItems.Count)
        {
            using var magick = new MagickImage();
            await magick.PingAsync(newFileInfo).ConfigureAwait(false);
            var item = new GalleryItemViewModel
            {
                FileInfo = newFileInfo
            };

            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(newFileInfo, magick.Width, magick.Height);
            item.FileName.Value = thumbData.FileName;
            item.FileSize.Value = thumbData.FileSize;
            item.FileDate.Value = thumbData.FileDate;
            item.FileLocation.Value = thumbData.FileLocation;
            item.ImageSize.Value = thumbData.ImageSize;

            tab.Gallery.GalleryItems.Insert(insertionIndex, item);

            if (thumbnailLoader != null)
            {
                var maxHeight = Math.Max(Settings.Gallery.DockedGalleryItemSize, Settings.Gallery.ExpandedGalleryItemSize);
                if (maxHeight <= 0)
                {
                    maxHeight = GalleryDefaults.DefaultDockedGalleryHeight;
                }

                var thumb = await thumbnailLoader.GetThumbnailAsync(newFileInfo, (uint)maxHeight).ConfigureAwait(false);
                if (thumb != null)
                {
                    thumbnailCache?.Add(tab.Id, newFileInfo.FullName, thumb);
                }
                item.Image.Value = thumb;
            }
        }

        if (wasCurrentFileRenamed)
        {
            var currentModel = tab.Model;
            if (currentModel != null)
            {
                var newModel = new ImageModel
                {
                    FileInfo = newFileInfo,
                    Image = currentModel.Image,
                    ImageType = currentModel.ImageType
                };
                tab.Model = newModel;
            }
        }

        var fileToCheck = wasCurrentFileRenamed ? newFileInfo : currentFile;

        if (fileToCheck != null)
        {
            var newIndex = files.FindIndex(x =>
                x.FullName.Equals(fileToCheck.FullName, StringComparison.OrdinalIgnoreCase));
            if (newIndex >= 0)
            {
                tab.ImageIterator.SetCurrentIndex(newIndex);
            }
        }

        _cache.Resynchronize(tab.Id, files);
        tab.UpdateTabTitle();
        
        FileHistoryManager.Rename(e.OldFullPath, e.FullPath);
    }
    /// Update the tabs FileInfo to reflect an updated new file size
    private async ValueTask OnFileChangedAsync(TabViewModel tab, FileSystemEventArgs e)
    {
        var newFile = new FileInfo(e.FullPath);
        var previousFile = tab.Model.FileInfo;
        if (previousFile is null)
        {
            return;
        }
        if (newFile.Length == previousFile.Length)
        {
            // Don't do anything if the file size hasn't changed
            return;
        }
        
        if (tab.ImageIterator?.Files is not List<FileInfo> files)
        {
            return;
        }
        var index = files.FindIndex(x => x.FullName.AsSpan().Equals(newFile.FullName.AsSpan(), StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            files[index] = newFile;
        }
        
        if (!string.Equals(e.FullPath, tab.FileInfo?.CurrentValue?.FullName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            using var magick = new MagickImage();
            await magick.PingAsync(newFile).ConfigureAwait(false);
            tab.Model.PixelWidth = magick.Width;
            tab.Model.PixelHeight = magick.Height;
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(FileWatcherService), nameof(OnFileChangedAsync), exception);
        }
        tab.Model.FileInfo = newFile;
        tab.FileInfo.Value = newFile;
        tab.UpdateTabTitle();
    }

    #region IDispose

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var (_, (watcher, subscription, _)) in _watchers)
            {
                subscription.Dispose();
                watcher.Dispose();
            }

            _watchers.Clear();
        }

        GC.SuppressFinalize(this);
    }

    #endregion
}
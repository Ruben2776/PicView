using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
using PicView.Core.Navigation;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.FileHandling;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using Windows.Devices.Geolocation;

namespace PicView.WPF.ChangeImage;

internal static class FileUpdateNavigation
{
    internal static FileSystemWatcher? watcher;
    private static bool _running;

    internal static void Initiate(string path)
    {
        // Don't run when browsing archive
        if (!string.IsNullOrWhiteSpace(ArchiveHelper.TempFilePath))
        {
            if (watcher is not null)
            {
                watcher = null;
            }
            return;
        }
        try
        {
            watcher ??= new FileSystemWatcher();
            watcher.Path = path;
            watcher.EnableRaisingEvents = true;
            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = SettingsHelper.Settings.Sorting.IncludeSubDirectories;
            watcher.Created += async (_, e) => await OnFileAdded(e).ConfigureAwait(false);
            watcher.Deleted += async (_, e) => await OnFileDeleted(e).ConfigureAwait(false);
            watcher.Renamed += async (_, e) => await OnFileRenamed(e).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(Initiate)} exception:\n{exception.Message}");
#endif
        }
    }

    #region Methods

    internal static async Task UpdateTitle(int index)
    {
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            var width = ConfigureWindows.GetMainWindow.MainImage.Source?.Width ?? 0;
            var height = ConfigureWindows.GetMainWindow.MainImage.Source?.Height ?? 0;
            try
            {
                ChangeTitlebar.SetTitle.SetTitleString((int)width, (int)height, index, null);
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(OnFileRenamed)} update title exception:\n{exception.Message}");
#endif
            }
        });
    }

    internal static async Task UpdateGalleryAsync(int index, int oldIndex, FileInfo fileInfo, string oldPath, string newPath, bool sameFile)
    {
        if (UC.GetPicGallery is null)
        {
            return;
        }

        if (index < 0 || oldIndex < 0)
        {
            return;
        }

        // Fix incorrect selection
        await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
        {
            GalleryNavigation.SelectedGalleryItem = - 1;
            GalleryNavigation.SetSelected(GalleryNavigation.SelectedGalleryItem, false);
            GalleryNavigation.SetSelected(oldIndex, false);
        });

        while (GalleryLoad.IsLoading)
        {
            await Task.Delay(100);
        }
        var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(index, null, fileInfo);
        await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
        {
            var item = UC.GetPicGallery.Container.Children.OfType<PicGalleryItem>().FirstOrDefault(x => x.FilePath == oldPath);
            if (item is not null)
            {
                item.UpdateValues(thumbData.FileName, thumbData.FileDate, thumbData.FileSize, thumbData.FileLocation);
                // I need to change it's position here because it's not updating the position when it's renamed
                UC.GetPicGallery.Container.Children.Remove(item);
                UC.GetPicGallery.Container.Children.Insert(index, item);
            }
            else
            {
#if DEBUG
                Trace.WriteLine($"{nameof(OnFileRenamed)} item null");
#endif
            }
        });
        if (sameFile)
        {
            await Task.Delay(100); // Fixes not scrolling
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                GalleryNavigation.SelectedGalleryItem = index;
                GalleryNavigation.SetSelected(GalleryNavigation.SelectedGalleryItem, true);
                GalleryNavigation.SetSelected(index, true);
                GalleryNavigation.ScrollToGalleryCenter();
            });
        }
    }

    #endregion Methods

    private static async Task OnFileRenamed(RenamedEventArgs e)
    {
        if (FileFunctions.IsFileBeingRenamed)
        {
            return;
        }
        if (e.FullPath.IsSupported() == false)
        {
            if (Navigation.Pics.Contains(e.OldFullPath))
            {
                Navigation.Pics.Remove(e.OldFullPath);
            }
            return;
        }
        if (_running) { return; }
        _running = true;

        var oldIndex = Navigation.Pics.IndexOf(e.OldFullPath);
        var sameFile = Navigation.FolderIndex == Navigation.Pics.IndexOf(e.OldFullPath);

        var fileInfo = new FileInfo(e.FullPath);
        if (fileInfo.Exists == false) { return; }

        var newList = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);
        if (newList.Count == 0) { return; }

        if (fileInfo.Exists == false) { return; }

        Navigation.Pics = newList;

        var index = Navigation.Pics.IndexOf(e.FullPath);
        if (index < 0) { return; }

        if (fileInfo.Exists == false)
        {
            return;
        }
        var bitmapSource = !sameFile ? null : PreLoader.Get(Navigation.FolderIndex)?.BitmapSource;
        PreLoader.Remove(index);

        if (sameFile)
        {
            Navigation.FolderIndex = index;
            await PreLoader.AddAsync(Navigation.FolderIndex, fileInfo, bitmapSource).ConfigureAwait(false);
        }
        await UpdateTitle(index);
        _running = false;
        FileHistoryNavigation.Rename(e.OldFullPath, e.FullPath);
        await UpdateGalleryAsync(index, oldIndex, fileInfo, e.OldFullPath, e.FullPath, sameFile).ConfigureAwait(false);
        await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);
    }

    private static async Task OnFileDeleted(FileSystemEventArgs e)
    {
        if (FileFunctions.IsFileBeingRenamed)
        {
            return;
        }
        if (e.FullPath.IsSupported() == false)
        {
            return;
        }

        if (Navigation.Pics.Contains(e.FullPath) == false)
        {
            return;
        }

        if (_running) { return; }
        _running = true;
        var sameFile = Navigation.FolderIndex == Navigation.Pics.IndexOf(e.FullPath);
        if (!Navigation.Pics.Remove(e.FullPath))
        {
            return;
        }
        Navigation.FolderIndex--;

        PreLoader.Remove(Navigation.FolderIndex);

        if (sameFile)
        {
            await Navigation.GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);
        }
        else
        {
            await UpdateTitle(Navigation.FolderIndex);
        }
        _running = false;

        FileHistoryNavigation.Remove(e.FullPath);

        if (UC.GetPicGallery is not null)
        {
            while (GalleryLoad.IsLoading)
            {
                await Task.Delay(100);
            }
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                var item = UC.GetPicGallery.Container.Children.OfType<PicGalleryItem>().FirstOrDefault(x => x.FilePath == e.FullPath);
                if (item is null)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(OnFileDeleted)} item null");
#endif
                    return;
                }
                try
                {
                    UC.GetPicGallery.Container.Children.Remove(item);
                }
                catch (Exception exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(OnFileDeleted)} remove gallery item exception:\n{exception.Message}");
#endif
                }
            });
        }
    }

    private static async Task OnFileAdded(FileSystemEventArgs e)
    {
        if (FileFunctions.IsFileBeingRenamed)
        {
            return;
        }
        if (Navigation.Pics.Contains(e.FullPath))
        {
            return;
        }
        if (e.FullPath.IsSupported() == false)
        {
            return;
        }
        var fileInfo = new FileInfo(e.FullPath);
        if (fileInfo.Exists == false) { return; }

        if (_running) { return; }
        _running = true;

        var newList = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);
        if (newList.Count == 0) { return; }
        if (newList.Count == Navigation.Pics.Count) { return; }

        if (fileInfo.Exists == false) { return; }

        Navigation.Pics = newList;

        await UpdateTitle(Navigation.FolderIndex);

        _running = false;

        var index = Navigation.Pics.IndexOf(e.FullPath);
        if (index < 0) { return; }

        var nextIndex = index + 1;
        if (index >= Navigation.Pics.Count)
        {
            nextIndex = 0;
        }
        var prevIndex = index - 1;
        if (prevIndex < 0)
        {
            prevIndex = Navigation.Pics.Count - 1;
        }
        if (PreLoader.Contains(index) || PreLoader.Contains(nextIndex) || PreLoader.Contains(prevIndex))
        {
            PreLoader.Clear();
            await PreLoader.PreLoadAsync(Navigation.FolderIndex, Navigation.Pics.Count).ConfigureAwait(false);
        }

        if (UC.GetPicGallery is not null)
        {
            if (Navigation.Pics.Count < Navigation.FolderIndex || Navigation.Pics.Count < 1 || index > Navigation.Pics.Count)
            {
                return;
            }

            while (GalleryLoad.IsLoading)
            {
                // Don't add when they're being loaded
                await Task.Delay(500);
            }

            var exists = false;
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                if (UC.GetPicGallery.Container.Children.Count == Navigation.Pics.Count)
                    exists = true;
            });
            if (exists)
            {
                return;
            }
            var thumbSource = await Thumbnails.GetBitmapSourceThumbAsync(e.FullPath,
                (int)GalleryNavigation.PicGalleryItemSize, fileInfo).ConfigureAwait(false);
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(index, thumbSource, fileInfo);
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                var item = new PicGalleryItem(thumbSource, e.FullPath, false)
                {
                    FilePath = thumbData.FileName,
                    ThumbFileDate =
                    {
                        Text = thumbData.FileDate
                    },
                    ThumbFileLocation =
                    {
                        Text = thumbData.FileLocation
                    },
                    ThumbFileSize =
                    {
                        Text = thumbData.FileSize
                    }
                };
                try
                {
                    UC.GetPicGallery.Container.Children.Insert(index, item);
                }
                catch (Exception exception)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(OnFileAdded)} add gallery item exception:\n{exception.Message}");
#endif
                }
            });
        }
    }
}
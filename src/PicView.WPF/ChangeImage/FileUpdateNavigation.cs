using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.IO;
using PicView.WPF.FileHandling;

namespace PicView.WPF.ChangeImage;

internal static class FileUpdateNavigation
{
    internal static FileSystemWatcher? watcher;

    internal static void Initiate(string path)
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

    private static async Task OnFileRenamed(RenamedEventArgs e)
    {
        if (e.FullPath.IsSupported() == false)
        {
            if (Navigation.Pics.Contains(e.OldFullPath))
            {
                Navigation.Pics.Remove(e.OldFullPath);
            }
            return;
        }
        var index = Navigation.Pics.IndexOf(e.FullPath);
        if (index < 0) { return; }

        Navigation.Pics[Navigation.Pics.IndexOf(e.OldFullPath)] = e.FullPath;
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ChangeTitlebar.SetTitle.SetTitleString);
        if (PreLoader.Contains(index))
        {
            PreLoader.Remove(index);
            await PreLoader.AddAsync(index).ConfigureAwait(false);
        }
        if (UC.GetPicGallery is not null)
        {
            var thumbData = await GalleryLoad.GalleryThumbHolder.GetThumbDataAsync(index).ConfigureAwait(false);
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                if (UC.GetPicGallery.Container.Children[index] is not PicGalleryItem item)
                {
                    return;
                }

                item.FileName = thumbData.FileName;
                item.ThumbFileDate.Text = thumbData.FileDate;
                item.ThumbFileLocation.Text = thumbData.FileLocation;
                item.ThumbFileSize.Text = thumbData.FileSize;
            });
        }
    }

    private static async Task OnFileDeleted(FileSystemEventArgs e)
    {
        if (e.FullPath.IsSupported() == false)
        {
            return;
        }

        var index = Navigation.Pics.IndexOf(e.FullPath);
        if (index < 0) { return; }
        if (Navigation.Pics.Remove(e.FullPath)) { return; }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ChangeTitlebar.SetTitle.SetTitleString);
        if (UC.GetPicGallery is not null)
        {
            while (GalleryLoad.IsLoading)
            {
                await Task.Delay(100);
            }
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                UC.GetPicGallery.Container.Children.RemoveAt(index);
            });
        }

        if (PreLoader.Contains(index))
        {
            PreLoader.Remove(index);
        }
    }

    private static async Task OnFileAdded(FileSystemEventArgs e)
    {
        if (e.FullPath.IsSupported() == false)
        {
            return;
        }
        var fileInfo = new FileInfo(e.FullPath);
        if (fileInfo.Exists == false) { return; }

        var newList = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);
        if (newList.Count == 0) { return; }
        if (newList.Count == Navigation.Pics.Count) { return; }
        Navigation.Pics = newList;

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ChangeTitlebar.SetTitle.SetTitleString);
        var index = Navigation.Pics.IndexOf(e.FullPath);
        if (index < 0) { return; }

        if (UC.GetPicGallery is not null)
        {
            var thumbData = await GalleryLoad.GalleryThumbHolder.GetThumbDataAsync(index, fileInfo).ConfigureAwait(false);
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                var item = new PicGalleryItem(thumbData.BitmapSource, e.FullPath, false)
                {
                    FileName = thumbData.FileName,
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
                UC.GetPicGallery.Container.Children.Insert(index, item);
            });
        }
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
    }
}
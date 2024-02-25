using PicView.Avalonia.Models;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;
using System.Diagnostics;
using PicView.Avalonia.Services;

namespace PicView.Avalonia.Navigation
{
    public class ImageIterator
    {
        public event FileSystemEventHandler? FileAdded;

        public event EventHandler<bool>? FileDeleted;

        public event FileSystemEventHandler? FileRenamed;

        public int Index;
        public FileInfo FileInfo;
        public bool Reverse;
        public PreLoader PreLoader { get; } = new();

        private static FileSystemWatcher? _watcher;
        private static bool _running;

        public ImageIterator(FileInfo fileInfo, List<string> pics)
        {
            ArgumentNullException.ThrowIfNull(fileInfo);

            FileInfo = fileInfo;
            Index = pics.IndexOf(fileInfo.FullName);
            _watcher ??= new FileSystemWatcher();
#if DEBUG
            Debug.Assert(fileInfo.DirectoryName != null, "fileInfo.DirectoryName != null");
#endif
            _watcher.Path = fileInfo.DirectoryName;
            _watcher.EnableRaisingEvents = true;
            _watcher.Filter = "*.*";
            _watcher.IncludeSubdirectories = SettingsHelper.Settings.Sorting.IncludeSubDirectories;
            _watcher.Created += async (_, e) => await OnFileAdded(e, pics).ConfigureAwait(false);
            _watcher.Deleted += (_, e) => OnFileDeleted(e, pics);
            _watcher.Renamed += (_, e) => OnFileRenamed(e, pics);
        }

        public async Task Preload(ImageService imageService, List<string> pics)
        {
            await PreLoader.PreLoadAsync(Index, pics.Count, Reverse, imageService, pics).ConfigureAwait(false);
        }

        public async Task AddAsync(int index, ImageService imageService, ImageModel imageModel, List<string> pics)
        {
            await PreLoader.AddAsync(index, imageService, pics, imageModel).ConfigureAwait(false);
        }

        public int GetIteration(int index, NavigateTo navigateTo, List<string> pics)
        {
            int next;
            switch (navigateTo)
            {
                case NavigateTo.Next:
                case NavigateTo.Previous:
                    var indexChange = navigateTo == NavigateTo.Next ? 1 : -1;
                    Reverse = navigateTo == NavigateTo.Previous;
                    if (SettingsHelper.Settings.UIProperties.Looping)
                    {
                        next = (index + indexChange + pics.Count) % pics.Count;
                    }
                    else
                    {
                        var newIndex = index + indexChange;
                        if (newIndex < 0)
                        {
                            return 0;
                        }
                        if (newIndex >= pics.Count)
                        {
                            return pics.Count - 1;
                        }
                        next = newIndex;
                    }

                    break;

                case NavigateTo.First:
                case NavigateTo.Last:
                    if (pics.Count > PreLoader.MaxCount)
                        PreLoader.Clear();
                    next = navigateTo == NavigateTo.First ? 0 : pics.Count - 1;
                    break;

                default: return -1;
            }
            return next;
        }

        private void OnFileRenamed(RenamedEventArgs e, List<string> pics)
        {
            if (e.FullPath.IsSupported() == false)
            {
                if (pics.Contains(e.OldFullPath))
                {
                    pics.Remove(e.OldFullPath);
                }
                return;
            }
            if (_running) { return; }
            _running = true;

            var oldIndex = pics.IndexOf(e.OldFullPath);
            var sameFile = Index == pics.IndexOf(e.OldFullPath);

            var fileInfo = new FileInfo(e.FullPath);
            if (fileInfo.Exists == false) { return; }

            var newList = FileListHelper.RetrieveFiles(fileInfo).ToList();
            if (newList.Count == 0) { return; }

            if (fileInfo.Exists == false) { return; }

            pics = newList;

            var index = pics.IndexOf(e.FullPath);
            if (index < 0) { return; }

            if (fileInfo.Exists == false)
            {
                return;
            }

            PreLoader.Remove(index, pics);

            _running = false;
            //FileHistoryNavigation.Rename(e.OldFullPath, e.FullPath);
            //await UpdateGalleryAsync(index, oldIndex, fileInfo, e.OldFullPath, e.FullPath, sameFile).ConfigureAwait(false);
            //await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);
            FileRenamed?.Invoke(this, e);
        }

        private void OnFileDeleted(FileSystemEventArgs e, List<string> pics)
        {
            if (e.FullPath.IsSupported() == false)
            {
                return;
            }

            if (pics.Contains(e.FullPath) == false)
            {
                return;
            }

            if (_running) { return; }
            _running = true;
            var sameFile = Index == pics.IndexOf(e.FullPath);
            if (!pics.Remove(e.FullPath))
            {
                return;
            }
            Index--;

            PreLoader.Remove(Index, pics);

            if (sameFile)
            {
                //await Navigation.GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);
            }
            else
            {
                //await UpdateTitle(Navigation.FolderIndex);
            }
            _running = false;

            //FileHistoryNavigation.Remove(e.FullPath);
            FileDeleted?.Invoke(this, sameFile);
        }

        private async Task OnFileAdded(FileSystemEventArgs e, List<string> pics)
        {
            if (pics.Contains(e.FullPath))
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

            var newList = await Task.FromResult(FileListHelper.RetrieveFiles(fileInfo).ToList()); // TODO update to sync with platform
            if (newList.Count == 0) { return; }
            if (newList.Count == pics.Count) { return; }

            if (fileInfo.Exists == false) { return; }

            pics = newList;

            _running = false;

            var index = pics.IndexOf(e.FullPath);
            if (index < 0) { return; }

            var nextIndex = index + 1;
            if (index >= pics.Count)
            {
                nextIndex = 0;
            }
            var prevIndex = index - 1;
            if (prevIndex < 0)
            {
                prevIndex = pics.Count - 1;
            }
            if (PreLoader.Contains(index, pics) || PreLoader.Contains(nextIndex, pics) || PreLoader.Contains(prevIndex, pics))
            {
                PreLoader.Clear();
            }
            FileAdded?.Invoke(this, e);
        }
    }
}
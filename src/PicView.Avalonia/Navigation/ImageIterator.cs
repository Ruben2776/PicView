using PicView.Avalonia.Helpers;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Models;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;
using System.Diagnostics;
using PicView.Avalonia.Gallery;
using PicView.Core.Gallery;
using Timer = System.Timers.Timer;

namespace PicView.Avalonia.Navigation
{
    public class ImageIterator
    {
        public event FileSystemEventHandler? FileAdded;

        public event EventHandler<bool>? FileDeleted;

        public event FileSystemEventHandler? FileRenamed;

        public List<string> Pics { get; set; }

        public bool IsFileBeingRenamed { get; set; }

        public int Index;
        public FileInfo FileInfo;
        public bool Reverse;
        public PreLoader PreLoader { get; } = new();

        private static FileSystemWatcher? _watcher;
        private static bool _running;
        private readonly MainViewModel? _vm;

        public ImageIterator(FileInfo fileInfo, MainViewModel vm)
        {
            ArgumentNullException.ThrowIfNull(fileInfo);

            FileInfo = fileInfo;

            _vm = vm;
            Pics = vm.PlatformService.GetFiles(fileInfo);
            Index = Pics.IndexOf(fileInfo.FullName);
#if DEBUG
            Debug.Assert(fileInfo.DirectoryName != null, "fileInfo.DirectoryName != null");
#endif
            InitiateWatcher(fileInfo);
        }

        private void InitiateWatcher(FileInfo fileInfo)
        {
            _watcher = new FileSystemWatcher();
#if DEBUG
            Debug.Assert(fileInfo.DirectoryName != null, "fileInfo.DirectoryName != null");
#endif
            _watcher.Path = fileInfo.DirectoryName;
            _watcher.EnableRaisingEvents = true;
            _watcher.Filter = "*.*";
            _watcher.IncludeSubdirectories = SettingsHelper.Settings.Sorting.IncludeSubDirectories;
            _watcher.Created += async (_, e) => await OnFileAdded(e).ConfigureAwait(false);
            _watcher.Deleted += (_, e) => OnFileDeleted(e);
            _watcher.Renamed += (_, e) => OnFileRenamed(e);
        }

        public async Task Preload()
        {
            await PreLoader.PreLoadAsync(Index, Pics.Count, Reverse, Pics).ConfigureAwait(false);
        }

        public async Task AddAsync(int index, ImageModel imageModel)
        {
            await PreLoader.AddAsync(index, Pics, imageModel).ConfigureAwait(false);
        }

        public async Task ReloadFileList()
        {
            Pics = await Task.FromResult(_vm.PlatformService.GetFiles(FileInfo)).ConfigureAwait(false);
            Index = Pics.IndexOf(FileInfo.FullName);
            InitiateWatcher(FileInfo);
        }

        public int GetIteration(int index, NavigateTo navigateTo)
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
                        next = (index + indexChange + Pics.Count) % Pics.Count;
                    }
                    else
                    {
                        var newIndex = index + indexChange;
                        if (newIndex < 0)
                        {
                            return 0;
                        }
                        if (newIndex >= Pics.Count)
                        {
                            return Pics.Count - 1;
                        }
                        next = newIndex;
                    }

                    break;

                case NavigateTo.First:
                case NavigateTo.Last:
                    if (Pics.Count > PreLoader.MaxCount)
                        PreLoader.Clear();
                    next = navigateTo == NavigateTo.First ? 0 : Pics.Count - 1;
                    break;

                default: return -1;
            }
            return next;
        }

        private void OnFileRenamed(RenamedEventArgs e)
        {
            if (IsFileBeingRenamed)
            {
                return;
            }
            if (e.FullPath.IsSupported() == false)
            {
                if (Pics.Contains(e.OldFullPath))
                {
                    Pics.Remove(e.OldFullPath);
                }
                return;
            }
            if (_running) { return; }
            _running = true;

            var oldIndex = Pics.IndexOf(e.OldFullPath);
            var sameFile = Index == Pics.IndexOf(e.OldFullPath);

            var fileInfo = new FileInfo(e.FullPath);
            if (fileInfo.Exists == false) { return; }

            var newList = FileListHelper.RetrieveFiles(fileInfo).ToList();
            if (newList.Count == 0) { return; }

            if (fileInfo.Exists == false) { return; }

            Pics = newList;

            var index = Pics.IndexOf(e.FullPath);
            if (index < 0) { return; }

            if (fileInfo.Exists == false)
            {
                return;
            }

            PreLoader.Remove(index, Pics);

            _running = false;
            //FileHistoryNavigation.Rename(e.OldFullPath, e.FullPath);
            //await UpdateGalleryAsync(index, oldIndex, fileInfo, e.OldFullPath, e.FullPath, sameFile).ConfigureAwait(false);
            //await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);
            FileRenamed?.Invoke(this, e);
        }

        private void OnFileDeleted(FileSystemEventArgs e)
        {
            if (IsFileBeingRenamed)
            {
                return;
            }
            if (e.FullPath.IsSupported() == false)
            {
                return;
            }

            if (Pics.Contains(e.FullPath) == false)
            {
                return;
            }

            if (_running) { return; }
            _running = true;
            var sameFile = Index == Pics.IndexOf(e.FullPath);
            if (!Pics.Remove(e.FullPath))
            {
                return;
            }
            Index--;

            PreLoader.Remove(Index, Pics);

            _running = false;

            //FileHistoryNavigation.Remove(e.FullPath);
            FileDeleted?.Invoke(this, sameFile);
        }

        private async Task OnFileAdded(FileSystemEventArgs e)
        {
            if (IsFileBeingRenamed)
            {
                return;
            }
            if (Pics.Contains(e.FullPath))
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

            var newList = await Task.FromResult(_vm.PlatformService.GetFiles(fileInfo));
            if (newList.Count == 0) { return; }
            if (newList.Count == Pics.Count) { return; }

            if (fileInfo.Exists == false) { return; }
            
            while (GalleryLoad.IsLoading)
            {
                await Task.Delay(200);
            }
            Pics = newList;

            _running = false;

            var index = Pics.IndexOf(e.FullPath);
            if (index < 0) { return; }

            var nextIndex = index + 1;
            if (index >= Pics.Count)
            {
                nextIndex = 0;
            }
            var prevIndex = index - 1;
            if (prevIndex < 0)
            {
                prevIndex = Pics.Count - 1;
            }
            if (PreLoader.Contains(index, Pics) || PreLoader.Contains(nextIndex, Pics) || PreLoader.Contains(prevIndex, Pics))
            {
                PreLoader.Clear();
            }
            
            FileAdded?.Invoke(this, e);
            
            if (_vm.GalleryItems.Count > 0 && _vm.ImageIterator.Index < _vm.GalleryItems.Count)
            {
                var galleryItem = _vm.GalleryItems[_vm.ImageIterator.Index];
                if (_vm.GalleryItems.Contains(galleryItem))
                {
                    return;
                }
                var newFileInfo = new FileInfo(Pics[index]);
                var galleryViewModel = new GalleryViewModel(_vm.GalleryItemSize);
                galleryViewModel.ImageSource = await ThumbnailHelper.GetThumb(newFileInfo, (int)galleryViewModel.GalleryItemSize);
                _vm.GalleryItems.Add(galleryViewModel);
                _vm.GalleryItems.Move(_vm.GalleryItems.IndexOf(galleryViewModel), Index);
            }
        }

        public async Task LoadNextPic(NavigateTo navigateTo, MainViewModel vm)
        {
            var index = GetIteration(Index, navigateTo);
            if (index < 0)
            {
                return;
            }

            if (!MainKeyboardShortcuts.IsKeyHeldDown)
            {
                await LoadPicAtIndex(index, vm);
            }
            else
            {
                await TimerPic(index, vm);
            }
        }

        public async Task LoadPicAtIndex(int index, MainViewModel vm) => await Task.Run(async () =>
        {
            try
            {
                Index = index;

                var preLoadValue = PreLoader.Get(index, Pics);
                var x = 0;
                if (preLoadValue is not null)
                {
                    if (preLoadValue.IsLoading)
                    {
                        vm.SetLoadingTitle();
                        vm.IsLoading = true;
                        await NavigationHelper.LoadingPreview(index, vm);
                    }

                    while (preLoadValue.IsLoading)
                    {
                        x++;
                        await Task.Delay(20);
                        if (Index != index)
                        {
                            await Preload();
                            vm.CurrentView = vm.ImageViewer;
                            return;
                        }

                        if (x > 200)
                        {
                            await GetPreload();
                            break;
                        }
                    }
                }

                if (preLoadValue is null)
                {
                    await GetPreload();
                }

                vm.CurrentView = vm.ImageViewer;

                if (Index != index)
                {
                    await Preload();
                    return;
                }

                vm.SetImageModel(preLoadValue.ImageModel);
                await vm.ImageViewer.SetImage(preLoadValue.ImageModel.Image, preLoadValue.ImageModel.ImageType);
                vm.IsLoading = false;
                WindowHelper.SetSize(preLoadValue.ImageModel.PixelWidth, preLoadValue.ImageModel.PixelHeight, 0, vm);
                vm.SetTitle(preLoadValue.ImageModel, vm.ImageIterator);
                vm.GetIndex = Index + 1;
                if (SettingsHelper.Settings.WindowProperties.KeepCentered)
                {
                    WindowHelper.CenterWindowOnScreen(false);
                }

                if (vm.GalleryItems is not null)
                {
                    if (vm.GalleryItems.Count > 0 && Index < vm.GalleryItems.Count - 1)
                    {
                        vm.SelectedGalleryItemIndex = Index;
                        GalleryNavigation.CenterScrollToSelectedItem(vm);
                    }
                }

                await AddAsync(Index, preLoadValue?.ImageModel);
                await Preload();
                return;

                async Task GetPreload()
                {
                    await PreLoader.AddAsync(index, Pics)
                        .ConfigureAwait(false);
                    preLoadValue = PreLoader.Get(index, Pics);
                    if (Index != index)
                    {
                        await Preload();
                        return;
                    }

                    if (preLoadValue is null)
                    {
                        throw new ArgumentNullException(nameof(LoadNextPic),
                            nameof(preLoadValue) + " is null");
                    }
                }
            }
            catch (Exception)
            {
                // TODO display exception to user
            }
        }).ConfigureAwait(false);

        public async Task LoadPicFromString(string path, MainViewModel vm)
        {
            if (!Path.Exists(path))
            {
                // TODO load from URL if not a file
                throw new FileNotFoundException(path);
            }

            await LoadPicFromFile(new FileInfo(path), vm).ConfigureAwait(false);
        }

        public async Task LoadPicFromFile(FileInfo fileInfo, MainViewModel vm)
        {
            vm.SetLoadingTitle();
            try
            {
                ArgumentNullException.ThrowIfNull(fileInfo);

                var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
                WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, imageModel.Rotation, vm);
                await vm.ImageViewer.SetImage(imageModel.Image, imageModel.ImageType);
                vm.ImageIterator = new ImageIterator(imageModel.FileInfo, _vm);
                await AddAsync(Index, imageModel);
                await LoadPicAtIndex(Index, vm);
                vm.ImageIterator.FileAdded += (_, e) => { vm.SetTitle(); };
                vm.ImageIterator.FileRenamed += (_, e) => { vm.SetTitle(); };
                vm.ImageIterator.FileDeleted += async (_, isSameFile) =>
                {
                    if (isSameFile) //change if deleting current file
                    {
                        if (Index < 0 || Index >= Pics.Count)
                        {
                            return;
                        }
                        await LoadPicFromString(Pics[Index], vm);
                    }
                    else
                    {
                        vm.SetTitle();
                    }
                };
                if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
                {
                   _ = Task.Run(() => GalleryLoad.LoadGallery(vm, fileInfo.DirectoryName));
                }
            }
            catch (Exception e)
            {
                // TODO display exception to user
            }
        }

        private static Timer? _timer;
        private static bool _updateSource;

        internal async Task TimerPic(int index, MainViewModel vm)
        {
            if (_timer is null)
            {
                _timer = new Timer(TimeSpan.FromSeconds(SettingsHelper.Settings.UIProperties.NavSpeed))
                {
                    AutoReset = false,
                    Enabled = true
                };
            }
            else if (_timer.Enabled)
            {
                return;
            }
            _timer?.Start();
            Index = index;
            _updateSource = true; // Update it when key released
            var preLoadValue = PreLoader.Get(index, Pics);

            if (preLoadValue != null)
            {
                var showThumb = true;
                while (preLoadValue.IsLoading)
                {
                    if (showThumb)
                    {
                        await NavigationHelper.LoadingPreview(index, vm);
                        showThumb = false;
                    }

                    await Task.Delay(10);
                }
            }
            else
            {
                await NavigationHelper.LoadingPreview(index, vm);
                await PreLoader.AddAsync(index, Pics).ConfigureAwait(false);
                preLoadValue = PreLoader.Get(index, Pics);
                if (preLoadValue is null)
                {
                    return;
                }
            }
            vm.SetImageModel(preLoadValue.ImageModel);

            await vm.ImageViewer.SetImage(preLoadValue.ImageModel.Image, preLoadValue.ImageModel.ImageType);
            WindowHelper.SetSize(preLoadValue.ImageModel.PixelWidth, preLoadValue.ImageModel.PixelHeight, 0, vm);
            if (vm.GalleryItems is not null)
            {
                if (vm.GalleryItems.Count > 0 && Index < vm.GalleryItems.Count - 1)
                {
                    vm.SelectedGalleryItemIndex = Index;
                }
            }
            vm.SetTitle(preLoadValue.ImageModel, vm.ImageIterator);
            vm.GetIndex = Index + 1;

            _updateSource = false;
            await PreLoader.PreLoadAsync(index, Pics.Count, Reverse, Pics).ConfigureAwait(false);
        }

        internal static async Task TimerPicUpdate()
        {
            _timer = null;

            if (_updateSource == false)
            {
                return;
            }

            // Update picture in case it didn't load. Won't happen normally

            //var preLoadValue = PreLoader.Get(Index, Pics);
            //if (preLoadValue is null)
            //{
            //    await PreLoader.AddAsync(Index).ConfigureAwait(false);
            //    preLoadValue = PreLoader.Get(FolderIndex);
            //    if (preLoadValue is null)
            //    {
            //        var fileInfo = new FileInfo(Pics[FolderIndex]);
            //        var bitmapSource = await Image2BitmapSource.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
            //        preLoadValue = new PreLoader.PreLoadValue(bitmapSource, fileInfo, null);
            //    }
            //}

            //while (preLoadValue.BitmapSource is null)
            //{
            //    await Task.Delay(10).ConfigureAwait(false);
            //}
            //try
            //{
            //    await UpdateImage.UpdateImageValuesAsync(FolderIndex, preLoadValue).ConfigureAwait(false);
            //}
        }
    }
}
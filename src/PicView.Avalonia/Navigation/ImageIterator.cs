using PicView.Avalonia.Helpers;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
using PicView.Core.Gallery;
using Timer = System.Timers.Timer;

namespace PicView.Avalonia.Navigation;
public class ImageIterator
{
    public event FileSystemEventHandler? FileAdded;

    public event EventHandler<bool>? FileDeleted;

    public event FileSystemEventHandler? FileRenamed;

    public List<string> Pics { get; set; }

    public bool IsFileBeingRenamed { get; set; }

    public int Index{ get; set; }
    
    public FileInfo FileInfo{ get; private set; }
    public bool Reverse { get; private set; }
    public PreLoader PreLoader { get; } = new();

    private static FileSystemWatcher? _watcher;
    private static bool _running;
    private readonly MainViewModel? _vm;

    public ImageIterator(FileInfo fileInfo, MainViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);
        
        _vm = vm;
        Pics = vm.PlatformService.GetFiles(fileInfo);
        Index = Directory.Exists(fileInfo.FullName) ? 0 : Pics.IndexOf(fileInfo.FullName);
#if DEBUG
        Debug.Assert(fileInfo.DirectoryName != null, "fileInfo.DirectoryName != null");
#endif
        InitiateWatcher(fileInfo);
    }

    public void InitiateWatcher(FileInfo fileInfo)
    {
        FileInfo = fileInfo;
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
        _watcher.Renamed += async (_, e) => await OnFileRenamed(e);
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

    private async Task OnFileRenamed(RenamedEventArgs e)
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

        await PreLoader.RefreshFileInfo(index, Pics);

        _running = false;
        //FileHistoryNavigation.Rename(e.OldFullPath, e.FullPath);
        //await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var mainView = await Dispatcher.UIThread.InvokeAsync(() => desktop.MainWindow.GetControl<MainView>("MainView"));

        var galleryListBox = mainView.GalleryView.GalleryListBox;
        if (galleryListBox.Items.Count > 0 && index < galleryListBox.Items.Count)
        {
            if (galleryListBox.Items[index] is GalleryItem item)
            {
                var galleryThumbInfo = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(fileInfo);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    item.FileName.Text = galleryThumbInfo.FileName;
                    item.FileSize.Text = galleryThumbInfo.FileSize;
                    item.FileDate.Text = galleryThumbInfo.FileDate;
                    item.FileSize.Text = galleryThumbInfo.FileSize;
                });
            }
        }
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
        var index = Pics.IndexOf(e.FullPath);
        var sameFile = Index == index;
        if (!Pics.Remove(e.FullPath))
        {
            return;
        }
        Index--;

        PreLoader.Remove(Index, Pics);

        _running = false;

        //FileHistoryNavigation.Remove(e.FullPath);
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var mainView = Dispatcher.UIThread.Invoke(() => desktop.MainWindow.GetControl<MainView>("MainView"));

        var galleryListBox = mainView.GalleryView.GalleryListBox;
        if (galleryListBox.Items.Count > 0 && index < galleryListBox.Items.Count)
        {
            galleryListBox.Items.RemoveAt(index);
        }

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
        
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var mainView =
            await Dispatcher.UIThread.InvokeAsync(() => desktop.MainWindow.GetControl<MainView>("MainView"));

        var galleryListBox = mainView.GalleryView.GalleryListBox;
        if (galleryListBox.Items.Count > 0 && index < galleryListBox.Items.Count)
        {
            if (galleryListBox.Items[index] is not GalleryItem galleryItem) { return; }
            var imageModel = await ImageHelper.GetImageModelAsync(fileInfo, true, (int)_vm.GetGalleryItemHeight);
            ImageHelper.SetImage(imageModel.Image, galleryItem.GalleryImage, imageModel.ImageType);
            var galleryThumbInfo = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(fileInfo);
            galleryItem.FileLocation.Text = galleryThumbInfo.FileLocation;
            galleryItem.FileDate.Text = galleryThumbInfo.FileDate;
            galleryItem.FileSize.Text = galleryThumbInfo.FileSize;
            galleryItem.FileName.Text = galleryThumbInfo.FileName;
            if (galleryListBox.Items.Contains(galleryItem))
            {
                return;
            }

            try
            {
                galleryListBox.Items.Add(galleryItem);
                await GalleryFunctions.SortGalleryItems(Pics, _vm);
            }
            catch (Exception exception)
            {
#if DEBUG
                Console.WriteLine(exception);
#endif
            }
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
            if (preLoadValue is not null)
            {
                var showThumb = true;
                while (preLoadValue.IsLoading)
                {
                    if (showThumb)
                    {
                        await LoadingPreview(index, vm);
                        if (Index != index)
                        {
                            return;
                        }
                        showThumb = false;
                    }
                    
                    await Task.Delay(20);
                    if (Index != index)
                    {
                        return;
                    }
                }
            }
            else
            {
                await LoadingPreview(index, vm);
                var added = await PreLoader.AddAsync(index, Pics);
                if (Index != index)
                {
                    // Skip loading if user went to next value
                    return;
                }
                if (added)
                {
                    preLoadValue = PreLoader.Get(index, Pics);
                }
            }

            if (Index != index || preLoadValue is null)
            {
                return;
            }

            UpdateSource(vm, preLoadValue);
        }
        catch (Exception e)
        {
           TooltipHelper.ShowTooltipMessage(e.Message);
        }
        finally
        {
            vm.IsLoading = false;
        }
    });

    public async Task LoadPicFromString(string path, MainViewModel vm)
    {
        if (!Path.Exists(path))
        {
            // TODO load from URL if not a file
            throw new FileNotFoundException(path); // TODO: Replace with reload function
        }
        if (Directory.Exists(path))

            if (path.Equals(vm.ImageIterator.FileInfo.DirectoryName))
            {
                await vm.ImageIterator.LoadPicAtIndex(0, vm).ConfigureAwait(false);
            }
            else
            {
                await ChangeDirectoryAndLoad().ConfigureAwait(false);
            }
        else
        {
            if (Path.GetDirectoryName(path) == Path.GetDirectoryName(Pics[Index]))
            {
                await LoadPicFromFile(new FileInfo(path), vm).ConfigureAwait(false);
            }
            else
            {
                await ChangeDirectoryAndLoad().ConfigureAwait(false);
            }
        }
        return;

        async Task ChangeDirectoryAndLoad()
        {
            var fileInfo = new FileInfo(path);
            vm.ImageIterator = new ImageIterator(fileInfo, _vm);
            await vm.ImageIterator.LoadPicFromFile(new FileInfo(vm.ImageIterator.Pics[vm.ImageIterator.Index]), vm).ConfigureAwait(false);
        }
    }

    public async Task LoadPicFromFile(FileInfo fileInfo, MainViewModel vm)
    {
        vm.SetLoadingTitle();
        using var image = new MagickImage();
        image.Ping(fileInfo);
        var thumb = image.GetExifProfile()?.CreateThumbnail();
        if (thumb is not null)
        {
            var byteArray = await Task.FromResult(thumb.ToByteArray());
            var stream = new MemoryStream(byteArray);
            var writableBitmap = WriteableBitmap.Decode(stream);
            vm.ImageSource = writableBitmap;
            vm.ImageType = ImageType.Bitmap;
        }
        var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, imageModel.Rotation, vm);
        vm.ImageSource = imageModel.Image;
        vm.ImageType = imageModel.ImageType;
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

    private static Timer? _timer;

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
            if (!MainKeyboardShortcuts.IsKeyHeldDown)
            {
                _timer = null;
            }
            return;
        }
        _timer?.Start();
        Index = index;
        var preLoadValue = PreLoader.Get(index, Pics);

        if (preLoadValue != null)
        {
            if (preLoadValue.IsLoading)
            {
                vm.SetLoadingTitle();
                await Task.Delay(250);
                if (Index != index)
                {
                    return;
                }

                if (preLoadValue.IsLoading)
                {
                    await LoadingPreview(index, vm);
                }

                var x = 0;
                while (preLoadValue.IsLoading)
                {
                    await Task.Delay(200);
                    x++;
                    if (x > 10)
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            await LoadingPreview(index, vm);
            await PreLoader.AddAsync(index, Pics).ConfigureAwait(false);
            preLoadValue = PreLoader.Get(index, Pics);
            if (preLoadValue is null)
            {
                return;
            }
        }
        UpdateSource(vm, preLoadValue);
    }
    
    private void UpdateSource(MainViewModel vm, PreLoader.PreLoadValue preLoadValue)
    {
        vm.IsLoading = false;
        vm.SetImageModel(preLoadValue.ImageModel);
        vm.ImageSource = preLoadValue.ImageModel.Image;
        vm.ImageType = preLoadValue.ImageModel.ImageType;
        WindowHelper.SetSize(preLoadValue.ImageModel.PixelWidth, preLoadValue.ImageModel.PixelHeight, 0, vm);
        vm.SetTitle(preLoadValue.ImageModel, vm.ImageIterator);
        vm.GetIndex = Index + 1;
        if (SettingsHelper.Settings.WindowProperties.KeepCentered)
        {
            WindowHelper.CenterWindowOnScreen(false);
        }

        vm.SelectedGalleryItemIndex = Index;
        TooltipHelper.CloseToolTipMessage();
        if (GalleryFunctions.IsBottomGalleryOpen)
        {
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        }

        _ =  AddAsync(Index, preLoadValue.ImageModel);
        _ = Preload();
    }
    
    public async Task LoadingPreview(int index, MainViewModel vm)
    {
        if (index != Index)
        {
            return;
        }
        vm.SetLoadingTitle();
        vm.SelectedGalleryItemIndex = index;
        using var image = new MagickImage();
        image.Ping(vm.ImageIterator.Pics[index]);
        var thumb = image.GetExifProfile()?.CreateThumbnail();
        if (thumb is null)
        {
            await Set();
            return;
        }

        var byteArray = await Task.FromResult(thumb.ToByteArray());
        if (byteArray is null)
        {
            await Set();
            return;
        }
        var stream = new MemoryStream(byteArray);
        if (index != Index)
        {
            return;
        }

        vm.ImageSource = new Bitmap(stream);
        vm.ImageType = ImageType.Bitmap;
        WindowHelper.SetSize(image?.Width ?? 0, image?.Height ?? 0, 0, vm);
        return;

        async Task Set()
        {
            if (index == Index)
            {
                vm.IsLoading = true;
                await Dispatcher.UIThread.InvokeAsync(() =>  vm.ImageViewer.MainImage.Source = null);
            }
        }
    }
}

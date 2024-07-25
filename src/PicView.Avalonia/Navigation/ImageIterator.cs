using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.UI;
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
        _watcher.Created += async (_, e) => await OnFileAdded(e);
        _watcher.Deleted += async (_, e) => await OnFileDeleted(e);
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

        await PreLoader.RefreshFileInfo(oldIndex, Pics);

        _running = false;
        //FileHistoryNavigation.Rename(e.OldFullPath, e.FullPath);
        //await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);
        GalleryFunctions.RemoveGalleryItem(oldIndex, _vm);
        await GalleryFunctions.AddGalleryItem(index,fileInfo, _vm);
        await GalleryFunctions.SortGalleryItems(Pics, _vm);
        FileRenamed?.Invoke(this, e);
    }

    private async Task OnFileDeleted(FileSystemEventArgs e)
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
        
        PreLoader.Remove(index, Pics);

        if (sameFile)
        {
            if (Pics.Count <= 0)
            {
                NavigationHelper.ShowStartUpMenu(_vm);
                return;
            }
            await NavigationHelper.Iterate(next:false, _vm);
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            GalleryFunctions.RemoveGalleryItem(index, _vm);
        });
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            if (Pics.Count == 1)
            {
                _vm.GalleryMode = GalleryMode.BottomToClosed;
            }
        }

        //FileHistoryNavigation.Remove(e.FullPath);
        _running = false;

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

        var cleared = false;
        if (PreLoader.Contains(index, Pics) || PreLoader.Contains(nextIndex, Pics) || PreLoader.Contains(prevIndex, Pics))
        {
            PreLoader.Clear();
            cleared = true;
        }

        FileAdded?.Invoke(this, e);
        
        await GalleryFunctions.AddGalleryItem(index, fileInfo, _vm);
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown && Pics.Count > 1)
        {
            if (_vm.GalleryMode is GalleryMode.BottomToClosed or GalleryMode.FullToClosed)
            {
                _vm.GalleryMode = GalleryMode.ClosedToBottom;
            }
        }
        _vm.SelectedGalleryItemIndex = Index; // Fixes deselection bug

        if (cleared)
        {
            await Preload();
        }
    }

    public async Task LoadNextPic(NavigateTo navigateTo)
    {
        var index = GetIteration(Index, navigateTo);
        if (index < 0)
        {
            return;
        }

        if (!MainKeyboardShortcuts.IsKeyHeldDown)
        {
            await LoadPicAtIndex(index);
        }
        else
        {
            await TimerPic(index);
        }
    }

    public async Task LoadPicAtIndex(int index) => await Task.Run(async () =>
    {
        if (index < 0 || index >= Pics.Count)
        {
            NavigationHelper.ShowStartUpMenu(_vm);
            return;
        }
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
                        await LoadingPreview(index);
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
                await LoadingPreview(index);
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

            UpdateSource(preLoadValue);
        }
        catch (Exception e)
        {
           TooltipHelper.ShowTooltipMessage(e.Message);
        }
        finally
        {
            _vm.IsLoading = false;
        }
    });

    public async Task LoadPicFromString(string path)
    {
        if (!Path.Exists(path))
        {
            return;
            // TODO load from URL if not a file
            throw new FileNotFoundException(path); // TODO: Replace with reload function
        }
        if (Directory.Exists(path))

            if (path.Equals(_vm.ImageIterator.FileInfo.DirectoryName))
            {
                await _vm.ImageIterator.LoadPicAtIndex(0).ConfigureAwait(false);
            }
            else
            {
                await ChangeDirectoryAndLoad().ConfigureAwait(false);
            }
        else
        {
            if (Path.GetDirectoryName(path) == Path.GetDirectoryName(Pics[Index]))
            {
                await LoadPicFromFile(new FileInfo(path)).ConfigureAwait(false);
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
            _vm.ImageIterator = new ImageIterator(fileInfo, _vm);
            await _vm.ImageIterator.LoadPicFromFile(new FileInfo(_vm.ImageIterator.Pics[_vm.ImageIterator.Index])).ConfigureAwait(false);
        }
    }

    public async Task LoadPicFromFile(FileInfo fileInfo)
    {
        SetTitleHelper.SetLoadingTitle(_vm);
        using var image = new MagickImage();
        image.Ping(fileInfo);
        var thumb = image.GetExifProfile()?.CreateThumbnail();
        if (thumb is not null)
        {
            var byteArray = await Task.FromResult(thumb.ToByteArray());
            var stream = new MemoryStream(byteArray);
            var writableBitmap = WriteableBitmap.Decode(stream);
            _vm.ImageSource = writableBitmap;
            _vm.ImageType = ImageType.Bitmap;
        }
        var imageModel = await ImageHelper.GetImageModelAsync(fileInfo).ConfigureAwait(false);
        WindowHelper.SetSize(imageModel.PixelWidth, imageModel.PixelHeight, imageModel.Rotation, _vm);
        _vm.ImageSource = imageModel.Image;
        _vm.ImageType = imageModel.ImageType;
        var isSameFolder = Path.GetDirectoryName(fileInfo.FullName) == Path.GetDirectoryName(Pics[Index]);
        if (isSameFolder)
        {
            Index = Pics.IndexOf(fileInfo.FullName);
        }
        await AddAsync(Index, imageModel);
        await LoadPicAtIndex(Index);
        FileAdded += (_, e) => { SetTitleHelper.SetTitle(_vm); };
        FileRenamed += (_, e) => { SetTitleHelper.SetTitle(_vm); };
        FileDeleted += async (_, isSameFile) =>
        {
            if (isSameFile) //change if deleting current file
            {
                if (Index < 0 || Index >= Pics.Count)
                {
                    return;
                }

                await LoadPicFromString(Pics[Index]);
            }
            else
            {
                SetTitleHelper.SetTitle(_vm);
            }
        };
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            if (Pics.Count == 1)
            {
                _vm.GalleryMode = GalleryMode.BottomToClosed;
            }

            if (isSameFolder)
            {
                // Fixes not scrolling to selected item
                GalleryNavigation.CenterScrollToSelectedItem(_vm);
            }
            else
            {
                await GalleryLoad.ReloadGalleryAsync(_vm, fileInfo.DirectoryName);
            }
        }
        else
        {
            GalleryFunctions.Clear(_vm);
        }
    }

    private static Timer? _timer;

    internal async Task TimerPic(int index)
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
                SetTitleHelper.SetLoadingTitle(_vm);
                await Task.Delay(250);
                if (Index != index)
                {
                    return;
                }

                if (preLoadValue.IsLoading)
                {
                    await LoadingPreview(index);
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
            await LoadingPreview(index);
            await PreLoader.AddAsync(index, Pics).ConfigureAwait(false);
            preLoadValue = PreLoader.Get(index, Pics);
            if (preLoadValue is null)
            {
                return;
            }
        }
        UpdateSource(preLoadValue);
    }
    
    public void UpdateSource(PreLoader.PreLoadValue preLoadValue)
    {
        _vm.IsLoading = false;
        _vm.SetImageModel(preLoadValue.ImageModel);
        _vm.ImageSource = preLoadValue.ImageModel.Image;
        _vm.ImageType = preLoadValue.ImageModel.ImageType;
        WindowHelper.SetSize(preLoadValue.ImageModel.PixelWidth, preLoadValue.ImageModel.PixelHeight, preLoadValue.ImageModel.Rotation, _vm);
        SetTitleHelper.SetTitle(_vm, preLoadValue.ImageModel);
        _vm.GetIndex = Index + 1;
        if (SettingsHelper.Settings.WindowProperties.KeepCentered)
        {
            WindowHelper.CenterWindowOnScreen(false);
        }

        if (_vm.SelectedGalleryItemIndex != Index)
        {
            _vm.SelectedGalleryItemIndex = Index;
            if (GalleryFunctions.IsBottomGalleryOpen)
            {
                GalleryNavigation.CenterScrollToSelectedItem(_vm);
            }
        }
        TooltipHelper.CloseToolTipMessage();

        _ =  AddAsync(Index, preLoadValue.ImageModel);
        _ = Preload();
    }
    
    public async Task LoadingPreview(int index)
    {
        if (index != Index)
        {
            return;
        }
        SetTitleHelper.SetLoadingTitle(_vm);
        _vm.SelectedGalleryItemIndex = index;
        if (GalleryFunctions.IsBottomGalleryOpen)
        {
            GalleryNavigation.CenterScrollToSelectedItem(_vm);
        }
        using var image = new MagickImage();
        image.Ping(_vm.ImageIterator.Pics[index]);
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

        _vm.ImageSource = new Bitmap(stream);
        _vm.ImageType = ImageType.Bitmap;
        WindowHelper.SetSize(image?.Width ?? 0, image?.Height ?? 0, 0, _vm);
        return;

        async Task Set()
        {
            if (index == Index)
            {
                _vm.IsLoading = true;
                await Dispatcher.UIThread.InvokeAsync(() =>  _vm.ImageViewer.MainImage.Source = null);
            }
        }
    }
}

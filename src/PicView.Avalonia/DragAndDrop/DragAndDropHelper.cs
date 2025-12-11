using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.FileHandling;
using PicView.Core.Preloading;
using PicView.Core.ProcessHandling;
using PicView.Core.Sizing;

namespace PicView.Avalonia.DragAndDrop;

public static class DragAndDropHelper
{
    private static DragDropView? _dragDropView;
    private static PreLoadValue? _preLoadValue;

    #region Public Entry Points

    public static async Task Drop(DragEventArgs e, MainViewModel vm)
    {
        RemoveDragDropView();

        var files = e.Data.GetFiles();
        if (files == null)
        {
            await HandleDropFromUrl(e, vm);
            return;
        }

        var storageItems = files as IStorageItem[] ?? files.ToArray();
        var firstFile = storageItems.FirstOrDefault();
        if (firstFile == null)
        {
            return;
        }
        
        // Handle opening additional files in new windows if needed
        if (storageItems.Length > 1)
        {
            _ = Task.Run(() => HandleAdditionalFiles(storageItems.Skip(1)));
        }

        var path = firstFile.Path.LocalPath;

        if (e.Data.Contains("text/x-moz-url"))
        {
            await HandleDropFromUrl(e, vm);
            await EnsureImageViewerDisplayed(vm);
        }
        else if (path.IsSupported())
        {
            await EnsureImageViewerDisplayed(vm);
            await LoadSupportedFile(path, vm);
        }
        else if (Directory.Exists(path))
        {
            await EnsureImageViewerDisplayed(vm);
            await NavigationManager.LoadPicFromDirectoryAsync(path, vm).ConfigureAwait(false);
        }
        else if (path.IsArchive())
        {
            await EnsureImageViewerDisplayed(vm);
            await NavigationManager.LoadPicFromArchiveAsync(path, vm).ConfigureAwait(false);
        }
    }

    public static async Task DragEnter(DragEventArgs e, MainViewModel vm, Control control)
    {
        var files = e.Data.GetFiles();
        if (files != null)
        {
            await HandleDragEnterWithFiles(files, vm, control);
        }
        else
        {
            // Try handling as URL
            var handled = await HandleDragEnterFromUrl(e.Data.Get("text/x-moz-url"), vm);
            if (!handled)
            {
                RemoveDragDropView();
            }
        }
    }

    public static void DragLeave(DragEventArgs e, Control control)
    {
        if (control.IsPointerOver)
        {
            return;
        }

        RemoveDragDropView();
        _preLoadValue = null;
    }

    public static void RemoveDragDropView()
    {
        UIHelper.GetMainView.MainGrid.Children.Remove(_dragDropView);
        _dragDropView = null;
    }

    #endregion

    #region Private Helpers

    private static void HandleAdditionalFiles(IEnumerable<IStorageItem> additionalFiles)
    {
        if (Settings.UIProperties.OpenInSameWindow)
        {
            return;
        }

        foreach (var file in additionalFiles)
        {
            var filepath = file.Path.LocalPath;
            if (filepath.IsSupported())
            {
                ProcessHelper.StartNewProcess(filepath);
            }
        }
    }

    private static async Task HandleDropFromUrl(DragEventArgs e, MainViewModel vm)
    {
        var urlObject = e.Data.Get("text/x-moz-url");
        if (urlObject is byte[] bytes)
        {
            var dataStr = Encoding.Unicode.GetString(bytes);
            var url = dataStr.Split((char)10).FirstOrDefault();
            if (url != null)
            {
                await LoadFromUrl(url, vm);
            }
        }
    }

    private static async Task LoadFromUrl(string url, MainViewModel vm)
    {
        // Remove preview first and show loading
        RemoveDragDropView();
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;
        if (vm.ImageViewer?.MainImage != null)
        {
            vm.ImageViewer.MainImage.Source = null;
        }

        if (url.StartsWith("file://"))
        {
            var file = url[7..];
            if (file.StartsWith('/'))
            {
                file = file[1..];
            }

            if (file.IsArchive())
            {
                await NavigationManager.LoadPicFromArchiveAsync(file, vm).ConfigureAwait(false);
            }
            else
            {
                await NavigationManager.LoadPicFromFile(file, vm).ConfigureAwait(false);
            }
        }
        else
        {
            await NavigationManager.LoadPicFromUrlAsync(url, vm).ConfigureAwait(false);
        }
    }

    private static async Task HandleDragEnterWithFiles(IEnumerable<IStorageItem> files, MainViewModel vm,
        Control control)
    {
        var fileArray = files as IStorageItem[] ?? files.ToArray();
        if (fileArray.Length == 0)
        {
            return;
        }

        await EnsureDragDropViewCreated(vm, control);

        var firstFile = fileArray[0];
        var path = firstFile.Path.LocalPath;

        if (Directory.Exists(path))
        {
            await ShowDirectoryIcon(control);
        }
        else if (path.IsArchive())
        {
            await ShowArchiveIcon(control);
        }
        else if (path.IsSupported())
        {
            await ShowFilePreview(new FileInfo(path));
        }
    }

    private static async Task ShowDirectoryIcon(Control control)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!control.IsPointerOver)
            {
                _dragDropView?.AddDirectoryIcon();
            }
        });
    }

    private static async Task ShowArchiveIcon(Control control)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!control.IsPointerOver)
            {
                _dragDropView?.AddZipIcon();
            }
        });
    }

    private static async Task ShowFilePreview(FileInfo fileInfo)
    {
        var ext = fileInfo.Extension;
        if (ext.Equals(".svg", StringComparison.InvariantCultureIgnoreCase) ||
            ext.Equals(".svgz", StringComparison.InvariantCultureIgnoreCase))
        {
            await Dispatcher.UIThread.InvokeAsync(() => _dragDropView?.UpdateSvgThumbnail(fileInfo.FullName));
            return;
        }

        await LoadAndShowThumbnail(fileInfo);
    }

    private static async Task LoadAndShowThumbnail(FileInfo fileInfo)
    {
        Bitmap? thumb;
        // Try to get preloaded image first
        var preload = NavigationManager.TryGetPreLoadValue(fileInfo);
        if (preload?.ImageModel?.Image is Bitmap bmp)
        {
            thumb = bmp;
            
            await UpdateThumbnailUI(thumb);
        }
        else
        {
            // Generate thumbnail
            thumb = await GetThumbnails.GetThumbAsync(fileInfo, SizeDefaults.WindowMinSize - 30)
                .ConfigureAwait(false);
            await UpdateThumbnailUI(thumb);
            
            // Load full image in background
            await PreloadFullImage(fileInfo, preload, thumb);
        }
    }

    private static async Task PreloadFullImage(FileInfo fileInfo, PreLoadValue? preload, Bitmap? thumb)
    {
        await Task.Run(async () =>
        {
            var sameDirectory = fileInfo.DirectoryName ==
                                NavigationManager.ImageIterator.InitialFileInfo.DirectoryName;

            if (sameDirectory)
            {
                if (preload is null)
                {
                    _preLoadValue = await NavigationManager.GetPreLoadValueAsync(fileInfo);
                    thumb = _preLoadValue.ImageModel.Image as Bitmap;
                    if (thumb is not null)
                    {
                        await UpdateThumbnailUI(thumb);
                    }
                }
                else
                {
                    _preLoadValue = preload;
                    await UpdateThumbnailUI(thumb);
                }
            }
            else if (preload is null)
            {
                var model = await GetImageModel.GetImageModelAsync(fileInfo);
                await UpdateThumbnailUI(thumb);
                _preLoadValue = new PreLoadValue(model);
            }
            else
            {
                _preLoadValue = preload;
                await UpdateThumbnailUI(thumb);
            }
        });
    }

    private static async Task UpdateThumbnailUI(Bitmap? thumb) =>
        await Dispatcher.UIThread.InvokeAsync(() => _dragDropView?.UpdateThumbnail(thumb));

    private static async Task<bool> HandleDragEnterFromUrl(object? urlObject, MainViewModel vm)
    {
        if (urlObject is null)
        {
            _dragDropView?.RemoveThumbnail();
            return false;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _dragDropView ??= new DragDropView { DataContext = vm };
            if (!_dragDropView.IsLinkChainVisible)
            {
                _dragDropView.AddLinkChain();
            }

            if (!UIHelper.GetMainView.MainGrid.Children.Contains(_dragDropView))
            {
                UIHelper.GetMainView.MainGrid.Children.Add(_dragDropView);
            }
        });

        return true;
    }

    private static async Task EnsureDragDropViewCreated(MainViewModel vm, Control control)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_dragDropView == null)
            {
                _dragDropView = new DragDropView { DataContext = vm };
                if (!control.IsPointerOver)
                {
                    UIHelper.GetMainView.MainGrid.Children.Add(_dragDropView);
                }
            }
            else
            {
                _dragDropView.RemoveThumbnail();
            }
        });
    }

    private static async Task LoadSupportedFile(string path, MainViewModel vm)
    {
        if (_preLoadValue is not null && NavigationManager.CanNavigate(vm))
        {
            if (Path.GetDirectoryName(path) is { } currentDirectory && NavigationManager.GetInitialFileInfo?.DirectoryName is {} preloadDirectory
                && currentDirectory == preloadDirectory)
            {
                // Check for edge case error
                var isAddedToPreloader = NavigationManager.AddToPreloader(new FileInfo(path), _preLoadValue.ImageModel);
                if (isAddedToPreloader)
                {
                    NavigationManager.ImageIterator.Resynchronize();
                    await NavigationManager.LoadPicFromFile(path, vm, _preLoadValue.ImageModel.FileInfo);
                }
                else
                {
                    await NavigationManager.LoadPicFromStringAsync(path, vm).ConfigureAwait(false);
                }
            }
            else
            {
                await NavigationManager.LoadPicFromStringAsync(path, vm).ConfigureAwait(false);
            }
        }
        else
        {
            await NavigationManager.LoadPicFromStringAsync(path, vm).ConfigureAwait(false);
        }
    }

    private static async Task EnsureImageViewerDisplayed(MainViewModel vm)
    {
        if (vm.MainWindow.CurrentView.CurrentValue != vm.ImageViewer)
        {
            await Dispatcher.UIThread.InvokeAsync(() => vm.MainWindow.CurrentView.Value = vm.ImageViewer);
        }
    }

    #endregion
}
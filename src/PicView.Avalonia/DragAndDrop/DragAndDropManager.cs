using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Views.UC;
using PicView.Core.FileHandling;
using PicView.Core.ProcessHandling;
using PicView.Core.Sizing;
using PicView.Core.Svg;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.DragAndDrop;

public static class DragAndDropManager
{
    private static DragDropView? _dragDropView;

    #region Public Entry Points

    public static async ValueTask Drop(DragEventArgs e, MainWindow mainWindow)
    {
        RemoveDragDropView(mainWindow);

        var files = e.DataTransfer.TryGetFiles();
        if (files is null)  
        {
            await HandleDropFromUrl(e, mainWindow).ConfigureAwait(false);
            return;
        }

        if (files.Length < 1)
        {
            return;
        }

        var firstFile = files[0];
        if (firstFile is null)
        {
            return;
        }
        
        // Handle opening additional files in new windows if needed
        if (files.Length > 1)
        {
            _ = Task.Run(() => HandleAdditionalFiles(files.Skip(1)));
        }
        
        var path = firstFile.Path.LocalPath;
        var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
        if (path.IsSupported())
        {
            await LoadSupportedFile(mainWindow, path, core).ConfigureAwait(false);
        }
        else if (Directory.Exists(path) || path.IsArchive())
        {
            await SourceOpener.OpenAsync(mainWindow, core, path).ConfigureAwait(false);
        }
    }

    public static async ValueTask DragEnter(DragEventArgs e, MainWindow mainWindow)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is not null)
        {
            await HandleDragEnterWithFiles(files, mainWindow).ConfigureAwait(true);
        }
        else
        {
            // // Try handling as URL
            var value = e.DataTransfer.Items[0];

            var handled = HandleDragEnterFromUrl(value, mainWindow);
            if (!handled)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    RemoveDragDropView(mainWindow);
                });
            }
        }
    }

    public static void DragLeave(MainWindow mainWindow)
    {
        if (mainWindow.IsPointerOver)
        {
            return;
        }

        RemoveDragDropView(mainWindow);
    }

    public static void RemoveDragDropView(MainWindow mainWindow)
    {
        if (_dragDropView is null)
        {
            return;
        }

        mainWindow.UIHelper.GetMainView?.MainPanel.Children.Remove(_dragDropView);
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

    private static async Task HandleDropFromUrl(DragEventArgs e, MainWindow mainWindow)
    {
        var item = e.DataTransfer.Items[0].TryGetRaw(DataFormat.CreateBytesPlatformFormat("text/x-moz-url"));
        if (item is not byte[] bytes)
        {
            return;
        }

        var dataStr = Encoding.Unicode.GetString(bytes);
        var url = dataStr.Split((char)10).FirstOrDefault();
        if (url != null)
        {
            await LoadFromUrl(url, mainWindow).ConfigureAwait(false);
        }
    }

    private static async Task LoadFromUrl(string url, MainWindow mainWindow)
    {
        // Remove preview first and show loading
        RemoveDragDropView(mainWindow);

        var source = url;
        if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            source = url[7..];
            if (source.StartsWith('/', StringComparison.Ordinal))
            {
                source = source[1..];
            }
        }

        await SourceOpener.OpenAsync(mainWindow, mainWindow.ViewModel.Core, source).ConfigureAwait(false);
    }

    private static async Task HandleDragEnterWithFiles(IEnumerable<IStorageItem> files, MainWindow mainWindow)
    {
        var fileArray = files as IStorageItem[] ?? files.ToArray();
        if (fileArray.Length is 0)
        {
            return;
        }

        EnsureDragDropViewCreated(mainWindow);

        var firstFile = fileArray[0];
        var path = firstFile.Path.LocalPath;

        if (Directory.Exists(path))
        {
            ShowDirectoryIcon(mainWindow);
        }
        else if (path.IsArchive())
        {
            ShowArchiveIcon(mainWindow);
        }
        else if (path.IsSupported())
        {
            await ShowFilePreview(new FileInfo(path), mainWindow).ConfigureAwait(false);
        }
    }

    private static void ShowDirectoryIcon(Control control)
    {
        Dispatcher.CurrentDispatcher.Post(() =>
        {
            if (!control.IsPointerOver)
            {
                _dragDropView?.AddDirectoryIcon();
            }
        });
    }

    private static void ShowArchiveIcon(Control control)
    {
        Dispatcher.CurrentDispatcher.Invoke(() =>
        {
            if (!control.IsPointerOver)
            {
                _dragDropView?.AddZipIcon();
            }
        });
    }

    private static async Task ShowFilePreview(FileInfo fileInfo, MainWindow mainWindow)
    {
        var ext = fileInfo.Extension;
        if (ext.Equals(".svg", StringComparison.InvariantCultureIgnoreCase) ||
            ext.Equals(".svgz", StringComparison.InvariantCultureIgnoreCase))
        {
            var svg = await Task.Run(() => TempFileSingleImage.LoadSvgAsync(fileInfo).AsTask()).ConfigureAwait(false);
            if (svg is SvgDrawing drawing)
            {
                await Dispatcher.UIThread.InvokeAsync(() => _dragDropView?.UpdateSvgThumbnail(drawing, mainWindow));
            }
            else if (svg is Bitmap bitmap)
            {
                await Dispatcher.UIThread.InvokeAsync(() => _dragDropView?.UpdateThumbnail(bitmap, mainWindow));
            }

            return;
        }

        await LoadAndShowThumbnail(fileInfo, mainWindow).ConfigureAwait(false);
    }

    private static async Task LoadAndShowThumbnail(FileInfo fileInfo, MainWindow mainWindow)
    {
        var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
        Bitmap? thumb;
        // Try to get preloaded image first
        if (core.SharedCache.TryGetReady(fileInfo, out var ready) && ready.Image is Bitmap bmp)
        {
            thumb = bmp;
            await Dispatcher.UIThread.InvokeAsync(() => _dragDropView?.UpdateThumbnail(thumb, mainWindow));
        }
        else
        {
            // A thumbnail first, kept by no one since the drag is brief
            thumb = core.SharedThumbnails is { } thumbnails
                ? await thumbnails.GetAsync(fileInfo, (uint)(SizeDefaults.WindowMinSize - 30), owner: null)
                    .ConfigureAwait(false) as Bitmap
                : null;
            await Dispatcher.UIThread.InvokeAsync(() => _dragDropView?.UpdateThumbnail(thumb, mainWindow));
            
            // Load full image in background
            await Task.Run(async () =>
            {
                var model = await GetImageModel.GetImageModelAsync(fileInfo).ConfigureAwait(false);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (model is null || _dragDropView is null)
                    {
                        return;
                    }
                    _dragDropView.UpdateThumbnail(model.Image as Bitmap, mainWindow);
                });
            }).ConfigureAwait(false);
        }
    }
        

    private static bool HandleDragEnterFromUrl(object? urlObject, MainWindow mainWindow)
    {
        if (urlObject is null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _dragDropView?.RemoveThumbnail();
            });

            return false;
        }

        Dispatcher.UIThread.Post(() =>
        {
            _dragDropView ??= new DragDropView(mainWindow);
            if (!_dragDropView.IsLinkChainVisible)
            {
                _dragDropView.AddLinkChain();
            }

            if (mainWindow.UIHelper.GetMainView is not null && !mainWindow.UIHelper.GetMainView.MainPanel.Children.Contains(_dragDropView))
            {
                mainWindow.UIHelper.GetMainView.MainPanel.Children.Add(_dragDropView);
            }
        });

        return true;
    }

    private static void EnsureDragDropViewCreated(MainWindow mainWindow)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_dragDropView == null)
            {
                _dragDropView = new DragDropView(mainWindow);
                if (!mainWindow.IsPointerOver)
                {
                    mainWindow.UIHelper.GetMainView.MainPanel.Children.Add(_dragDropView);
                }
            }
            else
            {
                _dragDropView.RemoveThumbnail();
            }
        });
    }

    private static async ValueTask LoadSupportedFile(MainWindow mainWindow, string path, CoreViewModel core)
    {
        // A file dropped from a browser may sit in the temp folder and be deleted right after, which the file
        // watcher would follow, and browsing it would show the folder's unrelated files. So it is shown as a
        // Single Image held in memory. A file that cannot be loaded that way is opened as usual.
        if (TempFileManager.IsInTempFolder(path) && mainWindow.DataContext is MainWindowViewModel vm)
        {
            var fileInfo = new FileInfo(path);
            // As a bitmap also for an SVG, so that saving, copying and printing the dropped image keep working
            if (await TempFileSingleImage.ShowAsync(vm, fileInfo, fileInfo.Name, svgAsDrawing: false)
                    .ConfigureAwait(false))
            {
                return;
            }
        }

        await SourceOpener.OpenAsync(mainWindow, core, path).ConfigureAwait(false);
    }

    #endregion
}
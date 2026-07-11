using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.Views.UC;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;
using PicView.Core.Preloading;
using PicView.Core.ProcessHandling;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Core.FileSorting;

namespace PicView.Avalonia.DragAndDrop;

public static class DragAndDropManager
{
    private static DragDropView? _dragDropView;
    private static PreLoadValue? _preLoadValue;

    #region Public Entry Points

    public static async ValueTask Drop(DragEventArgs e, TabOverviewViewModel tabOverview, MainWindow mainWindow)
    {
        RemoveDragDropView(mainWindow);

        var files = e.DataTransfer.TryGetFiles();
        if (files == null)  
        {
            await HandleDropFromUrl(e, tabOverview, mainWindow);
            return;
        }

        if (files.Length < 1)
        {
            return;
        }

        var firstFile = files[0];
        if (firstFile == null)
        {
            return;
        }
        
        // Handle opening additional files in new windows if needed
        if (files.Length > 1)
        {
            _ = Task.Run(() => HandleAdditionalFiles(files.Skip(1)));
        }
        
        SwitchToImageViewerIfNecessary();
        var path = firstFile.Path.LocalPath;

        if (path.IsSupported())
        {
            await LoadSupportedFile(mainWindow, path, tabOverview);
        }
        else if (Directory.Exists(path))
        {
            await tabOverview.LoadFromDirectoryAsync(path);
        }
        else if (path.IsArchive())
        {
            await tabOverview.LoadFromStringAsync(path);
        }
        else
        {
            SwitchStartUpMenuIfNecessary();
        }
    }

    public static async ValueTask DragEnter(DragEventArgs e, MainWindow mainWindow)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files != null)
        {
            await HandleDragEnterWithFiles(files, mainWindow);
        }
        else
        {
            // // Try handling as URL
            var value = e.DataTransfer.Items[0];

            var handled = HandleDragEnterFromUrl(value, mainWindow);
            if (!handled)
            {
                RemoveDragDropView(mainWindow);
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
        _preLoadValue = null;
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
    
    private static void SwitchToImageViewerIfNecessary()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (Application.Current.DataContext is not CoreViewModel core)
            {
                return;
            }

            var currentView = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.CurrentView;
            if (currentView.CurrentValue is StartUpMenu)
            {
                currentView.Value = new ImageViewer();
            }
        });
    }
    
    private static void SwitchStartUpMenuIfNecessary()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (Application.Current.DataContext is not CoreViewModel core)
            {
                return;
            }
            
            var tab = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue;
            if (tab.Image.CurrentValue is not null)
            {
                // Image is being viewed, return
                return;
            }
            var currentView = tab.CurrentView;
            if (currentView.CurrentValue is ImageViewer)
            {
                currentView.Value = new StartUpMenu();
            }
        });
    }

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

    private static async Task HandleDropFromUrl(DragEventArgs e, TabOverviewViewModel tabOverview, MainWindow mainWindow)
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
            await LoadFromUrl(url, tabOverview, mainWindow);
        }
    }

    private static async Task LoadFromUrl(string url, TabOverviewViewModel tabOverview, MainWindow mainWindow)
    {
        // Remove preview first and show loading
        RemoveDragDropView(mainWindow);
        
        var tab = tabOverview.ActiveTab.Value;
        
        if (url.StartsWith("file://"))
        {
            var file = url[7..];
            if (file.StartsWith('/'))
            {
                file = file[1..];
            }
            
            if (tab.CurrentView.CurrentValue is not ImageViewer)
            {
                tab.CurrentView.Value = new ImageViewer();
            }

            await tabOverview.LoadFromFileAsync(file);
        }
        else
        {
            if (tab.CurrentView.CurrentValue is not ImageViewer)
            {
                tab.CurrentView.Value = new ImageViewer();
            }
            await tabOverview.LoadFromUrlAsync(url);
        }
    }

    private static async Task HandleDragEnterWithFiles(IEnumerable<IStorageItem> files, MainWindow mainWindow)
    {
        var fileArray = files as IStorageItem[] ?? files.ToArray();
        if (fileArray.Length == 0)
        {
            return;
        }

        await EnsureDragDropViewCreated(mainWindow);

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
            await ShowFilePreview(new FileInfo(path), mainWindow);
        }
    }

    private static void ShowDirectoryIcon(Control control)
    {
        Dispatcher.CurrentDispatcher.InvokeAsync(() =>
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
            Dispatcher.CurrentDispatcher.Invoke(() => _dragDropView?.UpdateSvgThumbnail(fileInfo.FullName, mainWindow));
            return;
        }

        await LoadAndShowThumbnail(fileInfo, mainWindow);
    }

    private static async Task LoadAndShowThumbnail(FileInfo fileInfo, MainWindow mainWindow)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        Bitmap? thumb;
        // Try to get preloaded image first
        var preload = core.SharedCache.TryGet(fileInfo, out var preLoadValue);
        if (preload && preLoadValue?.ImageModel?.Image is Bitmap bmp)
        {
            thumb = bmp;
            Dispatcher.UIThread.Invoke(() => _dragDropView.UpdateThumbnail(thumb, mainWindow));
        }
        else
        {
            // Generate thumbnail
            thumb = await GetThumbnails.GetThumbAsync(fileInfo, SizeDefaults.WindowMinSize - 30)
                .ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => _dragDropView.UpdateThumbnail(thumb, mainWindow));
            
            // Load full image in background
            await Task.Run(async () =>
            {
                var model = await GetImageModel.GetImageModelAsync(fileInfo);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (model is null || _dragDropView is null)
                    {
                        return;
                    }
                    _dragDropView.UpdateThumbnail(model.Image as Bitmap, mainWindow);
                });
                _preLoadValue = new PreLoadValue(model);
            }).ConfigureAwait(false);
        }
    }
        

    private static bool HandleDragEnterFromUrl(object? urlObject, MainWindow mainWindow)
    {
        if (urlObject is null)
        {
            _dragDropView?.RemoveThumbnail();
            return false;
        }

        Dispatcher.CurrentDispatcher.Invoke(() =>
        {
            _dragDropView ??= new DragDropView(mainWindow);
            if (!_dragDropView.IsLinkChainVisible)
            {
                _dragDropView.AddLinkChain();
            }

            if (mainWindow.UIHelper.GetMainView != null && !mainWindow.UIHelper.GetMainView.MainPanel.Children.Contains(_dragDropView))
            {
                mainWindow.UIHelper.GetMainView.MainPanel.Children.Add(_dragDropView);
            }
        });

        return true;
    }

    private static async Task EnsureDragDropViewCreated(MainWindow mainWindow)
    {
        await Dispatcher.CurrentDispatcher.InvokeAsync(() =>
        {
            if (_dragDropView == null)
            {
                _dragDropView = new DragDropView(mainWindow);
                if (!mainWindow.IsPointerOver && mainWindow.UIHelper.GetMainView != null)
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

    private static async ValueTask LoadSupportedFile(MainWindow mainWindow, string path, TabOverviewViewModel tabOverview)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        var tab = tabOverview.ActiveTab.CurrentValue;
        if (!tab.IsInitialized)
        {
            await QuickLoad.QuickLoadAsync(mainWindow, core, path, continueFromLeftOff: false).ConfigureAwait(false);
            return;
        }
        var droppedFileInfo = new FileInfo(path);
        
        var droppedDir = droppedFileInfo.DirectoryName ?? string.Empty;
        var currentDir = tab.ImageIterator?.CurrentDirectory ?? string.Empty;

        IReadOnlyList<FileInfo> files;
        bool isSameDir;
        if (string.Equals(droppedDir, currentDir, StringComparison.OrdinalIgnoreCase))
        {
            files = tab.ImageIterator?.Files ?? [];
            isSameDir = true;
        }
        else
        {
            files = FileListRetriever.RetrieveFiles(droppedFileInfo, core.PlatformService.CompareStrings);
            isSameDir = false;
        }

        var index = files.FindIndex(x => x.FullName.AsSpan().Equals(droppedFileInfo.FullName.AsSpan(), StringComparison.OrdinalIgnoreCase));

        if (_preLoadValue is not null)
        {
            core.SharedCache.Add(tab.Id, index, _preLoadValue, files.Count, false);
        }
        
        if (isSameDir)
        {
            await tabOverview.LoadFromIndexAsync(index, tab).ConfigureAwait(false);
        }
        else
        {
            await tabOverview.LoadFromFileAsync(path).ConfigureAwait(false);
        }
    }

    #endregion
}
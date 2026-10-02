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
using PicView.Avalonia.StartUp;
using PicView.Avalonia.Views.UC;
using PicView.Core.FileHandling;
using PicView.Core.Preloading;
using PicView.Core.ProcessHandling;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Core.Models;

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
            await HandleDropFromUrl(e, tabOverview, mainWindow).ConfigureAwait(false);
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
        var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
        if (path.IsSupported())
        {
            await LoadSupportedFile(mainWindow, path, tabOverview, core).ConfigureAwait(false);
        }
        else if (Directory.Exists(path))
        {
            if (tabOverview.ActiveTab.CurrentValue.IsInitialized)
            {
                await tabOverview.LoadFromDirectoryAsync(path).ConfigureAwait(false);
            }
            else
            {
                await QuickLoad.QuickLoadAsync(mainWindow, core, path, continueFromLeftOff: false).ConfigureAwait(false);
            }
        }
        else if (path.IsArchive())
        {
            if (tabOverview.ActiveTab.CurrentValue.IsInitialized)
            {
                var vm = core.MainWindows.ActiveWindow.CurrentValue;
                vm.IsLoadingIndicatorShown.Value = true;
                try
                {
                    await tabOverview.LoadFromArchiveAsync(path).ConfigureAwait(false);
                }
                finally
                {
                    vm.IsLoadingIndicatorShown.Value = false;
                }
            }
            else
            {
                await QuickLoad.QuickLoadAsync(mainWindow, core, path, continueFromLeftOff: false).ConfigureAwait(false);
            }
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
        Dispatcher.UIThread.Post(() =>
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
            await LoadFromUrl(url, tabOverview, mainWindow).ConfigureAwait(false);
        }
    }

    private static async Task LoadFromUrl(string url, TabOverviewViewModel tabOverview, MainWindow mainWindow)
    {
        // Remove preview first and show loading
        RemoveDragDropView(mainWindow);
        
        var tab = tabOverview.ActiveTab.Value;
        
        if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            var file = url[7..];
            if (file.StartsWith('/', StringComparison.Ordinal))
            {
                file = file[1..];
            }
            
            if (tab.CurrentView.CurrentValue is not ImageViewer)
            {
                tab.CurrentView.Value = new ImageViewer();
            }

            var vm = mainWindow.DataContext as MainWindowViewModel;
            vm.IsLoadingIndicatorShown.Value = true;
            try
            {
                await tabOverview.LoadFromFileAsync(file).ConfigureAwait(false);
            }
            finally
            {
                vm.IsLoadingIndicatorShown.Value = false;
            }
        }
        else
        {
            if (tab.CurrentView.CurrentValue is not ImageViewer)
            {
                tab.CurrentView.Value = new ImageViewer();
            }
            await tabOverview.LoadFromUrlAsync(url).ConfigureAwait(false);
        }
    }

    private static async Task HandleDragEnterWithFiles(IEnumerable<IStorageItem> files, MainWindow mainWindow)
    {
        var fileArray = files as IStorageItem[] ?? files.ToArray();
        if (fileArray.Length == 0)
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
            await Dispatcher.UIThread.InvokeAsync(() => _dragDropView?.UpdateSvgThumbnail(fileInfo.FullName, mainWindow));
            return;
        }

        await LoadAndShowThumbnail(fileInfo, mainWindow).ConfigureAwait(false);
    }

    private static async Task LoadAndShowThumbnail(FileInfo fileInfo, MainWindow mainWindow)
    {
        var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
        Bitmap? thumb;
        // Try to get preloaded image first
        var preload = core.SharedCache.TryGet(fileInfo, out var preLoadValue);
        if (preload && preLoadValue?.ImageModel?.Image is Bitmap bmp)
        {
            thumb = bmp;
            await Dispatcher.UIThread.InvokeAsync(() => _dragDropView?.UpdateThumbnail(thumb, mainWindow));
        }
        else
        {
            // Generate thumbnail
            thumb = await GetThumbnails.GetThumbAsync(fileInfo, SizeDefaults.WindowMinSize - 30)
                .ConfigureAwait(false);
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
                _preLoadValue = new PreLoadValue(model);
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

            if (mainWindow.UIHelper.GetMainView != null && !mainWindow.UIHelper.GetMainView.MainPanel.Children.Contains(_dragDropView))
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

    private static async ValueTask LoadSupportedFile(MainWindow mainWindow, string path, TabOverviewViewModel tabOverview,
        CoreViewModel core)
    {
        var tab = tabOverview.ActiveTab.CurrentValue;
        if (OperatingSystem.IsWindows())
        {
            // We need to determine if the path is a temporary file, since it might be deleted shortly after
            // Normalize paths by removing trailing slashes
            var tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var dirName = Path.GetDirectoryName(path)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Use OrdinalIgnoreCase for Windows paths
            if (dirName is not null && dirName.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase))
            {
                var fileInfo = new FileInfo(path);
                var image = await GetImage.GetNonStandardBitmapAsync(fileInfo).ConfigureAwait(false);
                UpdateImage.SetSingleImage(mainWindow.DataContext as MainWindowViewModel, mainWindow, image, SingleImageType.TempFile, fileInfo.Name);
                return;
            }
        }
        if (!tab.IsInitialized)
        {
            await QuickLoad.QuickLoadAsync(mainWindow, core, path, continueFromLeftOff: false).ConfigureAwait(false);
            return;
        }
        await tabOverview.LoadFromFileAsync(path).ConfigureAwait(false);
    }

    #endregion
}
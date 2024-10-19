using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.ProcessHandling;

namespace PicView.Avalonia.DragAndDrop;

public static class DragAndDropHelper
{
    private static DragDropView? _dragDropView;

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
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? firstFile.Path.AbsolutePath
            : firstFile.Path.LocalPath;
        if (e.Data.Contains("text/x-moz-url"))
        {
            await HandleDropFromUrl(e, vm);
            if (vm.CurrentView != vm.ImageViewer)
            {
                await Dispatcher.UIThread.InvokeAsync(() => vm.CurrentView = vm.ImageViewer);
            }
        }
        else if (path.IsSupported())
        {
            await NavigationHelper.LoadPicFromStringAsync(path, vm).ConfigureAwait(false);
            if (vm.CurrentView != vm.ImageViewer)
            {
                await Dispatcher.UIThread.InvokeAsync(() => vm.CurrentView = vm.ImageViewer);
            }
        }
        else if (Directory.Exists(path))
        {
            await NavigationHelper.LoadPicFromDirectoryAsync(path, vm).ConfigureAwait(false);
            if (vm.CurrentView != vm.ImageViewer)
            {
                await Dispatcher.UIThread.InvokeAsync(() => vm.CurrentView = vm.ImageViewer);
            }
        }
        else if (path.IsArchive())
        {
            await NavigationHelper.LoadPicFromArchiveAsync(path, vm).ConfigureAwait(false);
            if (vm.CurrentView != vm.ImageViewer)
            {
                await Dispatcher.UIThread.InvokeAsync(() => vm.CurrentView = vm.ImageViewer);
            }
        }

        if (!SettingsHelper.Settings.UIProperties.OpenInSameWindow)
        {
            foreach (var file in storageItems.Skip(1))
            {
                var filepath = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? file.Path.AbsolutePath
                    : file.Path.LocalPath;
                if (filepath.IsSupported())
                {
                    ProcessHelper.StartNewProcess(filepath);
                }
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
                await NavigationHelper.LoadPicFromUrlAsync(url, vm).ConfigureAwait(false);
            }
        }
    }

    public static async Task DragEnter(DragEventArgs e, MainViewModel vm, Control control)
    {
        var files = e.Data.GetFiles();
        if (files == null)
        {
            var handledFromUrl = await HandleDragEnterFromUrl(e, vm);
            if (!handledFromUrl)
            {
                RemoveDragDropView();
            }
        }

        await HandleDragEnter(files, e, vm, control);
    }

    private static async Task HandleDragEnter(IEnumerable<IStorageItem> files, DragEventArgs e, MainViewModel vm, Control control)
    {
        var fileArray = files as IStorageItem[] ?? files.ToArray();
        if (fileArray is null || fileArray.Length < 1)
        {
            RemoveDragDropView();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_dragDropView == null)
            {
                _dragDropView = new DragDropView
                {
                    DataContext = vm
                };
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
        var firstFile = fileArray[0];
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? firstFile.Path.AbsolutePath
            : firstFile.Path.LocalPath;
        if (Directory.Exists(path))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!control.IsPointerOver)
                {
                    _dragDropView.AddDirectoryIcon();
                }
            });
        }
        else
        {
            if (path.IsArchive())
            {
                if (!control.IsPointerOver)
                {
                    _dragDropView.AddZipIcon();
                }
            }
            else if (path.IsSupported())
            {
                var ext = Path.GetExtension(path);
                if (ext.Equals(".svg", StringComparison.InvariantCultureIgnoreCase) || ext.Equals(".svgz", StringComparison.InvariantCultureIgnoreCase))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _dragDropView?.UpdateSvgThumbnail(path);
                    });
                }
                else
                {
                    var thumb = await GetThumbnails.GetThumbAsync(path, SizeDefaults.WindowMinSize - 30)
                        .ConfigureAwait(false);

                    await Dispatcher.UIThread.InvokeAsync(() => { _dragDropView?.UpdateThumbnail(thumb); });
                }
            }
            else
            {
                var handledFromUrl = await HandleDragEnterFromUrl(e, vm);
                if (!handledFromUrl)
                {
                    RemoveDragDropView();
                }
            }
        }
    }
    
    private static async Task<bool> HandleDragEnterFromUrl(object? urlObject, MainViewModel vm)
    {
        if (urlObject is null)
        {
            return false;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_dragDropView == null)
            {
                _dragDropView = new DragDropView { DataContext = vm };
                _dragDropView.AddLinkChain();
                UIHelper.GetMainView.MainGrid.Children.Add(_dragDropView);
            }
            else
            {
                _dragDropView.RemoveThumbnail();
            }
        });
        return true;
    }

    private static async Task<bool> HandleDragEnterFromUrl(DragEventArgs e, MainViewModel vm)
    {
        var urlObject = e.Data.Get("text/x-moz-url");
        return await HandleDragEnterFromUrl(urlObject, vm);
    }

    public static void DragLeave(DragEventArgs e, Control control)
    {
        if (!control.IsPointerOver)
        {
            RemoveDragDropView();
        }
    }

    public static void RemoveDragDropView()
    {
        UIHelper.GetMainView.MainGrid.Children.Remove(_dragDropView);
        _dragDropView = null;
    }
}
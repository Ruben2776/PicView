using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.ProcessHandling;

namespace PicView.Avalonia.UI;

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
        if (path.IsSupported())
        {
            await NavigationHelper.LoadPicFromStringAsync(path, vm).ConfigureAwait(false);
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
            await HandleDragEnterFromUrl(e, vm);
            return;
        }

        await HandleDragEnter(files, vm, control);
    }

    private static async Task HandleDragEnter(IEnumerable<IStorageItem> files, MainViewModel vm, Control control)
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
                var thumb = await GetThumbnails.GetThumbAsync(path, 340).ConfigureAwait(false);

                if (thumb is not null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => { _dragDropView?.UpdateThumbnail(thumb); });
                }
            }
            else
            {
                RemoveDragDropView();
            }
        }
    }

    private static async Task HandleDragEnterFromUrl(DragEventArgs e, MainViewModel vm)
    {
        var urlObject = e.Data.Get("text/x-moz-url");
        if (urlObject != null)
        {
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
        }
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
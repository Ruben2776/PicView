using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;
using PicView.Core.ProcessHandling;

namespace PicView.Avalonia.Views;

public partial class MainView : UserControl
{
    private DragDropView? _dragDropView;
    public MainView()
    {
        InitializeComponent();

        Loaded += delegate
        {
            AddHandler(DragDrop.DragEnterEvent, DragEnter);
            AddHandler(DragDrop.DragLeaveEvent, DragLeave);
            AddHandler(DragDrop.DropEvent, Drop);

            GotFocus += CloseTitlebarIfOpen;
            LostFocus += HandleLostFocus;
            PointerPressed += PointerPressedBehavior;

            MainContextMenu.Opened += OnMainContextMenuOpened;
            
            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            HideInterfaceLogic.AddHoverButtonEvents(AltButtonsPanel, vm);
            PointerWheelChanged += async (_, e) => await vm.ImageViewer.PreviewOnPointerWheelChanged(this, e);
        };

    }

    private void PointerPressedBehavior(object? sender, PointerPressedEventArgs e)
    {
        CloseTitlebarIfOpen(sender, e);
        if (MainKeyboardShortcuts.ShiftDown)
        {
            var hostWindow = (Window)VisualRoot!;
            WindowHelper.WindowDragAndDoubleClickBehavior(hostWindow, e);
        }
        
        MainKeyboardShortcuts.ClearKeyDownModifiers();
        RemoveDragDropView();
    }
    
    private void CloseTitlebarIfOpen(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        if (vm.IsEditableTitlebarOpen)
        {
            vm.IsEditableTitlebarOpen = false;
        }
    }
    
    private void HandleLostFocus(object? sender, EventArgs e)
    {
        RemoveDragDropView();
    }

    private void OnMainContextMenuOpened(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var count = FileHistoryNavigation.GetCount();
        if (RecentFilesCM.Items.Count < count)
        {
            for (var i = RecentFilesCM.Items.Count; i < count; i++)
            {
                AddOrReplaceMenuItem(i, vm, isReplace: false);
            }
        }
        else
        {
            for (var i = 0; i < count; i++)
            {
                AddOrReplaceMenuItem(i, vm, isReplace: true);
            }
        }
    }

    private void AddOrReplaceMenuItem(int index, MainViewModel vm, bool isReplace)
    {
        if (!Application.Current.TryGetResource("LogoAccentColor", ThemeVariant.Default, out var secondaryAccentColor))
        {
            return;
        }

        var secondaryAccentBrush = new SolidColorBrush((Color)(secondaryAccentColor ?? Brushes.Yellow));
        var fileLocation = FileHistoryNavigation.GetFileLocation(index);
        var selected = vm.ImageIterator?.CurrentIndex == vm.ImageIterator?.ImagePaths.IndexOf(fileLocation);
        var header = Path.GetFileNameWithoutExtension(fileLocation);
        header = header.Length > 60 ? header.Shorten(60) : header;
        
        var item = new MenuItem
        {
            Header = header,
        };

        if (selected)
        {
            item.Foreground = secondaryAccentBrush;
        }
        
        item.Click += async delegate
        {
            await NavigationHelper.LoadPicFromStringAsync(fileLocation, vm).ConfigureAwait(false);
        };
        
        ToolTip.SetTip(item, fileLocation);

        if (isReplace)
        {
            RecentFilesCM.Items[index] = item;
        }
        else
        {
            RecentFilesCM.Items.Insert(index, item);
        }
    }

    private async Task Drop(object? sender, DragEventArgs e)
    {
        RemoveDragDropView();
        
        if (DataContext is not MainViewModel vm)
            return;

        var files = e.Data.GetFiles();
        if (files == null)
        {
            await HandleDropFromUrl(e, vm);
            return;
        }

        var storageItems = files as IStorageItem[] ?? files.ToArray();
        var firstFile = storageItems.FirstOrDefault();
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? firstFile.Path.AbsolutePath : firstFile.Path.LocalPath;
        await NavigationHelper.LoadPicFromStringAsync(path, vm).ConfigureAwait(false);
        if (!SettingsHelper.Settings.UIProperties.OpenInSameWindow)
        {
            foreach (var file in storageItems.Skip(1))
            {
                var filepath = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? file.Path.AbsolutePath : file.Path.LocalPath;
                ProcessHelper.StartNewProcess(filepath);
            }
        }
    }
    
    private async Task HandleDropFromUrl(DragEventArgs e, MainViewModel vm)
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
    
    private async Task DragEnter(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var files = e.Data.GetFiles();
        if (files == null)
        {
            await HandleDragEnterFromUrl(e, vm);
            return;
        }

        await HandleDragEnter(files, vm);
    }

    private async Task HandleDragEnter(IEnumerable<IStorageItem> files, MainViewModel vm)
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
                if (!IsPointerOver)
                {
                    MainGrid.Children.Add(_dragDropView);
                }
            }
            else
            {
                _dragDropView.RemoveThumbnail();
            }
        });
        var firstFile = fileArray[0];
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? firstFile.Path.AbsolutePath : firstFile.Path.LocalPath;
        if (Directory.Exists(path))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!IsPointerOver)
                {
                    _dragDropView.AddDirectoryIcon();
                }
            });
        }
        else
        {
            if (path.IsArchive())
            {
                if (!IsPointerOver)
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
                    var thumb = await ImageHelper.GetThumbAsync(path, SizeDefaults.WindowMinSize - 30).ConfigureAwait(false);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _dragDropView?.UpdateThumbnail(thumb);
                    });
                }

            }
            else
            {
                RemoveDragDropView();
            }
        }
    }
    
    private async Task HandleDragEnterFromUrl(DragEventArgs e, MainViewModel vm)
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
                    MainGrid.Children.Add(_dragDropView);
                }
                else
                {
                    _dragDropView.RemoveThumbnail();
                }
            });
        }
    }
    
    private void DragLeave(object? sender, DragEventArgs e)
    {
        if (!IsPointerOver)
        {
            RemoveDragDropView();
        }
    }
    
    public void RemoveDragDropView()
    {
        MainGrid.Children.Remove(_dragDropView);
        _dragDropView = null;
    }
}

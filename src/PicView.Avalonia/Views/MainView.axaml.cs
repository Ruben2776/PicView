using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Extensions;

namespace PicView.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        // TODO add visual feedback for drag and drop
        //AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
        
        GotFocus += CloseTitlebarIfOpen;
        PointerPressed += CloseTitlebarIfOpen;
        
        MainContextMenu.Opened += OnMainContextMenuOpened;
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

        MainKeyboardShortcuts.ClearKeyDownModifiers();
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
        if (DataContext is not MainViewModel vm)
            return;

        var data = e.Data.GetFiles();
        if (data == null)
        {
            // TODO Handle URL and folder drops
            return;
        }

        var storageItems = data as IStorageItem[] ?? data.ToArray();
        var firstFile = storageItems.FirstOrDefault();
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? firstFile.Path.AbsolutePath : firstFile.Path.LocalPath;
        await NavigationHelper.LoadPicFromStringAsync(path, vm).ConfigureAwait(false);
        foreach (var file in storageItems.Skip(1))
        {
            // TODO Open each file in a new window if the setting to open in the same window is false
        }
    }
}

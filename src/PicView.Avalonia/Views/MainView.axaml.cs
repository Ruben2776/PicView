using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Avalonia.Crop;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.Input;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Extensions;

namespace PicView.Avalonia.Views;

public partial class MainView : UserControl
{
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO Implement setting as wallpaper for macOS
                WallpaperMenuItem.IsEnabled = false;
            }
            
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
        if (MainKeyboardShortcuts.ShiftDown && !CropFunctions.IsCropping)
        {
            var hostWindow = (Window)VisualRoot!;
            WindowFunctions.WindowDragBehavior(hostWindow, e);
        }
        
        DragAndDropHelper.RemoveDragDropView();
    }
    
    private void CloseTitlebarIfOpen(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (!vm.IsEditableTitlebarOpen)
        {
            return;
        }

        vm.IsEditableTitlebarOpen = false;
        MainKeyboardShortcuts.IsKeysEnabled = true;
        Focus();
    }
    
    private static void HandleLostFocus(object? sender, EventArgs e)
    {
        DragAndDropHelper.RemoveDragDropView();
    }

    private void OnMainContextMenuOpened(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        CropMenuItem.IsEnabled = CropFunctions.DetermineIfShouldBeEnabled(vm);

        // Set source for ChangeCtrlZoomImage
        // TODO should probably be refactored inside a command (It doesn't update the UI in the zoom view, so should be made into a command)
        if (!Application.Current.TryGetResource("ScanEyeImage", Application.Current.RequestedThemeVariant, out var scanEyeImage ))
        {
            return;
        }
        if (!Application.Current.TryGetResource("LeftRightArrowsImage", Application.Current.RequestedThemeVariant, out var leftRightArrowsImage ))
        {
            return;
        }
        var isNavigatingWithCtrl = SettingsHelper.Settings.Zoom.CtrlZoom;
        vm.ChangeCtrlZoomImage = isNavigatingWithCtrl ? leftRightArrowsImage as DrawingImage : scanEyeImage as DrawingImage;

        // Update file history
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
        if (!Application.Current.TryGetResource("SecondaryAccentColor", Application.Current.RequestedThemeVariant, out var secondaryAccentColor))
        {
            return;
        }

        try
        {
#if DEBUG
            Debug.Assert(secondaryAccentColor != null, nameof(secondaryAccentColor) + " != null");
#endif
            var secondaryAccentBrush = (SolidColorBrush)secondaryAccentColor;
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
#if DEBUG
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
#else
        catch (Exception){}
#endif
    }

    private async Task Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        await DragAndDropHelper.Drop(e, vm);
    }
    
    private async Task DragEnter(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        await DragAndDropHelper.DragEnter(e, vm, this);
    }
    
    private void DragLeave(object? sender, DragEventArgs e)
    {
        DragAndDropHelper.DragLeave(e, this);
    }

    private void SetWallpaperClick(object? sender, RoutedEventArgs e)
    {
        Task.Run(FunctionsHelper.SetAsWallpaper);
        MainContextMenu.Close();
    }
}

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Crop;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.UI.FileHistory;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Conversion;
using PicView.Core.FileHistory;

namespace PicView.Avalonia.Views.Main;

public partial class MainView : UserControl
{
    public FileHistoryMenuController? FileHistoryMenuController;
    
    public MainView()
    {
        InitializeComponent();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // TODO: Add macOS support
            CopyFileMenuItem.IsVisible = false;
            
            // Move alt hover to left side on macOS and switch button order
            AltButtonsPanel.HorizontalAlignment = HorizontalAlignment.Left; 
            AltButtonsPanel.Children.Move(AltButtonsPanel.Children.IndexOf(AltClose),0);
            AltButtonsPanel.Children.Move(AltButtonsPanel.Children.IndexOf(AltMinimize),2);
            AltMinimize.RenderTransform = new ScaleTransform{ScaleX = -1};
        }

        if (!Settings.Theme.Dark && !Settings.Theme.GlassTheme)
        {
            if (!Application.Current.TryGetResource("MainTextColor",
                    Application.Current.RequestedThemeVariant, out var mainTextColor) ||
                !Application.Current.TryGetResource("SecondaryTextColor", Application.Current.RequestedThemeVariant, out var secondaryTextColor))
            {
                return;
            }

            if (mainTextColor is not Color color || secondaryTextColor is not Color secondaryColor)
            {
                return;
            }

            var brush = new SolidColorBrush(color);
            var secondaryBrush = new SolidColorBrush(secondaryColor);
            HistoryClearButton.PointerEntered += delegate       
            {
                HistoryClearTextBlock.Foreground = secondaryBrush;
                HistoryClearPath.Fill = secondaryBrush;
            };
            HistoryClearButton.PointerExited += delegate       
            {
                HistoryClearTextBlock.Foreground = brush;
                HistoryClearPath.Fill = brush;
            };
            HistoryFileButton.PointerEntered += delegate       
            {
                HistoryFileNameTextBlock.Foreground = secondaryBrush;
            };
            HistoryFileButton.PointerExited += delegate       
            {
                HistoryFileNameTextBlock.Foreground = brush;
            };
        }

        Loaded += delegate
        {
            AddHandler(DragDrop.DragEnterEvent, DragEnter);
            AddHandler(DragDrop.DragLeaveEvent, DragLeave);
            AddHandler(DragDrop.DropEvent, Drop);

            GotFocus += CloseTitlebarIfOpen;
            LostFocus += HandleLostFocus;
            PointerPressed += PointerPressedBehavior;

            if (Resources.TryGetResource("MainContextMenu", Application.Current.ActualThemeVariant, out var value))
            {
                if (value is ContextMenu mainContextMenu)
                {
                    mainContextMenu.Opened += async (sender, args) =>  await OnMainContextMenuOpened(sender, args);
                }
            }
            
            if (!FileHistoryManager.IsSortingDescending)
            {
                if (Application.Current.TryGetResource("SortAscImage",
                        Application.Current.RequestedThemeVariant, out var sortAscImage))
                {
                    HistorySortButton.Icon = sortAscImage as DrawingImage;
                }
            }
            
            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            // Initialize the history menu controller
            // TODO: rewrite FileHistory to MVVM
            FileHistoryMenuController = new FileHistoryMenuController(RecentFilesCM, HistorySortButton, HistoryClearButton, HistoryFileButton, vm);
            HistoryFileButton.Click += async delegate
            {
                await FunctionsMapper.ShowRecentHistoryFile();
            };

            // Setup hover fade buttons
            _ = new HoverFadeButtonHandler(ClickArrowRight, vm, ClickArrowRight.PolyButton);
            _ = new HoverFadeButtonHandler(ClickArrowLeft, vm, ClickArrowLeft.PolyButton);
            _ = new HoverFadeButtonHandler(AltButtonsPanel, vm);

            PointerWheelChanged += async (_, e) =>
                await MouseShortcuts.HandlePointerWheelChanged(e).ConfigureAwait(false);
        };
    }

    private void PointerPressedBehavior(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount is 2 && Settings.UIProperties.DoubleClickBehavior is 2)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PlatformWindowService.ToggleFullscreen();
            }
        }
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

        if (!vm.MainWindow.IsEditableTitlebarOpen.Value)
        {
            return;
        }

        vm.MainWindow.IsEditableTitlebarOpen.Value = false;
        MainKeyboardShortcuts.IsKeysEnabled = true;
        Focus();
    }
    
    private static void HandleLostFocus(object? sender, EventArgs e)
    {
        DragAndDropHelper.RemoveDragDropView();
    }

    private async Task OnMainContextMenuOpened(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CropMenuItem.IsEnabled = CropFunctions.DetermineIfShouldBeEnabled(vm);
            vm.PicViewer.ShouldOptimizeImageBeEnabled.Value = ConversionHelper.DetermineIfOptimizeImageShouldBeEnabled(vm.PicViewer.FileInfo?.CurrentValue);

            // Set source for ChangeCtrlZoomImage
            if (!Application.Current.TryGetResource("ScanEyeImage", Application.Current.RequestedThemeVariant, out var scanEyeImage))
            {
                return;
            }
            if (!Application.Current.TryGetResource("LeftRightArrowsImage", Application.Current.RequestedThemeVariant, out var leftRightArrowsImage))
            {
                return;
            }
            var isNavigatingWithCtrl = Settings.Zoom.CtrlZoom;
            vm.MainWindow.ChangeCtrlZoomImage.Value = isNavigatingWithCtrl ? leftRightArrowsImage as DrawingImage : scanEyeImage as DrawingImage;
        });
        
        // Update file history menu items in Dispatcher with low priority to avoid slowdown
        await Dispatcher.UIThread.InvokeAsync(() => FileHistoryMenuController?.UpdateFileHistoryMenu(),
            DispatcherPriority.Background);
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
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Conversion;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;
using R3;

namespace PicView.Avalonia.Views.Main;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        if (OperatingSystem.IsMacOS())
        {
            // Move alt hover to left side on macOS and switch button order
            DockPanel.SetDock(AltClose, Dock.Left);
            DockPanel.SetDock(AltMinimize, Dock.Left);
            DockPanel.SetDock(AltRestore, Dock.Left);
            DockPanel.SetDock(AltTitleBorder, Dock.Right);
            AltTitleBorder.BorderThickness = new Thickness(1,0,0,1);
        }
        
        if (!Settings.Theme.Dark)
        {
            AltClose.XButton.BorderThickness = new Thickness(0,0,0,1);
            AltRestore.RestoreButton.BorderThickness = new Thickness(0,0,0,1);
            AltMinimize.XButton.BorderThickness = new Thickness(0,0,0,1);
        }
        
        Loaded += delegate
        {
            AddHandler(DragDrop.DragEnterEvent, DragEnter);
            AddHandler(DragDrop.DragLeaveEvent, DragLeave);
            AddHandler(DragDrop.DropEvent, Drop);

            LostFocus += HandleLostFocus;
            PointerPressed += PointerPressedBehavior;
            
            MainTabControl.TabCreated += MainTabControlOnTabCreated;
            MainTabControl.SelectionChanged += MainTabControlOnSelectionChanged;

            if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
            {
                return;
            }
            mainWindow.Disposables.Add(new HoverFadeButtonHandler(AltButtonsPanel));

            if (DataContext is MainWindowViewModel vm)
            {
                // The buttons sit below the tab strip while it shows
                mainWindow.Disposables.Add(vm.WindowTabs.IsTabStripShown.Subscribe(shown =>
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() => AltButtonsPanel.Margin =
                        shown ? new Thickness(0, SizeDefaults.TabHeight, 0, 0) : new Thickness(0))));
            }
        };
    }

    private void HandleContextMenuOpened()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        
        tab.ShouldCropBeEnabled.Value = CropManager.SetIfCropEnabled(TopLevel.GetTopLevel(this) as MainWindow);
        tab.ShouldOptimizeImageBeEnabled.Value = ConversionHelper.DetermineIfOptimizeImageShouldBeEnabled(tab.FileInfo.CurrentValue);
        tab.ShouldPrintBeEnabled.Value = ConversionHelper.DetermineIfPrintMenuItemShouldBeEnabled(tab);
    }

    private void MainTabControlOnTabCreated(object? sender, TabCreatedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        var tabs = vm.WindowTabs.Tabs.CurrentValue;

        // Only set the StartUpMenu if the View is currently null.
        // This prevents overwriting the view (e.g. an image) when reordering tabs,
        // as reordering triggers the TabCreated event again by recreating containers.
        if (e.CreatedItem is not TabViewModel { CurrentView.Value: null } tabViewModel)
        {
            return;
        }

        if (tabViewModel.Model?.FileInfo is not null)
        {
            tabViewModel.CurrentView.Value = new ImageViewer(tabViewModel);
        }
        else
        {
            // Sized before it is laid out, so the window keeps its size
            var startUpMenu = new StartUpMenu();
            if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            {
                mainWindow.WindowFitter.FitStartUpMenu(startUpMenu);
            }
            tabViewModel.CurrentView.Value = startUpMenu;
            tabViewModel.SetNewTabTitle();
        }

        // Fix blank tab title when creating first new tab
        if (tabs.Count is 2)
        {
            if (tabs[0].CurrentView.CurrentValue is StartUpMenu)
            {
                tabs[0].SetNewTabTitle();
            }
        }
    }
    private void MainTabControlOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (e.AddedItems[0] is not TabViewModel tab)
        {
            return;
        }

        vm.WindowTabs.SelectTab(tab);
        if (string.IsNullOrEmpty(tab.Title.CurrentValue))
        {
            if (tab.Model?.FileInfo?.Exists == true)
            {
                tab.UpdateTabTitle();
            }
            else
            {
                tab.SetNewTabTitle();
            }
        }

        tab.ImageIterator?.UpdateNavigationProperties();
    }

    private void PointerPressedBehavior(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }

        DragAndDropManager.RemoveDragDropView(mainWindow);
        
        if (e.Properties.IsLeftButtonPressed)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && !CropManager.IsCropping(mainWindow))
            {
                WindowFunctions.WindowDragBehavior(mainWindow, e);
            }
        }
        else if (e.Properties.IsRightButtonPressed)
        {
            if (!Resources.TryGetResource("MainContextMenu", Application.Current.ActualThemeVariant, out var value))
            {
                return;
            }

            if (value is not ContextMenu mainContextMenu || DataContext is not MainWindowViewModel vm)
            {
                return;
            }

            var tab = vm.WindowTabs.ActiveTab.CurrentValue;
            var view = tab.CurrentView.CurrentValue;
            switch (view)
            {
                case CropControl:
                    return; // Don't show this control's context menu, to not interfere with crop control's context menu
                case ImageViewer viewer:
                {
                    // The click arrows and hoverbar have their own right-click interaction
                    if (viewer.ClickArrowLeft.PolyButton.IsPointerOver || viewer.ClickArrowRight.PolyButton.IsPointerOver)
                    {
                        return;
                    }
                    if (viewer.HoverBar.BottomBorder.IsPointerOver)
                    {
                        return;
                    }
                    if (viewer.GalleryView.IsPointerOver)
                    {
                        return;
                    }

                    break;
                }
            }
            mainContextMenu.Open(this);
            HandleContextMenuOpened();
        }
    }
    
    private void HandleLostFocus(object? sender, EventArgs e)
    {
        DragAndDropManager.RemoveDragDropView(TopLevel.GetTopLevel(this) as MainWindow);
    }

    private async ValueTask Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        await DragAndDropManager.Drop(e, vm.WindowTabs, TopLevel.GetTopLevel(this) as MainWindow).ConfigureAwait(false);
    }
    
    private async ValueTask DragEnter(object? sender, DragEventArgs e)
    {
        await DragAndDropManager.DragEnter(e, TopLevel.GetTopLevel(this) as MainWindow).ConfigureAwait(false);
    }
    
    private void DragLeave(object? sender, DragEventArgs e)
    {
        DragAndDropManager.DragLeave(TopLevel.GetTopLevel(this) as MainWindow);
    }

    private void DragMove(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow { DataContext: MainWindowViewModel } mainWindow)
        {
            return;
        }
        WindowFunctions.WindowDragAndDoubleClickBehavior(mainWindow, e);
    }
}
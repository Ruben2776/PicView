using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Conversion;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;

namespace PicView.Avalonia.Views.Main;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Move alt hover to left side on macOS and switch button order
            DockPanel.SetDock(AltClose, Dock.Left);
            DockPanel.SetDock(AltMinimize, Dock.Left);
            DockPanel.SetDock(AltRestore, Dock.Left);
            DockPanel.SetDock(AltTitleBorder, Dock.Right);
            AltTitleBorder.BorderThickness = new Thickness(1,0,0,1);
        }


        Loaded += delegate
        {
            AddHandler(DragDrop.DragEnterEvent, DragEnter);
            AddHandler(DragDrop.DragLeaveEvent, DragLeave);
            AddHandler(DragDrop.DropEvent, Drop);

            LostFocus += HandleLostFocus;
            PointerPressed += PointerPressedBehavior;

            if (Resources.TryGetResource("MainContextMenu", Application.Current.ActualThemeVariant, out var value))
            {
                if (value is ContextMenu mainContextMenu)
                {
                    mainContextMenu.Opening += OnMainContextMenuOpening;
                }
            }
            
            //MainTabControl.TabDetached += MainTabControlOnTabDetached;
            MainTabControl.TabCreated += MainTabControlOnTabCreated;
            MainTabControl.SelectionChanged += MainTabControlOnSelectionChanged;

            if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
            {
                return;
            }
            mainWindow.Disposables.Add(new HoverFadeButtonHandler(AltButtonsPanel));
        };
    }

    private void OnMainContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (tab.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            // Cancel the context menu if the hover bar is visible, because custom pop-up dialogs are shown instead.
            if (imageViewer.HoverBar.Opacity > 0)
            {
                e.Cancel = true;
            }
        }
        
        CropManager.SetIfCropEnabled(TopLevel.GetTopLevel(this) as MainWindow);
        tab.ShouldOptimizeImageBeEnabled.Value = ConversionHelper.DetermineIfOptimizeImageShouldBeEnabled(tab.FileInfo.CurrentValue);
        
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
        vm.ChangeCtrlZoomImage.Value = isNavigatingWithCtrl ? leftRightArrowsImage as DrawingImage : scanEyeImage as DrawingImage;
    }

    private void MainTabControlOnTabCreated(object? sender, TabCreatedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        var tabs = vm.WindowTabs.Tabs.CurrentValue;
        if (tabs.Count >= 2)
        {
            AltButtonsPanel.Margin = new Thickness(0, SizeDefaults.TabHeight, 0, 0);
        }
        else
        {
            AltButtonsPanel.Margin = new Thickness(0);
        }
        
        // Only set the StartUpMenu if the View is currently null.
        // This prevents overwriting the view (e.g. an image) when reordering tabs,
        // as reordering triggers the TabCreated event again by recreating containers.
        if (e.CreatedItem is not TabViewModel { CurrentView.Value: null } tabViewModel)
        {
            return;
        }

        if (tabViewModel.Model?.FileInfo is not null)
        {
            tabViewModel.CurrentView.Value = new ImageViewer();
        }
        else
        {
            var startUpMenu = new StartUpMenu();
            
            if (Settings.WindowProperties.AutoFit)
            {
                // Keep the StartUpMenu the same size when creating a new tab
                startUpMenu.Width = Bounds.Width;
                startUpMenu.Height = Bounds.Height - SizeDefaults.TabHeight;
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
            if (MainKeyboardShortcuts.ShiftDown && !CropManager.IsCropping(mainWindow))
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
                    if (viewer.ClickArrowLeft.IsPointerOver || viewer.ClickArrowRight.IsPointerOver)
                    {
                        return;
                    }
                    if (viewer.HoverBar.IsPointerOver)
                    {
                        return;
                    }

                    break;
                }
            }
            mainContextMenu.Open(this);
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
        
        await DragAndDropManager.Drop(e, vm.WindowTabs, TopLevel.GetTopLevel(this) as MainWindow);
    }
    
    private async ValueTask DragEnter(object? sender, DragEventArgs e)
    {
        await DragAndDropManager.DragEnter(e, TopLevel.GetTopLevel(this) as MainWindow);
    }
    
    private void DragLeave(object? sender, DragEventArgs e)
    {
        DragAndDropManager.DragLeave(TopLevel.GetTopLevel(this) as MainWindow);
    }

    private void DragMove(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not MainWindow { DataContext: MainWindowViewModel vm } mainWindow)
        {
            return;
        }
        WindowFunctions.WindowDragAndDoubleClickBehavior(mainWindow, e, vm.PlatformWindowService);
    }
}
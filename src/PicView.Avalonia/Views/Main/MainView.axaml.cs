using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Conversion;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;

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

            MainTabControl.SelectionChanged += MainTabControlOnSelectionChanged;

            if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
            {
                return;
            }
            mainWindow.Disposables.Add(new HoverFadeButtonHandler(AltButtonsPanel, AltButtonHolder));

            if (DataContext is MainWindowViewModel vm)
            {
                // The buttons sit below the tab strip while it shows
                mainWindow.Disposables.Add(vm.WindowTabs.IsTabStripShown.Subscribe(shown =>
                    Dispatcher.UIThread.Post(() => AltButtonsPanel.Margin =
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
        tab.ShouldCloseImageBeVisible.Value = tab.CurrentView.Value is NavigableView;
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
            // A tab of the tab strip shows its own menu
            if (IsOnTabStrip(e.Source))
            {
                return;
            }


            if (!Resources.TryGetResource("MainContextMenu", Application.Current.ActualThemeVariant, out var value))
            {
                return;
            }

            if (value is not ContextMenu mainContextMenu || DataContext is not MainWindowViewModel vm)
            {
                return;
            }

            var tab = vm.WindowTabs.ActiveTab.CurrentValue;
            var view = tab.CurrentView.Value;
            switch (view)
            {
                case CropControl:
                    return; // Don't show this control's context menu, to not interfere with crop control's context menu
                case NavigableView nav:
                {
                    // The click arrows and hoverbar have their own right-click interaction
                    if (nav.ClickArrowLeft.PolyButton.IsPointerOver || nav.ClickArrowRight.PolyButton.IsPointerOver)
                    {
                        return;
                    }
                    if (nav.HoverBar.BottomBorder.IsPointerOver)
                    {
                        return;
                    }
                    if (nav.GalleryView.IsPointerOver || nav.BarHost.IsPointerOver)
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
    
    private bool IsOnTabStrip(object? source)
    {
        var tabItem = (source as Visual)?.FindAncestorOfType<TabItem>(includeSelf: true);
        // Only this window's tab strip: a tab control inside a tab's content has its own tabs
        return tabItem is not null && MainTabControl.IndexFromContainer(tabItem) >= 0;
    }

    /// <summary>
    /// Enables only the tab menu's items that would close a tab: the last tab, the first tab's left and the last tab's
    /// right have nothing to close.
    /// </summary>
    private void TabContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (sender is not ContextMenu { DataContext: TabViewModel tab } menu)
        {
            return;
        }

        var tabs = tab.ParentWindowContext.WindowTabs.Tabs.Value;
        var index = tabs.IndexOf(tab);
        if (index < 0)
        {
            e.Cancel = true;
            return;
        }

        var enabled = new[]
        {
            tabs.Count > 1, // Close
            tabs.Count > 1, // Close other tabs
            index > 0, // Close tabs to the left
            index < tabs.Count - 1 // Close tabs to the right
        };
        for (var i = 0; i < menu.Items.Count && i < enabled.Length; i++)
        {
            if (menu.Items[i] is MenuItem item)
            {
                item.IsEnabled = enabled[i];
            }
        }
    }

    private void HandleLostFocus(object? sender, EventArgs e)
    {
        DragAndDropManager.RemoveDragDropView(TopLevel.GetTopLevel(this) as MainWindow);
    }

    private async ValueTask Drop(object? sender, DragEventArgs e)
    {
        await DragAndDropManager.Drop(e, TopLevel.GetTopLevel(this) as MainWindow).ConfigureAwait(false);
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
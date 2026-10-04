using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.UC;

public partial class FileHistoryItem : UserControl
{
    public FileHistoryItem()
    {
        InitializeComponent();
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        MainButton.AddHandler(PointerPressedEvent, MainButtonOnClick, RoutingStrategies.Tunnel);
    }

    private async ValueTask MainButtonOnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not FileHistoryEntryViewModel entry)
        {
            return;
        }
        var vm = core.MainWindows.ActiveWindow.CurrentValue;
        vm.TopTitlebarViewModel.DropDownMenu.IsDropDownMenuVisible.Value = false;
        
        var tabs = vm.WindowTabs;
        var tab = tabs.ActiveTab.CurrentValue;
        var path = entry.FilePath.CurrentValue;
        
        if (!tab.IsInitialized)
        {
            if ( TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
            {
                return;
            }
            await QuickLoad.QuickLoadAsync(mainWindow, core, path, true).ConfigureAwait(false);
            return;
        }

        var isViewStartUpMenu = false;
        if (tab.CurrentView.CurrentValue is StartUpMenu)
        {
            tab.CurrentView.Value = new ImageViewer();
            isViewStartUpMenu = true;
        }
        
        var isLoadedSuccessfully = await tabs.LoadFromStringAsync(path).ConfigureAwait(true);
        if (!isLoadedSuccessfully && isViewStartUpMenu)
        {
            tab.CurrentView.Value = new StartUpMenu();
        }
        else
        {
            if (TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
            {
                return;
            }
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
        }
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        ButtonPanel.IsVisible = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        ButtonPanel.IsVisible = false;
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        PointerEntered -= OnPointerEntered;
        PointerExited -= OnPointerExited;
    }
}
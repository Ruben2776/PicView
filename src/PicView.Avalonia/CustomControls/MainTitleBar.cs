using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

public class MainTitleBar : UserControl, ITitleBar
{
    public Button? SharedDropDownMenuButton { get; protected init; }
    public Button? SharedSearchButton { get; protected init; }
    protected MainTitleBar()
    {
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        PointerPressed += OnPointerPressed;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if ( TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        WindowDragAndDoubleClickBehavior(mainWindow, e);
    }

    private void WindowDragAndDoubleClickBehavior(MainWindow mainWindow, PointerPressedEventArgs e)
    {
        if (VisualRoot is null || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.IsEditableTitlebarOpen.Value || mainWindow.UIHelper.GetDropDownMenu.IsOpen)
        {
            return;
        }

        if (vm.TopTitlebarViewModel.IsMainMenuVisible.CurrentValue)
        {
            // Clicked in main menu, handle accordingly
            e.Handled = false;
            return;
        }

        WindowFunctions.WindowDragAndDoubleClickBehavior(mainWindow, e, vm.PlatformWindowService);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
        PointerPressed -= OnPointerPressed;
    }
}
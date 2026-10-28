using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.UC;

public partial class EditableTitlebar : UserControl
{
    public EditableTitlebar()
    {
        InitializeComponent();
        LostFocus += HandleLostFocus;
        PointerEntered += HandlePointerEntered;
        PointerPressed += HandlePointerPressed;
        TextBox.LostFocus += HandleLostFocus;
    }

    private void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed ||
            vm.IsEditableTitlebarOpen.CurrentValue)
        {
            return;
        }

        SelectFileName();
    }

    private void HandlePointerEntered(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        Cursor = vm.IsEditableTitlebarOpen.CurrentValue
            ? new Cursor(StandardCursorType.Ibeam)
            : new Cursor(StandardCursorType.Arrow);
    }

    private void HandleLostFocus(object? sender, RoutedEventArgs e) => CloseTitlebar();

    public void CloseTitlebar()
    {
        TextBox.ClearSelection();
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.IsEditableTitlebarOpen.Value = false;
        Cursor = new Cursor(StandardCursorType.Arrow);
        TextBlock.Text = vm.WindowTabs.ActiveTab.CurrentValue.Title
            .CurrentValue;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (!vm.IsEditableTitlebarOpen.CurrentValue)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CloseTitlebar();
            e.Handled = true;
            TopLevel.GetTopLevel(this)?.Focus();
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        
        if (DataContext is not MainWindowViewModel vm
            || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        
        if (!vm.IsEditableTitlebarOpen.CurrentValue)
        {
            if (e.Key != Key.Escape)
            {
                _ = mainWindow.KeybindRouter.KeyDownAsync(new Keybind(e.Key, e.KeyModifiers),
                    mainWindow.FocusManager?.GetFocusedElement());
            }
        
            return;
        }
        
        switch (e.Key)
        {
            case Key.Enter:
                _ = RenameHelper.RenameAction(vm, TextBox.Text, mainWindow);
                break;
            case Key.Escape:
                mainWindow.UIHelper.GetMainView.Focus();
                break;
        }
    }

    public void SelectFileName()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var file = vm.WindowTabs.ActiveTab.CurrentValue.Model
            .FileInfo;
        if (file is null)
        {
            return;
        }

        var filename = file.Name;
        TextBox.Text = file.Name;

        var start = TextBox.Text.Length - filename.Length;
        var end = Path.GetFileNameWithoutExtension(filename).Length;
        TextBox.SelectionStart = start;
        TextBox.SelectionEnd = end;

        vm.IsEditableTitlebarOpen.Value = true;
        Cursor = new Cursor(StandardCursorType.Ibeam);
        TextBox.Focus();
    }
}
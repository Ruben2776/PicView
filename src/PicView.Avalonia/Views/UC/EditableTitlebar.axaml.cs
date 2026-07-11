using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Core.FileHandling;
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
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed ||
            core.MainWindows.ActiveWindow.CurrentValue.IsEditableTitlebarOpen.CurrentValue)
        {
            return;
        }

        SelectFileName();
    }

    private void HandlePointerEntered(object? sender, PointerEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        
        Cursor = core.MainWindows.ActiveWindow.CurrentValue.IsEditableTitlebarOpen.CurrentValue
            ? new Cursor(StandardCursorType.Ibeam)
            : new Cursor(StandardCursorType.Arrow);
    }

    private void HandleLostFocus(object? sender, RoutedEventArgs e) => CloseTitlebar();

    public void CloseTitlebar()
    {
        TextBox.ClearSelection();
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        core.MainWindows.ActiveWindow.CurrentValue.IsEditableTitlebarOpen.Value = false;
        Cursor = new Cursor(StandardCursorType.Arrow);
        MainKeyboardShortcuts.IsKeysEnabled = true;
        TextBlock.Text = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Title
            .CurrentValue;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        if (!core.MainWindows.ActiveWindow.CurrentValue.IsEditableTitlebarOpen.CurrentValue)
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

        MainKeyboardShortcuts.IsKeysEnabled = false;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not MainWindowViewModel vm
            || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        
        if (!core.MainWindows.ActiveWindow.CurrentValue.IsEditableTitlebarOpen.CurrentValue)
        {
            if (e.Key != Key.Escape)
            {
                _ = MainKeyboardShortcuts.MainWindow_KeysDownAsync(e, vm, mainWindow).ConfigureAwait(false);
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
                MainKeyboardShortcuts.IsKeysEnabled = true;
                break;
        }
    }

    public void SelectFileName()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        var file = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Model
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

        core.MainWindows.ActiveWindow.CurrentValue.IsEditableTitlebarOpen.Value = true;
        Cursor = new Cursor(StandardCursorType.Ibeam);
        TextBox.Focus();
    }
}
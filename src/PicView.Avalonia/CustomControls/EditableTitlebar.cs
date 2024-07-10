using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.CustomControls;

public class EditableTitlebar : TextBox
{
    protected override Type StyleKeyOverride => typeof(EditableTitlebar);
    
    private bool _isRenaming;

    public EditableTitlebar()
    {
        LostFocus += OnLostFocus;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!_isRenaming)
        {
            if (VisualRoot is null) { return; }
            var hostWindow = (Window)VisualRoot;
            hostWindow.BeginMoveDrag(e);
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!_isRenaming)
        {
            Cursor = Cursor.Default;
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        ClearSelection();
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        vm.RefreshTitle();
    }

    #region Rename

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key is Key.Enter or Key.Return)
        {
            _ = HandleRename();
        }
        else if (e.Key == Key.Escape)
        {
            UIHelper.GetMainView.Focus();
        }
    }

    private async Task HandleRename()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }
        
        if (vm.FileInfo is null)
        {
            return;
        }
        _isRenaming = true;
        var path = vm.FileInfo.FullName;
        var newPath = Path.Combine(vm.FileInfo.DirectoryName, Text);
        var renamed = await Task.FromResult(FileHelper.RenameFile(path, newPath));
        if (renamed)
        {
            vm.SetTitle();
        }
        _isRenaming = false;
    }
    
    public void SelectFileName()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }
        
        if (vm.FileInfo is null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(Text))
        {
            Text = vm.FileInfo.Name;
        }
        var filename = vm.FileInfo.Name;
        var start = Text.Length - filename.Length;
        var end = Path.GetFileNameWithoutExtension(filename).Length;
        SelectionStart = start;
        SelectionEnd = end;
        Focus();
    }

    #endregion

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        // TODO: Add custom resize logic
        base.OnSizeChanged(e);
    }
}

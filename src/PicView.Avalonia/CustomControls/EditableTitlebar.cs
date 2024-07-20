using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.CustomControls;

[TemplatePart("PART_TextBlock", typeof(TextBlock))]
[TemplatePart("PART_TextPresenter", typeof(TextPresenter))]
[TemplatePart("PART_Border", typeof(Border))]
public class EditableTitlebar : TextBox
{
    protected override Type StyleKeyOverride => typeof(EditableTitlebar);
    
    public bool IsRenaming { get; private set; }
    
    private TextBlock? _textBlock;

    private Border? _border;

    public EditableTitlebar()
    {
        LostFocus += OnLostFocus;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _textBlock = e.NameScope.Find<TextBlock>("PART_TextBlock");
        _border = e.NameScope.Find<Border>("PART_Border");
        if (_textBlock is null)
        {
            return;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            SelectFileName();
            return;
        }
        if (!IsRenaming)
        {
            if (VisualRoot is null) { return; }
            var hostWindow = (Window)VisualRoot;
            WindowHelper.WindowDragAndDoubleClickBehavior(hostWindow, e);
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
        _textBlock.IsVisible = true;
        _border.IsVisible = false;
        Cursor = new Cursor(StandardCursorType.Arrow);
        MainKeyboardShortcuts.IsKeysEnabled = true;
    }

    #region Rename
    
    protected override void OnKeyDown(KeyEventArgs e)
    {
        MainKeyboardShortcuts.IsKeysEnabled = false;
        base.OnKeyDown(e);
    }
    
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key is Key.Enter or Key.Return)
        {
            _ = HandleRename();
            MainKeyboardShortcuts.IsKeysEnabled = true;
        }
        else if (e.Key == Key.Escape)
        {
            UIHelper.GetMainView.Focus();
            MainKeyboardShortcuts.IsKeysEnabled = true;
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
        IsRenaming = true;
        var path = vm.FileInfo.FullName;
        var newPath = Path.Combine(vm.FileInfo.DirectoryName, Text);
        var renamed = await Task.FromResult(FileHelper.RenameFile(path, newPath));
        if (renamed)
        {
            vm.SetTitle();
        }
        IsRenaming = false;
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
        _textBlock.IsVisible = false;
        _border.IsVisible = true;
        Cursor = new Cursor(StandardCursorType.Ibeam);
        Focus();
    }

    #endregion
}

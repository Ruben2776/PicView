using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;

namespace PicView.Avalonia.CustomControls;

[TemplatePart("PART_TextBlock", typeof(TextBlock))]
[TemplatePart("PART_TextPresenter", typeof(TextPresenter))]
[TemplatePart("PART_Border", typeof(Border))]
public class EditableTitlebar : TextBox
{
    #region Properties
    protected override Type StyleKeyOverride => typeof(EditableTitlebar);

    public bool IsRenaming
    {
        get
        {
            if (DataContext is not MainViewModel vm)
            {
                return false;
            }
            return vm.ImageIterator?.IsFileBeingRenamed ?? false;
        }
        private set
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            vm.ImageIterator.IsFileBeingRenamed = value;
        }
    }
    
    public bool IsOpen { get; private set; }
    
    private TextBlock? _textBlock;

    private Border? _border;
    
    #endregion
    
    #region Constructor, overrides and behavior

    public EditableTitlebar()
    {
        LostFocus += OnLostFocus;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _textBlock = e.NameScope.Find<TextBlock>("PART_TextBlock");
        _border = e.NameScope.Find<Border>("PART_Border");
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (IsRenaming || IsOpen)
            {
                return;
            }

            IsOpen = true;
            SelectFileName();
            return;
        }

        if (IsRenaming || VisualRoot is null)
        {
            return;
        }
        
        var hostWindow = (Window)VisualRoot;
        if (e.ClickCount == 2 && e.GetCurrentPoint(hostWindow).Properties.IsLeftButtonPressed && !IsOpen)
        {
            _ = WindowHelper.ToggleFullscreen(hostWindow.DataContext as MainViewModel);
            return;
        }
        hostWindow.BeginMoveDrag(e);
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        ClearSelection();
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        SetTitleHelper.RefreshTitle(vm);
        _textBlock.IsVisible = true;
        _border.IsVisible = false;
        Cursor = new Cursor(StandardCursorType.Arrow);
        MainKeyboardShortcuts.IsKeysEnabled = true;
        IsOpen = false;
    }
    
    #endregion

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
        vm.IsLoading = true;
        IsRenaming = true;
        var oldPath = vm.FileInfo.FullName;
        var newPath = Path.Combine(vm.FileInfo.DirectoryName, Text);
        
        // Check if the file is being moved to a different directory
        if (Path.GetDirectoryName(oldPath) != Path.GetDirectoryName(newPath))
        {
            await Renamed().ConfigureAwait(false);
            return;
        }
        
        // Handle renaming with different extensions
        if (Path.GetExtension(newPath) != Path.GetExtension(oldPath))
        {
            var saved = await SaveImageFileHelper.SaveImageAsync(stream: null, path: oldPath, destination: newPath, width:null, height: null, quality: null, ext: Path.GetExtension(newPath)).ConfigureAwait(false);
            while (FileHelper.IsFileInUse(oldPath))
            {
                await Task.Delay(50); // Fixes "this action can't be completed because the file is open"
            }

            if (saved)
            {
                // Navigate to newly saved file
                await vm.ImageIterator.LoadPicFromFile(new FileInfo(newPath)).ConfigureAwait(false); 
                
                // Delete old file
                var deleteMsg = FileDeletionHelper.DeleteFileWithErrorMsg(oldPath, false);
                if (!string.IsNullOrWhiteSpace(deleteMsg))
                {
                    // Show error message to user
                    TooltipHelper.ShowTooltipMessage(deleteMsg);
                    vm.IsLoading = false;
                    return;
                }
            }
            await End();
            return;
        }
        var renamed = await Renamed().ConfigureAwait(false);
        if (!renamed)
        {
            IsRenaming = false;
            return;
        }
        await End();
        return;

        async Task<bool> Renamed()
        {
            return await Task.FromResult(FileHelper.RenameFile(oldPath, newPath));
        }

        async Task End()
        {
            SetTitleHelper.SetTitle(vm);
            vm.IsLoading = false;
            IsRenaming = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ClearSelection();
                SetTitleHelper.RefreshTitle(vm);
                _textBlock.IsVisible = true;
                _border.IsVisible = false;
                Cursor = new Cursor(StandardCursorType.Arrow);
                MainKeyboardShortcuts.IsKeysEnabled = true;
                UIHelper.GetMainView.Focus();
            });
        }
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

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.Views.UC;

public partial class EditableTitlebar : UserControl
{
    #region Properties

    public bool IsRenaming
    {
        get
        {
            if (DataContext is not MainViewModel vm)
            {
                return false;
            }
            return vm.ImageIterator?.IsRenamingInProgress ?? false;
        }
        private set
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            vm.ImageIterator.IsRenamingInProgress = value;
        }
    }

    #endregion
    public EditableTitlebar()
    {
        InitializeComponent();
        LostFocus += OnLostFocus;
        PointerEntered += OnPointerEntered;
        PointerPressed += OnPointerPressed;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            return;
        }

        if (IsRenaming || vm.IsEditableTitlebarOpen)
        {
            return;
        }

        vm.IsEditableTitlebarOpen = true;
        SelectFileName();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        Cursor = vm.IsEditableTitlebarOpen ?
            new Cursor(StandardCursorType.Ibeam) :
            new Cursor(StandardCursorType.Arrow);
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        CloseTitlebar();
    }
    
    public void CloseTitlebar()
    {
        TextBox.ClearSelection();
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        SetTitleHelper.RefreshTitle(vm);
        vm.IsEditableTitlebarOpen = false;
        Cursor = new Cursor(StandardCursorType.Arrow);
        MainKeyboardShortcuts.IsKeysEnabled = true;
        TextBlock.Text = vm.Title;
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
        vm.IsLoading = true;
        IsRenaming = true;
        var oldPath = vm.FileInfo.FullName;
        var newPath = Path.Combine(vm.FileInfo.DirectoryName, TextBox.Text);
        
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
                await NavigationHelper.LoadPicFromFile(newPath, vm).ConfigureAwait(false);
                
                // Delete old file
                var deleteMsg = FileDeletionHelper.DeleteFileWithErrorMsg(oldPath, false);
                if (!string.IsNullOrWhiteSpace(deleteMsg))
                {
                    // Show error message to user
                    await TooltipHelper.ShowTooltipMessageAsync(deleteMsg);
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
                TextBox.ClearSelection();
                SetTitleHelper.RefreshTitle(vm);
                vm.IsEditableTitlebarOpen = false;
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
        TextBox.Text = vm.FileInfo.Name;
        var filename = vm.FileInfo.Name;
        var start = TextBox.Text.Length - filename.Length;
        var end = Path.GetFileNameWithoutExtension(filename).Length;
        TextBox.SelectionStart = start;
        TextBox.SelectionEnd = end;
        vm.IsEditableTitlebarOpen = true;
        Cursor = new Cursor(StandardCursorType.Ibeam);
        Focus();
    }

    #endregion
}

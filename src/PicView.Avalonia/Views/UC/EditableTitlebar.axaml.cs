using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Input;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
using PicView.Core.Localization;

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
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm ||
            !e.GetCurrentPoint(this).Properties.IsRightButtonPressed ||
            vm.MainWindow.IsEditableTitlebarOpen.CurrentValue)
        {
            return;
        }

        SelectFileName();
    }

    private void HandlePointerEntered(object? sender, PointerEventArgs e)
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
        {
            return;
        }

        Cursor = vm.MainWindow.IsEditableTitlebarOpen.CurrentValue
            ? new Cursor(StandardCursorType.Ibeam)
            : new Cursor(StandardCursorType.Arrow);
    }

    private void HandleLostFocus(object? sender, RoutedEventArgs e) => CloseTitlebar();

    public void CloseTitlebar()
    {
        TextBox.ClearSelection();
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.MainWindow.IsEditableTitlebarOpen.Value = false;
        Cursor = new Cursor(StandardCursorType.Arrow);
        MainKeyboardShortcuts.IsKeysEnabled = true;
        TextBlock.Text = vm.PicViewer.Title.CurrentValue;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
        {
            return;
        }

        if (!vm.MainWindow.IsEditableTitlebarOpen.CurrentValue)
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

        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
        {
            return;
        }

        if (!vm.MainWindow.IsEditableTitlebarOpen.CurrentValue)
        {
            if (e.Key != Key.Escape)
            {
                _ = MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
            }

            return;
        }

        if (e.Key == Key.Enter)
        {
            var oldPath = vm.PicViewer.FileInfo.CurrentValue.FullName;
            var newPath = Path.Combine(vm.PicViewer.FileInfo.CurrentValue.DirectoryName, TextBox.Text);
            Task.Run(async () =>
            {
                if (newPath == oldPath)
                {
                    ShowFileExistsError(vm);
                    return;
                }

                try
                {
                    vm.MainWindow.IsLoadingIndicatorShown.Value = true;
                    NavigationManager.DisableWatcher();
                    
                    var renamed = await FileRenamer.AttemptRenameAsync(
                            oldPath, 
                            newPath, 
                            ErrorHandling.ReloadAsync(vm),
                            vm.PlatformService.DeleteFile(oldPath, true))
                        .ConfigureAwait(false);

                
                    MainKeyboardShortcuts.IsKeysEnabled = true;
                    if (renamed)
                    {
                        vm.MainWindow.IsLoadingIndicatorShown.Value = false;
                        vm.MainWindow.IsEditableTitlebarOpen.Value = false;
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            TextBox.ClearSelection();
                            Cursor = new Cursor(StandardCursorType.Arrow);
                            UIHelper.GetMainView.Focus();
                        });
                    }
                }
                finally
                {
                    vm.MainWindow.IsLoadingIndicatorShown.Value = false;
                    if (Settings.Navigation.IsFileWatcherEnabled)
                    {
                        NavigationManager.EnableWatcher();
                    }
                }
            });
        }
        else if (e.Key == Key.Escape)
        {
            UIHelper.GetMainView.Focus();
            MainKeyboardShortcuts.IsKeysEnabled = true;
        }
    }
    
    private void ShowFileExistsError(MainViewModel vm)
    {
        CloseTitlebar();
        vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        TooltipHelper.ShowTooltipMessage(TranslationManager.GetTranslation("FileAlreadyExistsError"), true);
    }

    public void SelectFileName()
    {
        if (UIHelper.GetMainView.DataContext is not MainViewModel vm)
        {
            return;
        }

        if (vm.PicViewer.FileInfo.CurrentValue is null)
        {
            return;
        }

        var filename = vm.PicViewer.FileInfo.CurrentValue.Name;
        TextBox.Text = filename;

        var start = TextBox.Text.Length - filename.Length;
        var end = Path.GetFileNameWithoutExtension(filename).Length;
        TextBox.SelectionStart = start;
        TextBox.SelectionEnd = end;

        vm.MainWindow.IsEditableTitlebarOpen.Value = true;
        Cursor = new Cursor(StandardCursorType.Ibeam);
        TextBox.Focus();
    }
}
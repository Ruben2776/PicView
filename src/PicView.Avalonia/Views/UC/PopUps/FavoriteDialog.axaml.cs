using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class FavoriteDialog : AnimatedPopUp
{
    private DisposableBag _disposables;

    public FavoriteDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.Favorites.AddFromFolderCommand.SubscribeAwait(async (_, _) =>
        {
            var directory = await FilePicker.SelectDirectory().ConfigureAwait(false);
            if (directory is null)
            {
                return;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                PathBox.Text = directory;
            });
        }, DebugHelper.LogError(nameof(FavoriteDialog), nameof(vm.Favorites.AddFromFolderCommand))
            , AwaitOperation.Drop).AddTo(ref _disposables);

        CancelButton.Click += CloseMenu;
        CloseButton.Click += CloseMenu;
        SaveButton.Click += ApplyButtonOnClick;
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                //
                e.Handled = true;
                break;
            case Key.Escape:
                CancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
                break;
        }
    }

    private void ApplyButtonOnClick(object? sender, RoutedEventArgs e)
    {
        //
        _ = AnimatedClosing();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Loaded -= OnLoaded;
        SaveButton.Click -= ApplyButtonOnClick;
        KeyDown -= OnKeyDown;
        CloseButton.Click -= CloseMenu;
        CancelButton.Click -= CloseMenu;
        _disposables.Dispose();
    }

    private void CloseMenu(object? sender, RoutedEventArgs e)
    {
        _ = AnimatedClosing();
    }
}

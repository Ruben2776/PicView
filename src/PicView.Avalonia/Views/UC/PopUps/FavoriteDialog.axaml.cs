using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.Favorites;
using PicView.Core.ViewModels;
using R3;
using MouseButton = PicView.Avalonia.Input.MouseButton;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class FavoriteDialog : AnimatedPopUp
{
    private DisposableBag _disposables;
    private readonly string? _initialPath;
    private readonly string? _initialKeybind;
    private bool _isSaving;

    public FavoriteDialog(string? initialPath, string? initialKeybind)
    {
        _initialPath = initialPath;
        _initialKeybind = initialKeybind;
        InitializeComponent();

        if (!string.IsNullOrWhiteSpace(_initialPath))
        {
            PathBox.Text = _initialPath;
        }

        if (!string.IsNullOrWhiteSpace(_initialKeybind))
        {
            var parsed = Keybind.Parse(_initialKeybind);
            if (parsed.HasValue && (parsed.Value.Key is not Key.None || parsed.Value.MouseButton is not MouseButton.None))
            {
                KeyBox.Keybinds = [parsed.Value];
            }
        }

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_initialPath) && string.IsNullOrWhiteSpace(PathBox.Text))
        {
            PathBox.Text = _initialPath;
        }

        if (!string.IsNullOrWhiteSpace(_initialKeybind) && (KeyBox.Keybinds is null || KeyBox.Keybinds.Count == 0))
        {
            var parsed = Keybind.Parse(_initialKeybind);
            if (parsed.HasValue && (parsed.Value.Key != Key.None || parsed.Value.MouseButton != MouseButton.None))
            {
                KeyBox.Keybinds = [parsed.Value];
            }
        }

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.Favorites ??= new FavoritesViewModel(vm);
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

        SaveButton.AddHandler(Button.ClickEvent, ApplyButtonOnClick);
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                _ = ApplyButtonOnClick(sender, e);
                e.Handled = true;
                break;
            case Key.Escape:
                CancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
                break;
        }
    }

    private async Task ApplyButtonOnClick(object? sender, RoutedEventArgs e)
    {
        if (_isSaving)
        {
            return;
        }

        var path = PathBox.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _isSaving = true;
        try
        {
            var keybind = KeyBox.Keybinds is { Count: > 0 }
                ? KeyBox.Keybinds[0].GetFormattedForJSON()
                : KeyBox.Tags is { Count: > 0 } ? KeyBox.Tags[0] : string.Empty;

            var entry = new FavoriteEntry(path, keybind);

            if (DataContext is MainWindowViewModel vm)
            {
                vm.Favorites ??= new FavoritesViewModel(vm);
                await vm.Favorites.AddOrEditAsync(entry).ConfigureAwait(true);
            }
            else
            {
                await FavoritesManager.AddAsync(entry).ConfigureAwait(true);
            }

            await AnimatedClosing().ConfigureAwait(true);
        }
        finally
        {
            _isSaving = false;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Loaded -= OnLoaded;
        SaveButton.RemoveHandler(Button.ClickEvent, ApplyButtonOnClick);
        KeyDown -= OnKeyDown;
        _disposables.Dispose();
    }

    private void CloseMenu(object? sender, RoutedEventArgs e)
    {
        _ = AnimatedClosing();
    }
}

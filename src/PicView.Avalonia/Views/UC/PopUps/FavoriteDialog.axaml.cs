using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Input;
using PicView.Core.DebugTools;
using PicView.Core.Favorites;
using PicView.Core.Keybindings;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Views.UC.PopUps;

/// <summary>
/// Adds or edits a favorite. The KeyBox edits the Keybind of the favorite's slot; it is disabled when the
/// favorite has no slot, or when a new favorite would get none.
/// </summary>
public partial class FavoriteDialog : AnimatedPopUp
{
    private DisposableBag _disposables;
    private readonly string? _initialPath;
    private bool _isSaving;

    public FavoriteDialog(string? initialPath)
    {
        _initialPath = initialPath;
        InitializeComponent();

        if (!string.IsNullOrWhiteSpace(_initialPath))
        {
            PathBox.Text = _initialPath;
        }

        var slot1 = !string.IsNullOrWhiteSpace(initialPath) && FavoritesManager.Contains(initialPath)
            ? FavoritesManager.SlotOf(initialPath)
            : FavoritesManager.NextFreeSlot();

        if (slot1 is { } slot)
        {
            var keybinds = Keymaps.Current.KeybindsFor(KeybindCategoryDefinition.FavoriteAction(slot));
            if (keybinds.Count > 0)
            {
                KeyBox.Keybinds = [keybinds[0]];
            }
        }
        else
        {
            KeyBox.IsEnabled = false;
            KeyBox.Cursor = new Cursor(StandardCursorType.No);
        }

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_initialPath) && string.IsNullOrWhiteSpace(PathBox.Text))
        {
            PathBox.Text = _initialPath;
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
            int? slot;
            var favoritesVm = DataContext is MainWindowViewModel vm ? vm.Favorites ??= new FavoritesViewModel(vm) : null;
            if (favoritesVm is not null)
            {
                slot = await favoritesVm.AddFavorite(path).ConfigureAwait(true);
            }
            else
            {
                slot = await FavoritesManager.AddAsync(path).ConfigureAwait(true);
            }

            if (KeyBox.IsEnabled && slot is { } favoriteSlot)
            {
                Keybind? keybind = KeyBox.Keybinds is { Count: > 0 } ? KeyBox.Keybinds[0] : null;
                await Keymaps.SetFavoriteKeybindAsync(favoriteSlot, keybind).ConfigureAwait(true);

                // The favorites menu shows each favorite's Keybind
                favoritesVm?.UpdateFavorites();
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

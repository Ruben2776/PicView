using PicView.Core.Favorites;
using R3;

namespace PicView.Core.ViewModels;

public class FavoritesEntryViewModel : IDisposable 
{
    private MainWindowViewModel _vm = null!;
    public BindableReactiveProperty<string> FilePath { get; } = new();
    public BindableReactiveProperty<string> FolderName { get; } = new();
    public BindableReactiveProperty<string> HotKey { get; } = new();
    public BindableReactiveProperty<bool> IsCurrentItem { get; } = new(false);
    
    public ReactiveCommand<Unit> OpenCommand { get; } = new();
    public ReactiveCommand<Unit> RemoveCommand { get; } = new();
    public ReactiveCommand<Unit> OpenEditDialogCommand { get; } = new();
    
    public void Initialize(string path, string folderName, string hotKey, bool isCurrentItem, MainWindowViewModel vm)
    {
        _vm = vm;
        
        FilePath.Value = path;
        FolderName.Value = folderName;
        HotKey.Value = hotKey;
        IsCurrentItem.Value = isCurrentItem;

        OpenCommand.SubscribeAwait(Open);
        RemoveCommand.SubscribeAwait(Remove);
        OpenEditDialogCommand.Subscribe(Edit);
    }

    private void Edit(Unit unit)
    {
        if (_vm is null)
        {
            return;
        }

        (_vm.Favorites ??= new FavoritesViewModel(_vm)).EditFavoritesDialogCommand.Execute(
            new FavoriteEntry(FilePath.CurrentValue, HotKey.CurrentValue));
    }

    private async ValueTask Open(Unit unit, CancellationToken cancellationToken)
    {
        if (_vm.Mapper is not null)
        {
            await _vm.Mapper.OpenPathAsync(FilePath.CurrentValue).ConfigureAwait(false);
        }
    }

    private async ValueTask Remove(Unit unit, CancellationToken cancellationToken)
    {
        await FavoritesManager.RemoveAsync(FilePath.CurrentValue).ConfigureAwait(false);
        _vm.Favorites?.UpdateFavorites();
    }
    
    public void Dispose()
    {
        OpenCommand.Dispose();
        RemoveCommand.Dispose();
        OpenEditDialogCommand.Dispose();
        
        FilePath.Dispose();
        FolderName.Dispose();
        HotKey.Dispose();
        IsCurrentItem.Dispose();
        
        GC.SuppressFinalize(this);
    }
}

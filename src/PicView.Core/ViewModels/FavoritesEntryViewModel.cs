using PicView.Core.FileHistory;
using R3;

namespace PicView.Core.ViewModels;

public class FavoritesEntryViewModel : IDisposable 
{
    private MainWindowViewModel _vm = null!;
    public BindableReactiveProperty<string> FilePath { get; } = new();
    public BindableReactiveProperty<string> FolderName { get; } = new();
    public BindableReactiveProperty<string> HotKey { get; } = new();
    
    public ReactiveCommand<Unit> RemoveCommand { get; } = new();
    
    public void Initialize(string path, string folderName, string hotKey, MainWindowViewModel vm)
    {
        _vm = vm;
        
        FilePath.Value = path;
        FolderName.Value = folderName;
        HotKey.Value = hotKey;

        RemoveCommand.SubscribeAwait(Remove);
    }

    private async ValueTask Remove(Unit unit, CancellationToken cancellationToken)
    {
        await FavoritesManager.RemoveAsync(FilePath.CurrentValue).ConfigureAwait(false);
        _vm.Favorites?.UpdateFavorites();
    }
    
    public void Dispose()
    {
        RemoveCommand.Dispose();
        
        FilePath.Dispose();
        FolderName.Dispose();
        HotKey.Dispose();
        
        GC.SuppressFinalize(this);
    }
}

using PicView.Core.FileHistory;
using R3;

namespace PicView.Core.ViewModels;

public class FileHistoryEntryViewModel : IDisposable 
{
    private MainWindowViewModel _vm;
    public BindableReactiveProperty<string> FilePath { get; } = new();
    public BindableReactiveProperty<string> FileName { get; } = new();
    public BindableReactiveProperty<bool> IsPinned { get; } = new();
    public BindableReactiveProperty<bool> IsCurrentItem { get; } = new(false);
    public BindableReactiveProperty<int> Index { get;  } = new();
    
    public ReactiveCommand<Unit> PinCommand { get; } = new();
    public ReactiveCommand<Unit> UnpinCommand { get; } = new();
    public ReactiveCommand<Unit> RemoveCommand { get; } = new();
    
    public void Initialize(string path, string fileName, bool isPinned, bool isCurrentItem, int index, MainWindowViewModel vm)
    {
        _vm = vm;
        
        FilePath.Value = path;
        FileName.Value = fileName;
        IsPinned.Value = isPinned;
        IsCurrentItem.Value = isCurrentItem;
        Index.Value = index;

        PinCommand.Subscribe(Pin);
        UnpinCommand.Subscribe(Unpin);
        RemoveCommand.Subscribe(Remove);
    }

    private void Pin(Unit unit)
    {
        IsPinned.Value = true;
        FileHistoryManager.Pin(FilePath.CurrentValue);
        _vm.FileHistory.UpdateHistory();
    }

    private void Unpin(Unit unit)
    {
        IsPinned.Value = false;
        FileHistoryManager.UnPin(FilePath.CurrentValue);
        _vm.FileHistory.UpdateHistory();
    }

    private void Remove(Unit unit)
    {
        FileHistoryManager.Remove(FilePath.CurrentValue);
        _vm.FileHistory.UpdateHistory();
    }
    
    public void Dispose()
    {
        PinCommand.Dispose();
        UnpinCommand.Dispose();
        RemoveCommand.Dispose();
        
        FilePath.Dispose();
        FileName.Dispose();
        IsPinned.Dispose();
        IsCurrentItem.Dispose();
        Index.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
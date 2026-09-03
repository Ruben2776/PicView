using PicView.Avalonia.CustomControls;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

public static class FileSaver
{
    private static FileSavingService? _fileSaverService;

    public static async ValueTask<bool> SaveCurrentFile(MainWindowViewModel vm)
    {
        _fileSaverService ??= new FileSavingService();
        return await _fileSaverService.SaveCurrentFile(vm).ConfigureAwait(false);
    }
    
    public static async ValueTask<bool> SaveFileAs(MainWindowViewModel vm)
    {
        _fileSaverService ??= new FileSavingService();
        return await _fileSaverService.SaveFileAs(vm).ConfigureAwait(false);
    }
    
    public static async ValueTask<bool> SaveFileAsync(string? filename, string destination, MainWindowViewModel vm)
    {
        _fileSaverService ??= new FileSavingService();
        return await _fileSaverService.SaveFileAsync(filename, destination, vm).ConfigureAwait(false);
    }
}
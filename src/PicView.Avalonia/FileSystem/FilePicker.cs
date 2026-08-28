using PicView.Avalonia.CustomControls;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

public static class FilePicker
{
    private static FilePickerService? _filePickerService;
    public static async Task SelectAndLoadFile(MainWindow mainWindow, MainWindowViewModel vm)
    {
        _filePickerService ??= new FilePickerService();
        await _filePickerService.SelectAndLoadFile(mainWindow, vm).ConfigureAwait(false);
    }

    public static async Task<string?> SelectFile()
    {
        _filePickerService ??= new FilePickerService();
        return await _filePickerService.SelectFile().ConfigureAwait(false);
    }

    public static async ValueTask<bool> PickAndSaveFileAsAsync(string? fileName, MainWindowViewModel vm)
    {
        _filePickerService ??= new FilePickerService();
        return await _filePickerService.PickAndSaveFileAsAsync(fileName, vm).ConfigureAwait(false);
    }

    public static async Task<string?> PickFileForSavingAsync(string? fileName, string? ext = null)
    {
        _filePickerService ??= new FilePickerService();
        return await _filePickerService.PickFileForSavingAsync(fileName, ext).ConfigureAwait(false);
    }

    public static async Task<string> SelectDirectory()
    {
        _filePickerService ??= new FilePickerService();
        return await _filePickerService.SelectDirectory().ConfigureAwait(false);
    }
}
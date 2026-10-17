using Avalonia;
using Avalonia.Threading;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

public static class FileSaver
{
    private static FileSavingService? _fileSaverService;

    /// <inheritdoc cref="FileSavingService.SaveAsync(TabViewModel)" />
    public static async ValueTask<bool> SaveCurrentFile(MainWindowViewModel vm)
    {
        var service = await GetServiceAsync().ConfigureAwait(false);
        return await service.SaveAsync(vm.WindowTabs.ActiveTab.CurrentValue).ConfigureAwait(false);
    }

    /// <inheritdoc cref="FileSavingService.SaveAsAsync(TabViewModel)" />
    public static async ValueTask<bool> SaveFileAs(MainWindowViewModel vm)
    {
        var service = await GetServiceAsync().ConfigureAwait(false);
        return await service.SaveAsAsync(vm.WindowTabs.ActiveTab.CurrentValue).ConfigureAwait(false);
    }

    private static async ValueTask<FileSavingService> GetServiceAsync()
    {
        if (_fileSaverService is not null)
        {
            return _fileSaverService;
        }

        var core = Dispatcher.UIThread.CheckAccess()
            ? Application.Current?.DataContext as CoreViewModel
            : await Dispatcher.UIThread.InvokeAsync(() => Application.Current?.DataContext as CoreViewModel);
        return _fileSaverService = new FileSavingService(new FilePickerService(), () => core?.Effects);
    }
}

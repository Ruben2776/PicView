using Avalonia;
using Avalonia.Threading;
using PicView.Core.Saving;
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

    /// <inheritdoc cref="FileSavingService.ExportCommonFormatAsync(TabViewModel)" />
    public static async ValueTask<string?> ExportCommonFormat(MainWindowViewModel vm)
    {
        var service = await GetServiceAsync().ConfigureAwait(false);
        return await service.ExportCommonFormatAsync(vm.WindowTabs.ActiveTab.CurrentValue).ConfigureAwait(false);
    }

    private static async ValueTask<FileSavingService> GetServiceAsync()
    {
        if (_fileSaverService is not null)
        {
            return _fileSaverService;
        }

        var core = Dispatcher.UIThread.CheckAccess()
            ? Application.Current.DataContext as CoreViewModel
            : await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
        if (core is null)
        {
            // Not cached: a writer without the app's effects and Single Image pixels would stay after the app has them
            return new FileSavingService(new FilePickerService(), new ShownFileWriter(() => null));
        }

        return _fileSaverService = new FileSavingService(new FilePickerService(), core.Writer);
    }
}

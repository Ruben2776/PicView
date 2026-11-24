using Avalonia;
using Avalonia.Threading;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
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

    /// <summary>
    /// A file in a common format that shows the file at <paramref name="path"/>, such as a gallery item's, for a
    /// wallpaper or lock screen: the file itself when it is already common, else a temporary PNG, since the OS cannot
    /// show the others. <see langword="null"/> when it could not be written.
    /// </summary>
    public static async ValueTask<string?> ExportCommonFormat(string path)
    {
        if (path.IsCommon())
        {
            return path;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        return await SaveImageFileHelper.SaveImageAsync(null, path, tempPath, ext: ".png").ConfigureAwait(false)
            ? tempPath
            : null;
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

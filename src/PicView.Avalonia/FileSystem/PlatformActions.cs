using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

/// <summary>
/// The Actions that ask the operating system about the Shown File or open a file with it, and the same requests for
/// a Gallery Item's file. Each does nothing when the window has no platform service, and chooses itself which thread
/// the work runs on.
/// </summary>
public static class PlatformActions
{
    public static ValueTask OpenWith(MainWindowViewModel vm) =>
        OpenWith(vm.Core.PlatformService, ShownPath(vm));

    public static ValueTask OpenInExplorer(MainWindowViewModel vm) =>
        OpenInExplorer(vm.Core.PlatformService, ShownPath(vm));

    /// <summary>
    /// Opens a file of the app itself, such as the settings file, with the program the platform chooses.
    /// </summary>
    public static ValueTask ShowFile(MainWindowViewModel vm, string path) =>
        ShowFile(vm.Core.PlatformService, path);

    /// <summary>
    /// Sets the Shown File as the wallpaper, as a save would write it. Says so when it could not be set.
    /// </summary>
    public static ValueTask SetAsWallpaper(MainWindowViewModel vm, WallpaperStyle style) =>
        SetExportedImage(vm, () => FileSaver.ExportCommonFormat(vm), Wallpaper(style));

    /// <summary>
    /// Sets the file at <paramref name="path"/>, such as a Gallery Item's, as the wallpaper: as shown when it is the
    /// Shown File, else as it is on disk when it is in a common format, else as a temporary PNG.
    /// </summary>
    public static async ValueTask SetAsWallpaper(MainWindowViewModel vm, string path, WallpaperStyle style) =>
        await SetExportedImage(vm, ExportFor(vm, path), Wallpaper(style)).ConfigureAwait(false);

    /// <summary>
    /// Sets the Shown File as the lock screen image, as a save would write it. Says so when it could not be set.
    /// </summary>
    public static async ValueTask SetAsLockScreen(MainWindowViewModel vm) =>
        await SetExportedImage(vm, () => FileSaver.ExportCommonFormat(vm), LockScreen).ConfigureAwait(false);

    /// <inheritdoc cref="SetAsWallpaper(MainWindowViewModel, string, WallpaperStyle)" path="/summary"/>
    /// <remarks>Here for the lock screen image.</remarks>
    public static async ValueTask SetAsLockScreen(MainWindowViewModel vm, string path) =>
        await SetExportedImage(vm, ExportFor(vm, path), LockScreen).ConfigureAwait(false);

    public static ValueTask ShowFileProperties(MainWindowViewModel vm)
    {
        var path = ShownPath(vm);
        return new ValueTask(Task.Run(() => FileManager.ShowFileProperties(path!)));
    }

    public static async ValueTask DeleteFile(MainWindowViewModel vm, MainWindow mainWindow, bool recycle)
    {
        if (vm.Core.PlatformService is not { } platform)
        {
            return;
        }

        await FileManager.DeleteFileWithOptionalDialog(recycle, ShownPath(vm)!, platform, mainWindow)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Opens the About window from <paramref name="window"/> and checks for a newer version there.
    /// </summary>
    public static async ValueTask CheckForUpdates(MainWindow window)
    {
        await Dispatcher.UIThread.InvokeAsync(window.MainWindowInitializer.ShowAboutWindow);
        if (window.ViewModel?.Core.AboutView is not { } aboutView)
        {
            return;
        }

        await aboutView.UpdateCurrentVersion().ConfigureAwait(false);
    }

    internal static ValueTask OpenWith(IPlatformSpecificService? platform, string? path)
    {
        platform?.OpenWith(path!);
        return ValueTask.CompletedTask;
    }

    internal static ValueTask OpenInExplorer(IPlatformSpecificService? platform, string? path)
    {
        platform?.LocateOnDisk(path!);
        return ValueTask.CompletedTask;
    }

    internal static ValueTask ShowFile(IPlatformSpecificService? platform, string path) => OpenWith(platform, path);

    /// <summary>
    /// Asks the platform to set an image, for the wallpaper and the lock screen alike: claims
    /// <paramref name="tab"/> Busy, gets a file the OS can show from <paramref name="export"/>, hands it to
    /// <paramref name="set"/> and tells <paramref name="report"/> what to say when that did not work. A failure is
    /// reported once: the general error text when there is no file or the platform refuses, the exception's message
    /// when one is thrown.
    /// </summary>
    /// <remarks>Setting the lock screen is slow, so the platform is asked on the thread pool.</remarks>
    internal static async ValueTask SetExportedImage(TabViewModel tab, IPlatformSpecificService? platform,
        Func<ValueTask<string?>> export, Func<IPlatformSpecificService, string, Task<bool>> set,
        Action<string> report)
    {
        if (platform is null)
        {
            return;
        }

        using var busy = tab.ClaimBusy();
        try
        {
            var file = await export().ConfigureAwait(false);
            if (string.IsNullOrEmpty(file) || !await Task.Run(() => set(platform, file)).ConfigureAwait(false))
            {
                report(TranslationManager.Translation.UnexpectedError ?? "Error");
            }
        }
        catch (Exception e)
        {
            report(e.Message);
            DebugHelper.LogDebug(nameof(PlatformActions), nameof(SetExportedImage), e);
        }
    }

    /// <summary>
    /// The export for the file at <paramref name="path"/>: <paramref name="shown"/> when it is the tab's Shown File,
    /// so its effects, turn and flip are kept, else <paramref name="onDisk"/>.
    /// </summary>
    internal static Func<ValueTask<string?>> ExportFor(TabViewModel tab, string path,
        Func<ValueTask<string?>> shown, Func<string, ValueTask<string?>> onDisk) =>
        string.Equals(path, tab.FileInfo.CurrentValue?.FullName, StringComparison.Ordinal)
            ? shown
            : () => onDisk(path);

    /// <summary>The platform call that sets the wallpaper, which counts as set when it does not throw.</summary>
    internal static Func<IPlatformSpecificService, string, Task<bool>> Wallpaper(WallpaperStyle style) =>
        async (platform, file) =>
        {
            await platform.SetAsWallpaper(file, style).ConfigureAwait(false);
            return true;
        };

    private static async Task<bool> LockScreen(IPlatformSpecificService platform, string file) =>
        await platform.SetAsLockScreen(file).ConfigureAwait(false);

    private static Func<ValueTask<string?>> ExportFor(MainWindowViewModel vm, string path) =>
        ExportFor(vm.WindowTabs.ActiveTab.CurrentValue, path, () => FileSaver.ExportCommonFormat(vm),
            FileSaver.ExportCommonFormat);

    private static async ValueTask SetExportedImage(MainWindowViewModel vm, Func<ValueTask<string?>> export,
        Func<IPlatformSpecificService, string, Task<bool>> set) => 
        await SetExportedImage(vm.WindowTabs.ActiveTab.CurrentValue, vm.Core.PlatformService, export, set,
            message => TooltipHelper.ShowTooltipMessage(vm, message)).ConfigureAwait(false);

    private static string? ShownPath(MainWindowViewModel vm) =>
        vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo?.FullName;
}

using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

/// <summary>
/// The Actions that ask the operating system about the shown file or open a file with it. Each does nothing when the
/// window has no platform service, and chooses itself which thread the work runs on.
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

    public static ValueTask SetAsLockScreen(MainWindowViewModel vm) =>
        SetAsLockScreen(vm.Core.PlatformService, ShownPath(vm));

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

    /// <remarks>Setting the lock screen is slow, so it runs on the thread pool. With no file open there is nothing to set.</remarks>
    internal static async ValueTask SetAsLockScreen(IPlatformSpecificService? platform, string? path)
    {
        if (platform is null || path is null)
        {
            return;
        }

        await Task.Run(() => platform.SetAsLockScreen(path)).ConfigureAwait(false);
    }

    private static string? ShownPath(MainWindowViewModel vm) =>
        vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo?.FullName;
}

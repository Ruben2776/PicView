using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
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

    /// <summary>
    /// Sets the active tab's image as the lock screen image, as a save would write it. Says so when the platform
    /// refuses it.
    /// </summary>
    public static async ValueTask SetAsLockScreen(MainWindowViewModel vm)
    {
        using var busy = vm.WindowTabs.ActiveTab.CurrentValue.ClaimBusy();
        try
        {
            var file = await FileSaver.ExportCommonFormat(vm).ConfigureAwait(false);
            if (string.IsNullOrEmpty(file))
            {
                return;
            }

            if (!await SetAsLockScreen(vm.Core.PlatformService, file).ConfigureAwait(false))
            {
                TooltipHelper.ShowTooltipMessage(vm, TranslationManager.Translation.UnexpectedError);
            }
        }
        catch (Exception e)
        {
            TooltipHelper.ShowTooltipMessage(vm, e.Message);
            DebugHelper.LogDebug(nameof(PlatformActions), nameof(SetAsLockScreen), e);
        }
    }

    /// <summary>
    /// Sets the file at <paramref name="path"/>, such as a gallery item's, as the lock screen image: as it is when it is
    /// in a common format, else as a temporary PNG. Says so when it could not be converted or the platform refuses it.
    /// </summary>
    public static async ValueTask SetAsLockScreen(string path, MainWindowViewModel vm, MainWindow window)
    {
        using var busy = vm.WindowTabs.ActiveTab.CurrentValue.ClaimBusy();
        try
        {
            var file = await FileSaver.ExportCommonFormat(path).ConfigureAwait(false);
            if (file is null || !await SetAsLockScreen(vm.Core.PlatformService, file).ConfigureAwait(false))
            {
                await TooltipHelper.ShowTooltipMessageAsync(TranslationManager.Translation.UnexpectedError, false, window,
                    TimeSpan.FromSeconds(.5)).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            await TooltipHelper.ShowTooltipMessageAsync(e.Message, false, window,
                TimeSpan.FromSeconds(.5)).ConfigureAwait(false);
            DebugHelper.LogDebug(nameof(PlatformActions), nameof(SetAsLockScreen), e);
        }
    }

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

    /// <returns>Whether the platform set the image.</returns>
    /// <remarks>Setting the lock screen is slow, so it runs on the thread pool. With no file there is nothing to set.</remarks>
    internal static async ValueTask<bool> SetAsLockScreen(IPlatformSpecificService? platform, string? path)
    {
        if (platform is null || path is null)
        {
            return false;
        }
        var file = await FileSaver.ExportCommonFormat(path).ConfigureAwait(false);
        if (string.IsNullOrEmpty(file))
        {
            return false;
        }

        return await Task.Run(() => platform.SetAsLockScreen(path)).ConfigureAwait(false);
    }

    private static string? ShownPath(MainWindowViewModel vm) =>
        vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo?.FullName;
}

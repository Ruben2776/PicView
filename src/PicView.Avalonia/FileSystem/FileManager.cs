using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Core.DebugTools;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

public static class FileManager
{
    /// <summary>
    /// Deletes a specified file with an optional confirmation dialog based on the recycle flag.
    /// </summary>
    /// <param name="recycle">A flag indicating whether to recycle the file (if true) or delete it permanently (if false).</param>
    /// <param name="path">The fully qualified path to the file to be deleted.</param>
    /// <param name="platformService">The platform-specific service responsible for performing the file deletion operation.</param>
    /// <param name="mainWindow">The main window of the application.</param>
    public static async ValueTask DeleteFileWithOptionalDialog(bool recycle, string path,
        IPlatformSpecificService platformService, MainWindow mainWindow)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (recycle && Settings.UIProperties.ShowRecycleConfirmation ||
                !recycle && Settings.UIProperties.ShowPermanentDeletionConfirmation)
            {
               await Dispatcher.UIThread.InvokeAsync(ShowDeleteDialog);
            }
            else
            {
                var success = await platformService.DeleteFile(path, recycle);

                if (success)
                {
                    var msg = recycle
                        ? TranslationManager.Translation.SentFileToRecycleBin
                        : TranslationManager.Translation.DeletedFile;
                    TooltipHelper.ShowTooltipMessage(msg + Environment.NewLine + Path.GetFileName(path));
                }
                else if (File.Exists(path))
                {
                    TooltipHelper.ShowTooltipMessage(TranslationManager.Translation.UnexpectedError, true);
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileManager), nameof(DeleteFileWithOptionalDialog), ex);
        }

        return;

        void ShowDeleteDialog()
        {
            var prompt = recycle
                ? TranslationManager.Translation.DeleteFile
                : TranslationManager.Translation.DeleteFilePermanently;
            var deleteDialog = new DeleteDialog(prompt, path, recycle);
            mainWindow.UIHelper.GetMainView.MainPanel.Children.Add(deleteDialog);
            // Dialog handles the deletion
        }
    }

    /// <summary>
    /// Displays the properties of a specified file using the platform-specific service.
    /// </summary>
    /// <param name="path">The fully qualified path to the file whose properties need to be displayed.</param>
    public static void ShowFileProperties(string path)
    {
        var core = Dispatcher.UIThread.Invoke(() => Application.Current.DataContext as CoreViewModel);
        if (core is null)
        {
            return;
        }
        try
        {
            core.PlatformService.ShowFileProperties(path);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileManager), nameof(ShowFileProperties), ex);
        }
    }

    /// <summary>
    /// Locates the specified file or directory on the disk by highlighting it in the native file explorer.
    /// </summary>
    /// <param name="path">The path of the file or directory to be located on the disk.</param>
    public static async Task LocateOnDisk(string path)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        try
        {
            await Task.Run(() => core.PlatformService.LocateOnDisk(path));
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileManager), nameof(LocateOnDisk), ex);
        }
    }

    /// <summary>
    /// Opens the file located at the specified path with an application chosen by the user.
    /// </summary>
    /// <param name="path">The full file path of the file to be opened.</param>
    public static async Task OpenWith(string path)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        try
        {
            await Task.Run(() => core.PlatformService!.OpenWith(path));
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileManager), nameof(OpenWith), ex);
        }
    }
}
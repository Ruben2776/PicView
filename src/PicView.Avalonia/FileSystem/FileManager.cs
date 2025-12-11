using System.Runtime.InteropServices;
using Avalonia.Threading;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Core.DebugTools;
using PicView.Core.Localization;

namespace PicView.Avalonia.FileSystem;

public static class FileManager
{
    /// <summary>
    ///     Deletes the current file, either permanently or by moving to recycle bin
    /// </summary>
    public static async Task DeleteFileWithOptionalDialog(bool recycle, string path, IPlatformSpecificService platformService)
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
            LogAndShowError(ex, nameof(DeleteFileWithOptionalDialog));
        }

        return;

        void ShowDeleteDialog()
        {
            var prompt = recycle
                ? TranslationManager.Translation.DeleteFile
                : TranslationManager.Translation.DeleteFilePermanently;
            var deleteDialog = new DeleteDialog(prompt, path, recycle);
            UIHelper.GetMainView.MainGrid.Children.Add(deleteDialog);
            // Dialog handles the deletion
        }
    }

    /// <summary>
    ///     Shows properties dialog for the specified file
    /// </summary>
    public static async Task ShowFileProperties(string path, MainViewModel vm)
    {
        if (!ValidateParameters(path, vm.PlatformService))
        {
            return;
        }

        try
        {
            await Task.Run(() => vm.PlatformService!.ShowFileProperties(path));
        }
        catch (Exception ex)
        {
            LogAndShowError(ex, nameof(ShowFileProperties));
        }
    }

    /// <summary>
    ///     Prints the specified image file
    /// </summary>
    public static async Task Print(string? path, MainViewModel vm)
    {
        try
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = true;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await Task.Run(() => vm.PlatformService.Print(path));
            }
            else
            {
                // TODO: Refactor this for Windows
                var file = await ImageFormatConverter.ConvertToCommonSupportedFormatAsync(path, vm)
                    .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(file))
                {
                    TooltipHelper.ShowTooltipMessage(TranslationManager.Translation.UnexpectedError);
                    return;
                }

                await Task.Run(() => vm.PlatformService.Print(file));
            }
        }
        catch (Exception ex)
        {
            LogAndShowError(ex, nameof(Print));
        }
        finally
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }

    /// <summary>
    ///     Opens the file location in file explorer
    /// </summary>
    public static async Task LocateOnDisk(string path, MainViewModel vm)
    {
        if (!ValidateParameters(path, vm.PlatformService))
        {
            return;
        }

        try
        {
            await Task.Run(() => vm.PlatformService!.LocateOnDisk(path));
        }
        catch (Exception ex)
        {
            LogAndShowError(ex, nameof(LocateOnDisk));
        }
    }

    /// <summary>
    ///     Shows the dialog to open the file with another application
    /// </summary>
    public static async Task OpenWith(string path, MainViewModel vm)
    {
        if (!ValidateParameters(path, vm.PlatformService))
        {
            return;
        }

        try
        {
            await Task.Run(() => vm.PlatformService!.OpenWith(path));
        }
        catch (Exception ex)
        {
            LogAndShowError(ex, nameof(LocateOnDisk));
        }
    }

    #region Private Helper Methods

    /// <summary>
    ///     Validates common parameters for file operations
    /// </summary>
    private static bool ValidateParameters(string? path, object? platformService)
    {
        return !string.IsNullOrWhiteSpace(path) && platformService != null;
    }

    /// <summary>
    ///     Logs errors and shows appropriate error messages
    /// </summary>
    private static void LogAndShowError(Exception ex, string methodName)
    {
        DebugHelper.LogDebug(nameof(FileManager), methodName, ex);
        TooltipHelper.ShowTooltipMessage(ex.Message);
    }

    #endregion
}
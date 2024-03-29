﻿using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.FileHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Sizing;
using static PicView.WPF.ChangeImage.Navigation;

namespace PicView.WPF.ChangeImage;

internal static class ErrorHandling
{
    /// <summary>
    /// Check if index is valid and user is intended to change image
    /// </summary>
    /// <returns>True if not intended to change image or index is not valid</returns>
    internal static bool CheckOutOfRange()
    {
        if (Pics is null)
        {
            return true;
        }

        if (Pics.Count < FolderIndex || Pics.Count <= 0)
        {
            return true;
        }
        var value = true;
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Send, (Action)(() =>
        {
            value = UC.GetCroppingTool is { IsVisible: true } || (UC.GetQuickResize?.Opacity > 0);
        }));
        return value;
    }

    internal static void UnexpectedError()
    {
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
        {
            Unload(true);
            Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("UnexpectedError"), true, TimeSpan.FromSeconds(5));
            ConfigureWindows.GetMainWindow.Title =
                TranslationHelper.GetTranslation("UnexpectedError") + " - PicView";
            ConfigureWindows.GetMainWindow.TitleText.Text =
                TranslationHelper.GetTranslation("UnexpectedError");
            ConfigureWindows.GetMainWindow.TitleText.ToolTip =
                TranslationHelper.GetTranslation("UnexpectedError");
            ConfigureWindows.GetMainWindow.MainImage.Cursor = Cursors.Arrow;

            if (UC.GetSpinWaiter is { IsVisible: true })
            {
                UC.GetSpinWaiter.Visibility = Visibility.Collapsed;
            }
        });
    }

    /// <summary>
    /// Clears data, to free objects no longer necessary to store in memory and allow changing folder without error.
    /// </summary>
    internal static void ChangeFolder()
    {
        if (Pics?.Count > 0 && Pics.Count > FolderIndex)
        {
            // Make a backup of xPicPath and FolderIndex
            if (!string.IsNullOrWhiteSpace(Pics[FolderIndex]))
            {
                BackupPath = Pics[FolderIndex];
            }
        }

        Pics.Clear();
        GalleryFunctions.Clear();
        PreLoader.Clear();
        FileDeletionHelper.DeleteTempFiles();
    }

    /// <summary>
    /// Refresh the current list of pics and reload them if there is some missing or changes.
    /// </summary>
    internal static async Task ReloadAsync(bool fromBackup = false)
    {
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(SetTitle.SetLoadingString);
        try
        {
            string path;
            if (SettingsHelper.Settings.Sorting.IncludeSubDirectories)
            {
                path = GetReloadPath() ?? BackupPath ?? string.Empty;
            }
            else
            {
                path = (fromBackup ? BackupPath ?? null : GetReloadPath()) ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                UnexpectedError();
            }
            else if (File.Exists(path))
            {
                if (SettingsHelper.Settings.Sorting.IncludeSubDirectories)
                {
                    var fileInfo = new FileInfo(Path.GetDirectoryName(path));
                    await ResetValues(fileInfo).ConfigureAwait(false);
                    await LoadPic.LoadPicAtIndexAsync(Pics.IndexOf(path), fileInfo).ConfigureAwait(false);
                }
                else
                {
                    var fileInfo = new FileInfo(path);
                    await ResetValues(fileInfo).ConfigureAwait(false);
                    await LoadPic.LoadPiFromFileAsync(null, fileInfo).ConfigureAwait(false);
                }

                if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
                {
                    var check = false;
                    await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                    {
                        check = UC.GetPicGallery?.Container.Children.Count <= 0;
                    });
                    if (check)
                    {
                        await GalleryLoad.ReloadGalleryAsync().ConfigureAwait(false);
                    }
                }
            }
            else if (Directory.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                await ResetValues(fileInfo).ConfigureAwait(false);
                await LoadPic.LoadPicFromFolderAsync(fileInfo, FolderIndex).ConfigureAwait(false);
            }
            else if (Base64Helper.IsBase64String(path))
            {
                await UpdateImage.UpdateImageFromBase64PicAsync(new FileInfo(path)).ConfigureAwait(false);
            }
            else if (Clipboard.ContainsImage())
            {
                await UpdateImage
                    .UpdateImageAsync(TranslationHelper.GetTranslation("ClipboardImage"), Clipboard.GetImage())
                    .ConfigureAwait(false);
            }
            else if (Uri.IsWellFormedUriString(path, UriKind.Absolute)) // Check if from web
            {
                await HttpFunctions.LoadPicFromUrlAsync(path).ConfigureAwait(false);
            }
            else
            {
                UnexpectedError();
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(ReloadAsync)} exception:\n{ex.Message}");
#endif
            Tooltip.ShowTooltipMessage(ex.Message, true, TimeSpan.FromSeconds(5));
        }
    }

    internal static string? GetReloadPath()
    {
        if (CheckOutOfRange())
        {
            return ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                var fileName = Path.GetFileName(ConfigureWindows.GetMainWindow.TitleText.Text);
                return fileName == TranslationHelper.GetTranslation("Loading") ? BackupPath : fileName;
            });
        }

        if (!string.IsNullOrWhiteSpace(BackupPath) && SettingsHelper.Settings.Sorting.IncludeSubDirectories
                                                    && Path.GetDirectoryName(BackupPath) !=
                                                    Path.GetDirectoryName(Pics[FolderIndex]))
        {
            return BackupPath;
        }

        return Pics[FolderIndex];
    }

    private static async Task ResetValues(FileInfo fileInfo)
    {
        PreLoader.Clear();
        Pics = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);

        var containerCheck = false;

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (UC.GetPicGallery?.Container?.Children?.Count > 0)
            {
                containerCheck = true;
            }
        });

        if (ConfigureWindows.GetImageInfoWindow is not null)
        {
            await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);
        }

        if (containerCheck)
        {
            GalleryFunctions.Clear();
            await GalleryLoad.LoadAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reset to default state
    /// </summary>
    internal static void Unload(bool showStartup)
    {
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke((Action)(() =>
        {
            ConfigureWindows.GetMainWindow.MainImage.Source = null;
            ConfigureWindows.GetMainWindow.MainImage.Width = 0;
            ConfigureWindows.GetMainWindow.MainImage.Height = 0;

            WindowSizing.SetWindowBehavior();

            if (showStartup == false)
            {
                ResetTitle();
            }

            UC.ToggleStartUpUC(!showStartup);
            if (UC.GetSpinWaiter is not null)
            {
                UC.GetSpinWaiter.Visibility = Visibility.Collapsed;
            }

            if (UC.GetPicGallery is not null)
            {
                UC.GetPicGallery.Visibility = Visibility.Collapsed;
            }
        }));

        Pics?.Clear();

        PreLoader.Clear();
        GalleryFunctions.Clear();
        ScaleImage.XWidth = ScaleImage.XHeight = 0;

        if (!string.IsNullOrWhiteSpace(ArchiveHelper.TempFilePath))
        {
            FileDeletionHelper.DeleteTempFiles();
            ArchiveHelper.TempFilePath = string.Empty;
        }

        Taskbar.NoProgress();
    }

    internal static void ResetTitle()
    {
        ConfigureWindows.GetMainWindow.TitleText.ToolTip = ConfigureWindows.GetMainWindow.TitleText.Text =
            TranslationHelper.GetTranslation("NoImage");
        ConfigureWindows.GetMainWindow.Title =
            TranslationHelper.GetTranslation("NoImage") + " - PicVIew";
    }
}
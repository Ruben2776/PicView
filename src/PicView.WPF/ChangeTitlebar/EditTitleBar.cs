using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.WPF.FileHandling;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Sizing;
using System.IO;
using System.Windows.Input;
using static PicView.WPF.ChangeImage.Navigation;

namespace PicView.WPF.ChangeTitlebar;

internal static class EditTitleBar
{
    private static string? _backupTitle;

    internal static void Bar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ConfigureWindows.GetMainWindow.TitleText.IsFocused)
        {
            return;
        }

        try
        {
            WindowSizing.Move(sender, e);
        }
        catch (Exception)
        {
            //
        }
        Refocus();
        e.Handled = true; // Disable text clicking
    }

    internal static void Bar_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Pics == null || !SettingsHelper.Settings.UIProperties.ShowInterface || Pics.Count == 0)
        {
            e.Handled = true; // Disable text clicking
        }
    }

    internal static void EditTitleBar_Text()
    {
        if (Pics == null || !SettingsHelper.Settings.UIProperties.ShowInterface || Pics.Count == 0)
        {
            return;
        }

        if (!ConfigureWindows.GetMainWindow.TitleText.IsFocused)
        {
            ConfigureWindows.GetMainWindow.TitleText.InnerTextBox.Focus();
            SelectFileName();
        }
        else
        {
            Refocus();
        }
    }

    /// <summary>
    /// Set the text to the filename
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal static void EditTitleBar_Text(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (Pics == null || !SettingsHelper.Settings.UIProperties.ShowInterface || Pics.Count == 0)
        {
            return;
        }

        e.Handled = true;

        _backupTitle = ConfigureWindows.GetMainWindow.TitleText.Text;
        ConfigureWindows.GetMainWindow.TitleText.Text = Pics[FolderIndex];
    }

    internal static void SelectFileName()
    {
        var filename = Path.GetFileName(Pics[FolderIndex]);
        var start = Pics[FolderIndex].Length - filename.Length;
        var end = Path.GetFileNameWithoutExtension(filename).Length;
        ConfigureWindows.GetMainWindow.TitleText.InnerTextBox.Select(start, end);
    }

    internal static async Task HandleRename()
    {
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => { Refocus(false); });

        if (Pics == null)
        {
            return;
        }

        var success = await FileFunctions.RenameFileWithErrorChecking(ConfigureWindows.GetMainWindow.TitleText.Text)
            .ConfigureAwait(false);
        if (success.HasValue == false)
        {
            Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("AnErrorOccuredMovingFile"));
        }

        await ImageInfo.UpdateValuesAsync(new FileInfo(Pics?[FolderIndex])).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes focus from titlebar and sets it to the mainwindow
    /// </summary>
    internal static void Refocus(bool backup = true)
    {
        if (!ConfigureWindows.GetMainWindow.TitleText.IsFocused)
        {
            return;
        }

        FocusManager.SetFocusedElement(FocusManager.GetFocusScope(ConfigureWindows.GetMainWindow.TitleText), null);
        Keyboard.ClearFocus();
        ConfigureWindows.GetMainWindow.Focus();

        if (!backup || _backupTitle == null) return;
        ConfigureWindows.GetMainWindow.TitleText.Text = _backupTitle;
        _backupTitle = string.Empty;
    }
}
using PicView.FileHandling;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;

namespace PicView.ChangeTitlebar
{
    internal static class EditTitleBar
    {
        private static string? backupTitle;

        internal static void Bar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ConfigureWindows.GetMainWindow.TitleText.IsFocused)
            {
                return;
            }

            WindowSizing.Move(sender, e);
            Refocus();
            e.Handled = true; // Disable text clicking
        }

        internal static void Bar_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Pics == null || !Settings.Default.ShowInterface || Pics.Count == 0)
            {
                e.Handled = true; // Disable text clicking
            }
        }

        internal static void EditTitleBar_Text()
        {
            if (Pics == null || !Settings.Default.ShowInterface || Pics.Count == 0)
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
            if (Pics == null || !Settings.Default.ShowInterface || Pics.Count == 0)
            {
                return;
            }

            e.Handled = true;

            backupTitle = ConfigureWindows.GetMainWindow.TitleText.Text;
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
            if (Pics == null)
            {
                return;
            }

            var success = await FileFunctions.RenameFileWithErrorChecking(ConfigureWindows.GetMainWindow.TitleText.Text).ConfigureAwait(false);
            if (success.HasValue == false)
            {
                Tooltip.ShowTooltipMessage(Application.Current.Resources["AnErrorOccuredMovingFile"]);
            }
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                Refocus(false);
            });

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

            if (backup && backupTitle != null)
            {
                ConfigureWindows.GetMainWindow.TitleText.Text = backupTitle;
                backupTitle = string.Empty;
            }
        }
    }
}
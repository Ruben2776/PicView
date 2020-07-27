using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.UILogic.Loading;
using PicView.UILogic.Sizing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;

namespace PicView.UILogic
{
    internal static class EditTitleBar
    {
        private static string backupTitle;

        internal static void Bar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (LoadWindows.GetMainWindow.TitleText.IsFocused)
            {
                return;
            }

            WindowLogic.Move(sender, e);
            Refocus();
            e.Handled = true; // Disable text clicking
        }

        internal static void Bar_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Pics == null || !Properties.Settings.Default.ShowInterface || Pics.Count == 0)
            {
                e.Handled = true; // Disable text clicking
            }
        }

        internal static void EditTitleBar_Text()
        {
            if (Pics == null || !Properties.Settings.Default.ShowInterface || Pics.Count == 0)
            {
                return;
            }
            if (!LoadWindows.GetMainWindow.TitleText.IsFocused)
            {
                LoadWindows.GetMainWindow.TitleText.InnerTextBox.Focus();
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
            if (Pics == null || !Properties.Settings.Default.ShowInterface || Pics.Count == 0)
            {
                return;
            }

            e.Handled = true;

            backupTitle = LoadWindows.GetMainWindow.TitleText.Text;
            LoadWindows.GetMainWindow.TitleText.Text = Pics[FolderIndex];
        }

        internal static void SelectFileName()
        {
            var filename = Path.GetFileName(Pics[FolderIndex]);
            var start = Pics[FolderIndex].Length - filename.Length;
            var end = Path.GetFileNameWithoutExtension(filename).Length;
            LoadWindows.GetMainWindow.TitleText.InnerTextBox.Select(start, end);
        }

        internal static void HandleRename()
        {
            if (FileFunctions.RenameFile(Pics[FolderIndex], LoadWindows.GetMainWindow.TitleText.Text))
            {
                Pics[FolderIndex] = LoadWindows.GetMainWindow.TitleText.Text;
                Refocus();
                Error_Handling.Reload(); // TODO proper renaming of window title, tooltip, etc.
            }
            else
            {
                Tooltip.ShowTooltipMessage(Application.Current.Resources["AnErrorOccuredMovingFile"]);
                Refocus();
            }
        }

        /// <summary>
        /// Removes focus from titlebar and sets it to the mainwindow
        /// </summary>
        internal static void Refocus()
        {
            if (!LoadWindows.GetMainWindow.TitleText.IsFocused)
            {
                return;
            }

            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(LoadWindows.GetMainWindow.TitleText), null);
            Keyboard.ClearFocus();
            LoadWindows.GetMainWindow.Focus();

            LoadWindows.GetMainWindow.TitleText.Text = backupTitle;
            backupTitle = string.Empty;
        }
    }
}
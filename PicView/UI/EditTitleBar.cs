using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.UI.Sizing;
using System.IO;
using System.Windows.Input;
using static PicView.Library.Fields;

namespace PicView.UI
{
    internal static class EditTitleBar
    {
        private static string backupTitle;

        internal static void Bar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!mainWindow.Bar.IsFocused)
            {
                if (e.ClickCount == 2)
                {
                    WindowLogic.Maximize_Restore();
                }

                e.Handled = true;
            }
        }

        internal static void EditTitleBar_Text()
        {
            if (Pics == null || !Properties.Settings.Default.ShowInterface || Pics.Count == 0)
            {
                return;
            }
            if (!mainWindow.Bar.IsFocused)
            {
                mainWindow.Bar.Bar.Focus();
                SelectFileName();
            }
            else
            {
                Refocus();
            }
        }

        internal static void EditTitleBar_Text(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Pics == null || !Properties.Settings.Default.ShowInterface || Pics.Count == 0)
            {
                return;
            }

            e.Handled = true;

            backupTitle = mainWindow.Bar.Text;
            mainWindow.Bar.Text = Pics[FolderIndex];
        }

        internal static void SelectFileName()
        {
            var filename = Path.GetFileName(Pics[FolderIndex]);
            var start = Pics[FolderIndex].Length - filename.Length;
            var end = Path.GetFileNameWithoutExtension(filename).Length;
            mainWindow.Bar.Bar.Select(start, end);
        }

        internal static void HandleRename()
        {
            if (FileFunctions.RenameFile(Pics[FolderIndex], mainWindow.Bar.Text))
            {
                Pics[FolderIndex] = mainWindow.Bar.Text;
                Refocus();
                Error_Handling.Reload(); // TODO proper renaming of window title, tooltip, etc.
            }
            else
            {
                Tooltip.ShowTooltipMessage("An error occured moving file");
                Refocus();
            }
        }

        internal static void Refocus()
        {
            if (!mainWindow.Bar.IsFocused)
            {
                return;
            }

            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(mainWindow.Bar), null);
            Keyboard.ClearFocus();
            mainWindow.Focus();

            mainWindow.Bar.Text = backupTitle;
            backupTitle = string.Empty;
        }
    }
}
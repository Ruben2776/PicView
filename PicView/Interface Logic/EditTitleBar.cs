using System.IO;
using System.Windows.Input;
using static PicView.Fields;

namespace PicView
{
    internal static class EditTitleBar
    {
        private static string backupTitle;

        internal static void Bar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Disable focus when unloaded
            if (Pics.Count == 0)
            {
                e.Handled = true;
            }
        }

        internal static void EditTitleBar_Text()
        {
            if (!Properties.Settings.Default.ShowInterface)
            {
                return;
            }
            if (!mainWindow.Bar.IsFocused)
            {
                mainWindow.Bar.Bar.Focus();
            }
            else
            {
                Refocus();
            }
        }

        internal static void EditTitleBar_Text(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Pics == null || !Properties.Settings.Default.ShowInterface)
            {
                return;
            }

            e.Handled = true;

            var absolutePath = Pics[FolderIndex];

            backupTitle = mainWindow.Bar.Text;
            mainWindow.Bar.Text = absolutePath;

            var filename = Path.GetFileName(absolutePath);
            var start = absolutePath.Length - filename.Length;
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
                Tooltip.ToolTipStyle("An error occured moving file");
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


using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.UI.Sizing;
using System.IO;
using System.Windows.Input;
using static PicView.ChangeImage.Navigation;
using static PicView.Library.Fields;

namespace PicView.UI
{
    internal static class EditTitleBar
    {
        private static string backupTitle;

        internal static void Bar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TheMainWindow.Bar.IsFocused)
            {
                return;
            }

            WindowLogic.Move(sender, e);
            Refocus();
            e.Handled = true; // Disable text clicking
        }

        internal static void EditTitleBar_Text()
        {
            if (Pics == null || !Properties.Settings.Default.ShowInterface || Pics.Count == 0)
            {
                return;
            }
            if (!TheMainWindow.Bar.IsFocused)
            {
                TheMainWindow.Bar.Bar.Focus();
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

            backupTitle = TheMainWindow.Bar.Text;
            TheMainWindow.Bar.Text = Pics[FolderIndex];
        }

        internal static void SelectFileName()
        {
            var filename = Path.GetFileName(Pics[FolderIndex]);
            var start = Pics[FolderIndex].Length - filename.Length;
            var end = Path.GetFileNameWithoutExtension(filename).Length;
            TheMainWindow.Bar.Bar.Select(start, end);
        }

        internal static void HandleRename()
        {
            if (FileFunctions.RenameFile(Pics[FolderIndex], TheMainWindow.Bar.Text))
            {
                Pics[FolderIndex] = TheMainWindow.Bar.Text;
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
            if (!TheMainWindow.Bar.IsFocused)
            {
                return;
            }

            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(TheMainWindow.Bar), null);
            Keyboard.ClearFocus();
            TheMainWindow.Focus();

            TheMainWindow.Bar.Text = backupTitle;
            backupTitle = string.Empty;
        }
    }
}
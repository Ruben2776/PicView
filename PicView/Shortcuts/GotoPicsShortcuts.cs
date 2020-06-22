using System.Globalization;
using System.Windows.Input;
using static PicView.ChangeImage.GoToLogic;
using static PicView.Library.Fields;
using static PicView.UI.UserControls.UC;

namespace PicView.Shortcuts
{
    internal static class GotoPicsShortcuts
    {
        internal static void GoToPicPreviewKeys(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D0:
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                case Key.NumPad0:
                case Key.NumPad1:
                case Key.NumPad2:
                case Key.NumPad3:
                case Key.NumPad4:
                case Key.NumPad5:
                case Key.NumPad6:
                case Key.NumPad7:
                case Key.NumPad8:
                case Key.NumPad9:
                case Key.Back:
                case Key.Delete:
                    break;  // Allow these keys
                case Key.A:
                case Key.C:
                case Key.X:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        // Allow Ctrl + A, Ctrl + C, Ctrl + X
                        break;
                    }
                    else
                    {
                        e.Handled = true;// only allowed on ctrl
                        return;
                    }
                case Key.Escape: // Escape logic
                    quickSettingsMenu.GoToPicBox.Text = FolderIndex.ToString(CultureInfo.CurrentCulture);
                    ClearGoTo();
                    break;

                case Key.Enter: // Execute it!
                     GoToPicEvent(sender, e);
                     ClearGoTo();
                    break;

                default:
                    e.Handled = true; // Don't allow other keys
                    break;
            }
        }
    }
}
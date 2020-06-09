using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.Fields;
using static PicView.GoToLogic;
using static PicView.UC;

namespace PicView
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
                    break;
                case Key.Escape:
                    quickSettingsMenu.GoToPicBox.Text = FolderIndex.ToString(CultureInfo.CurrentCulture);
                    quickSettingsMenu.GoToPicBox.CaretBrush = new SolidColorBrush(Colors.Transparent);
                    Close_UserControls();
                    Keyboard.ClearFocus();
                    mainWindow.Focus();
                    break;
                case Key.Enter:
                    GoToPicEvent(sender, e);
                    break;
                default: // Don't allow other keys
                    e.Handled = true;
                    break;
            }
        }
    } 
}


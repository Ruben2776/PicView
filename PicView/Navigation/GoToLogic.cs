using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.Fields;
using static PicView.UC;

namespace PicView
{
    internal static class GoToLogic
    {
        internal static void GoToPicEvent(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(quickSettingsMenu.GoToPicBox.Text.ToString(), out int x))
            {
                x--;
                x = x <= 0 ? 0 : x;
                x = x >= Pics.Count ? Pics.Count - 1 : x;
                Navigation.Pic(x);
                quickSettingsMenu.GoToPicBox.Text = (x + 1).ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                quickSettingsMenu.GoToPicBox.Text = FolderIndex.ToString(CultureInfo.CurrentCulture);
                // TODO add error message or something..
            }

        }

        internal static void ClearGoTo()
        {
            quickSettingsMenu.GoToPicBox.CaretBrush = new SolidColorBrush(Colors.Transparent);
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(quickSettingsMenu.GoToPicBox), null);
            Close_UserControls();
            Keyboard.ClearFocus();
            mainWindow.Focus();
            AjaxLoader.AjaxLoadingEnd();
        }
    }
}

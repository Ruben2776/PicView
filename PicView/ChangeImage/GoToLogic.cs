using PicView.Library;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static PicView.ChangeImage.Navigation;
using static PicView.UI.UserControls.UC;

namespace PicView.ChangeImage
{
    internal static class GoToLogic
    {
        internal static async void GoToPicEvent(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(GetQuickSettingsMenu.GoToPicBox.Text.ToString(), out int x))
            {
                x--;
                x = x <= 0 ? 0 : x;
                x = x >= Pics.Count ? Pics.Count - 1 : x;
                Pic(x);
                await Fields.TheMainWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    GetQuickSettingsMenu.GoToPicBox.Text = (x + 1).ToString(CultureInfo.CurrentCulture);
                }));
            }
            else if (Pics.Count > 0 && Pics.Count > FolderIndex)
            {
                GetQuickSettingsMenu.GoToPicBox.Text = FolderIndex.ToString(CultureInfo.CurrentCulture);
            }
        }

        internal static void ClearGoTo()
        {
            GetQuickSettingsMenu.GoToPicBox.CaretBrush = new SolidColorBrush(Colors.Transparent);
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(GetQuickSettingsMenu.GoToPicBox), null);
            Close_UserControls();
            Keyboard.ClearFocus();
            Fields.TheMainWindow.Focus();
        }
    }
}
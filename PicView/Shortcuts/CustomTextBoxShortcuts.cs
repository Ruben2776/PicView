using PicView.UILogic;
using System.Windows.Input;

namespace PicView
{
    internal static class CustomTextBoxShortcuts
    {
        internal static void CustomTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EditTitleBar.HandleRename();
            }
            else if (e.Key == Key.Escape)
            {
                EditTitleBar.Refocus();
                FileHandling.Open_Save.IsDialogOpen = true; // Hack to make escape not fall through
            }
        }
    }
}
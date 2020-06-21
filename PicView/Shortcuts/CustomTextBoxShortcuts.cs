using PicView.UI;
using System.Windows.Input;
using static PicView.Library.Fields;

namespace PicView
{
    internal static class CustomTextBoxShortcuts
    {
        internal static void CustomTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    EditTitleBar.HandleRename();
                    break;

                case Key.Escape:
                    EditTitleBar.Refocus();
                    IsDialogOpen = true; // Hack to make escape not fall through
                    break;
            }
        }
    }
}
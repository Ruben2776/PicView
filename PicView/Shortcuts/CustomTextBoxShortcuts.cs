using PicView.UI;
using System.Windows.Input;
using static PicView.Library.Fields;

namespace PicView
{
    internal static class CustomTextBoxShortcuts
    {
        internal static async System.Threading.Tasks.Task CustomTextBox_KeyDownAsync(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    await EditTitleBar.HandleRenameAsync().ConfigureAwait(false);
                    break;

                case Key.Escape:
                    EditTitleBar.Refocus();
                    IsDialogOpen = true; // Hack to make escape not fall through
                    break;
            }
        }
    }
}
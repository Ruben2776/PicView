using System.Threading.Tasks;
using System.Windows.Input;
using PicView.FileHandling;
using PicView.UILogic;

namespace PicView
{
    internal static class CustomTextBoxShortcuts
    {
        internal static async Task CustomTextBox_KeyDownAsync(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await EditTitleBar.HandleRename().ConfigureAwait(false);
            }
            else if (e.Key == Key.Escape)
            {
                EditTitleBar.Refocus();
                Open_Save.IsDialogOpen = true; // Hack to make escape not fall through
            }
        }
    }
}
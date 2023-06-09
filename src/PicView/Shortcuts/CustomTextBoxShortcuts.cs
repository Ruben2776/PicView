using PicView.ChangeTitlebar;
using PicView.FileHandling;
using System.Windows.Input;

namespace PicView.Shortcuts;

internal static class CustomTextBoxShortcuts
{
    internal static async Task CustomTextBox_KeyDownAsync(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await EditTitleBar.HandleRename().ConfigureAwait(false);
        }
        else if (e.Key == Key.Escape)
        {
            EditTitleBar.Refocus();
            OpenSave.IsDialogOpen = true; // Make escape not fall through
        }
    }
}
using PicView.Avalonia.CustomControls;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class DeleteDialog : AnimatedPopUp
{
    /// <param name="delete">Deletes the file and reports it, run when the user confirms.</param>
    public DeleteDialog(string prompt, string file, bool recycle, Func<Task> delete)
    {
        InitializeComponent();
        ConfirmButtonText.Text = recycle
            ? TranslationManager.Translation.MoveToRecycleBin
            : TranslationManager.Translation.DeleteFile;

        Loaded += delegate
        {
            PromptText.Text = prompt;
            PromptFileName.Text = Path.GetFileName(file) + "?";
            CancelButton.Click += async delegate { await AnimatedClosing().ConfigureAwait(true); };
            ConfirmButton.Click += async delegate
            {
                await delete().ConfigureAwait(true);
                await AnimatedClosing().ConfigureAwait(true);
            };

            Focus();
        };
    }
}
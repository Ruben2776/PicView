using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class DeleteDialog : AnimatedPopUp
{
    public DeleteDialog(string prompt, string file)
    {
        InitializeComponent();
        Loaded += delegate
        {
            PromptText.Text = prompt;
            PromptFileName.Text = Path.GetFileName(file) + "?";
            CancelButton.Click += async delegate { await AnimatedClosing(); };
            ConfirmButton.Click += async delegate
            {
                FileDeletionHelper.DeleteFileWithErrorMsg(file, false);
                await AnimatedClosing();
            };

            Focus();

            KeyDown += (_, e) =>
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        ConfirmButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        break;
                    case Key.Escape:
                        CancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        break;
                }

                e.Handled = true;
            };
        };
    }
}

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class DeleteDialog : AnimatedPopUp
{
    public DeleteDialog(string prompt, string file, bool recycle)
    {
        InitializeComponent();
        ConfirmButtonText.Text = recycle
            ? TranslationManager.Translation.MoveToRecycleBin
            : TranslationManager.Translation.DeleteFile;

        KeyChanged += OnKeyChanged;

        Loaded += delegate
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            PromptText.Text = prompt;
            PromptFileName.Text = Path.GetFileName(file) + "?";
            CancelButton.Click += async delegate { await AnimatedClosing(); };
            ConfirmButton.Click += async delegate
            {
                await vm.PlatformService.DeleteFile(file, recycle);
                await AnimatedClosing();
            };

            Focus();
        };
    }

    private void OnKeyChanged(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter)
        {
            ConfirmButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
    }
}
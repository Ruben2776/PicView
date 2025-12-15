using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;
using System.IO;

namespace PicView.Avalonia.Views.UC.PopUps;

public partial class SaveDialog : AnimatedPopUp
{
    private readonly TaskCompletionSource<bool> _tcs = new();

    // Expose this so callers can await when the dialog is finished
    public Task<bool> CloseTask => _tcs.Task;

    public SaveDialog(string prompt, string file)
    {
        InitializeComponent();
        ConfirmButtonText.Text = TranslationManager.Translation.Save;

        KeyChanged += OnKeyChanged;

        Loaded += delegate
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            PromptText.Text = prompt;
            PromptFileName.Text = Path.GetFileName(file) + "?";
            
            CancelButton.Click += async delegate 
            { 
                await NavigationManager.QuickReload();
                await vm.HistoryManager.SetHasChanges(false);

                // User chose "Cancel / Don't save"
                _tcs.TrySetResult(false);

                await AnimatedClosing(); 
            };

            ConfirmButton.Click += async delegate
            {
                await vm.PlatformService.SaveFile(file);
                await NavigationManager.QuickReload();
                await vm.HistoryManager.SetHasChanges(false);

                // User chose "Save"
                _tcs.TrySetResult(true);

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

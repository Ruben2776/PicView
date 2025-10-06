using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views.Main;

public partial class KeybindingsView : UserControl
{
    public KeybindingsView()
    {
        InitializeComponent();
        DefaultButton.Click += async delegate { await SetDefault(); };
        FullscreenBox.Text = $"{TranslationManager.Translation.Shift} + {TranslationManager.Translation.DoubleClick}";
        FullscreenBox.Text = $"{TranslationManager.Translation.Shift} + {TranslationManager.Translation.DoubleClick}";
        DragWindowBox.Text = $"{TranslationManager.Translation.Shift} + {TranslationManager.Translation.MouseDrag}";
        CloseBox.Text = TranslationManager.Translation.Esc;
        
        // Fix invisible text on macOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ApplicationShortcutsTextBlock.TextAlignment = TextAlignment.Left;
            ChangeKeybindingTextBlock.TextAlignment = TextAlignment.Left;
            NavigationTextBlock.TextAlignment = TextAlignment.Left;

            Loaded += delegate
            {
                ApplicationShortcutsTextBlock.TextAlignment = TextAlignment.Center;
                ChangeKeybindingTextBlock.TextAlignment = TextAlignment.Center;
                NavigationTextBlock.TextAlignment = TextAlignment.Center;
            };
        }
    }

    private async Task SetDefault()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        KeybindingManager.SetDefaultKeybindings(vm.PlatformService);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is not Window window)
            {
                return;
            }
            window.Close();
        });

        await FunctionsMapper.KeybindingsWindow();
    }
}
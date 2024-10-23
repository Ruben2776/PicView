using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views;

public partial class ShortcutsView : UserControl
{
    public ShortcutsView()
    {
        InitializeComponent();
        DefaultButton.Click += async delegate { await SetDefault(); };
        FullscreenBox.Text = $"{TranslationHelper.Translation.Shift} + {TranslationHelper.Translation.DoubleClick}";
        FullscreenBox.Text = $"{TranslationHelper.Translation.Shift} + {TranslationHelper.Translation.DoubleClick}";
        DragWindowBox.Text = $"{TranslationHelper.Translation.Shift} + {TranslationHelper.Translation.MouseDrag}";
        CloseBox.Text = TranslationHelper.Translation.Esc;
        
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
        
        await KeybindingManager.SetDefaultKeybindings(vm.PlatformService).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is not Window window)
            {
                return;
            }
            window.Close();
        });

        await FunctionsHelper.KeybindingsWindow();
    }
}
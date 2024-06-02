using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;
using System.Runtime.InteropServices;
using PicView.Avalonia.Helpers;

namespace PicView.Avalonia.Views;

public partial class ShortcutsView : UserControl
{
    public ShortcutsView()
    {
        InitializeComponent();
        DefaultButton.Click += async delegate { await SetDefault(); };
        FullscreenBox.Text = $"{TranslationHelper.GetTranslation("Shift")} + {TranslationHelper.GetTranslation("DoubleClick")}";
        FullscreenBox.Text = $"{TranslationHelper.GetTranslation("Shift")} + {TranslationHelper.GetTranslation("DoubleClick")}";
        DragWindowBox.Text = $"{TranslationHelper.GetTranslation("Shift")} + {TranslationHelper.GetTranslation("MouseDrag")}";
        CloseBox.Text = TranslationHelper.GetTranslation("Esc");
    }

    private async Task SetDefault()
    {
        await KeybindingsHelper.SetDefaultKeybindings().ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is not Window window)
            {
                return;
            }
            window.Close();
        });
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        await FunctionsHelper.KeybindingsWindow();
    }
}
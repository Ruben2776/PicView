using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;
using System.Runtime.InteropServices;

namespace PicView.Avalonia.Views;

public partial class ShortcutsView : UserControl
{
    public ShortcutsView()
    {
        InitializeComponent();
        DefaultButton.Click += async delegate { await SetDefault(); };
        var alt = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            "Option" : TranslationHelper.GetTranslation("Alt");
        NextBox.KeyUp += delegate
        {
            LastImageBox.Text = LastImageBox.GetFunctionKey();
            LastImageAltBox.Text = LastImageAltBox.GetFunctionKey();
            NextFolderBox.Text = NextFolderBox.GetFunctionKey();
            NextFolderAltBox.Text = NextFolderAltBox.GetFunctionKey();
        };
        PrevBox.KeyUp += delegate
        {
            FirstImageBox.Text = FirstImageBox.GetFunctionKey();
            FirstImageAltBox.Text = FirstImageAltBox.GetFunctionKey();
            PrevFolderBox.Text = PrevFolderBox.GetFunctionKey();
            PrevFolderAltBox.Text = PrevFolderAltBox.GetFunctionKey();
        };
        FullscreenBox1.Text = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            $"Option + {TranslationHelper.GetTranslation("Enter")}" :
            $"{TranslationHelper.GetTranslation("Alt")} + {TranslationHelper.GetTranslation("Enter")}";
        FullscreenBox2.Text = $"{TranslationHelper.GetTranslation("Shift")} + {TranslationHelper.GetTranslation("DoubleClick")}";
        OpenBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + O";
        ReloadBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + R";
        SaveBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + S";
        CopyBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + C";
        CopyFilePathBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + {alt} + C";
        CopyImageBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + {TranslationHelper.GetTranslation("Shift")} + C";
        PasteBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + V";
        CutBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + X";
        DeleteBox.Text = $"{TranslationHelper.GetTranslation("Del")}";
        DeleteAltBox.Text = $"{TranslationHelper.GetTranslation("Shift")} +  {TranslationHelper.GetTranslation("Del")}";
        PrintBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + P";
        ToggleUIBox.Text = $"{alt} + Z";
        FullscreenBox2.Text = $"{TranslationHelper.GetTranslation("Shift")} + {TranslationHelper.GetTranslation("DoubleClick")}";
        DragWindowBox.Text = $"{TranslationHelper.GetTranslation("Shift")} + {TranslationHelper.GetTranslation("MouseDrag")}";
        CloseBox.Text = TranslationHelper.GetTranslation("Esc");
        CloseAltBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + Q";
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
        vm.ShowKeybindingsWindowCommand.Execute(null);
    }
}
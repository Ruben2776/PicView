using Avalonia.Controls;
using System.Runtime.InteropServices;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views;

public partial class ShortcutsView : UserControl
{
    public ShortcutsView()
    {
        InitializeComponent();
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
        //PasteBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + V";
        DeleteBox.Text = $"{TranslationHelper.GetTranslation("Del")}";
        DeleteAltBox.Text = $"{TranslationHelper.GetTranslation("Shift")} + {TranslationHelper.GetTranslation("Del")}";
        PrintBox.Text = $"{TranslationHelper.GetTranslation("Ctrl")} + P";
        ToggleUIBox.Text = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            $"Option + Z" :
            $"{TranslationHelper.GetTranslation("Alt")} + Z";
        FullscreenBox2.Text = $"{TranslationHelper.GetTranslation("Shift")} + {TranslationHelper.GetTranslation("DoubleClick")}";
    }
}
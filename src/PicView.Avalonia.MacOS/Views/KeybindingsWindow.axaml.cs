using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.MacOS.Views;

public partial class KeybindingsWindow : GenericWindow
{
    public KeybindingsWindow(KeybindingWindowConfig config)
    {
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.ApplicationShortcuts, false, config.WindowProperties);
        KeyUp += (_, e) => KeybindingsView.Controller?.HandleKeyPressed(e);
    }
}
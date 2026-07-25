using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Linux.Views;

public partial class KeybindingsWindow : GenericWindow
{
    public KeybindingsWindow(KeybindingWindowConfig config)
    {
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.ApplicationShortcuts, true, config.WindowProperties);
    }
}
using Avalonia.Media;
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
        if (Settings.Theme.GlassTheme)
        {
            WindowBorder.Background = Brushes.Transparent;
            XKeybindingsView.Background = Brushes.Transparent;
        }
        else if (!Settings.Theme.Dark)
        {
            XKeybindingsView.Background = UIHelper.GetMenuBackgroundColor();
        }
        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.ApplicationShortcuts, true, config.WindowProperties);
        MinWidth = MaxWidth = 618;
    }
}
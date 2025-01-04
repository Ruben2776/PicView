using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.MacOS.Views;

public partial class EffectsWindow : Window
{
    public EffectsWindow()
    {
        InitializeComponent();
        if (!SettingsHelper.Settings.Theme.Dark || SettingsHelper.Settings.Theme.GlassTheme)
        {
            XEffectsView.Background = Brushes.Transparent;
        }
        Loaded += delegate
        {
            MinWidth = MaxWidth = Width;
            Title = $"{TranslationHelper.Translation.Effects} - PicView";
        };
        KeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape)
            {
                Close();
            }
        };
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }
}
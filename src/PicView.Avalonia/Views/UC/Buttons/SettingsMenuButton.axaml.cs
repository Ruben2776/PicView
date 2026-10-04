using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using PicView.Core.Keybindings;

namespace PicView.Avalonia.Views.UC.Buttons;

public partial class SettingsMenuButton : UserControl
{
    public SettingsMenuButton()
    {
        InitializeComponent();
        ToolTip.SetTip(UserSettingsItem, CurrentSettingsPath);
        ToolTip.SetTip(KeybindingsItem, KeybindingFunctions.CurrentKeybindingsPath);
        
        Loaded += OnLoaded;

        if (OperatingSystem.IsMacOS())
        {
            SettingsButton.CornerRadius = new CornerRadius(6,10,0,6);
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
        {
            SettingsButton.Background = Brushes.Transparent;
            SettingsButton.Classes.Remove("noBorderHover");
            SettingsButton.Classes.Add("hover");
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        Loaded -= OnLoaded;
    }
}
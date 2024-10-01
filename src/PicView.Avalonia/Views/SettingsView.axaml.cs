using Avalonia.Controls;

namespace PicView.Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            MainTabControl.MinHeight = MainTabControl.Bounds.Height;
        };
    }
}
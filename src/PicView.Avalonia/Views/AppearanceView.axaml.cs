using Avalonia.Controls;
using Avalonia.Interactivity;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.PicViewTheme;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;

public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        Loaded += AppearanceView_Loaded;
    }

    private void AppearanceView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        GalleryStretchMode.DetermineStretchMode(vm);
        
        ThemeBox.SelectedItem = SettingsHelper.Settings.Theme.Dark ? DarkThemeBox : LightThemeBox;
        ThemeBox.SelectionChanged += delegate
        {
            ThemeManager.SetTheme(ThemeBox.SelectedIndex == 0);
        };
        
        
    }
}
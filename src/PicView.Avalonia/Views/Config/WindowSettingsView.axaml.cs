using Avalonia.Controls;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views.Config;

public partial class WindowSettingsView : UserControl
{
    public WindowSettingsView()
    {
        InitializeComponent();

        Loaded += delegate
        {
            WindowMaximizeModeBox.SelectedIndex = Settings.WindowProperties.WindowMaximizeMode;
        
            WindowMaximizeModeBox.SelectionChanged += async delegate
            {
                if (WindowMaximizeModeBox.SelectedIndex == -1) return;
                
                Settings.WindowProperties.WindowMaximizeMode = WindowMaximizeModeBox.SelectedIndex;

                if (DataContext is MainViewModel vm)
                    vm.GlobalSettings.WindowMaximizeMode.Value = WindowMaximizeModeBox.SelectedIndex;

                await SaveSettingsAsync();
            };
            
            WindowMaximizeModeBox.DropDownOpened += delegate
            {
                if (WindowMaximizeModeBox.SelectedIndex == -1)
                    WindowMaximizeModeBox.SelectedIndex = Settings.WindowProperties.WindowMaximizeMode;
            };
        };
    }
}

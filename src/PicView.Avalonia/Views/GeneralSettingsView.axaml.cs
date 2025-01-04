using Avalonia.Controls;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;

public partial class GeneralSettingsView : UserControl
{
    public GeneralSettingsView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            ApplicationStartupBox.SelectedIndex = SettingsHelper.Settings.StartUp.OpenLastFile ? 1 : 0;
            
            ApplicationStartupBox.SelectionChanged += async delegate
            {
                if (ApplicationStartupBox.SelectedIndex == -1)
                {
                    return;
                }
                SettingsHelper.Settings.StartUp.OpenLastFile = ApplicationStartupBox.SelectedIndex == 1;
                await SettingsHelper.SaveSettingsAsync();
            };
            ApplicationStartupBox.DropDownOpened += delegate
            {
                if (ApplicationStartupBox.SelectedIndex == -1)
                {
                    ApplicationStartupBox.SelectedIndex = SettingsHelper.Settings.StartUp.OpenLastFile ? 0 : 1;
                }
            };
        };
    }
}
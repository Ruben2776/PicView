using Avalonia.Controls;

namespace PicView.Avalonia.Views.Config;

public partial class GeneralSettingsView : UserControl
{
    public GeneralSettingsView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            ApplicationStartupBox.SelectedIndex = Settings.StartUp.OpenLastFile ? 1 : 0;

            ApplicationStartupBox.SelectionChanged += async delegate
            {
                if (ApplicationStartupBox.SelectedIndex == -1)
                {
                    return;
                }

                Settings.StartUp.OpenLastFile = ApplicationStartupBox.SelectedIndex == 1;
                await SaveSettingsAsync();
            };
            ApplicationStartupBox.DropDownOpened += delegate
            {
                if (ApplicationStartupBox.SelectedIndex == -1)
                {
                    ApplicationStartupBox.SelectedIndex = Settings.StartUp.OpenLastFile ? 0 : 1;
                }
            };


            DeletingFileBox.SelectedIndex = Settings.Navigation.IsNavigatingBackwardsWhenDeleting ? 1 : 0;

            DeletingFileBox.SelectionChanged += async delegate
            {
                if (DeletingFileBox.SelectedIndex == -1)
                {
                    return;
                }

                Settings.Navigation.IsNavigatingBackwardsWhenDeleting = DeletingFileBox.SelectedIndex == 1;
                await SaveSettingsAsync();
            };
            DeletingFileBox.DropDownOpened += delegate
            {
                if (DeletingFileBox.SelectedIndex == -1)
                {
                    DeletingFileBox.SelectedIndex = Settings.Navigation.IsNavigatingBackwardsWhenDeleting ? 0 : 1;
                }
            };
        };
    }
}
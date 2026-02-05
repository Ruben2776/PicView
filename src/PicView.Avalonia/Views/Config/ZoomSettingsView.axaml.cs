using Avalonia.Controls;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views.Config;

public partial class ZoomSettingsView : UserControl
{
    public ZoomSettingsView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            MouseWheelBox.SelectedIndex = Settings.Zoom.CtrlZoom ? 0 : 1;

            MouseWheelBox.SelectionChanged += async delegate
            {
                if (MouseWheelBox.SelectedIndex == -1)
                {
                    return;
                }

                Settings.Zoom.CtrlZoom = MouseWheelBox.SelectedIndex == 0;
                await SaveSettingsAsync();
            };
            MouseWheelBox.DropDownOpened += delegate
            {
                if (MouseWheelBox.SelectedIndex == -1)
                {
                    MouseWheelBox.SelectedIndex = Settings.Zoom.CtrlZoom ? 0 : 1;
                }
            };
            IsShowingZoomPreviewerToggleButton.IsCheckedChanged += delegate
            {
                if (DataContext is not MainViewModel vm)
                {
                    return;
                }

                if (!IsShowingZoomPreviewerToggleButton.IsChecked.HasValue)
                {
                    return;
                }

                vm.MainWindow.IsZoomPreviewerVisible.Value = IsShowingZoomPreviewerToggleButton.IsChecked.Value;
            };
        };
    }
}

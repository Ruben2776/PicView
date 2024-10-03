using Avalonia.Controls;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;

public partial class ZoomSettingsView : UserControl
{
    public ZoomSettingsView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            MouseWheelBox.SelectedIndex = SettingsHelper.Settings.Zoom.CtrlZoom ? 0 : 1;

            MouseWheelBox.SelectionChanged += async delegate
            {
                if (MouseWheelBox.SelectedIndex == -1)
                {
                    return;
                }

                SettingsHelper.Settings.Zoom.CtrlZoom = MouseWheelBox.SelectedIndex == 0;
                await SettingsHelper.SaveSettingsAsync();
            };
            MouseWheelBox.DropDownOpened += delegate
            {
                if (MouseWheelBox.SelectedIndex == -1)
                {
                    MouseWheelBox.SelectedIndex = SettingsHelper.Settings.Zoom.CtrlZoom ? 0 : 1;
                }
            };
        };
    }
}

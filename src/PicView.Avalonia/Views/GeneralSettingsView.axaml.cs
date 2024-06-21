using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;

public partial class GeneralSettingsView : UserControl
{
    public GeneralSettingsView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            ApplicationStartupBox.SelectionChanged += (sender, args) =>
            {
                SettingsHelper.Settings.StartUp.OpenLastFile = ApplicationStartupBox.SelectedIndex == 1;
                _ = SettingsHelper.SaveSettingsAsync();
            };
            MouseWheelBox.SelectionChanged += (sender, args) =>
            {
                SettingsHelper.Settings.Zoom.CtrlZoom = MouseWheelBox.SelectedIndex == 0;
                _ = SettingsHelper.SaveSettingsAsync();
            };
            ScrollDirectionBox.SelectionChanged += (sender, args) =>
            {
                SettingsHelper.Settings.Zoom.HorizontalReverseScroll = ScrollDirectionBox.SelectedIndex == 0;
                _ = SettingsHelper.SaveSettingsAsync();
            };
            MouseWheelBox.SelectedIndex = SettingsHelper.Settings.Zoom.CtrlZoom ? 0 : 1;
            ScrollDirectionBox.SelectedIndex = SettingsHelper.Settings.Zoom.HorizontalReverseScroll ? 0 : 1;
            ApplicationStartupBox.SelectedIndex = SettingsHelper.Settings.StartUp.OpenLastFile ? 1 : 0;
        };
    }
}
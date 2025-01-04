using Avalonia.Controls;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;
    public partial class ImageSettingsView : UserControl
    {
        public ImageSettingsView()
        {
            InitializeComponent();
            Loaded += delegate
            {
                ImageAliasingBox.SelectedIndex = SettingsHelper.Settings.ImageScaling.IsScalingSetToNearestNeighbor ? 1 : 0;
            
                ImageAliasingBox.SelectionChanged += async delegate
                {
                    if (ImageAliasingBox.SelectedIndex == -1)
                    {
                        return;
                    }
                    SettingsHelper.Settings.ImageScaling.IsScalingSetToNearestNeighbor = ImageAliasingBox.SelectedIndex == 1;
                    if (DataContext is MainViewModel vm)
                    {
                        vm.ImageViewer.TriggerScalingModeUpdate(true);
                    }
                    await SettingsHelper.SaveSettingsAsync();
                };
                ImageAliasingBox.DropDownOpened += delegate
                {
                    if (ImageAliasingBox.SelectedIndex == -1)
                    {
                        ImageAliasingBox.SelectedIndex = SettingsHelper.Settings.ImageScaling.IsScalingSetToNearestNeighbor ? 0 : 1;
                    }
                };
            };
        }
    }

using Avalonia.Controls;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;
    public partial class MouseWheelView : UserControl
    {
        public MouseWheelView()
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
            
            ScrollDirectionBox.SelectedIndex = SettingsHelper.Settings.Zoom.HorizontalReverseScroll ? 0 : 1;
            
            ScrollDirectionBox.SelectionChanged += async delegate
            {
                if (ScrollDirectionBox.SelectedIndex == -1)
                {
                    return;
                }
                SettingsHelper.Settings.Zoom.HorizontalReverseScroll = ScrollDirectionBox.SelectedIndex == 0;
                await SettingsHelper.SaveSettingsAsync();
            };
            ScrollDirectionBox.DropDownOpened += delegate
            {
                if (ScrollDirectionBox.SelectedIndex == -1)
                {
                    ScrollDirectionBox.SelectedIndex = SettingsHelper.Settings.Zoom.HorizontalReverseScroll ? 0 : 1;
                }
            };
        }
    }

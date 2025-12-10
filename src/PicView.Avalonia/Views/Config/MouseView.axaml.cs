using Avalonia.Controls;

namespace PicView.Avalonia.Views.Config;
    public partial class MouseView : UserControl
    {
        public MouseView()
        {
            InitializeComponent();
            Loaded += delegate
            {
                DoubleClickBox.SelectedIndex = Settings.UIProperties.DoubleClickBehavior switch
                {
                    1 => 0,
                    2 => 1,
                    _ => DoubleClickBox.Items.Count - 1
                };

                DoubleClickBox.SelectionChanged += async delegate
                {
                    Settings.UIProperties.DoubleClickBehavior = DoubleClickBox.SelectedIndex switch
                    {
                        0 => 1,
                        2 => 2,
                        _ => 0
                    };

                    await SaveSettingsAsync();
                };

                if (Settings.Navigation.IsNavigatingFileHistory)
                {
                    MouseSideButtonBox.SelectedIndex = 0;
                }
                else if (Settings.Navigation.IsNavigatingBetweenDirectories)
                {
                    MouseSideButtonBox.SelectedIndex = 1;
                }
                else
                {
                    MouseSideButtonBox.SelectedIndex = 2;
                }
                
                MouseSideButtonBox.SelectionChanged += async delegate
                {
                    if (MouseSideButtonBox.SelectedIndex == -1)
                    {
                        return;
                    }

                    switch (MouseSideButtonBox.SelectedIndex)
                    {
                        case 0:
                            Settings.Navigation.IsNavigatingFileHistory = true;
                            Settings.Navigation.IsNavigatingBetweenDirectories = false;
                            break;
                        case 1:
                            Settings.Navigation.IsNavigatingBetweenDirectories = true;
                            Settings.Navigation.IsNavigatingFileHistory = false;
                            break;
                        case 2:
                            Settings.Navigation.IsNavigatingFileHistory = false;
                            Settings.Navigation.IsNavigatingBetweenDirectories = false;
                            break;
                        
                    }
                    await SaveSettingsAsync();
                };
                
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
            };
            
            ScrollDirectionBox.SelectedIndex = Settings.Zoom.HorizontalReverseScroll ? 0 : 1;
            
            ScrollDirectionBox.SelectionChanged += async delegate
            {
                if (ScrollDirectionBox.SelectedIndex == -1)
                {
                    return;
                }
                Settings.Zoom.HorizontalReverseScroll = ScrollDirectionBox.SelectedIndex == 0;
                await SaveSettingsAsync();
            };
            ScrollDirectionBox.DropDownOpened += delegate
            {
                if (ScrollDirectionBox.SelectedIndex == -1)
                {
                    ScrollDirectionBox.SelectedIndex = Settings.Zoom.HorizontalReverseScroll ? 0 : 1;
                }
            };
        }
    }

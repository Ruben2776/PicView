using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views.UC.Buttons;
public partial class ClickArrowRight : UserControl
{
    public ClickArrowRight()
    {
        InitializeComponent();
        Loaded += delegate
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            PointerWheelChanged += async (_, e) => await ImageViewer.PreviewOnPointerWheelChanged(this, e);
            AddHandler(PointerPressedEvent, ManagePointerPressed, RoutingStrategies.Tunnel);
            PolyButton.Click += (_, _) =>
            {
                vm.MainWindow.IsClickArrowRightClicked = true;
                UIHelper.SetButtonInterval(PolyButton);
            };
        };
    }

    private void ManagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.Properties;

        if (PolyButton.IsPointerOver)
        {
            if (props.IsRightButtonPressed)
            {
                DialogManager.AddNavigationDialog();
            }
        }
        else
        {
            if (props.IsRightButtonPressed)
            {
                UIHelper.ShowMainContextMenu();
            }
        }
    }
}

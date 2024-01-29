using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace PicView.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Loaded += delegate
        {
            var fileMenu = new UC.Menus.FileMenu
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 168, 0),
                Opacity = 1
            };

            MainGrid.Children.Add(fileMenu);
        };
    }
}
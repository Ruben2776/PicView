using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using System.Reactive.Concurrency;

namespace PicView.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Loaded += delegate
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                var fileMenu = new UC.Menus.FileMenu()
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 168, 0),
                    Opacity = 1
                };

                MainGrid.Children.Add(fileMenu);

                var imageMenu = new UC.Menus.ImageMenu
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 65, 0),
                    Opacity = 1
                };

                MainGrid.Children.Add(imageMenu);

                var settingsMenu = new UC.Menus.SettingsMenu
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(220, 0, 0, 0),
                    Opacity = 1
                };

                MainGrid.Children.Add(settingsMenu);

                var toolsMenu = new UC.Menus.ToolsMenu()
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(240, 0, 0, 0),
                    Opacity = 1
                };

                MainGrid.Children.Add(toolsMenu);
            });
        };
    }
}
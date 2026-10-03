using Avalonia.Controls;
using Avalonia.Interactivity;
using PicView.Avalonia.Controllers;

namespace PicView.Avalonia.Views.Main;

public partial class ImageInfoView : UserControl
{
    public ImageInfoViewController? Controller;

    public ImageInfoView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Controller = new ImageInfoViewController(this);
        Controller.Initialize();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Controller?.Dispose();
        Controller = null;
    }
}


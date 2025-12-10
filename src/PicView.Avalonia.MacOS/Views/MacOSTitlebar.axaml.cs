using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;

namespace PicView.Avalonia.MacOS.Views;

public partial class MacOSTitlebar : UserControl
{
    private RotationContextMenu? _rotationContextMenu;
    
    public MacOSTitlebar()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (Settings.Theme.GlassTheme)
            {
                TopWindowBorder.Background = Brushes.Transparent;

                EditableTitlebar.Background = Brushes.Transparent;
                EditableTitlebar.BorderThickness = new Thickness(0);

                FlipButton.Background = Brushes.Transparent;
                FlipButton.BorderThickness = new Thickness(0);

                GalleryButton.Background = Brushes.Transparent;
                GalleryButton.BorderThickness = new Thickness(0);

                RotateRightButton.Background = Brushes.Transparent;
                RotateRightButton.BorderThickness = new Thickness(0);
                
                var brush = UIHelper.GetBrush("SecondaryTextColor");
                EditableTitlebar.Foreground = brush;
                FlipButton.Foreground = brush;
                GalleryButton.Foreground = brush;
                RotateRightButton.Foreground = brush;
            }

            _rotationContextMenu = new RotationContextMenu();
            _rotationContextMenu.UpdateSubscription();
            FlipButton.ContextMenu = _rotationContextMenu;
            RotateRightButton.ContextMenu = _rotationContextMenu;
            
            FlipButton.PointerPressed += (_, e) => { OpenContextMenu(e); };
            RotateRightButton.AddHandler(PointerPressedEvent, RotateRightButtonHandler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);
        };
        PointerPressed += (_, e) => MoveWindow(e);
    }

    private void RotateRightButtonHandler(object? sender, PointerPressedEventArgs e)
    {
        if (e.Properties.IsLeftButtonPressed)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.MainWindow.IsTitlebarRotationClicked = true;
            }
        }
        else if (e.Properties.IsRightButtonPressed)
        {
            OpenContextMenu(e);
        }
    }


    private void OpenContextMenu(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            return;
        }

        // Context menu doesn't want to be opened normally
        _rotationContextMenu.Open();
    }

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null || DataContext is not MainViewModel vm)
        {
            return;
        }

        var hostWindow = (Window)VisualRoot;
        WindowFunctions.WindowDragAndDoubleClickBehavior(hostWindow, e, vm.PlatformWindowService);
    }
}
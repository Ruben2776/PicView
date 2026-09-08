using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.Controllers;

namespace PicView.Avalonia.Views.Main;

public partial class SettingsView : UserControl
{
    private SettingsSearchController? _controller;

    public SettingsView()
    {
        InitializeComponent();
        
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            FileAssociationsListBoxItem.IsVisible = FileAssociationsListBoxItem.IsVisible = false;
            FileAssociationsSection.IsVisible = false;
        }
        
        _controller = new SettingsSearchController(this);
        _controller.Initialize();

        ContentScrollViewer.ScrollChanged += OnScrollChanged;
        KeyDown += OnKeyDown;
        MainPanel.PointerPressed += MainPanelOnPointerPressed;
        ContentScrollViewer.PointerPressed += MainPanelOnPointerPressed;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ContentScrollViewer.ScrollChanged -= OnScrollChanged;
        KeyDown -= OnKeyDown;
        MainPanel.PointerPressed -= MainPanelOnPointerPressed;
        ContentScrollViewer.PointerPressed -= MainPanelOnPointerPressed;

        _controller?.Dispose();
        _controller = null;
    }
    
    private void MainPanelOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _controller?.ResetFilters();
        _controller?.ClosePopup();

        if (!e.Properties.IsRightButtonPressed)
        {
            return;
        }
        if (Resources.TryGetValue("SettingsContextMenu", out var value) && value is ContextMenu contextMenu)
        {
            contextMenu.Open();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _controller?.HandleKeyDown(e);
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        _controller?.HandleScrollChanged(ContentScrollViewer.Offset.Y, ContentPanel.Spacing * 2);
    }

    private void CloseItem_OnClick(object? sender, RoutedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (TopLevel.GetTopLevel(this) is not Window window)
            {
                return;
            }
            window.Close();
        });
    }
}

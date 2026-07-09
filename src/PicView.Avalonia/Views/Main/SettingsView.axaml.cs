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

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            FileAssociationsListBoxItem.IsVisible = FileAssociationsListBoxItem.IsVisible = false;
        }
        
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _controller = new SettingsSearchController(this);
        _controller.Initialize();

        ContentScrollViewer.ScrollChanged += OnScrollChanged;
        KeyDown += OnKeyDown;
        MainPanel.PointerPressed += MainPanelOnPointerPressed;
        ContentScrollViewer.PointerPressed += MainPanelOnPointerPressed;

        if (!Settings.Theme.Dark)
        {
            SetLightTheme();
        }
    }

    private void SetLightTheme()
    {
        GeneralSection.DeletingToggleButton.Classes.Remove("altHover");
        GeneralSection.DeletingToggleButton.Classes.Add("hover");
        
        GeneralSection.RecyclingToggleButton.Classes.Remove("altHover");
        GeneralSection.RecyclingToggleButton.Classes.Add("hover");
        
        GeneralSection.SubdirectoriesToggleButton.Classes.Remove("altHover");
        GeneralSection.SubdirectoriesToggleButton.Classes.Add("hover");
        
        GeneralSection.FileHistoryToggleButton.Classes.Remove("altHover");
        GeneralSection.FileHistoryToggleButton.Classes.Add("hover");
        
        AppearanceSection.ConstrainingBackgroundColorToggleButton.Classes.Remove("altHover");
        AppearanceSection.ConstrainingBackgroundColorToggleButton.Classes.Add("hover");

        InterfaceSection.ShowBottomToolbarButton.Classes.Remove("altHover");
        InterfaceSection.ShowBottomToolbarButton.Classes.Add("hover");
        
        InterfaceSection.ShowUIButton.Classes.Remove("altHover");
        InterfaceSection.ShowUIButton.Classes.Add("hover");
        
        InterfaceSection.HideUIButton.Classes.Remove("altHover");
        InterfaceSection.HideUIButton.Classes.Add("hover");
        
        InterfaceSection.ToggleFadeUIButton.Classes.Remove("altHover");
        InterfaceSection.ToggleFadeUIButton.Classes.Add("hover");
                
        InterfaceSection.ToggleHoverButton.Classes.Remove("altHover");
        InterfaceSection.ToggleHoverButton.Classes.Add("hover");
        
        ImageSection.ScrollToggleButton.Classes.Remove("altHover");
        ImageSection.ScrollToggleButton.Classes.Add("hover");
        
        ImageSection.SideBySideToggleButton.Classes.Remove("altHover");
        ImageSection.SideBySideToggleButton.Classes.Add("hover");
        
        WindowSection.AutoFitToggleButton.Classes.Remove("altHover");
        WindowSection.AutoFitToggleButton.Classes.Add("hover");
        
        WindowSection.TopMostToggleButton.Classes.Remove("altHover");
        WindowSection.TopMostToggleButton.Classes.Add("hover");
        
        WindowSection.StayCenteredToggleButton.Classes.Remove("altHover");
        WindowSection.StayCenteredToggleButton.Classes.Add("hover");
        
        WindowSection.OpenInSameWindowToggleButton.Classes.Remove("altHover");
        WindowSection.OpenInSameWindowToggleButton.Classes.Add("hover");
        
        WindowSection.ShowConfirmationEscToggleButton.Classes.Remove("altHover");
        WindowSection.ShowConfirmationEscToggleButton.Classes.Add("hover");
        
        GallerySection.ShowBottomDockedGalleryButton.Classes.Remove("altHover");
        GallerySection.ShowBottomDockedGalleryButton.Classes.Add("hover");
        
        GallerySection.ShowTopDockedGalleryButton.Classes.Remove("altHover");
        GallerySection.ShowTopDockedGalleryButton.Classes.Add("hover");
        
        GallerySection.ShowLeftDockedGalleryButton.Classes.Remove("altHover");
        GallerySection.ShowLeftDockedGalleryButton.Classes.Add("hover");
        
        GallerySection.ShowRightDockedGalleryButton.Classes.Remove("altHover");
        GallerySection.ShowRightDockedGalleryButton.Classes.Add("hover");
        
        GallerySection.CloseDockedGalleryButton.Classes.Remove("altHover");
        GallerySection.CloseDockedGalleryButton.Classes.Add("hover");
        
        GallerySection.UIHiddenDockedGalleryToggleButton.Classes.Remove("altHover");
        GallerySection.UIHiddenDockedGalleryToggleButton.Classes.Add("hover");
        
        NavigationSection.LoopingToggleButton.Classes.Remove("altHover");
        NavigationSection.LoopingToggleButton.Classes.Add("hover");   
        
        NavigationSection.TaskBarToggleButton.Classes.Remove("altHover");
        NavigationSection.TaskBarToggleButton.Classes.Add("hover");
        
        NavigationSection.IncludeSubdirToggleButton.Classes.Remove("altHover");
        NavigationSection.IncludeSubdirToggleButton.Classes.Add("hover");      
        
        ZoomSection.ZoomToFitToggleButton.Classes.Remove("altHover");
        ZoomSection.ZoomToFitToggleButton.Classes.Add("hover");
        
        ZoomSection.ResetZoomToggleButton.Classes.Remove("altHover");
        ZoomSection.ResetZoomToggleButton.Classes.Add("hover");
        
        ZoomSection.AllowZoomOutToggleButton.Classes.Remove("altHover");
        ZoomSection.AllowZoomOutToggleButton.Classes.Add("hover");
        
        ZoomSection.UseAnimatedZoomToggleButton.Classes.Remove("altHover");
        ZoomSection.UseAnimatedZoomToggleButton.Classes.Add("hover");
        
        ZoomSection.ShowZoomPercentageToggleButton.Classes.Remove("altHover");
        ZoomSection.ShowZoomPercentageToggleButton.Classes.Add("hover");
        
        ZoomSection.ShowZoomPreviewerToggleButton.Classes.Remove("altHover");
        ZoomSection.ShowZoomPreviewerToggleButton.Classes.Add("hover");
        
        MouseSection.TouchPadButton.Classes.Remove("altHover");
        MouseSection.TouchPadButton.Classes.Add("hover");
        
        MouseSection.MouseButton.Classes.Remove("altHover");
        MouseSection.MouseButton.Classes.Add("hover");
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

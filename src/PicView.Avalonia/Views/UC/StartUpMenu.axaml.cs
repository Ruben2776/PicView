using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Sizing;

namespace PicView.Avalonia.Views.UC;

public partial class StartUpMenu : UserControl
{
    public StartUpMenu()
    {
        InitializeComponent();
        SizeChanged += (_, e) => ResponsiveSize(e.NewSize.Width, e.NewSize.Height);
        Loaded += StartUpMenu_Loaded;
    }

    private void StartUpMenu_Loaded(object? sender, RoutedEventArgs e)
    {
        SelectFileButton.PointerEntered += (_, _) =>
        {
            if (!this.TryFindResource("SelectFileBrush", Application.Current.RequestedThemeVariant, out var brush))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = ColorManager.PrimaryAccentColor;
        };

        SelectFileButton.PointerExited += (_, _) =>
        {
            if (!this.TryFindResource("SelectFileBrush", Application.Current.RequestedThemeVariant, out var brush))
                return;

            if (!this.TryFindResource("SecondaryTextColor", Application.Current.RequestedThemeVariant, out var color))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = color as Color? ?? default;
        };

        OpenLastFileButton.PointerEntered += (_, _) =>
        {
            if (!this.TryFindResource("OpenLastFileBrush", Application.Current.RequestedThemeVariant, out var brush))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = ColorManager.PrimaryAccentColor;
        };

        OpenLastFileButton.PointerExited += (_, _) =>
        {
            if (!this.TryFindResource("OpenLastFileBrush", Application.Current.RequestedThemeVariant, out var brush))
                return;

            if (!this.TryFindResource("SecondaryTextColor", Application.Current.RequestedThemeVariant, out var color))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = color as Color? ?? default;
        };

        PasteButton.PointerEntered += (_, _) =>
        {
            if (!this.TryFindResource("PasteBrush", Application.Current.RequestedThemeVariant, out var brush))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = ColorManager.PrimaryAccentColor;
        };

        PasteButton.PointerExited += (_, _) =>
        {
            if (!this.TryFindResource("PasteBrush", Application.Current.RequestedThemeVariant, out var brush))
                return;
            
            if (!this.TryFindResource("SecondaryTextColor", Application.Current.RequestedThemeVariant, out var color))
                return;
            
            var pasteBrush = brush as SolidColorBrush;
            pasteBrush.Color = color as Color? ?? default;
        };

        if (DataContext is not MainViewModel vm)
            return;

        TitleManager.SetNoImageTitle(vm);
        UIHelper.GetHoverBar?.IsVisible = false;
    }

    public void ResponsiveSize(double width, double height)
    {
        if (DataContext is not MainViewModel vm)
            return;
        
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        
        var titleMaxWidth = ImageSizeCalculationHelper.GetTitleMaxWidth(width, height, desktop.MainWindow.MinWidth, desktop.MainWindow.MinHeight, vm.PlatformWindowService.CombinedTitleButtonsWidth, desktop.MainWindow.Width);
        var scrollOffset = Settings.Zoom.ScrollEnabled ? SizeDefaults.ScrollbarSize : 0;

        vm.MainWindow.TitleMaxWidth.Value = titleMaxWidth - scrollOffset;
    }
}
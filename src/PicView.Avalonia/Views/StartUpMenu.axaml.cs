using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Calculations;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;

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
            if (!this.TryFindResource("SelectFileBrush", ThemeVariant.Default, out var brush))
                return;

            if (!this.TryFindResource("AccentColor", ThemeVariant.Default, out var color))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = color as Color? ?? default;
        };

        SelectFileButton.PointerExited += (_, _) =>
        {
            if (!this.TryFindResource("SelectFileBrush", ThemeVariant.Default, out var brush))
                return;

            if (!this.TryFindResource("MainTextColor", ThemeVariant.Default, out var color))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = color as Color? ?? default;
        };

        OpenLastFileButton.PointerEntered += (_, _) =>
        {
            if (!this.TryFindResource("OpenLastFileBrush", ThemeVariant.Default, out var brush))
                return;

            if (!this.TryFindResource("AccentColor", ThemeVariant.Default, out var color))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = color as Color? ?? default;
        };

        OpenLastFileButton.PointerExited += (_, _) =>
        {
            if (!this.TryFindResource("OpenLastFileBrush", ThemeVariant.Default, out var brush))
                return;

            if (!this.TryFindResource("MainTextColor", ThemeVariant.Default, out var color))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = color as Color? ?? default;
        };

        PasteButton.PointerEntered += (_, _) =>
        {
            if (!this.TryFindResource("PasteBrush", ThemeVariant.Default, out var brush))
                return;

            if (!this.TryFindResource("AccentColor", ThemeVariant.Default, out var color))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = color as Color? ?? default;
        };

        PasteButton.PointerExited += (_, _) =>
        {
            if (!this.TryFindResource("PasteBrush", ThemeVariant.Default, out var brush))
                return;

            if (!this.TryFindResource("MainTextColor", ThemeVariant.Default, out var color))
                return;

            var selectFileBrush = brush as SolidColorBrush;
            selectFileBrush.Color = color as Color? ?? default;
        };

        if (DataContext is not MainViewModel vm)
            return;

        SetTitleHelper.ResetTitle(vm);
    }

    public void ResponsiveSize(double width, double height)
    {
        const int breakPoint = 900;
        const int bottomMargin = 16;
        const int logoWidth = 350;
        
        LogoViewbox.Height = double.NaN;
        
        if (DataContext is not MainViewModel vm)
            return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        
        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            ShowIcon();
            vm.TitleMaxWidth = logoWidth;
            return;
        }

        switch (width)
        {
            case < breakPoint:
                ShowIcon();
                break;
            case > breakPoint:
                ShowFullLogo();
                break;
        }

        vm.TitleMaxWidth = ImageSizeCalculationHelper.GetTitleMaxWidth(vm.RotationAngle, width, height,
            desktop.MainWindow.MinWidth, desktop.MainWindow.MinHeight, ImageSizeCalculationHelper.GetInterfaceSize(),
            desktop.MainWindow.Width, ScreenHelper.ScreenSize.Scaling);
        
        return;

        void ShowIcon()
        {
            if (this.TryFindResource("Icon", ThemeVariant.Default, out var icon))
                Logo.Source = icon as DrawingImage;
            LogoViewbox.Width = logoWidth;
            Buttons.Margin = new Thickness(0, 0, 0, bottomMargin);
            Buttons.VerticalAlignment = VerticalAlignment.Bottom;
        }

        void ShowFullLogo()
        {
            if (this.TryFindResource("Logo", ThemeVariant.Default, out var logo))
                Logo.Source = logo as DrawingImage;
            LogoViewbox.Width = double.NaN;
            Buttons.Margin = new Thickness(0, 220, 25, bottomMargin - 100);
            Buttons.VerticalAlignment = VerticalAlignment.Center;
        }
    }
}
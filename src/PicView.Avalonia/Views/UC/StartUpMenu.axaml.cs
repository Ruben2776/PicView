using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views.UC;

public partial class StartUpMenu : UserControl
{
    public StartUpMenu()
    {
        InitializeComponent();
        SizeChanged += (_, e) => ResponsiveSize(e.NewSize.Width);
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

        vm.ResetTitle();
    }

    public void ResponsiveSize(double width)
    {
        const int breakPoint = 900;
        const int bottomMargin = 16;
        switch (width)
        {
            case < breakPoint:
                if (this.TryFindResource("Icon", ThemeVariant.Default, out var icon))
                    Logo.Source = icon as DrawingImage;
                LogoViewbox.Width = 350;
                Buttons.Margin = new Thickness(0, 0, 0, bottomMargin);
                Buttons.VerticalAlignment = VerticalAlignment.Bottom;
                break;

            case > breakPoint:
                if (this.TryFindResource("Logo", ThemeVariant.Default, out var logo))
                    Logo.Source = logo as DrawingImage;
                LogoViewbox.Width = double.NaN;
                Buttons.Margin = new Thickness(0, 220, 25, bottomMargin - 100);
                Buttons.VerticalAlignment = VerticalAlignment.Center;
                break;
        }
    }
}
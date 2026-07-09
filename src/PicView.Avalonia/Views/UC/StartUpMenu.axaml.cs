using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.Navigation;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.UC;

public partial class StartUpMenu : UserControl
{
    public StartUpMenu()
    {
        MinHeight = SizeDefaults.WindowMinSize;
        MinWidth = SizeDefaults.WindowMinSize;
        InitializeComponent();

        SizeChanged += (_, e) => ResponsiveSize(e.NewSize.Width);
        Loaded += StartUpMenu_Loaded;
    }

    private void StartUpMenu_Loaded(object? sender, RoutedEventArgs e)
    {
        FilePasteLabel.Content = TranslationManager.Translation.FilePaste ?? "Paste";
        OpenFileDialogLabel.Content = TranslationManager.Translation.OpenFileDialog ?? "Open File";
        OpenLastFileLabel.Content = TranslationManager.Translation.OpenLastFile ?? "Open Last File";

        SelectFileButton.PointerEntered += SelectFileButtonOnPointerEntered;
        SelectFileButton.PointerExited  += SelectFileButtonOnPointerExited;
        SelectFileButton.Click += SelectFileButtonOnClick;

        OpenLastFileButton.PointerEntered += OpenLastFileLabelOnPointerEntered;
        OpenLastFileButton.PointerExited += OpenLastFileLabelOnPointerExited;
        OpenLastFileButton.Click += OpenLastFileButtonOnClick;

        PasteButton.PointerEntered += PasteButtonOnPointerEntered;
        PasteButton.PointerExited += PasteButtonOnPointerExited;
        PasteButton.AddHandler(PointerPressedEvent, PasteClick, RoutingStrategies.Tunnel);

        if (!Settings.Theme.Dark)
        {
            Background = new SolidColorBrush(Color.FromArgb(242, 217, 217, 217));
        }

        if (DataContext is not TabViewModel tab)
        {
            return;
        }

        var noImage = TranslationManager.Translation.NoImage ?? "No image";
        tab.Title.Value = noImage;
        tab.WindowTitle.Value = StringExtensions.CombineWithAppName(noImage);
        tab.TitleTooltip.Value = noImage;
    }

    private async ValueTask PasteClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || DataContext is not TabViewModel tab
            || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }

        tab.CurrentView.Value = new ImageViewer();
        var isPastedSuccessfully = await ClipboardPasteOperations.Paste(core.MainWindows.ActiveWindow.CurrentValue, mainWindow);
        if (!isPastedSuccessfully)
        {
            tab.CurrentView.Value = new StartUpMenu();
        }
    }

    private void OpenLastFileButtonOnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current.DataContext is not CoreViewModel core || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        _ = UINavigationHelper.OpenLastFile(mainWindow, core.MainWindows.ActiveWindow.CurrentValue).ConfigureAwait(false);
    }

    private void PasteButtonOnPointerExited(object? sender, PointerEventArgs e)
    {
        if (!this.TryFindResource("PasteBrush", Application.Current.RequestedThemeVariant, out var brush))
        {
            return;
        }

        if (!this.TryFindResource("SecondaryTextColor", Application.Current.RequestedThemeVariant, out var color))
        {
            return;
        }

        var pasteBrush = brush as SolidColorBrush;
        pasteBrush.Color = color as Color? ?? default;
    }

    private void PasteButtonOnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!this.TryFindResource("PasteBrush", Application.Current.RequestedThemeVariant, out var brush))
        {
            return;
        }

        var selectFileBrush = brush as SolidColorBrush;
        selectFileBrush.Color = ColorManager.PrimaryAccentColor;
    }

    private void OpenLastFileLabelOnPointerExited(object? sender, PointerEventArgs e)
    {
        if (!this.TryFindResource("OpenLastFileBrush", Application.Current.RequestedThemeVariant, out var brush))
        {
            return;
        }

        if (!this.TryFindResource("SecondaryTextColor", Application.Current.RequestedThemeVariant, out var color))
        {
            return;
        }

        var selectFileBrush = brush as SolidColorBrush;
        selectFileBrush.Color = color as Color? ?? default;
    }

    private void OpenLastFileLabelOnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!this.TryFindResource("OpenLastFileBrush", Application.Current.RequestedThemeVariant, out var brush))
        {
            return;
        }

        var selectFileBrush = brush as SolidColorBrush;
        selectFileBrush.Color = ColorManager.PrimaryAccentColor;
    }

    private void SelectFileButtonOnClick(object? sender, RoutedEventArgs e)
    {
        // There is problems with DataContext and commands, so just use event
        if (Application.Current.DataContext is not CoreViewModel core || TopLevel.GetTopLevel(this) is not MainWindow mainWindow)
        {
            return;
        }
        FilePicker.SelectAndLoadFile(mainWindow, core.MainWindows.ActiveWindow.CurrentValue).ConfigureAwait(false);
    }

    private void SelectFileButtonOnPointerExited(object? sender, PointerEventArgs e)
    {
        if (!this.TryFindResource("SelectFileBrush", Application.Current.RequestedThemeVariant, out var brush))
        {
            return;
        }

        if (!this.TryFindResource("SecondaryTextColor", Application.Current.RequestedThemeVariant, out var color))
        {
            return;
        }

        var selectFileBrush = brush as SolidColorBrush;
        selectFileBrush.Color = color as Color? ?? default;
    }

    private void SelectFileButtonOnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!this.TryFindResource("SelectFileBrush", Application.Current.RequestedThemeVariant, out var brush))
        {
            return;
        }
        
        var selectFileBrush = brush as SolidColorBrush;
        selectFileBrush.Color = ColorManager.PrimaryAccentColor;
    }

    public void ResponsiveSize(double width)
    {
        const int breakPoint = 900;
        const int bottomMargin = 16;
        const int logoWidth = 350;

        LogoViewbox.Height = double.NaN;

        if (Settings.WindowProperties.Fullscreen || Settings.WindowProperties.Maximized)
        {
            ShowFullLogo();
        }
        else if (Settings.WindowProperties.AutoFit)
        {
            ShowIcon();
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

        return;

        void ShowIcon()
        {
            if (this.TryFindResource("LogoImage", Application.Current.RequestedThemeVariant, out var icon))
                Logo.Source = icon as DrawingImage;
            LogoViewbox.Width = logoWidth;
            Buttons.Margin = new Thickness(0, 0, 0, bottomMargin);
            Buttons.VerticalAlignment = VerticalAlignment.Bottom;
        }

        void ShowFullLogo()
        {
            if (this.TryFindResource("LogoFullImage", Application.Current.RequestedThemeVariant, out var logo))
                Logo.Source = logo as DrawingImage;
            LogoViewbox.Width = double.NaN;
            if (Settings.WindowProperties.Fullscreen || Settings.WindowProperties.Maximized)
            {
                Buttons.Margin = new Thickness(0, 0, 0, bottomMargin + SizeDefaults.WindowMinSize / 2);
                Buttons.VerticalAlignment = VerticalAlignment.Bottom;
            }
            else
            {
                Buttons.Margin = new Thickness(0, 220, 25, bottomMargin - 100);
                Buttons.VerticalAlignment = VerticalAlignment.Center;
            }
        }
    }
    
    ~StartUpMenu()
    {
        SelectFileButton.PointerEntered -= SelectFileButtonOnPointerEntered;
        SelectFileButton.PointerExited -= SelectFileButtonOnPointerExited;
        SelectFileButton.Click -= SelectFileButtonOnClick;
        
        OpenLastFileLabel.PointerEntered -= OpenLastFileLabelOnPointerEntered;
        OpenLastFileLabel.PointerExited -= OpenLastFileLabelOnPointerExited;
        
        PasteButton.PointerEntered -= PasteButtonOnPointerEntered;
        PasteButton.PointerExited -= PasteButtonOnPointerExited;
        PasteButton.RemoveHandler(PointerPressedEvent, PasteClick);
    }
}
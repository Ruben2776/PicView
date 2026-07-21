using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class ImageInfoWindow: Win32GenericWindow, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public ImageInfoWindow(MainWindowViewModel viewModel)
    {
        Debug.Assert(viewModel.InfoWindow.ImageInfoWindowConfig != null);
        var config = viewModel.InfoWindow.ImageInfoWindowConfig;
        DataContext = viewModel;
        InitializeComponent();
        
        if (Settings.Theme.GlassTheme)
        {
            BorderRectangle.Height = 0;
            
            TopWindowBorder.Background = Brushes.Transparent;
            StarOutlineButtons.Background = Brushes.Transparent;
            RemoveRatingButton.Background = Brushes.Transparent;
            ExifView.Background = Brushes.Transparent;
            
            CloseButton.Background = Brushes.Transparent;
            CloseButton.BorderThickness = new Thickness(0);
            MinimizeButton.Background = Brushes.Transparent;
            MinimizeButton.BorderThickness = new Thickness(0);
            
            RecycleButton.BorderThickness = new Thickness(0);
            DuplicateButton.BorderThickness = new Thickness(0);
            OptimizeButton.BorderThickness = new Thickness(0);
            OpenWithButton.BorderThickness = new Thickness(0);
            LocateOnDiskButton.BorderThickness = new Thickness(0);
            
            if (!Application.Current.TryGetResource("SecondaryTextColor",
                    Application.Current.RequestedThemeVariant, out var textColor))
            {
                return;
            }

            if (textColor is not Color color)
            {
                return;
            }
            
            MinimizeButton.Foreground = new SolidColorBrush(color);
            CloseButton.Foreground = new SolidColorBrush(color);
            RecycleButton.Foreground = new SolidColorBrush(color);
            DuplicateButton.Foreground = new SolidColorBrush(color);
            OptimizeButton.Foreground = new SolidColorBrush(color);
            OpenWithButton.Foreground = new SolidColorBrush(color);
            LocateOnDiskButton.Foreground = new SolidColorBrush(color);
        }

        if (Settings.Theme.GlassTheme || !Settings.Theme.Dark)
        {
            RecycleButton.Classes.Remove("noBorderHover");
            RecycleButton.Classes.Add("hover");
            
            DuplicateButton.Classes.Remove("noBorderHover");
            DuplicateButton.Classes.Add("hover");
            
            OptimizeButton.Classes.Remove("noBorderHover");
            OptimizeButton.Classes.Add("hover");
            
            OpenWithButton.Classes.Remove("noBorderHover");
            OpenWithButton.Classes.Add("hover");
            
            LocateOnDiskButton.Classes.Remove("noBorderHover");
            LocateOnDiskButton.Classes.Add("hover");
            
            RemoveRatingButton.Classes.Remove("noBorderHover");
            RemoveRatingButton.Classes.Add("hover");
        }
        if (!Settings.Theme.Dark)
        {
            if (!Settings.Theme.GlassTheme)
            {
                ExifView.Background = UIHelper.GetMenuBackgroundColor();
            }
            var copyButtons =  ExifView.GetVisualChildren().OfType<CopyButton>();
            foreach (var btn in copyButtons)
            {
                btn.Classes.Add("hover");
            }
        }
        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.ImageInfo + " - PicView", false, config.WindowProperties);
    }
    
    public void Dispose()
    {
        Disposable.Dispose(_disposables);
        GC.SuppressFinalize(this);
    }

    ~ImageInfoWindow()
    {
        Disposable.Dispose(_disposables);
    }
}
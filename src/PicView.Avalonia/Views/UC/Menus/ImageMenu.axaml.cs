using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;
using R3;

namespace PicView.Avalonia.Views.UC.Menus;

public partial class ImageMenu : AnimatedMenu
{
    public ImageMenu()
    {
        InitializeComponent();
        Loaded += delegate
        {
            Observable.EveryValueChanged(this, x => x.IsVisible, UIHelper.GetFrameProvider)
                .Skip(1)
                .Where(isVisible => !isVisible)
                .Subscribe(_ => { SlideShowButton.Flyout.Hide(); });

            // Determine if crop should be enabled every time it opens
            this.GetObservable(IsOpenProperty).ToObservable()
                .Where(x => x)
                .Subscribe(_ => { DetermineIfCropShouldBeEnabled(); });

            Item2.Header = $"2 {TranslationManager.Translation.SecAbbreviation}";
            Item5.Header = $"5 {TranslationManager.Translation.SecAbbreviation}";
            Item10.Header = $"10 {TranslationManager.Translation.SecAbbreviation}";
            Item20.Header = $"20 {TranslationManager.Translation.SecAbbreviation}";
            Item30.Header = $"30 {TranslationManager.Translation.SecAbbreviation}";
            Item60.Header = $"60 {TranslationManager.Translation.SecAbbreviation}";
            Item90.Header = $"90 {TranslationManager.Translation.SecAbbreviation}";
            Item120.Header = $"120 {TranslationManager.Translation.SecAbbreviation}";

            if (Settings.Theme.Dark)
            {
                return;
            }


            TopBorder.Background = Brushes.White;

            CropButton.Classes.Remove("altHover");
            CropButton.Classes.Add("hover");

            GalleryButton.Classes.Remove("altHover");
            GalleryButton.Classes.Add("hover");

            SideBySideButton.Classes.Remove("altHover");
            SideBySideButton.Classes.Add("hover");
            if (TryGetResource("SideBrush", Application.Current.RequestedThemeVariant,
                    out var sideBrush))
            {
                if (sideBrush is SolidColorBrush brush)
                {
                    UIHelper.SetButtonHover(SideBySideButton, brush);
                }
            }

            ResizeImageButton.Classes.Remove("altHover");
            ResizeImageButton.Classes.Add("hover");
            if (TryGetResource("ResizeBrush", Application.Current.RequestedThemeVariant,
                    out var value))
            {
                if (value is SolidColorBrush brush)
                {
                    UIHelper.SetButtonHover(ResizeImageButton, brush);
                }
            }

            SlideShowButton.Classes.Remove("altHover");
            SlideShowButton.Classes.Add("hover");
            if (Application.Current.TryGetResource("SlideshowAltImage", Application.Current.RequestedThemeVariant,
                    out var slideshowAltImage) && Application.Current.TryGetResource("SlideshowImage",
                    Application.Current.RequestedThemeVariant,
                    out var slideshowImage))
            {
                if (slideshowAltImage is DrawingImage imgAlt && slideshowImage is DrawingImage img &&
                    TryGetResource("SlideShowBrush",
                        Application.Current.RequestedThemeVariant,
                        out var slideshowBrush))
                {
                    UIHelper.SetButtonHover(SlideShowButton, slideshowBrush as SolidColorBrush);
                    SlideShowButton.PointerEntered += (_, _) => { SlideShowImage.Source = imgAlt; };
                    SlideShowButton.PointerExited += (_, _) => { SlideShowImage.Source = img; };
                }
            }

            ImageInfoButton.Classes.Remove("altHover");
            ImageInfoButton.Classes.Add("hover");
        };
    }

    private void DetermineIfCropShouldBeEnabled()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        CropFunctions.DetermineIfShouldBeEnabled(vm);
    }
}
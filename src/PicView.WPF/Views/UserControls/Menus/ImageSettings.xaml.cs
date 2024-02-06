using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.WPF.ConfigureSettings;
using PicView.WPF.Editing.Crop;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.TransformImage;
using System.Windows.Media;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Menus;

public partial class ImageSettings
{
    public ImageSettings()
    {
        InitializeComponent();

        // RotateLeftButton
        RotateLeftButton.Click += async (_, _) =>
            await Rotation.RotateAndMoveCursor(true, RotateLeftButton).ConfigureAwait(false);
        SetButtonIconMouseOverAnimations(RotateLeftButton, RotateLeftButtonBrush, RotateLeftIconBrush);

        // RotateRightButton
        RotateRightButton.Click += async (_, _) =>
            await Rotation.RotateAndMoveCursor(false, RotateRightButton).ConfigureAwait(false);
        SetButtonIconMouseOverAnimations(RotateRightButton, RotateRightButtonBrush, RotateRightIconBrush);

        // FlipButton
        FlipButton.Click += (_, _) => Rotation.Flip();
        SetButtonIconMouseOverAnimations(FlipButton, FlipButtonBrush, FlipIconBrush);

        // ResizeButton
        SetButtonIconMouseOverAnimations(ResizeButtonBorder, ResizeBorderBrush,
            (SolidColorBrush)Resources["ResizeIcon"]);
        ResizeButton.Click += (_, _) => UpdateUIValues.ToggleQuickResize();
        ResizeButtonBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.ToggleQuickResize();

        // CropButton
        SetButtonIconMouseOverAnimations(CropButtonBorder, CropBorderBrush, (SolidColorBrush)Resources["CropIcon"]);
        CropButton.Click += (_, _) => CropFunctions.StartCrop();
        CropButtonBorder.MouseLeftButtonDown += (_, _) => CropFunctions.StartCrop();

        // OptimizeButton
        SetButtonIconMouseOverAnimations(OptimizeBorder, BgBorderBrush, (SolidColorBrush)Resources["OptimizeIcon"]);
        OptimizeButton.Click += async (_, _) =>
            await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);
        OptimizeBorder.MouseLeftButtonDown += async (_, _) =>
            await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

        // SlideShowBorder
        SetButtonIconMouseOverAnimations(SlideShowBorder, SlideShowBorderBrush,
            (SolidColorBrush)Resources["SlideshowIcon"]);
        SlideShowButton.Click += delegate
        {
            UC.Close_UserControls();
            Slideshow.StartSlideshow();
        };
        SlideShowBorder.MouseLeftButtonDown += delegate
        {
            UC.Close_UserControls();
            Slideshow.StartSlideshow();
        };

        // BottomGalleryBorder
        SetButtonIconMouseOverAnimations(BottomGalleryButton, BottomGalleryBrush,
            (SolidColorBrush)Resources["BottomGalleryIcon"]);
        BottomGalleryBorder.MouseLeftButtonDown +=
            async (_, _) => await ContainedGalleryClick().ConfigureAwait(false);
        BottomGalleryButton.Click += async (_, _) => await ContainedGalleryClick().ConfigureAwait(false);

        UpdateLanguage();
    }

    internal void UpdateLanguage()
    {
        var showHideGallery = SettingsHelper.Settings.Gallery.IsBottomGalleryShown
            ? "ShowBottomGallery"
            : "HideBottomGallery";
        ShowBottomGalleryText.Text = showHideGallery;
        SlideShowBorder.ToolTip = TranslationHelper.GetTranslation("StartSlideshow");
        SlideShowButtonTextBlock.Text = TranslationHelper.GetTranslation("Slideshow");
        OptimizeBorder.ToolTip = TranslationHelper.GetTranslation("OptimizeImage");
        OptimizeButtonTextBlock.Text = TranslationHelper.GetTranslation("OptimizeImage");
        CropButtonTextBlock.Text = TranslationHelper.GetTranslation("Crop");
        ResizeButtonBorder.ToolTip = TranslationHelper.GetTranslation("ResizeImage");
        ResizeButtonTextBlock.Text = TranslationHelper.GetTranslation("Resize");
        FlipButton.ToolTip = TranslationHelper.GetTranslation("Flip");
        RotateRightButton.ToolTip = TranslationHelper.GetTranslation("RotateRight");
        RotateLeftButton.ToolTip = TranslationHelper.GetTranslation("RotateLeft");
    }

    private static async Task ContainedGalleryClick()
    {
        UC.Close_UserControls();
        SettingsHelper.Settings.Gallery.IsBottomGalleryShown = !SettingsHelper.Settings.Gallery.IsBottomGalleryShown;
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            GalleryNavigation.SetSize(SettingsHelper.Settings.Gallery.BottomGalleryItemSize);
            GalleryFunctions.ReCalculateItemSizes();
            await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);
        }
        else
        {
            GalleryToggle.CloseBottomGallery();
        }
    }
}
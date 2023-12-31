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
        RotateLeftButton.ToolTip = TranslationHelper.GetTranslation("RotateLeft");

        // RotateRightButton
        RotateRightButton.Click += async (_, _) =>
            await Rotation.RotateAndMoveCursor(false, RotateRightButton).ConfigureAwait(false);
        SetButtonIconMouseOverAnimations(RotateRightButton, RotateRightButtonBrush, RotateRightIconBrush);
        RotateRightButton.ToolTip = TranslationHelper.GetTranslation("RotateRight");

        // FlipButton
        FlipButton.Click += (_, _) => Rotation.Flip();
        SetButtonIconMouseOverAnimations(FlipButton, FlipButtonBrush, FlipIconBrush);
        FlipButton.ToolTip = TranslationHelper.GetTranslation("Flip");

        // ResizeButton
        SetButtonIconMouseOverAnimations(ResizeButtonBorder, ResizeBorderBrush,
            (SolidColorBrush)Resources["ResizeIcon"]);
        ResizeButton.Click += (_, _) => UpdateUIValues.ToggleQuickResize();
        ResizeButtonBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.ToggleQuickResize();
        ResizeButtonBorder.ToolTip = TranslationHelper.GetTranslation("ResizeImage");
        ResizeButtonTextBlock.Text = TranslationHelper.GetTranslation("Resize");

        // CropButton
        SetButtonIconMouseOverAnimations(CropButtonBorder, CropBorderBrush, (SolidColorBrush)Resources["CropIcon"]);
        CropButton.Click += (_, _) => CropFunctions.StartCrop();
        CropButtonBorder.MouseLeftButtonDown += (_, _) => CropFunctions.StartCrop();
        CropButtonTextBlock.Text = TranslationHelper.GetTranslation("Crop");

        // OptimizeButton
        SetButtonIconMouseOverAnimations(OptimizeBorder, BgBorderBrush, (SolidColorBrush)Resources["OptimizeIcon"]);
        OptimizeButton.Click += async (_, _) =>
            await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);
        OptimizeBorder.MouseLeftButtonDown += async (_, _) =>
            await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);
        OptimizeBorder.ToolTip = TranslationHelper.GetTranslation("OptimizeImage");
        OptimizeButtonTextBlock.Text = TranslationHelper.GetTranslation("OptimizeImage");

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
        SlideShowBorder.ToolTip = TranslationHelper.GetTranslation("StartSlideshow");
        SlideShowButtonTextBlock.Text = TranslationHelper.GetTranslation("Slideshow");

        // BottomGalleryBorder
        SetButtonIconMouseOverAnimations(BottomGalleryButton, BottomGalleryBrush,
            (SolidColorBrush)Resources["BottomGalleryIcon"]);
        BottomGalleryBorder.MouseLeftButtonDown +=
            async (_, _) => await ContainedGalleryClick().ConfigureAwait(false);
        BottomGalleryButton.Click += async (_, _) => await ContainedGalleryClick().ConfigureAwait(false);
        var showHideGallery = SettingsHelper.Settings.Gallery.IsBottomGalleryShown
            ? "ShowBottomGallery"
            : "HideBottomGallery";
        ShowBottomGalleryText.Text = showHideGallery;
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
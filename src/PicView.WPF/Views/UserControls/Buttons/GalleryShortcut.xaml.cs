using PicView.WPF.Animations;
using PicView.WPF.PicGallery;

namespace PicView.WPF.Views.UserControls.Buttons;

public partial class GalleryShortcut
{
    public GalleryShortcut()
    {
        InitializeComponent();

        Loaded += delegate
        {
            MouseLeftButtonDown += async (_, _) =>
                await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);

            MouseEnter += delegate
            {
                MouseOverAnimations.AltInterfaceMouseOver(ImagePath1Fill, CanvasBGcolor, BorderBrushKey);
                MouseOverAnimations.AltInterfaceMouseOver(ImagePath2Fill, CanvasBGcolor, BorderBrushKey);
                MouseOverAnimations.AltInterfaceMouseOver(ImagePath3Fill, CanvasBGcolor, BorderBrushKey);
            };

            MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(ImagePath1Fill, CanvasBGcolor, BorderBrushKey);
                MouseOverAnimations.AltInterfaceMouseLeave(ImagePath2Fill, CanvasBGcolor, BorderBrushKey);
                MouseOverAnimations.AltInterfaceMouseLeave(ImagePath3Fill, CanvasBGcolor, BorderBrushKey);
            };
            Loaded += delegate
            {
                ToolTip = Core.Localization.TranslationHelper.GetTranslation("ShowImageGallery");
            };
        };
    }
}
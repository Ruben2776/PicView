using PicView.Animations;
using PicView.PicGallery;
using System.Windows.Controls;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class GalleryShortcut : UserControl
    {
        public GalleryShortcut()
        {
            InitializeComponent();

            Loaded += delegate
            {
                MouseLeftButtonDown += async (_, _) => await GalleryToggle.OpenHorizontalGalleryAsync().ConfigureAwait(false);

                PreviewMouseLeftButtonDown += delegate
                {
                    MouseOverAnimations.AltInterfacePreviewMouseOver(ImagePath1Fill, BorderBrushKey);
                    MouseOverAnimations.AltInterfacePreviewMouseOver(ImagePath2Fill, BorderBrushKey);
                    MouseOverAnimations.AltInterfacePreviewMouseOver(ImagePath3Fill, BorderBrushKey);
                };

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
            };
        }
    }
}
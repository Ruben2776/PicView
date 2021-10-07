using PicView.Animations;
using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class X2 : UserControl
    {
        public X2()
        {
            InitializeComponent();
            MouseLeftButtonUp += (_, _) =>
            {
                if (PicView.PicGallery.GalleryFunctions.IsVerticalFullscreenOpen || PicView.PicGallery.GalleryFunctions.IsHorizontalFullscreenOpen)
                {
                    SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
                }
                else
                {
                    PicView.PicGallery.GalleryToggle.CloseHorizontalGallery();
                }
            };

            MouseEnter += (_, _) =>
            {
                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            MouseLeave += (_, _) =>
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
            };


        }
    }
}
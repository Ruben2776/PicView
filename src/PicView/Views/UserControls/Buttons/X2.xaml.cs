using PicView.Animations;
using PicView.PicGallery;
using PicView.UILogic;
using System.Windows;

namespace PicView.Views.UserControls.Buttons;

public partial class X2
{
    public X2()
    {
        InitializeComponent();
        MouseLeftButtonDown += (_, _) =>
        {
            if (GalleryFunctions.IsGalleryOpen)
            {
                GalleryToggle.CloseHorizontalGallery();
            }
            else
            {
                SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
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
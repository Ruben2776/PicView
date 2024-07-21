using System.Windows;
using PicView.WPF.Animations;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;

namespace PicView.WPF.Views.UserControls.Buttons;

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
                if (UC.GetQuickResize is { IsVisible: true })
                {
                    return;
                }

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
        Loaded += delegate
        {
            ToolTip = Core.Localization.TranslationHelper.GetTranslation("CloseGallery");
        };
    }
}
using PicView.Animations;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls.Buttons
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class X2 : UserControl
    {
        public X2()
        {
            InitializeComponent();
            MouseLeftButtonDown += (_, _) =>
            {
                if (GalleryFunctions.IsHorizontalFullscreenOpen)
                {
                    SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
                }
                else if (GalleryFunctions.IsHorizontalOpen)
                {
                    GalleryToggle.CloseHorizontalGallery();
                }
                else if (Settings.Default.ShowInterface == false || Settings.Default.Fullscreen)
                {
                    if (UC.GetPicGallery is null or { IsVisible : false})
                    {
                        SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
                    }
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
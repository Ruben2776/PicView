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

            MouseEnter += (sender, e) =>
            {
                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            MouseLeave += (sender, e) =>
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            TheButton.Click += (_, _) =>
            {
                if (ConfigureWindows.GetFakeWindow is not null && ConfigureWindows.GetFakeWindow.IsVisible)
                {
                    SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
                }
                else
                {
                    PicView.PicGallery.GalleryToggle.CloseHorizontalGallery();
                }

            };
        }
    }
}
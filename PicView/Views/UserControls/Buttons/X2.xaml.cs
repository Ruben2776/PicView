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
                if (ConfigureWindows.GetFakeWindow is not null && ConfigureWindows.GetFakeWindow.IsVisible)
                {
                    SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
                }
                else if (Properties.Settings.Default.Fullscreen == false)
                {
                    PicView.PicGallery.GalleryToggle.CloseHorizontalGallery();
                }
                else
                {
                    SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
                }
            };
            border.MouseLeftButtonUp += (_, _) =>
            {
                if (ConfigureWindows.GetFakeWindow is not null && ConfigureWindows.GetFakeWindow.IsVisible)
                {
                    SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow);
                }
                else if (Properties.Settings.Default.Fullscreen == false)
                {
                    PicView.PicGallery.GalleryToggle.CloseHorizontalGallery();
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
}
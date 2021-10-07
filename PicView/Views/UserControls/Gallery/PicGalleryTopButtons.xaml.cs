using PicView.PicGallery;
using PicView.UILogic;
using PicView.Animations;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls.Gallery
{
    /// <summary>
    /// Interaction logic for PicGalleryTopButtons.xaml
    /// </summary>
    public partial class PicGalleryTopButtons : UserControl
    {
        public PicGalleryTopButtons()
        {
            InitializeComponent();

            RestoreButton.MouseLeftButtonUp += delegate { PicView.PicGallery.GalleryToggle.CloseFullscreenGallery(); };

            MouseEnter += delegate
            {
                if (!Properties.Settings.Default.Fullscreen)
                {
                    ToolTip = Application.Current.Resources["Fullscreen"];
                }
                else
                {
                    ToolTip = Application.Current.Resources["RestoreDown"];
                }

                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, RestoreBg, BorderBrushKey);
            };

            MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, RestoreBg, BorderBrushKey);
            };
        }
    }
}
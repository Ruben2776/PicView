using PicView.Animations;
using PicView.PicGallery;
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

            RestoreButton.MouseLeftButtonUp += delegate { GalleryToggle.CloseFullscreenGallery(); };

            RestoreButton.MouseEnter += delegate
            {
                ToolTip = Application.Current.Resources["RestoreDown"];
                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, RestoreBg, BorderBrushKey);
            };

            RestoreButton.MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, RestoreBg, BorderBrushKey);
            };
        }
    }
}
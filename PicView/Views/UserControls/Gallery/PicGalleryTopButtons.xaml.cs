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

            RestoreButton.MouseEnter += delegate
            {
                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, RestoreBg, BorderBrushKey);
            };

            RestoreButton.MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, RestoreBg, BorderBrushKey);
            };

            RestoreButton.MouseLeftButtonDown += async delegate { await GalleryToggle.ToggleAsync().ConfigureAwait(false); };

            RestoreButton.ToolTip = Application.Current.Resources["RestoreDown"];

            CloseButton.MouseLeftButtonDown += delegate { SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow); };

            CloseButton.ToolTip = Application.Current.Resources["Close"];

            MinButton.MouseLeftButtonDown += delegate { SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow); };
        }
    }
}
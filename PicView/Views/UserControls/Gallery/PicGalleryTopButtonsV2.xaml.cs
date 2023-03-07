using PicView.Animations;
using PicView.PicGallery;
using PicView.UILogic;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls.Gallery
{
    public partial class PicGalleryTopButtonsV2 : UserControl
    {
        public PicGalleryTopButtonsV2()
        {
            InitializeComponent();

            // RestoreButton
            RestoreButton.MouseEnter += delegate
            {
                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, RestoreBg, BorderBrushKey);
            };

            RestoreButton.MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, RestoreBg, BorderBrushKey);
            };

            RestoreButton.MouseLeftButtonDown += delegate { GalleryToggle.CloseFullscreenGallery(); };

            RestoreButton.ToolTip = Application.Current.Resources["RestoreDown"];

            // CloseButton
            CloseButton.MouseLeftButtonDown += delegate { SystemCommands.CloseWindow(ConfigureWindows.GetMainWindow); };

            CloseButton.ToolTip = Application.Current.Resources["Close"];

            CloseButton.MouseEnter += delegate
            {
                MouseOverAnimations.AltInterfaceMouseOver(PolyFillX, CloseBg, CloseBrushKey);
            };

            CloseButton.MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFillX, CloseBg, CloseBrushKey);
            };

            // MinButtun
            MinButton.MouseEnter += delegate
            {
                MouseOverAnimations.AltInterfaceMouseOver(MinPolyFill, MinBGcolor, MinBorderBrushKey);
            };

            MinButton.MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(MinPolyFill, MinBGcolor, MinBorderBrushKey);
            };

            MinButton.MouseLeftButtonDown += delegate { SystemCommands.MinimizeWindow(ConfigureWindows.GetMainWindow); };
        }
    }
}
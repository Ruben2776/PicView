using PicView.UILogic.Animations;
using PicView.UILogic.Sizing;
using System.Windows;
using System.Windows.Controls;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class Restorebutton : UserControl
    {
        public Restorebutton()
        {
            InitializeComponent();

            PreviewMouseLeftButtonDown += delegate
            {
                MouseOverAnimations.AltInterfacePreviewMouseOver(PolyFill, BorderBrushKey);
            };

            MouseLeftButtonUp += delegate { WindowLogic.Fullscreen_Restore(); };

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

                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
            };
        }
    }
}
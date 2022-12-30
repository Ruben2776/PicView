using PicView.Animations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PicView.Views.UserControls.Buttons
{
    public partial class ClickArrow : UserControl
    {
        public ClickArrow(bool right)
        {
            InitializeComponent();

            if (!right)
            {
                Arrow.LayoutTransform = new ScaleTransform
                {
                    ScaleX = -1
                };
                border.BorderThickness = new Thickness(0, 1, 1, 1);
                border.CornerRadius = new CornerRadius(0, 2, 2, 0);
                Canvas.SetLeft(Arrow, 12);
            }

            MouseEnter += delegate
            {
                MouseOverAnimations.AltInterfaceMouseOver(PolyFill, CanvasBGcolor, BorderBrushKey);
            };

            MouseLeave += delegate
            {
                MouseOverAnimations.AltInterfaceMouseLeave(PolyFill, CanvasBGcolor, BorderBrushKey);
            };
        }
    }
}
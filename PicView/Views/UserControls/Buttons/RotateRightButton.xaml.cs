using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class RotateRightButton : UserControl
    {
        public RotateRightButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    PreviewMouseButtonDownAnim(RotateButtonBrush);
                };

                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(RotateButtonBrush, true);
                };

                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnimBgColor(RotateButtonBrush, false);
                };

                TheButton.Click += delegate
                {
                    UILogic.TransformImage.Rotation.Rotate(true);
                };
            };
        }
    }
}
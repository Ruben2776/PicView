using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.UILogic.UserControls
{
    public partial class RotateLeftButton : UserControl
    {
        public RotateLeftButton()
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
                    TransformImage.Rotation.Rotate(false);
                    // TODO move cursor if necessary
                }; 
            };
        }
    }
}
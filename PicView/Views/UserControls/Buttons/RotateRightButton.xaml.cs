using System.Windows.Controls;
using System.Windows.Media;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.UILogic.UserControls
{
    public partial class RotateRightButton : UserControl
    {
        public RotateRightButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(RotateButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RotateButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(RotateButtonBrush, false);
                TheButton.Click += delegate { UC.Close_UserControls(); TransformImage.Rotation.Rotate(true); };

                ToolTip = "Rotate right"; // TODO add translation
            };
        }
    }
}
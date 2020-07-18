using System.Windows.Controls;
using static PicView.UI.Animations.MouseOverAnimations;

namespace PicView.UI.UserControls
{
    public partial class RotateLeftButton : UserControl
    {
        public RotateLeftButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(RotateButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RotateButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(RotateButtonBrush, false);
                TheButton.Click += delegate { TransformImage.Rotation.Rotate(false); }; // TODO move cursor if necessary

                ToolTip = "Rotate left"; // TODO add translation
            };
        }
    }
}
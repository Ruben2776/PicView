using PicView.ChangeImage;
using PicView.UI.Animations;
using System.Windows.Controls;
using static PicView.UI.Animations.MouseOverAnimations;

namespace PicView.UI.UserControls
{
    public partial class RightButton : UserControl
    {
        public RightButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(RightArrowFill);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RightArrowFill);
                TheButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(RightButtonBrush);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(RightArrowFill);
                TheButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(RightButtonBrush);
                TheButton.Click += (s, x) => Navigation.PicButton(false, true);
            };
        }
    }
}
using PicView.ChangeImage;
using PicView.UILogic.Animations;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
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
                TheButton.Click += async (s, x) => await Navigation.PicButtonAsync(false, true).ConfigureAwait(false);
            };
        }
    }
}
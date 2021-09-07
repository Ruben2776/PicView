using PicView.ChangeImage;
using PicView.UILogic.Animations;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class LeftButton : UserControl
    {
        public LeftButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(LeftArrowFill);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(LeftArrowFill);
                TheButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(LeftButtonBrush);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(LeftArrowFill);
                TheButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(LeftButtonBrush);
                TheButton.Click += async (s, x) => await Navigation.PicButtonAsync(false, false).ConfigureAwait(false);
            };
        }
    }
}
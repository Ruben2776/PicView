using PicView.Animations;
using PicView.ChangeImage;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class LeftButton : UserControl
    {
        public LeftButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += async (s, x) => await Navigation.PicButtonAsync(false, false).ConfigureAwait(false);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(LeftArrowFill);
                TheButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(LeftButtonBrush);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(LeftArrowFill);
                TheButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(LeftButtonBrush);
            };
        }
    }
}
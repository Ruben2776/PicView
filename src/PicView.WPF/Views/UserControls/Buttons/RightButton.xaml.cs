using PicView.WPF.Animations;
using PicView.WPF.ChangeImage;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons;

public partial class RightButton
{
    public RightButton()
    {
        InitializeComponent();

        Loaded += delegate
        {
            TheButton.PreviewMouseLeftButtonDown += async (s, x) =>
                await Navigation.PicButtonAsync(false, true).ConfigureAwait(false);
            TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RightArrowFill);
            TheButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(RightButtonBrush);
            TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(RightArrowFill);
            TheButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(RightButtonBrush);
        };
    }
}
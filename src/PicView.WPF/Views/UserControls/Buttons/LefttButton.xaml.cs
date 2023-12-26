using PicView.WPF.Animations;
using PicView.WPF.ChangeImage;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Buttons;

public partial class LeftButton
{
    public LeftButton()
    {
        InitializeComponent();

        Loaded += delegate
        {
            TheButton.PreviewMouseLeftButtonDown += async (s, x) =>
                await Navigation.PicButtonAsync(false, false).ConfigureAwait(false);
            TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(LeftArrowFill);
            TheButton.MouseEnter += (s, x) => AnimationHelper.MouseEnterBgTexColor(LeftButtonBrush);
            TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnim(LeftArrowFill);
            TheButton.MouseLeave += (s, x) => AnimationHelper.MouseLeaveBgTexColor(LeftButtonBrush);
        };
    }
}
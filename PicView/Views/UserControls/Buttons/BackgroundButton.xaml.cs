using PicView.Animations;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class BackGroundButton : UserControl
    {
        public BackGroundButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    PreviewMouseButtonDownAnim(IconBrush1);
                    PreviewMouseButtonDownAnim(IconBrush2);
                    PreviewMouseButtonDownAnim(IconBrush3);
                    PreviewMouseButtonDownAnim(IconBrush4);
                    PreviewMouseButtonDownAnim(IconBrush5);
                };

                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(IconBrush1);
                    ButtonMouseOverAnim(IconBrush2);
                    ButtonMouseOverAnim(IconBrush3);
                    ButtonMouseOverAnim(IconBrush4);
                    ButtonMouseOverAnim(IconBrush5);
                    AnimationHelper.MouseEnterBgTexColor(TheButtonBrush);
                };

                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(IconBrush1);
                    ButtonMouseLeaveAnim(IconBrush2);
                    ButtonMouseLeaveAnim(IconBrush3);
                    ButtonMouseLeaveAnim(IconBrush4);
                    ButtonMouseLeaveAnim(IconBrush5);
                    AnimationHelper.MouseLeaveBgTexColor(TheButtonBrush);
                };

                TheButton.Click += (_, _) => ConfigureSettings.ConfigColors.ChangeBackground();
            };
        }
    }
}
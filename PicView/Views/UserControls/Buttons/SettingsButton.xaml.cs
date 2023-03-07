using PicView.Animations;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class SettingsButton : UserControl
    {
        public SettingsButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    ButtonMouseOverAnim(IconBrush1, false, true);
                    ButtonMouseOverAnim(IconBrush2, false, true);
                    ButtonMouseOverAnim(txtBrush, false, true);
                    AnimationHelper.MouseEnterBgTexColor(TheButtonBrush);
                };

                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(IconBrush1);
                    ButtonMouseOverAnim(IconBrush2);
                    ButtonMouseOverAnim(txtBrush);
                    AnimationHelper.MouseEnterBgTexColor(TheButtonBrush);
                };

                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(IconBrush1);
                    ButtonMouseLeaveAnim(IconBrush2);
                    ButtonMouseLeaveAnim(txtBrush);
                    AnimationHelper.MouseLeaveBgTexColor(TheButtonBrush);
                };
            };
        }
    }
}
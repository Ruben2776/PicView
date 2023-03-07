using PicView.Animations;
using PicView.UILogic;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class InfoButton : UserControl
    {
        public InfoButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(IconBrush1);
                    ButtonMouseOverAnim(txtBrush);
                    AnimationHelper.MouseEnterBgTexColor(TheButtonBrush);
                };

                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(IconBrush1);
                    ButtonMouseLeaveAnim(txtBrush);
                    AnimationHelper.MouseLeaveBgTexColor(TheButtonBrush);
                };

                TheButton.Click += (_, _) => ConfigureWindows.InfoWindow();
            };
        }
    }
}
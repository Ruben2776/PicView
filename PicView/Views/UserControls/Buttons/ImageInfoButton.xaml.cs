using PicView.Animations;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class ImageInfoButton : UserControl
    {
        public ImageInfoButton()
        {
            InitializeComponent();

            Loaded += delegate
            {

                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(IconBrush);
                    AnimationHelper.MouseEnterBgTexColor(TheButtonBrush);
                };

                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnim(IconBrush);
                    AnimationHelper.MouseLeaveBgTexColor(TheButtonBrush);
                };

                TheButton.Click += (_, _) =>
                {
                    UILogic.UC.Close_UserControls();
                    UILogic.ConfigureWindows.ImageInfoWindow();
                };
            };
        }
    }
}
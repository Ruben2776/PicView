using PicView.UILogic.Animations;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class RotateLeftButton : UserControl
    {
        public RotateLeftButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    PreviewMouseButtonDownAnim(RotateButtonBrush);
                };
                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(RotateButtonBrush, true);
                };
                TheButton.MouseLeave += delegate
                {
                    ButtonMouseLeaveAnimBgColor(RotateButtonBrush, false);
                };
                TheButton.Click += delegate
                {
                    UILogic.TransformImage.Rotation.Rotate(false);
                    // TODO move cursor if necessary
                };


                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

            };
        }
    }
}
using PicView.Animations;
using System.Threading.Tasks;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class RotateRightButton : UserControl
    {
        public RotateRightButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    ButtonMouseOverAnim(IconBrush, false, true);
                    ButtonMouseOverAnim(TheButtonBrush, false, true);
                    AnimationHelper.MouseEnterBgTexColor(TheButtonBrush);
                };

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

                TheButton.Click += async delegate
                {
                    UILogic.TransformImage.Rotation.Rotate(true);
                    // Move cursor after rotating
                    await Task.Delay(15).ConfigureAwait(true); // Delay it, so that the move takes place after window has resized
                    var p = TheButton.PointToScreen(new System.Windows.Point(25, 25));
                    SystemIntegration.NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                };
            };
        }
    }
}
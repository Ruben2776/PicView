using PicView.UILogic.Animations;
using System.Threading.Tasks;
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
                    PreviewMouseButtonDownAnim(IconBrush);
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
                    UILogic.TransformImage.Rotation.Rotate(false);
                    // Move cursor after rotating
                    await Task.Delay(15).ConfigureAwait(true); // Delay it, so that the move takes place after window has resized
                    var p = TheButton.PointToScreen(new System.Windows.Point(25, 25));
                    SystemIntegration.NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                };
            };
        }
    }
}
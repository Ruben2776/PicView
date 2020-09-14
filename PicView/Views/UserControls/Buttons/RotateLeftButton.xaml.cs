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
                TheButton.Click += async delegate
                {
                    UILogic.TransformImage.Rotation.Rotate(false);
                    // Move cursor after rotating
                    await Task.Delay(15).ConfigureAwait(true); // Delay it, so that the move takes place after window has resized
                    var p = TheButton.PointToScreen(new System.Windows.Point(25, 25));
                    SystemIntegration.NativeMethods.SetCursorPos((int)p.X, (int)p.Y);
                };


                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

            };
        }
    }
}
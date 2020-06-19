using System.Windows.Controls;
using static PicView.UI.Animations.MouseOverAnimations;

namespace PicView.UI.UserControls
{
    public partial class FullscreenButton : UserControl
    {
        public FullscreenButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(FullscreenButtonBrush);
                MouseEnter += (s, x) => ButtonMouseOverAnim(FullscreenButtonBrush, true);
                MouseLeave += (s, x) => ButtonMouseLeaveAnim(FullscreenButtonBrush, true);
            };
        }
    }
}

using System.Windows.Controls;
using static PicView.MouseOverAnimations;

namespace PicView.UserControls
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

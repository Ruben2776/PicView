using System.Windows.Controls;
using static PicView.UI.Animations.MouseOverAnimations;

namespace PicView.UI.UserControls
{
    public partial class CloseButton : UserControl
    {
        public CloseButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(CloseButtonBrush);
                MouseEnter += (s, x) => ButtonMouseOverAnim(CloseButtonBrush, true);
                MouseLeave += (s, x) => ButtonMouseLeaveAnim(CloseButtonBrush, true);
            };
        }
    }
}
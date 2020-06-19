using System.Windows.Controls;
using static PicView.UI.Animations.MouseOverAnimations;

namespace PicView.UI.UserControls
{
    public partial class MinButton : UserControl
    {
        public MinButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(MinButtonBrush);
                MouseEnter += (s, x) => ButtonMouseOverAnim(MinButtonBrush, true);
                MouseLeave += (s, x) => ButtonMouseLeaveAnim(MinButtonBrush, true);
            };


        }
    }
}

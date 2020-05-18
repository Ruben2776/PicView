using System.Windows.Controls;
using static PicView.MouseOverAnimations;

namespace PicView.UserControls
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

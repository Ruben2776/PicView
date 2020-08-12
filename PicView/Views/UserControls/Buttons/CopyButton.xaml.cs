using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class CopyButton : UserControl
    {
        public CopyButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(CopyButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(CopyButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(CopyButtonBrush, false);
            };
        }
    }
}
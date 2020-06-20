using System.Windows.Controls;
using static PicView.UI.Animations.MouseOverAnimations;

namespace PicView.UI.UserControls
{
    public partial class MenuCloseButton : UserControl
    {
        public MenuCloseButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(CloseButtonBrush);
                MouseEnter += (s, x) => ButtonMouseOverAnim(CloseButtonBrush, true);
                MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(CloseButtonBrush, false);
                TheButton.Click += delegate { UC.Close_UserControls(); };
            };
        }
    }
}
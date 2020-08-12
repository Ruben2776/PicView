using System.Windows;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
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

                ToolTip = Application.Current.Resources["Close"];
            };
        }
    }
}
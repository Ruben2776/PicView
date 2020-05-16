using System.Windows.Controls;
using static PicView.MouseOverAnimations;

namespace PicView.UserControls
{
    public partial class CloseButton : UserControl
    {
        public CloseButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                PreviewMouseLeftButtonDown += (s, x) => ButtonMouseButtonDown(CloseButtonBrush);
                MouseEnter += (s, x) => ButtonMouseOver(CloseButtonBrush);
                MouseLeave += (s, x) => ButtonMouseLeave(CloseButtonBrush);
            };


        }
    }
}

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
                PreviewMouseLeftButtonDown += (s, x) => ButtonMouseButtonDown(FullscreenButtonBrush);
                MouseEnter += (s, x) => ButtonMouseOver(FullscreenButtonBrush);
                MouseLeave += (s, x) => ButtonMouseLeave(FullscreenButtonBrush);
            };
        }
    }
}

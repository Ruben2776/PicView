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
                PreviewMouseLeftButtonDown += (s, x) => ButtonMouseButtonDown(MinButtonBrush);
                MouseEnter += (s, x) => ButtonMouseOver(MinButtonBrush);
                MouseLeave += (s, x) => ButtonMouseLeave(MinButtonBrush);
            };


        }
    }
}

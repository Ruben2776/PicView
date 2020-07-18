using PicView.FileHandling;
using System.Windows.Controls;
using static PicView.UI.Animations.MouseOverAnimations;

namespace PicView.UI.UserControls
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
                TheButton.Click += delegate { Copy_Paste.Copyfile(); };

                ToolTip = "Copy image to clipholder"; // TODO add translation
            };
        }
    }
}
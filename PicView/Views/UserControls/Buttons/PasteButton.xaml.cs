using PicView.FileHandling;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class PasteButton : UserControl
    {
        public PasteButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(PasteButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(PasteButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(PasteButtonBrush, false);
                TheButton.Click += delegate { Copy_Paste.Paste(); };
            };
        }
    }
}
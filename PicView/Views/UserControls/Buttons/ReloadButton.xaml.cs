using PicView.ChangeImage;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class ReloadButton : UserControl
    {
        public ReloadButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ReloadButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ReloadButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ReloadButtonBrush, false);
                TheButton.Click += delegate { Error_Handling.Reload(); };
            };
        }
    }
}
using PicView.ChangeImage;
using PicView.FileHandling;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class RecycleButton : UserControl
    {
        public RecycleButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(RecycleButtonBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(RecycleButtonBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(RecycleButtonBrush, false);
                TheButton.Click += delegate { DeleteFiles.DeleteFile(false); };
            };
        }
    }
}
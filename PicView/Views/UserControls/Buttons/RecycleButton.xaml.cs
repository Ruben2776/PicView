using PicView.FileHandling;
using PicView.UILogic.Animations;
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

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush);
                }

                TheButton.Click += delegate { DeleteFiles.DeleteFile(false); };
            };
        }
    }
}
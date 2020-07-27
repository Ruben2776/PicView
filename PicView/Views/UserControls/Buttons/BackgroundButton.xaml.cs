using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.UILogic.UserControls
{
    public partial class BackGroundButton : UserControl
    {
        public BackGroundButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(bgBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(bgBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(bgBrush, false);
                TheButton.Click += ConfigureSettings.ConfigColors.ChangeBackground;
            };
        }
    }
}
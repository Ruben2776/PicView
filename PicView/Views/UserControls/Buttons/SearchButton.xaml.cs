using PicView.ChangeImage;
using PicView.UILogic.Animations;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class SearchButton : UserControl
    {
        public SearchButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                TheButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(BgBrush);
                TheButton.MouseEnter += (s, x) => ButtonMouseOverAnim(BgBrush, true);
                TheButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(BgBrush, false);

                if (!Properties.Settings.Default.DarkTheme)
                {
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush1);
                    AnimationHelper.LightThemeMouseEvent(this, IconBrush2);
                }

                TheButton.Click += delegate { Error_Handling.Reload(); };
            };
        }
    }
}
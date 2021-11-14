using PicView.Animations;
using PicView.UILogic;
using PicView.UILogic.Loading;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    public partial class ResizeButton : UserControl
    {
        public ResizeButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                var IconBrush = (SolidColorBrush)Resources["IconBrush"];
                TheButton.PreviewMouseLeftButtonDown += delegate
                {
                    ButtonMouseOverAnim(IconBrush, false, true);
                    ButtonMouseOverAnim(ButtonBrush, false, true);
                    AnimationHelper.MouseEnterBgTexColor(ButtonBrush);
                };

                TheButton.MouseEnter += delegate
                {
                    ButtonMouseOverAnim(IconBrush);
                    AnimationHelper.MouseEnterBgTexColor(ButtonBrush);
                };

                TheButton.Click += (_, _) => ToggleQuickResize();
            };
        }

        internal static void ToggleQuickResize()
        {
            UC.Close_UserControls();

            if (UC.GetQuickResize is null)
            {
                LoadControls.LoadQuickResize();
            }
            if (UC.GetQuickResize.Visibility == System.Windows.Visibility.Collapsed)
            {
                UC.GetQuickResize.Show();
            }
            else
            {
                UC.GetQuickResize.Hide();
            }
        }
    }
}

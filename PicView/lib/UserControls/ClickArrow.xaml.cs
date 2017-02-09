using PicView.lib;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.lib.UserControls
{
    public partial class ClickArrow : UserControl
    {
        private static ColorAnimation ccAnim;
        private static ColorAnimation ccAnim2;
        public ClickArrow(bool right)
        {
            InitializeComponent();

            ccAnim = new ColorAnimation
            {
                Duration = TimeSpan.FromSeconds(.32)
            };
            ccAnim2 = new ColorAnimation
            {
                Duration = TimeSpan.FromSeconds(.2)
            };
            canvas.MouseEnter += Arrow_MouseEnter;
            canvas.MouseLeave += Arrow_MouseLeave;
            canvas.PreviewMouseLeftButtonDown += Arrow_MouseLeftButtonDown;

            if (!right)
            {
                Arrow.LayoutTransform = new ScaleTransform()
                {
                    ScaleX = -1
                };
            }
        }

        void Arrow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ccAnim.From = AnimationHelper.GetPrefferedColorOver();
            ccAnim.To = AnimationHelper.GetPrefferedColorDown();
            ArrowFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

            ccAnim2.From = Color.FromArgb(167, 34, 34, 34);
            ccAnim2.To = Color.FromArgb(255, 34, 34, 34);
            CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
        }

        void Arrow_MouseLeave(object sender, MouseEventArgs e)
        {
            ccAnim.From = AnimationHelper.GetPrefferedColorOver();
            ccAnim.To = Colors.White;
            ArrowFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

            ccAnim2.From = Color.FromArgb(200, 34, 34, 34);
            ccAnim2.To = Color.FromArgb(167, 34, 34, 34);
            CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
        }

        void Arrow_MouseEnter(object sender, MouseEventArgs e)
        {
            ccAnim.From = Colors.White;
            ccAnim.To = AnimationHelper.GetPrefferedColorOver();
            ArrowFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

            ccAnim2.From = Color.FromArgb(167, 34, 34, 34);
            ccAnim2.To = Color.FromArgb(200, 34, 34, 34);
            CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
        }
    }
}

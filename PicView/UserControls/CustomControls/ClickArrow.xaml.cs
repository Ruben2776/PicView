using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.UserControls
{
    public partial class ClickArrow : UserControl
    {
        private static ColorAnimation ccAnim;
        private static ColorAnimation ccAnim2;
        private static Color bb;
        private static Color bg;
        private static Color bg2;
        private static Color fg;

        public ClickArrow(bool right)
        {
            InitializeComponent();

            if (!right)
            {
                Arrow.LayoutTransform = new ScaleTransform()
                {
                    ScaleX = -1
                };
                border.BorderThickness = new Thickness(0, 1, 1, 1);
                border.CornerRadius = new CornerRadius(0, 2, 2, 0);
                Canvas.SetLeft(Arrow, 12);
            }

            MouseEnter += (sender, e) =>
            {
                if (ccAnim == null)
                {
                    ccAnim = new ColorAnimation
                    {
                        Duration = TimeSpan.FromSeconds(.32)
                    };
                    ccAnim2 = new ColorAnimation
                    {
                        Duration = TimeSpan.FromSeconds(.2)
                    };
                    bb = (Color)Application.Current.Resources["BorderColor"];
                    bg = (Color)Application.Current.Resources["AltInterface"];
                    bg2 = (Color)Application.Current.Resources["AltInterfaceW"];
                    fg = (Color)Application.Current.Resources["MainColor"];
                }

                ccAnim.From =
                ccAnim.To = AnimationHelper.GetPrefferedColorOver();
                ArrowFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

                ccAnim2.From = bg;
                ccAnim2.To = bg2;
                CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
                AnimationHelper.MouseEnterColorEvent(bb.A, bb.R, bb.G, bb.B, BorderBrushKey, true);

            };
            MouseLeave += (sender, e) =>
            {
                ccAnim.From = AnimationHelper.GetPrefferedColorOver();
                ccAnim.To = fg;
                ArrowFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

                ccAnim2.From = bg2;
                ccAnim2.To = bg;
                CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
                AnimationHelper.MouseLeaveColorEvent(bb.A, bb.R, bb.G, bb.B, BorderBrushKey, true);
            };
        }
    }
}
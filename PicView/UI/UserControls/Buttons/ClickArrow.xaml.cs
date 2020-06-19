using PicView.UI.Animations;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.UI.UserControls
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

            bb = (Color)Application.Current.Resources["BorderColor"];
            bg = (Color)Application.Current.Resources["AltInterface"];
            bg2 = (Color)Application.Current.Resources["AltInterfaceW"];
            fg = (Color)Application.Current.Resources["MainColor"];

            PreviewMouseLeftButtonDown += (sender, e) =>
            {
                if (ccAnim == null)
                {
                    ccAnim = new ColorAnimation
                    {
                        Duration = TimeSpan.FromSeconds(.32)
                    };
                }

                var alpha = AnimationHelper.GetPrefferedColorOver();
                ccAnim.From = alpha;
                ccAnim.To = AnimationHelper.GetPrefferedColorDown();
                PolyFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
                AnimationHelper.MouseOverColorEvent(alpha.A, alpha.R, alpha.G, alpha.B, BorderBrushKey, true);

            };

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

                }

                ccAnim.From = fg;
                ccAnim.To = AnimationHelper.GetPrefferedColorOver();
                PolyFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

                ccAnim2.From = bg;
                ccAnim2.To = bg2;
                CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
                AnimationHelper.MouseOverColorEvent(bb.A, bb.R, bb.G, bb.B, BorderBrushKey, true);

            };
            MouseLeave += (sender, e) =>
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
                }

                ccAnim.From = AnimationHelper.GetPrefferedColorOver();
                ccAnim.To = fg;
                PolyFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

                ccAnim2.From = bg2;
                ccAnim2.To = bg;
                CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
                AnimationHelper.MouseLeaveColorEvent(bb.A, bb.R, bb.G, bb.B, BorderBrushKey, true);
            };
        }
    }
}
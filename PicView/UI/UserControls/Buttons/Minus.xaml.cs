using PicView.UI.Animations;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.UI.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class Minus : UserControl
    {
        private ColorAnimation ccAnim;
        private ColorAnimation ccAnim2;
        private readonly Color bb;
        private readonly Color bg;
        private readonly Color bg2;
        private readonly Color fg;

        public Minus()
        {
            InitializeComponent();

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
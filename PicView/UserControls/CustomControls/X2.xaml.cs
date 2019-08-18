using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class X2 : UserControl
    {
        private static ColorAnimation ccAnim;
        private static ColorAnimation ccAnim2;
        private static Color bb;
        private static Color bg;
        private static Color bg2;
        private static Color fg;

        public X2()
        {
            InitializeComponent();

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
                CrossFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

                ccAnim2.From = bg;
                ccAnim2.To = bg2;
                CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
                AnimationHelper.MouseEnterColorEvent(bb.A, bb.R, bb.G, bb.B, BorderBrushKey, true);

            };
            MouseLeave += (sender, e) =>
            {
                ccAnim.From = AnimationHelper.GetPrefferedColorOver();
                ccAnim.To = fg;
                CrossFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

                ccAnim2.From = bg2;
                ccAnim2.To = bg;
                CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
                AnimationHelper.MouseLeaveColorEvent(bb.A, bb.R, bb.G, bb.B, BorderBrushKey, true);
            };
        }
    }
}

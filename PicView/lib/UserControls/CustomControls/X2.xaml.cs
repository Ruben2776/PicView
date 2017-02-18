using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.lib.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class X2 : UserControl
    {
        private static ColorAnimation ccAnim;
        public X2()
        {
            InitializeComponent();
            ccAnim = new ColorAnimation
            {
                Duration = TimeSpan.FromSeconds(.32)
            };

            MouseEnter += (sender, e) =>
            {
                ccAnim.From = Colors.WhiteSmoke;
                ccAnim.To = AnimationHelper.GetPrefferedColorDown();
                CrossFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
            };
            MouseLeave += (sender, e) =>
            {
                ccAnim.From = AnimationHelper.GetPrefferedColorDown();
                ccAnim.To = Colors.WhiteSmoke;
                CrossFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
            };
        }
    }
}

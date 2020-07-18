using PicView.UILogic.Animations;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PicView.UILogic.UserControls
{
    /// <summary>
    /// Cool shady close button!
    /// </summary>
    public partial class X2 : UserControl
    {
        private static ColorAnimation ccAnim = new ColorAnimation{ Duration = TimeSpan.FromSeconds(.32) };
        private static ColorAnimation ccAnim2 = new ColorAnimation { Duration = TimeSpan.FromSeconds(.2) };
        private static readonly SolidColorBrush borderBrush = (SolidColorBrush)Application.Current.Resources["BorderBrush"];

        public X2()
        {
            InitializeComponent();

            MouseEnter += (sender, e) =>
            {
                ccAnim.From = (Color)Application.Current.Resources["MainColor"];
                ccAnim.To = AnimationHelper.GetPrefferedColorOver();

                PolyFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

                ccAnim2.From = (Color)Application.Current.Resources["AltInterface"];
                ccAnim2.To = (Color)Application.Current.Resources["AltInterfaceW"];

                CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
                AnimationHelper.MouseOverColorEvent(
                    borderBrush.Color.A,
                    borderBrush.Color.R,
                    borderBrush.Color.G,
                    borderBrush.Color.B,
                    BorderBrushKey,
                    true);
            };

            MouseLeave += (sender, e) =>
            {

                ccAnim.From = AnimationHelper.GetPrefferedColorOver();
                ccAnim.To = (Color)Application.Current.Resources["MainColor"];

                PolyFill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

                ccAnim2.From = (Color)Application.Current.Resources["AltInterfaceW"];
                ccAnim2.To = (Color)Application.Current.Resources["AltInterface"];

                CanvasBGcolor.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim2);
                AnimationHelper.MouseLeaveColorEvent(
                    borderBrush.Color.A,
                    borderBrush.Color.R,
                    borderBrush.Color.G,
                    borderBrush.Color.B,
                    BorderBrushKey,
                    true);
            };
        }
    }
}
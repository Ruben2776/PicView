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
    public partial class GalleryShortcut : UserControl
    {
        private static readonly ColorAnimation ccAnim = new ColorAnimation { Duration = TimeSpan.FromSeconds(.32) };
        private static readonly ColorAnimation ccAnim2 = new ColorAnimation { Duration = TimeSpan.FromSeconds(.2) };
        private static readonly SolidColorBrush borderBrush = (SolidColorBrush)Application.Current.Resources["BorderBrush"];

        public GalleryShortcut()
        {
            InitializeComponent();

            PreviewMouseLeftButtonDown += (sender, e) =>
            {
                var alpha = AnimationHelper.GetPrefferedColorOver();
                ccAnim.From = alpha;
                ccAnim.To = AnimationHelper.GetPrefferedColorDown();
                ImagePath1Fill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
                ImagePath2Fill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
                ImagePath3Fill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
                AnimationHelper.MouseOverColorEvent(alpha.A, alpha.R, alpha.G, alpha.B, BorderBrushKey, true);
            };

            MouseEnter += (sender, e) =>
            {
                ccAnim.From = ccAnim.From = (Color)Application.Current.Resources["IconColor"];
                ccAnim.To = AnimationHelper.GetPrefferedColorOver();
                ImagePath1Fill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
                ImagePath2Fill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
                ImagePath3Fill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

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
                ccAnim.To = (Color)Application.Current.Resources["IconColor"];

                ImagePath1Fill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
                ImagePath2Fill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);
                ImagePath3Fill.BeginAnimation(SolidColorBrush.ColorProperty, ccAnim);

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
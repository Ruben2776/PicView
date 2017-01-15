using PicView.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PicView.lib.UserControls
{
    /// <summary>
    /// A nice left arrow
    /// </summary>
    public partial class LeftArrow : UserControl
    {
        private static ColorAnimation ccAnim;
        private static ColorAnimation ccAnim2;
        public LeftArrow()
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
